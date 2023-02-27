using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Iris.Emulation.GBA
{
    internal sealed partial class Core
    {
        [Flags]
        private enum MemoryFlag
        {
            Read8 = 1 << 0,
            Read16 = 1 << 1,
            Read32 = 1 << 2,
            Write8 = 1 << 3,
            Write16 = 1 << 4,
            Write32 = 1 << 5,
            Mirrored = 1 << 6,

            None = 0,
            AllRead = Read8 | Read16 | Read32,
            AllWrite = Write8 | Write16 | Write32,
            All = AllRead | AllWrite | Mirrored,
        }

        private const int KB = 1024;

        private IntPtr _ROM;
        private int _ROMSize;

        private readonly IntPtr _SRAM = Marshal.AllocHGlobal(64 * KB);
        private readonly IntPtr _eWRAM = Marshal.AllocHGlobal(256 * KB);
        private readonly IntPtr _iWRAM = Marshal.AllocHGlobal(32 * KB);

        private readonly IntPtr[] _read8PageTable = new IntPtr[1 << 18];
        private readonly IntPtr[] _read16PageTable = new IntPtr[1 << 18];
        private readonly IntPtr[] _read32PageTable = new IntPtr[1 << 18];
        private readonly IntPtr[] _write8PageTable = new IntPtr[1 << 18];
        private readonly IntPtr[] _write16PageTable = new IntPtr[1 << 18];
        private readonly IntPtr[] _write32PageTable = new IntPtr[1 << 18];

        private void MapMemory(IntPtr data, int pageCount, UInt32 startAddress, UInt32 endAddress, MemoryFlag flags)
        {
            int startTablePageIndex = (int)(startAddress >> 10);
            int endPageTableIndex = (int)(endAddress >> 10);

            bool readable8 = (flags & MemoryFlag.Read8) == MemoryFlag.Read8;
            bool readable16 = (flags & MemoryFlag.Read16) == MemoryFlag.Read16;
            bool readable32 = (flags & MemoryFlag.Read32) == MemoryFlag.Read32;
            bool writable8 = (flags & MemoryFlag.Write8) == MemoryFlag.Write8;
            bool writable16 = (flags & MemoryFlag.Write16) == MemoryFlag.Write16;
            bool writable32 = (flags & MemoryFlag.Write32) == MemoryFlag.Write32;
            bool mirrored = (flags & MemoryFlag.Mirrored) == MemoryFlag.Mirrored;

            for (int pageTableIndex = startTablePageIndex, pageIndex = 0; pageTableIndex != endPageTableIndex; ++pageTableIndex, ++pageIndex)
            {
                if (pageIndex < pageCount)
                {
                    int pageOffset = pageIndex * KB;
                    IntPtr page = data + pageOffset;
                    _read8PageTable[pageTableIndex] = readable8 ? page : IntPtr.Zero;
                    _read16PageTable[pageTableIndex] = readable16 ? page : IntPtr.Zero;
                    _read32PageTable[pageTableIndex] = readable32 ? page : IntPtr.Zero;
                    _write8PageTable[pageTableIndex] = writable8 ? page : IntPtr.Zero;
                    _write16PageTable[pageTableIndex] = writable16 ? page : IntPtr.Zero;
                    _write32PageTable[pageTableIndex] = writable32 ? page : IntPtr.Zero;
                }
                else if (mirrored)
                {
                    int pageOffset = (pageIndex % pageCount) * KB;
                    IntPtr page = data + pageOffset;
                    _read8PageTable[pageTableIndex] = readable8 ? page : IntPtr.Zero;
                    _read16PageTable[pageTableIndex] = readable16 ? page : IntPtr.Zero;
                    _read32PageTable[pageTableIndex] = readable32 ? page : IntPtr.Zero;
                    _write8PageTable[pageTableIndex] = writable8 ? page : IntPtr.Zero;
                    _write16PageTable[pageTableIndex] = writable16 ? page : IntPtr.Zero;
                    _write32PageTable[pageTableIndex] = writable32 ? page : IntPtr.Zero;
                }
                else
                {
                    _read8PageTable[pageTableIndex] = IntPtr.Zero;
                    _read16PageTable[pageTableIndex] = IntPtr.Zero;
                    _read32PageTable[pageTableIndex] = IntPtr.Zero;
                    _write8PageTable[pageTableIndex] = IntPtr.Zero;
                    _write16PageTable[pageTableIndex] = IntPtr.Zero;
                    _write32PageTable[pageTableIndex] = IntPtr.Zero;
                }
            }

            if (writable8 || writable16 || writable32)
            {
                int length = pageCount * KB;

                for (int offset = 0; offset < length; ++offset)
                    Marshal.WriteByte(data, offset, 0);
            }
        }

        private void InitPageTables()
        {
            MapMemory(_eWRAM, 256, 0x0200_0000, 0x0300_0000, MemoryFlag.All);
            MapMemory(_iWRAM, 32, 0x0300_0000, 0x0400_0000, MemoryFlag.All);
            MapMemory(_ppu.PaletteRAM, 1, 0x0500_0000, 0x0600_0000, MemoryFlag.All & ~(MemoryFlag.Read8 | MemoryFlag.Write8));
            MapMemory(_ppu.VRAM, 96, 0x0600_0000, 0x0700_0000, MemoryFlag.All & ~(MemoryFlag.Read8 | MemoryFlag.Write8));
            MapMemory(_ppu.OAM, 1, 0x0700_0000, 0x0800_0000, MemoryFlag.All & ~(MemoryFlag.Read8 | MemoryFlag.Write8));
            MapMemory(_SRAM, 64, 0x0e00_0000, 0x1000_0000, MemoryFlag.Read8 | MemoryFlag.Write8 | MemoryFlag.Mirrored);
        }

        internal void LoadROM(string filename)
        {
            Byte[] data = File.ReadAllBytes(filename);

            if (_ROM != IntPtr.Zero)
                Marshal.FreeHGlobal(_ROM);

            _ROMSize = data.Length;
            _ROM = Marshal.AllocHGlobal(_ROMSize);
            Marshal.Copy(data, 0, _ROM, _ROMSize);

            int pageCount = _ROMSize / KB;
            MapMemory(_ROM, pageCount, 0x0800_0000, 0x0a00_0000, MemoryFlag.AllRead);
            MapMemory(_ROM, pageCount, 0x0a00_0000, 0x0c00_0000, MemoryFlag.AllRead);
            MapMemory(_ROM, pageCount, 0x0c00_0000, 0x0e00_0000, MemoryFlag.AllRead);
        }

        private Byte ReadMemory8(UInt32 address)
        {
            address &= 0x0fff_ffff;

            IntPtr page = _read8PageTable[address >> 10];

            if (page != IntPtr.Zero)
            {
                unsafe
                {
                    // much faster than Marshal.ReadByte
                    return Unsafe.Read<Byte>((Byte*)page + (address & 0x3ff));
                }
            }

            // page fault
            switch (address >> 24)
            {
                // BIOS
                case 0x0:
                case 0x1:
                    return BIOS_Read8(address);

                // IO and registers
                case 0x4:
                    {
                        static Byte GetLowByte(UInt16 value) => (Byte)value;
                        static Byte GetHighByte(UInt16 value) => (Byte)(value >> 8);

                        UInt32 offset = address - 0x400_0000;

                        return offset switch
                        {
                            0x000 => GetLowByte(_ppu.DISPCNT),
                            0x001 => GetHighByte(_ppu.DISPCNT),

                            0x004 => GetLowByte(_ppu.DISPSTAT),
                            0x005 => GetHighByte(_ppu.DISPSTAT),

                            0x006 => GetLowByte(_ppu.VCOUNT),
                            0x007 => GetHighByte(_ppu.VCOUNT),

                            0x008 => GetLowByte(_ppu.BG0CNT),
                            0x009 => GetHighByte(_ppu.BG0CNT),

                            0x00a => GetLowByte(_ppu.BG1CNT),
                            0x00b => GetHighByte(_ppu.BG1CNT),

                            0x00c => GetLowByte(_ppu.BG2CNT),
                            0x00d => GetHighByte(_ppu.BG2CNT),

                            0x00e => GetLowByte(_ppu.BG3CNT),
                            0x00f => GetHighByte(_ppu.BG3CNT),

                            0x048 => GetLowByte(_ppu.WININ),
                            0x049 => GetHighByte(_ppu.WININ),

                            0x04a => GetLowByte(_ppu.WINOUT),
                            0x04b => GetHighByte(_ppu.WINOUT),

                            0x050 => GetLowByte(_ppu.BLDCNT),
                            0x051 => GetHighByte(_ppu.BLDCNT),

                            0x052 => GetLowByte(_ppu.BLDALPHA),
                            0x053 => GetHighByte(_ppu.BLDALPHA),

                            0x062 => GetLowByte(_SOUND1CNT_H),
                            0x063 => GetHighByte(_SOUND1CNT_H),

                            0x064 => GetLowByte(_SOUND1CNT_X),
                            0x065 => GetHighByte(_SOUND1CNT_X),

                            0x068 => GetLowByte(_SOUND2CNT_L),
                            0x069 => GetHighByte(_SOUND2CNT_L),

                            0x06c => GetLowByte(_SOUND2CNT_H),
                            0x06d => GetHighByte(_SOUND2CNT_H),

                            0x070 => GetLowByte(_SOUND3CNT_L),
                            0x071 => GetHighByte(_SOUND3CNT_L),

                            0x078 => GetLowByte(_SOUND4CNT_L),
                            0x079 => GetHighByte(_SOUND4CNT_L),

                            0x07c => GetLowByte(_SOUND4CNT_H),
                            0x07d => GetHighByte(_SOUND4CNT_H),

                            0x080 => GetLowByte(_SOUNDCNT_L),
                            0x081 => GetHighByte(_SOUNDCNT_L),

                            0x082 => GetLowByte(_SOUNDCNT_H),
                            0x083 => GetHighByte(_SOUNDCNT_H),

                            0x084 => GetLowByte(_SOUNDCNT_X),
                            0x085 => GetHighByte(_SOUNDCNT_X),

                            0x088 => GetLowByte(_SOUNDBIAS),
                            0x089 => GetHighByte(_SOUNDBIAS),

                            0x0ba => GetLowByte(_DMA0CNT_H),
                            0x0bb => GetHighByte(_DMA0CNT_H),

                            0x0c6 => GetLowByte(_DMA1CNT_H),
                            0x0c7 => GetHighByte(_DMA1CNT_H),

                            0x0d2 => GetLowByte(_DMA2CNT_H),
                            0x0d3 => GetHighByte(_DMA2CNT_H),

                            0x0de => GetLowByte(_DMA3CNT_H),
                            0x0df => GetHighByte(_DMA3CNT_H),

                            0x100 => GetLowByte(_TM0CNT_L),
                            0x101 => GetHighByte(_TM0CNT_L),

                            0x102 => GetLowByte(_TM0CNT_H),
                            0x103 => GetHighByte(_TM0CNT_H),

                            0x104 => GetLowByte(_TM1CNT_L),
                            0x105 => GetHighByte(_TM1CNT_L),

                            0x106 => GetLowByte(_TM1CNT_H),
                            0x107 => GetHighByte(_TM1CNT_H),

                            0x108 => GetLowByte(_TM2CNT_L),
                            0x109 => GetHighByte(_TM2CNT_L),

                            0x10a => GetLowByte(_TM2CNT_H),
                            0x10b => GetHighByte(_TM2CNT_H),

                            0x10c => GetLowByte(_TM3CNT_L),
                            0x10d => GetHighByte(_TM3CNT_L),

                            0x10e => GetLowByte(_TM3CNT_H),
                            0x10f => GetHighByte(_TM3CNT_H),

                            0x120 => GetLowByte(_SIOMULTI0),
                            0x121 => GetHighByte(_SIOMULTI0),

                            0x122 => GetLowByte(_SIOMULTI1),
                            0x123 => GetHighByte(_SIOMULTI1),

                            0x124 => GetLowByte(_SIOMULTI2),
                            0x125 => GetHighByte(_SIOMULTI2),

                            0x126 => GetLowByte(_SIOMULTI3),
                            0x127 => GetHighByte(_SIOMULTI3),

                            0x128 => GetLowByte(_SIOCNT),
                            0x129 => GetHighByte(_SIOCNT),

                            0x12a => GetLowByte(_SIODATA8),
                            0x12b => GetHighByte(_SIODATA8),

                            0x130 => GetLowByte(_KEYINPUT),
                            0x131 => GetHighByte(_KEYINPUT),

                            0x132 => GetLowByte(_KEYCNT),
                            0x133 => GetHighByte(_KEYCNT),

                            0x134 => GetLowByte(_RCNT),
                            0x135 => GetHighByte(_RCNT),

                            0x200 => GetLowByte(_IE),
                            0x201 => GetHighByte(_IE),

                            0x202 => GetLowByte(_IF),
                            0x203 => GetHighByte(_IF),

                            0x204 => GetLowByte(_WAITCNT),
                            0x205 => GetHighByte(_WAITCNT),

                            0x208 => GetLowByte(_IME),
                            0x209 => GetHighByte(_IME),

                            _ => throw new Exception(string.Format("Emulation.GBA.Memory: Unhandled read from address 0x{0:x8}", address)),
                        };
                    }

                // ROM wait state 0
                case 0x8:
                case 0x9:
                    {
                        UInt32 offset = address - 0x800_0000;

                        if (offset < _ROMSize)
                        {
                            unsafe
                            {
                                // much faster than Marshal.ReadByte
                                return Unsafe.Read<Byte>((Byte*)_ROM + offset);
                            }
                        }
                    }
                    break;

                // ROM wait state 1
                case 0xa:
                case 0xb:
                    {
                        UInt32 offset = address - 0xa00_0000;

                        if (offset < _ROMSize)
                        {
                            unsafe
                            {
                                // much faster than Marshal.ReadByte
                                return Unsafe.Read<Byte>((Byte*)_ROM + offset);
                            }
                        }
                    }
                    break;

                // ROM wait state 2
                case 0xc:
                case 0xd:
                    {
                        UInt32 offset = address - 0xc00_0000;

                        if (offset < _ROMSize)
                        {
                            unsafe
                            {
                                // much faster than Marshal.ReadByte
                                return Unsafe.Read<Byte>((Byte*)_ROM + offset);
                            }
                        }
                    }
                    break;
            }

            throw new Exception(string.Format("Emulation.GBA.Memory: Unhandled read from address 0x{0:x8}", address));
        }

        private UInt16 ReadMemory16(UInt32 address)
        {
            address &= 0x0fff_fffe;

            IntPtr page = _read16PageTable[address >> 10];

            if (page != IntPtr.Zero)
            {
                unsafe
                {
                    // much faster than Marshal.ReadInt16
                    return Unsafe.Read<UInt16>((Byte*)page + (address & 0x3ff));
                }
            }

            // page fault
            switch (address >> 24)
            {
                // BIOS
                case 0x0:
                case 0x1:
                    return BIOS_Read16(address);

                // IO and registers
                case 0x4:
                    {
                        UInt32 offset = address - 0x400_0000;

                        return offset switch
                        {
                            0x000 => _ppu.DISPCNT,
                            0x004 => _ppu.DISPSTAT,
                            0x006 => _ppu.VCOUNT,
                            0x008 => _ppu.BG0CNT,
                            0x00a => _ppu.BG1CNT,
                            0x00c => _ppu.BG2CNT,
                            0x00e => _ppu.BG3CNT,
                            0x048 => _ppu.WININ,
                            0x04a => _ppu.WINOUT,
                            0x050 => _ppu.BLDCNT,
                            0x052 => _ppu.BLDALPHA,
                            0x062 => _SOUND1CNT_H,
                            0x064 => _SOUND1CNT_X,
                            0x068 => _SOUND2CNT_L,
                            0x06c => _SOUND2CNT_H,
                            0x070 => _SOUND3CNT_L,
                            0x078 => _SOUND4CNT_L,
                            0x07c => _SOUND4CNT_H,
                            0x080 => _SOUNDCNT_L,
                            0x082 => _SOUNDCNT_H,
                            0x084 => _SOUNDCNT_X,
                            0x088 => _SOUNDBIAS,
                            0x0ba => _DMA0CNT_H,
                            0x0c6 => _DMA1CNT_H,
                            0x0d2 => _DMA2CNT_H,
                            0x0de => _DMA3CNT_H,
                            0x100 => _TM0CNT_L,
                            0x102 => _TM0CNT_H,
                            0x104 => _TM1CNT_L,
                            0x106 => _TM1CNT_H,
                            0x108 => _TM2CNT_L,
                            0x10a => _TM2CNT_H,
                            0x10c => _TM3CNT_L,
                            0x10e => _TM3CNT_H,
                            0x120 => _SIOMULTI0,
                            0x122 => _SIOMULTI1,
                            0x124 => _SIOMULTI2,
                            0x126 => _SIOMULTI3,
                            0x128 => _SIOCNT,
                            0x12a => _SIODATA8,
                            0x130 => _KEYINPUT,
                            0x132 => _KEYCNT,
                            0x134 => _RCNT,
                            0x200 => _IE,
                            0x202 => _IF,
                            0x204 => _WAITCNT,
                            0x208 => _IME,
                            _ => throw new Exception(string.Format("Emulation.GBA.Memory: Unhandled read from address 0x{0:x8}", address)),
                        };
                    }

                // ROM wait state 0
                case 0x8:
                case 0x9:
                    {
                        UInt32 offset = address - 0x800_0000;

                        if (offset < _ROMSize)
                        {
                            unsafe
                            {
                                // much faster than Marshal.ReadInt16
                                return Unsafe.Read<UInt16>((Byte*)_ROM + offset);
                            }
                        }
                    }
                    break;

                // ROM wait state 1
                case 0xa:
                case 0xb:
                    {
                        UInt32 offset = address - 0xa00_0000;

                        if (offset < _ROMSize)
                        {
                            unsafe
                            {
                                // much faster than Marshal.ReadInt16
                                return Unsafe.Read<UInt16>((Byte*)_ROM + offset);
                            }
                        }
                    }
                    break;

                // ROM wait state 2
                case 0xc:
                case 0xd:
                    {
                        UInt32 offset = address - 0xc00_0000;

                        if (offset < _ROMSize)
                        {
                            unsafe
                            {
                                // much faster than Marshal.ReadInt16
                                return Unsafe.Read<UInt16>((Byte*)_ROM + offset);
                            }
                        }
                    }
                    break;
            }

            throw new Exception(string.Format("Emulation.GBA.Memory: Unhandled read from address 0x{0:x8}", address));
        }

        private UInt32 ReadMemory32(UInt32 address)
        {
            address &= 0x0fff_fffc;

            IntPtr page = _read32PageTable[address >> 10];

            if (page != IntPtr.Zero)
            {
                unsafe
                {
                    // much faster than Marshal.ReadInt32
                    return Unsafe.Read<UInt32>((Byte*)page + (address & 0x3ff));
                }
            }

            // page fault
            switch (address >> 24)
            {
                // BIOS
                case 0x0:
                case 0x1:
                    return BIOS_Read32(address);

                // IO and registers
                case 0x4:
                    {
                        UInt32 offset = address - 0x400_0000;

                        return offset switch
                        {
                            0x004 => (UInt32)((_ppu.VCOUNT << 16) | _ppu.DISPSTAT),
                            0x0c4 => (UInt32)(_DMA1CNT_H << 16),
                            0x0d0 => (UInt32)(_DMA2CNT_H << 16),
                            0x200 => (UInt32)((_IF << 16) | _IE),
                            _ => throw new Exception(string.Format("Emulation.GBA.Memory: Unhandled read from address 0x{0:x8}", address)),
                        };
                    }

                // ROM wait state 0
                case 0x8:
                case 0x9:
                    {
                        UInt32 offset = address - 0x800_0000;

                        if (offset < _ROMSize)
                        {
                            unsafe
                            {
                                // much faster than Marshal.ReadInt32
                                return Unsafe.Read<UInt32>((Byte*)_ROM + offset);
                            }
                        }
                    }
                    break;

                // ROM wait state 1
                case 0xa:
                case 0xb:
                    {
                        UInt32 offset = address - 0xa00_0000;

                        if (offset < _ROMSize)
                        {
                            unsafe
                            {
                                // much faster than Marshal.ReadInt32
                                return Unsafe.Read<UInt32>((Byte*)_ROM + offset);
                            }
                        }
                    }
                    break;

                // ROM wait state 2
                case 0xc:
                case 0xd:
                    {
                        UInt32 offset = address - 0xc00_0000;

                        if (offset < _ROMSize)
                        {
                            unsafe
                            {
                                // much faster than Marshal.ReadInt32
                                return Unsafe.Read<UInt32>((Byte*)_ROM + offset);
                            }
                        }
                    }
                    break;
            }

            throw new Exception(string.Format("Emulation.GBA.Memory: Unhandled read from address 0x{0:x8}", address));
        }

        private void WriteMemory8(UInt32 address, Byte value)
        {
            address &= 0x0fff_ffff;

            IntPtr page = _write8PageTable[address >> 10];

            if (page != IntPtr.Zero)
            {
                unsafe
                {
                    // much faster than Marshal.WriteByte
                    Unsafe.Write<Byte>((Byte*)page + (address & 0x3ff), value);
                }

                return;
            }

            // page fault
            switch (address >> 24)
            {
                // BIOS
                case 0x0:
                case 0x1:
                    break;

                // IO and registers
                case 0x4:
                    {
                        static void SetLowByte(ref UInt16 input, Byte value) => input = (UInt16)((input & 0xff00) | value);
                        static void SetHighByte(ref UInt16 input, Byte value) => input = (UInt16)((input & 0x00ff) | (value << 8));

                        UInt32 offset = address - 0x400_0000;

                        switch (offset)
                        {
                            case 0x000:
                                SetLowByte(ref _ppu.DISPCNT, value);
                                break;
                            case 0x001:
                                SetHighByte(ref _ppu.DISPCNT, value);
                                break;

                            case 0x004:
                                SetLowByte(ref _ppu.DISPSTAT, value);
                                break;
                            case 0x005:
                                SetHighByte(ref _ppu.DISPSTAT, value);
                                break;

                            case 0x008:
                                SetLowByte(ref _ppu.BG0CNT, value);
                                break;
                            case 0x009:
                                SetHighByte(ref _ppu.BG0CNT, value);
                                break;

                            case 0x00a:
                                SetLowByte(ref _ppu.BG1CNT, value);
                                break;
                            case 0x00b:
                                SetHighByte(ref _ppu.BG1CNT, value);
                                break;

                            case 0x00c:
                                SetLowByte(ref _ppu.BG2CNT, value);
                                break;
                            case 0x00d:
                                SetHighByte(ref _ppu.BG2CNT, value);
                                break;

                            case 0x00e:
                                SetLowByte(ref _ppu.BG3CNT, value);
                                break;
                            case 0x00f:
                                SetHighByte(ref _ppu.BG3CNT, value);
                                break;

                            case 0x010:
                                SetLowByte(ref _ppu.BG0HOFS, value);
                                break;
                            case 0x011:
                                SetHighByte(ref _ppu.BG0HOFS, value);
                                break;

                            case 0x012:
                                SetLowByte(ref _ppu.BG0VOFS, value);
                                break;
                            case 0x013:
                                SetHighByte(ref _ppu.BG0VOFS, value);
                                break;

                            case 0x014:
                                SetLowByte(ref _ppu.BG1HOFS, value);
                                break;
                            case 0x015:
                                SetHighByte(ref _ppu.BG1HOFS, value);
                                break;

                            case 0x016:
                                SetLowByte(ref _ppu.BG1VOFS, value);
                                break;
                            case 0x017:
                                SetHighByte(ref _ppu.BG1VOFS, value);
                                break;

                            case 0x018:
                                SetLowByte(ref _ppu.BG2HOFS, value);
                                break;
                            case 0x019:
                                SetHighByte(ref _ppu.BG2HOFS, value);
                                break;

                            case 0x01a:
                                SetLowByte(ref _ppu.BG2VOFS, value);
                                break;
                            case 0x01b:
                                SetHighByte(ref _ppu.BG2VOFS, value);
                                break;

                            case 0x01c:
                                SetLowByte(ref _ppu.BG3HOFS, value);
                                break;
                            case 0x01d:
                                SetHighByte(ref _ppu.BG3HOFS, value);
                                break;

                            case 0x01e:
                                SetLowByte(ref _ppu.BG3VOFS, value);
                                break;
                            case 0x01f:
                                SetHighByte(ref _ppu.BG3VOFS, value);
                                break;

                            case 0x040:
                                SetLowByte(ref _ppu.WIN0H, value);
                                break;
                            case 0x041:
                                SetHighByte(ref _ppu.WIN0H, value);
                                break;

                            case 0x042:
                                SetLowByte(ref _ppu.WIN1H, value);
                                break;
                            case 0x043:
                                SetHighByte(ref _ppu.WIN1H, value);
                                break;

                            case 0x044:
                                SetLowByte(ref _ppu.WIN0V, value);
                                break;
                            case 0x045:
                                SetHighByte(ref _ppu.WIN0V, value);
                                break;

                            case 0x046:
                                SetLowByte(ref _ppu.WIN1V, value);
                                break;
                            case 0x047:
                                SetHighByte(ref _ppu.WIN1V, value);
                                break;

                            case 0x048:
                                SetLowByte(ref _ppu.WININ, value);
                                break;
                            case 0x049:
                                SetHighByte(ref _ppu.WININ, value);
                                break;

                            case 0x04a:
                                SetLowByte(ref _ppu.WINOUT, value);
                                break;
                            case 0x04b:
                                SetHighByte(ref _ppu.WINOUT, value);
                                break;

                            case 0x04c:
                                SetLowByte(ref _ppu.MOSAIC, value);
                                break;
                            case 0x04d:
                                SetHighByte(ref _ppu.MOSAIC, value);
                                break;

                            case 0x050:
                                SetLowByte(ref _ppu.BLDCNT, value);
                                break;
                            case 0x051:
                                SetHighByte(ref _ppu.BLDCNT, value);
                                break;

                            case 0x052:
                                SetLowByte(ref _ppu.BLDALPHA, value);
                                break;
                            case 0x053:
                                SetHighByte(ref _ppu.BLDALPHA, value);
                                break;

                            case 0x054:
                                SetLowByte(ref _ppu.BLDY, value);
                                break;
                            case 0x055:
                                SetHighByte(ref _ppu.BLDY, value);
                                break;

                            case 0x062:
                                SetLowByte(ref _SOUND1CNT_H, value);
                                break;
                            case 0x063:
                                SetHighByte(ref _SOUND1CNT_H, value);
                                break;

                            case 0x064:
                                SetLowByte(ref _SOUND1CNT_X, value);
                                break;
                            case 0x065:
                                SetHighByte(ref _SOUND1CNT_X, value);
                                break;

                            case 0x068:
                                SetLowByte(ref _SOUND2CNT_L, value);
                                break;
                            case 0x069:
                                SetHighByte(ref _SOUND2CNT_L, value);
                                break;

                            case 0x06c:
                                SetLowByte(ref _SOUND2CNT_H, value);
                                break;
                            case 0x06d:
                                SetHighByte(ref _SOUND2CNT_H, value);
                                break;

                            case 0x070:
                                SetLowByte(ref _SOUND3CNT_L, value);
                                break;
                            case 0x071:
                                SetHighByte(ref _SOUND3CNT_L, value);
                                break;

                            case 0x078:
                                SetLowByte(ref _SOUND4CNT_L, value);
                                break;
                            case 0x079:
                                SetHighByte(ref _SOUND4CNT_L, value);
                                break;

                            case 0x07c:
                                SetLowByte(ref _SOUND4CNT_H, value);
                                break;
                            case 0x07d:
                                SetHighByte(ref _SOUND4CNT_H, value);
                                break;

                            case 0x080:
                                SetLowByte(ref _SOUNDCNT_L, value);
                                break;
                            case 0x081:
                                SetHighByte(ref _SOUNDCNT_L, value);
                                break;

                            case 0x082:
                                SetLowByte(ref _SOUNDCNT_H, value);
                                break;
                            case 0x083:
                                SetHighByte(ref _SOUNDCNT_H, value);
                                break;

                            case 0x084:
                                SetLowByte(ref _SOUNDCNT_X, value);
                                break;
                            case 0x085:
                                SetHighByte(ref _SOUNDCNT_X, value);
                                break;

                            case 0x088:
                                SetLowByte(ref _SOUNDBIAS, value);
                                break;
                            case 0x089:
                                SetHighByte(ref _SOUNDBIAS, value);
                                break;

                            case 0x0ba:
                                SetLowByte(ref _DMA0CNT_H, value);
                                break;
                            case 0x0bb:
                                SetHighByte(ref _DMA0CNT_H, value);
                                break;

                            case 0x0bc:
                                SetLowByte(ref _DMA1SAD_L, value);
                                break;
                            case 0x0bd:
                                SetHighByte(ref _DMA1SAD_L, value);
                                break;

                            case 0x0be:
                                SetLowByte(ref _DMA1SAD_H, value);
                                break;
                            case 0x0bf:
                                SetHighByte(ref _DMA1SAD_H, value);
                                break;

                            case 0x0c0:
                                SetLowByte(ref _DMA1DAD_L, value);
                                break;
                            case 0x0c1:
                                SetHighByte(ref _DMA1DAD_L, value);
                                break;

                            case 0x0c2:
                                SetLowByte(ref _DMA1DAD_H, value);
                                break;
                            case 0x0c3:
                                SetHighByte(ref _DMA1DAD_H, value);
                                break;

                            case 0x0c4:
                                SetLowByte(ref _DMA1CNT_L, value);
                                break;
                            case 0x0c5:
                                SetHighByte(ref _DMA1CNT_L, value);
                                break;

                            case 0x0c6:
                                SetLowByte(ref _DMA1CNT_H, value);
                                break;
                            case 0x0c7:
                                SetHighByte(ref _DMA1CNT_H, value);
                                break;

                            case 0x0c8:
                                SetLowByte(ref _DMA2SAD_L, value);
                                break;
                            case 0x0c9:
                                SetHighByte(ref _DMA2SAD_L, value);
                                break;

                            case 0x0ca:
                                SetLowByte(ref _DMA2SAD_H, value);
                                break;
                            case 0x0cb:
                                SetHighByte(ref _DMA2SAD_H, value);
                                break;

                            case 0x0cc:
                                SetLowByte(ref _DMA2DAD_L, value);
                                break;
                            case 0x0cd:
                                SetHighByte(ref _DMA2DAD_L, value);
                                break;

                            case 0x0ce:
                                SetLowByte(ref _DMA2DAD_H, value);
                                break;
                            case 0x0cf:
                                SetHighByte(ref _DMA2DAD_H, value);
                                break;

                            case 0x0d0:
                                SetLowByte(ref _DMA2CNT_L, value);
                                break;
                            case 0x0d1:
                                SetHighByte(ref _DMA2CNT_L, value);
                                break;

                            case 0x0d2:
                                SetLowByte(ref _DMA2CNT_H, value);
                                break;
                            case 0x0d3:
                                SetHighByte(ref _DMA2CNT_H, value);
                                break;

                            case 0x0de:
                                SetLowByte(ref _DMA3CNT_H, value);
                                break;
                            case 0x0df:
                                SetHighByte(ref _DMA3CNT_H, value);
                                break;

                            case 0x100:
                                SetLowByte(ref _TM0CNT_L, value);
                                break;
                            case 0x101:
                                SetHighByte(ref _TM0CNT_L, value);
                                break;

                            case 0x102:
                                SetLowByte(ref _TM0CNT_H, value);
                                break;
                            case 0x103:
                                SetHighByte(ref _TM0CNT_H, value);
                                break;

                            case 0x104:
                                SetLowByte(ref _TM1CNT_L, value);
                                break;
                            case 0x105:
                                SetHighByte(ref _TM1CNT_L, value);
                                break;

                            case 0x106:
                                SetLowByte(ref _TM1CNT_H, value);
                                break;
                            case 0x107:
                                SetHighByte(ref _TM1CNT_H, value);
                                break;

                            case 0x108:
                                SetLowByte(ref _TM2CNT_L, value);
                                break;
                            case 0x109:
                                SetHighByte(ref _TM2CNT_L, value);
                                break;

                            case 0x10a:
                                SetLowByte(ref _TM2CNT_H, value);
                                break;
                            case 0x10b:
                                SetHighByte(ref _TM2CNT_H, value);
                                break;

                            case 0x10c:
                                SetLowByte(ref _TM3CNT_L, value);
                                break;
                            case 0x10d:
                                SetHighByte(ref _TM3CNT_L, value);
                                break;

                            case 0x10e:
                                SetLowByte(ref _TM3CNT_H, value);
                                break;
                            case 0x10f:
                                SetHighByte(ref _TM3CNT_H, value);
                                break;

                            case 0x120:
                                SetLowByte(ref _SIOMULTI0, value);
                                break;
                            case 0x121:
                                SetHighByte(ref _SIOMULTI0, value);
                                break;

                            case 0x122:
                                SetLowByte(ref _SIOMULTI1, value);
                                break;
                            case 0x123:
                                SetHighByte(ref _SIOMULTI1, value);
                                break;

                            case 0x124:
                                SetLowByte(ref _SIOMULTI2, value);
                                break;
                            case 0x125:
                                SetHighByte(ref _SIOMULTI2, value);
                                break;

                            case 0x126:
                                SetLowByte(ref _SIOMULTI3, value);
                                break;
                            case 0x127:
                                SetHighByte(ref _SIOMULTI3, value);
                                break;

                            case 0x128:
                                SetLowByte(ref _SIOCNT, value);
                                break;
                            case 0x129:
                                SetHighByte(ref _SIOCNT, value);
                                break;

                            case 0x12a:
                                SetLowByte(ref _SIODATA8, value);
                                break;
                            case 0x12b:
                                SetHighByte(ref _SIODATA8, value);
                                break;

                            case 0x130:
                                SetLowByte(ref _KEYINPUT, value);
                                break;
                            case 0x131:
                                SetHighByte(ref _KEYINPUT, value);
                                break;

                            case 0x132:
                                SetLowByte(ref _KEYCNT, value);
                                break;
                            case 0x133:
                                SetHighByte(ref _KEYCNT, value);
                                break;

                            case 0x134:
                                SetLowByte(ref _RCNT, value);
                                break;
                            case 0x135:
                                SetHighByte(ref _RCNT, value);
                                break;

                            case 0x200:
                                SetLowByte(ref _IE, value);
                                UpdateInterrupts();
                                break;
                            case 0x201:
                                SetHighByte(ref _IE, value);
                                UpdateInterrupts();
                                break;

                            case 0x202:
                                _IF &= (UInt16)~value;
                                UpdateInterrupts();
                                break;
                            case 0x203:
                                _IF &= (UInt16)~(value << 8);
                                UpdateInterrupts();
                                break;

                            case 0x204:
                                SetLowByte(ref _WAITCNT, value);
                                break;
                            case 0x205:
                                SetHighByte(ref _WAITCNT, value);
                                break;

                            case 0x208:
                                SetLowByte(ref _IME, value);
                                UpdateInterrupts();
                                break;
                            case 0x209:
                                SetHighByte(ref _IME, value);
                                UpdateInterrupts();
                                break;

                            default:
                                throw new Exception(string.Format("Emulation.GBA.Memory: Unhandled write to address 0x{0:x8}", address));
                        }
                    }
                    break;

                // ROM
                case 0x8:
                case 0x9:
                case 0xa:
                case 0xb:
                case 0xc:
                case 0xd:
                    break;

                default:
                    throw new Exception(string.Format("Emulation.GBA.Memory: Unhandled write to address 0x{0:x8}", address));
            }
        }

        private void WriteMemory16(UInt32 address, UInt16 value)
        {
            address &= 0x0fff_fffe;

            IntPtr page = _write16PageTable[address >> 10];

            if (page != IntPtr.Zero)
            {
                unsafe
                {
                    // much faster than Marshal.WriteInt16
                    Unsafe.Write<UInt16>((Byte*)page + (address & 0x3ff), value);
                }

                return;
            }

            // page fault
            switch (address >> 24)
            {
                // BIOS
                case 0x0:
                case 0x1:
                    break;

                // IO and registers
                case 0x4:
                    {
                        UInt32 offset = address - 0x400_0000;

                        switch (offset)
                        {
                            case 0x000:
                                _ppu.DISPCNT = value;
                                break;
                            case 0x004:
                                _ppu.DISPSTAT = value;
                                break;
                            case 0x008:
                                _ppu.BG0CNT = value;
                                break;
                            case 0x00a:
                                _ppu.BG1CNT = value;
                                break;
                            case 0x00c:
                                _ppu.BG2CNT = value;
                                break;
                            case 0x00e:
                                _ppu.BG3CNT = value;
                                break;
                            case 0x010:
                                _ppu.BG0HOFS = value;
                                break;
                            case 0x012:
                                _ppu.BG0VOFS = value;
                                break;
                            case 0x014:
                                _ppu.BG1HOFS = value;
                                break;
                            case 0x016:
                                _ppu.BG1VOFS = value;
                                break;
                            case 0x018:
                                _ppu.BG2HOFS = value;
                                break;
                            case 0x01a:
                                _ppu.BG2VOFS = value;
                                break;
                            case 0x01c:
                                _ppu.BG3HOFS = value;
                                break;
                            case 0x01e:
                                _ppu.BG3VOFS = value;
                                break;
                            case 0x040:
                                _ppu.WIN0H = value;
                                break;
                            case 0x042:
                                _ppu.WIN1H = value;
                                break;
                            case 0x044:
                                _ppu.WIN0V = value;
                                break;
                            case 0x046:
                                _ppu.WIN1V = value;
                                break;
                            case 0x048:
                                _ppu.WININ = value;
                                break;
                            case 0x04a:
                                _ppu.WINOUT = value;
                                break;
                            case 0x04c:
                                _ppu.MOSAIC = value;
                                break;
                            case 0x050:
                                _ppu.BLDCNT = value;
                                break;
                            case 0x052:
                                _ppu.BLDALPHA = value;
                                break;
                            case 0x054:
                                _ppu.BLDY = value;
                                break;
                            case 0x062:
                                _SOUND1CNT_H = value;
                                break;
                            case 0x064:
                                _SOUND1CNT_X = value;
                                break;
                            case 0x068:
                                _SOUND2CNT_L = value;
                                break;
                            case 0x06c:
                                _SOUND2CNT_H = value;
                                break;
                            case 0x070:
                                _SOUND3CNT_L = value;
                                break;
                            case 0x078:
                                _SOUND4CNT_L = value;
                                break;
                            case 0x07c:
                                _SOUND4CNT_H = value;
                                break;
                            case 0x080:
                                _SOUNDCNT_L = value;
                                break;
                            case 0x082:
                                _SOUNDCNT_H = value;
                                break;
                            case 0x084:
                                _SOUNDCNT_X = value;
                                break;
                            case 0x088:
                                _SOUNDBIAS = value;
                                break;
                            case 0x0ba:
                                _DMA0CNT_H = value;
                                break;
                            case 0x0bc:
                                _DMA1SAD_L = value;
                                break;
                            case 0x0be:
                                _DMA1SAD_H = value;
                                break;
                            case 0x0c0:
                                _DMA1DAD_L = value;
                                break;
                            case 0x0c2:
                                _DMA1DAD_H = value;
                                break;
                            case 0x0c4:
                                _DMA1CNT_L = value;
                                break;
                            case 0x0c6:
                                _DMA1CNT_H = value;
                                break;
                            case 0x0c8:
                                _DMA2SAD_L = value;
                                break;
                            case 0x0ca:
                                _DMA2SAD_H = value;
                                break;
                            case 0x0cc:
                                _DMA2DAD_L = value;
                                break;
                            case 0x0ce:
                                _DMA2DAD_H = value;
                                break;
                            case 0x0d0:
                                _DMA2CNT_L = value;
                                break;
                            case 0x0d2:
                                _DMA2CNT_H = value;
                                break;
                            case 0x0de:
                                _DMA3CNT_H = value;
                                break;
                            case 0x100:
                                _TM0CNT_L = value;
                                break;
                            case 0x102:
                                _TM0CNT_H = value;
                                break;
                            case 0x104:
                                _TM1CNT_L = value;
                                break;
                            case 0x106:
                                _TM1CNT_H = value;
                                break;
                            case 0x108:
                                _TM2CNT_L = value;
                                break;
                            case 0x10a:
                                _TM2CNT_H = value;
                                break;
                            case 0x10c:
                                _TM3CNT_L = value;
                                break;
                            case 0x10e:
                                _TM3CNT_H = value;
                                break;
                            case 0x120:
                                _SIOMULTI0 = value;
                                break;
                            case 0x122:
                                _SIOMULTI1 = value;
                                break;
                            case 0x124:
                                _SIOMULTI2 = value;
                                break;
                            case 0x126:
                                _SIOMULTI3 = value;
                                break;
                            case 0x128:
                                _SIOCNT = value;
                                break;
                            case 0x12a:
                                _SIODATA8 = value;
                                break;
                            case 0x130:
                                _KEYINPUT = value;
                                break;
                            case 0x132:
                                _KEYCNT = value;
                                break;
                            case 0x134:
                                _RCNT = value;
                                break;
                            case 0x200:
                                _IE = value;
                                UpdateInterrupts();
                                break;
                            case 0x202:
                                _IF &= (UInt16)~value;
                                UpdateInterrupts();
                                break;
                            case 0x204:
                                _WAITCNT = value;
                                break;
                            case 0x208:
                                _IME = value;
                                UpdateInterrupts();
                                break;
                            default:
                                throw new Exception(string.Format("Emulation.GBA.Memory: Unhandled write to address 0x{0:x8}", address));
                        }
                    }
                    break;

                // ROM
                case 0x8:
                case 0x9:
                case 0xa:
                case 0xb:
                case 0xc:
                case 0xd:
                    break;

                default:
                    throw new Exception(string.Format("Emulation.GBA.Memory: Unhandled write to address 0x{0:x8}", address));
            }
        }

        private void WriteMemory32(UInt32 address, UInt32 value)
        {
            address &= 0x0fff_fffc;

            IntPtr page = _write32PageTable[address >> 10];

            if (page != IntPtr.Zero)
            {
                unsafe
                {
                    // much faster than Marshal.WriteInt32
                    Unsafe.Write<UInt32>((Byte*)page + (address & 0x3ff), value);
                }

                return;
            }

            // page fault
            switch (address >> 24)
            {
                // BIOS
                case 0x0:
                case 0x1:
                    break;

                // IO and registers
                case 0x4:
                    {
                        UInt32 offset = address - 0x400_0000;

                        switch (offset)
                        {
                            case 0x000:
                                _ppu.DISPCNT = (UInt16)value;
                                // 16 upper bits are undocumented (green swap register)
                                break;
                            case 0x0bc:
                                _DMA1SAD_L = (UInt16)value;
                                _DMA1SAD_H = (UInt16)(value >> 16);
                                break;
                            case 0x0c0:
                                _DMA1DAD_L = (UInt16)value;
                                _DMA1DAD_H = (UInt16)(value >> 16);
                                break;
                            case 0x0c4:
                                _DMA1CNT_L = (UInt16)value;
                                _DMA1CNT_H = (UInt16)(value >> 16);
                                break;
                            case 0x0c8:
                                _DMA2SAD_L = (UInt16)value;
                                _DMA2SAD_H = (UInt16)(value >> 16);
                                break;
                            case 0x0cc:
                                _DMA2DAD_L = (UInt16)value;
                                _DMA2DAD_H = (UInt16)(value >> 16);
                                break;
                            case 0x0d0:
                                _DMA2CNT_L = (UInt16)value;
                                _DMA2CNT_H = (UInt16)(value >> 16);
                                break;
                            case 0x208:
                                _IME = (UInt16)value;
                                // 16 upper bits are unused
                                break;
                            default:
                                throw new Exception(string.Format("Emulation.GBA.Memory: Unhandled write to address 0x{0:x8}", address));
                        }
                    }
                    break;

                // ROM
                case 0x8:
                case 0x9:
                case 0xa:
                case 0xb:
                case 0xc:
                case 0xd:
                    break;

                default:
                    throw new Exception(string.Format("Emulation.GBA.Memory: Unhandled write to address 0x{0:x8}", address));
            }
        }
    }
}
