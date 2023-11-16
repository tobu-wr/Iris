using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Iris.GBA
{
    internal sealed class Memory
    {
        private Communication? _communication;
        private Timer? _timer;
        private Sound? _sound;
        private DMA? _dma;
        private KeyInput? _keyInput;
        private SystemControl? _systemControl;
        private InterruptControl? _interruptControl;
        private BIOS? _bios;
        private Video? _video;

        [Flags]
        internal enum Flag
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

        private int _ROMSize;
        private const int SRAMSize = 64 * KB;
        private const int EWRAMSize = 256 * KB;
        private const int IWRAMSize = 32 * KB;

        private IntPtr _ROM;
        private readonly IntPtr _SRAM = Marshal.AllocHGlobal(SRAMSize);
        private readonly IntPtr _eWRAM = Marshal.AllocHGlobal(EWRAMSize);
        private readonly IntPtr _iWRAM = Marshal.AllocHGlobal(IWRAMSize);

        private const int PageSize = 1 * KB;
        private const int PageTableSize = 1 << 18;

        private readonly IntPtr[] _read8PageTable = new IntPtr[PageTableSize];
        private readonly IntPtr[] _read16PageTable = new IntPtr[PageTableSize];
        private readonly IntPtr[] _read32PageTable = new IntPtr[PageTableSize];
        private readonly IntPtr[] _write8PageTable = new IntPtr[PageTableSize];
        private readonly IntPtr[] _write16PageTable = new IntPtr[PageTableSize];
        private readonly IntPtr[] _write32PageTable = new IntPtr[PageTableSize];

        internal void Initialize(Communication communication, Timer timer, Sound sound, DMA dma, KeyInput keyInput, SystemControl systemControl, InterruptControl interruptControl, BIOS bios, Video video)
        {
            _communication = communication;
            _timer = timer;
            _sound = sound;
            _dma = dma;
            _keyInput = keyInput;
            _systemControl = systemControl;
            _interruptControl = interruptControl;
            _bios = bios;
            _video = video;

            InitPageTables();
        }

        internal void Reset()
        {
            // TODO
        }

        internal void Map(IntPtr data, int size, UInt32 startAddress, UInt32 endAddress, Flag flags)
        {
            int pageCount = size / PageSize;
            int startTablePageIndex = (int)(startAddress >> 10);
            int endPageTableIndex = (int)(endAddress >> 10);

            bool readable8 = flags.HasFlag(Flag.Read8);
            bool readable16 = flags.HasFlag(Flag.Read16);
            bool readable32 = flags.HasFlag(Flag.Read32);
            bool writable8 = flags.HasFlag(Flag.Write8);
            bool writable16 = flags.HasFlag(Flag.Write16);
            bool writable32 = flags.HasFlag(Flag.Write32);
            bool mirrored = flags.HasFlag(Flag.Mirrored);

            for (int pageTableIndex = startTablePageIndex, pageIndex = 0; pageTableIndex != endPageTableIndex; ++pageTableIndex, ++pageIndex)
            {
                if (pageIndex < pageCount)
                {
                    int pageOffset = pageIndex * PageSize;
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
                    int pageOffset = (pageIndex % pageCount) * PageSize;
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
                int length = pageCount * PageSize;

                for (int offset = 0; offset < length; ++offset)
                    Marshal.WriteByte(data, offset, 0);
            }
        }

        internal void Unmap(UInt32 startAddress, UInt32 endAddress)
        {
            // TODO
        }

        internal void InitPageTables()
        {
            Map(_eWRAM, EWRAMSize, 0x0200_0000, 0x0300_0000, Flag.All);
            Map(_iWRAM, IWRAMSize, 0x0300_0000, 0x0400_0000, Flag.All);
            Map(_SRAM, SRAMSize, 0x0e00_0000, 0x1000_0000, Flag.Read8 | Flag.Write8 | Flag.Mirrored);
        }

        internal void LoadROM(string filename)
        {
            Byte[] data = File.ReadAllBytes(filename);

            _ROMSize = data.Length;

            if (_ROM != IntPtr.Zero)
                Marshal.FreeHGlobal(_ROM);

            _ROM = Marshal.AllocHGlobal(_ROMSize);
            Marshal.Copy(data, 0, _ROM, _ROMSize);

            Map(_ROM, _ROMSize, 0x0800_0000, 0x0a00_0000, Flag.AllRead);
            Map(_ROM, _ROMSize, 0x0a00_0000, 0x0c00_0000, Flag.AllRead);
            Map(_ROM, _ROMSize, 0x0c00_0000, 0x0e00_0000, Flag.AllRead);
        }

        internal Byte Read8(UInt32 address)
        {
            address &= 0x0fff_ffff;

            IntPtr page = Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_read8PageTable), address >> 10);

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
                    return _bios!.Read8(address);

                // IO and registers
                case 0x4:
                    {
                        [MethodImpl(MethodImplOptions.AggressiveInlining)]
                        static Byte GetLowByte(UInt16 value) => (Byte)value;

                        [MethodImpl(MethodImplOptions.AggressiveInlining)]
                        static Byte GetHighByte(UInt16 value) => (Byte)(value >> 8);

                        UInt32 offset = address - 0x400_0000;

                        return offset switch
                        {
                            0x000 => GetLowByte(_video!._DISPCNT),
                            0x001 => GetHighByte(_video!._DISPCNT),

                            0x004 => GetLowByte(_video!._DISPSTAT),
                            0x005 => GetHighByte(_video!._DISPSTAT),

                            0x006 => GetLowByte(_video!._VCOUNT),
                            0x007 => GetHighByte(_video!._VCOUNT),

                            0x008 => GetLowByte(_video!._BG0CNT),
                            0x009 => GetHighByte(_video!._BG0CNT),

                            0x00a => GetLowByte(_video!._BG1CNT),
                            0x00b => GetHighByte(_video!._BG1CNT),

                            0x00c => GetLowByte(_video!._BG2CNT),
                            0x00d => GetHighByte(_video!._BG2CNT),

                            0x00e => GetLowByte(_video!._BG3CNT),
                            0x00f => GetHighByte(_video!._BG3CNT),

                            0x048 => GetLowByte(_video!._WININ),
                            0x049 => GetHighByte(_video!._WININ),

                            0x04a => GetLowByte(_video!._WINOUT),
                            0x04b => GetHighByte(_video!._WINOUT),

                            0x050 => GetLowByte(_video!._BLDCNT),
                            0x051 => GetHighByte(_video!._BLDCNT),

                            0x052 => GetLowByte(_video!._BLDALPHA),
                            0x053 => GetHighByte(_video!._BLDALPHA),

                            0x060 => GetLowByte(_sound!._SOUND1CNT_L),
                            0x061 => GetHighByte(_sound!._SOUND1CNT_L),

                            0x062 => GetLowByte(_sound!._SOUND1CNT_H),
                            0x063 => GetHighByte(_sound!._SOUND1CNT_H),

                            0x064 => GetLowByte(_sound!._SOUND1CNT_X),
                            0x065 => GetHighByte(_sound!._SOUND1CNT_X),

                            0x068 => GetLowByte(_sound!._SOUND2CNT_L),
                            0x069 => GetHighByte(_sound!._SOUND2CNT_L),

                            0x06c => GetLowByte(_sound!._SOUND2CNT_H),
                            0x06d => GetHighByte(_sound!._SOUND2CNT_H),

                            0x070 => GetLowByte(_sound!._SOUND3CNT_L),
                            0x071 => GetHighByte(_sound!._SOUND3CNT_L),

                            0x072 => GetLowByte(_sound!._SOUND3CNT_H),
                            0x073 => GetHighByte(_sound!._SOUND3CNT_H),

                            0x074 => GetLowByte(_sound!._SOUND3CNT_X),
                            0x075 => GetHighByte(_sound!._SOUND3CNT_X),

                            0x078 => GetLowByte(_sound!._SOUND4CNT_L),
                            0x079 => GetHighByte(_sound!._SOUND4CNT_L),

                            0x07c => GetLowByte(_sound!._SOUND4CNT_H),
                            0x07d => GetHighByte(_sound!._SOUND4CNT_H),

                            0x080 => GetLowByte(_sound!._SOUNDCNT_L),
                            0x081 => GetHighByte(_sound!._SOUNDCNT_L),

                            0x082 => GetLowByte(_sound!._SOUNDCNT_H),
                            0x083 => GetHighByte(_sound!._SOUNDCNT_H),

                            0x084 => GetLowByte(_sound!._SOUNDCNT_X),
                            0x085 => GetHighByte(_sound!._SOUNDCNT_X),

                            0x088 => GetLowByte(_sound!._SOUNDBIAS),
                            0x089 => GetHighByte(_sound!._SOUNDBIAS),

                            0x090 => GetLowByte(_sound!._WAVE_RAM0_L),
                            0x091 => GetHighByte(_sound!._WAVE_RAM0_L),

                            0x092 => GetLowByte(_sound!._WAVE_RAM0_H),
                            0x093 => GetHighByte(_sound!._WAVE_RAM0_H),

                            0x094 => GetLowByte(_sound!._WAVE_RAM1_L),
                            0x095 => GetHighByte(_sound!._WAVE_RAM1_L),

                            0x096 => GetLowByte(_sound!._WAVE_RAM1_H),
                            0x097 => GetHighByte(_sound!._WAVE_RAM1_H),

                            0x098 => GetLowByte(_sound!._WAVE_RAM2_L),
                            0x099 => GetHighByte(_sound!._WAVE_RAM2_L),

                            0x09a => GetLowByte(_sound!._WAVE_RAM2_H),
                            0x09b => GetHighByte(_sound!._WAVE_RAM2_H),

                            0x09c => GetLowByte(_sound!._WAVE_RAM3_L),
                            0x09d => GetHighByte(_sound!._WAVE_RAM3_L),

                            0x09e => GetLowByte(_sound!._WAVE_RAM3_H),
                            0x09f => GetHighByte(_sound!._WAVE_RAM3_H),

                            0x0ba => GetLowByte(_dma!._DMA0CNT_H),
                            0x0bb => GetHighByte(_dma!._DMA0CNT_H),

                            0x0c6 => GetLowByte(_dma!._DMA1CNT_H),
                            0x0c7 => GetHighByte(_dma!._DMA1CNT_H),

                            0x0d2 => GetLowByte(_dma!._DMA2CNT_H),
                            0x0d3 => GetHighByte(_dma!._DMA2CNT_H),

                            0x0de => GetLowByte(_dma!._DMA3CNT_H),
                            0x0df => GetHighByte(_dma!._DMA3CNT_H),

                            0x100 => GetLowByte(_timer!._TM0CNT_L),
                            0x101 => GetHighByte(_timer!._TM0CNT_L),

                            0x102 => GetLowByte(_timer!._TM0CNT_H),
                            0x103 => GetHighByte(_timer!._TM0CNT_H),

                            0x104 => GetLowByte(_timer!._TM1CNT_L),
                            0x105 => GetHighByte(_timer!._TM1CNT_L),

                            0x106 => GetLowByte(_timer!._TM1CNT_H),
                            0x107 => GetHighByte(_timer!._TM1CNT_H),

                            0x108 => GetLowByte(_timer!._TM2CNT_L),
                            0x109 => GetHighByte(_timer!._TM2CNT_L),

                            0x10a => GetLowByte(_timer!._TM2CNT_H),
                            0x10b => GetHighByte(_timer!._TM2CNT_H),

                            0x10c => GetLowByte(_timer!._TM3CNT_L),
                            0x10d => GetHighByte(_timer!._TM3CNT_L),

                            0x10e => GetLowByte(_timer!._TM3CNT_H),
                            0x10f => GetHighByte(_timer!._TM3CNT_H),

                            0x120 => GetLowByte(_communication!._SIODATA0),
                            0x121 => GetHighByte(_communication!._SIODATA0),

                            0x122 => GetLowByte(_communication!._SIODATA1),
                            0x123 => GetHighByte(_communication!._SIODATA1),

                            0x124 => GetLowByte(_communication!._SIODATA2),
                            0x125 => GetHighByte(_communication!._SIODATA2),

                            0x126 => GetLowByte(_communication!._SIODATA3),
                            0x127 => GetHighByte(_communication!._SIODATA3),

                            0x128 => GetLowByte(_communication!._SIOCNT),
                            0x129 => GetHighByte(_communication!._SIOCNT),

                            0x12a => GetLowByte(_communication!._SIODATA_SEND),
                            0x12b => GetHighByte(_communication!._SIODATA_SEND),

                            0x130 => GetLowByte(_keyInput!._KEYINPUT),
                            0x131 => GetHighByte(_keyInput!._KEYINPUT),

                            0x132 => GetLowByte(_keyInput!._KEYCNT),
                            0x133 => GetHighByte(_keyInput!._KEYCNT),

                            0x134 => GetLowByte(_communication!._RCNT),
                            0x135 => GetHighByte(_communication!._RCNT),

                            0x200 => GetLowByte(_interruptControl!._IE),
                            0x201 => GetHighByte(_interruptControl!._IE),

                            0x202 => GetLowByte(_interruptControl!._IF),
                            0x203 => GetHighByte(_interruptControl!._IF),

                            0x204 => GetLowByte(_systemControl!._WAITCNT),
                            0x205 => GetHighByte(_systemControl!._WAITCNT),

                            0x208 => GetLowByte(_interruptControl!._IME),
                            0x209 => GetHighByte(_interruptControl!._IME),

                            0x300 => _systemControl._POSTFLG,

                            _ => throw new Exception(string.Format("Iris.GBA.Memory: Unhandled read from address 0x{0:x8}", address)),
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

            throw new Exception(string.Format("Iris.GBA.Memory: Unhandled read from address 0x{0:x8}", address));
        }

        internal UInt16 Read16(UInt32 address)
        {
            address &= 0x0fff_fffe;

            IntPtr page = Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_read16PageTable), address >> 10);

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
                    return _bios!.Read16(address);

                // IO and registers
                case 0x4:
                    {
                        UInt32 offset = address - 0x400_0000;

                        return offset switch
                        {
                            0x000 => _video!._DISPCNT,
                            0x004 => _video!._DISPSTAT,
                            0x006 => _video!._VCOUNT,
                            0x008 => _video!._BG0CNT,
                            0x00a => _video!._BG1CNT,
                            0x00c => _video!._BG2CNT,
                            0x00e => _video!._BG3CNT,
                            0x048 => _video!._WININ,
                            0x04a => _video!._WINOUT,
                            0x050 => _video!._BLDCNT,
                            0x052 => _video!._BLDALPHA,
                            0x060 => _sound!._SOUND1CNT_L,
                            0x062 => _sound!._SOUND1CNT_H,
                            0x064 => _sound!._SOUND1CNT_X,
                            0x068 => _sound!._SOUND2CNT_L,
                            0x06c => _sound!._SOUND2CNT_H,
                            0x070 => _sound!._SOUND3CNT_L,
                            0x072 => _sound!._SOUND3CNT_H,
                            0x074 => _sound!._SOUND3CNT_X,
                            0x078 => _sound!._SOUND4CNT_L,
                            0x07c => _sound!._SOUND4CNT_H,
                            0x080 => _sound!._SOUNDCNT_L,
                            0x082 => _sound!._SOUNDCNT_H,
                            0x084 => _sound!._SOUNDCNT_X,
                            0x088 => _sound!._SOUNDBIAS,
                            0x090 => _sound!._WAVE_RAM0_L,
                            0x092 => _sound!._WAVE_RAM0_H,
                            0x094 => _sound!._WAVE_RAM1_L,
                            0x096 => _sound!._WAVE_RAM1_H,
                            0x098 => _sound!._WAVE_RAM2_L,
                            0x09a => _sound!._WAVE_RAM2_H,
                            0x09c => _sound!._WAVE_RAM3_L,
                            0x09e => _sound!._WAVE_RAM3_H,
                            0x0ba => _dma!._DMA0CNT_H,
                            0x0c6 => _dma!._DMA1CNT_H,
                            0x0d2 => _dma!._DMA2CNT_H,
                            0x0de => _dma!._DMA3CNT_H,
                            0x100 => _timer!._TM0CNT_L,
                            0x102 => _timer!._TM0CNT_H,
                            0x104 => _timer!._TM1CNT_L,
                            0x106 => _timer!._TM1CNT_H,
                            0x108 => _timer!._TM2CNT_L,
                            0x10a => _timer!._TM2CNT_H,
                            0x10c => _timer!._TM3CNT_L,
                            0x10e => _timer!._TM3CNT_H,
                            0x120 => _communication!._SIODATA0,
                            0x122 => _communication!._SIODATA1,
                            0x124 => _communication!._SIODATA2,
                            0x126 => _communication!._SIODATA3,
                            0x128 => _communication!._SIOCNT,
                            0x12a => _communication!._SIODATA_SEND,
                            0x130 => _keyInput!._KEYINPUT,
                            0x132 => _keyInput!._KEYCNT,
                            0x134 => _communication!._RCNT,
                            0x200 => _interruptControl!._IE,
                            0x202 => _interruptControl!._IF,
                            0x204 => _systemControl!._WAITCNT,
                            0x208 => _interruptControl!._IME,
                            _ => throw new Exception(string.Format("Iris.GBA.Memory: Unhandled read from address 0x{0:x8}", address)),
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

            throw new Exception(string.Format("Iris.GBA.Memory: Unhandled read from address 0x{0:x8}", address));
        }

        internal UInt32 Read32(UInt32 address)
        {
            address &= 0x0fff_fffc;

            IntPtr page = Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_read32PageTable), address >> 10);

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
                    return _bios!.Read32(address);

                // IO and registers
                case 0x4:
                    {
                        UInt32 offset = address - 0x400_0000;

                        return offset switch
                        {
                            0x000 => (UInt32)(_video!._DISPCNT),
                            0x004 => (UInt32)((_video!._VCOUNT << 16) | _video._DISPSTAT),
                            0x008 => (UInt32)((_video!._BG1CNT << 16) | _video._BG0CNT),
                            0x00c => (UInt32)((_video!._BG3CNT << 16) | _video._BG2CNT),
                            0x0b8 => (UInt32)(_dma!._DMA0CNT_H << 16),
                            0x0c4 => (UInt32)(_dma!._DMA1CNT_H << 16),
                            0x0d0 => (UInt32)(_dma!._DMA2CNT_H << 16),
                            0x0dc => (UInt32)(_dma!._DMA3CNT_H << 16),
                            0x150 => (UInt32)((_communication._JOY_RECV_H << 16) | _communication._JOY_RECV_L),
                            0x200 => (UInt32)((_interruptControl!._IF << 16) | _interruptControl._IE),
                            _ => throw new Exception(string.Format("Iris.GBA.Memory: Unhandled read from address 0x{0:x8}", address)),
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

            throw new Exception(string.Format("Iris.GBA.Memory: Unhandled read from address 0x{0:x8}", address));
        }

        internal void Write8(UInt32 address, Byte value)
        {
            address &= 0x0fff_ffff;

            IntPtr page = Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_write8PageTable), address >> 10);

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
                        [MethodImpl(MethodImplOptions.AggressiveInlining)]
                        static void SetLowByte(ref UInt16 input, Byte value) => input = (UInt16)((input & 0xff00) | value);

                        [MethodImpl(MethodImplOptions.AggressiveInlining)]
                        static void SetHighByte(ref UInt16 input, Byte value) => input = (UInt16)((input & 0x00ff) | (value << 8));

                        UInt32 offset = address - 0x400_0000;

                        switch (offset)
                        {
                            case 0x000:
                                SetLowByte(ref _video!._DISPCNT, value);
                                break;
                            case 0x001:
                                SetHighByte(ref _video!._DISPCNT, value);
                                break;

                            case 0x004:
                                SetLowByte(ref _video!._DISPSTAT, value);
                                break;
                            case 0x005:
                                SetHighByte(ref _video!._DISPSTAT, value);
                                break;

                            case 0x008:
                                SetLowByte(ref _video!._BG0CNT, value);
                                break;
                            case 0x009:
                                SetHighByte(ref _video!._BG0CNT, value);
                                break;

                            case 0x00a:
                                SetLowByte(ref _video!._BG1CNT, value);
                                break;
                            case 0x00b:
                                SetHighByte(ref _video!._BG1CNT, value);
                                break;

                            case 0x00c:
                                SetLowByte(ref _video!._BG2CNT, value);
                                break;
                            case 0x00d:
                                SetHighByte(ref _video!._BG2CNT, value);
                                break;

                            case 0x00e:
                                SetLowByte(ref _video!._BG3CNT, value);
                                break;
                            case 0x00f:
                                SetHighByte(ref _video!._BG3CNT, value);
                                break;

                            case 0x010:
                                SetLowByte(ref _video!._BG0HOFS, value);
                                break;
                            case 0x011:
                                SetHighByte(ref _video!._BG0HOFS, value);
                                break;

                            case 0x012:
                                SetLowByte(ref _video!._BG0VOFS, value);
                                break;
                            case 0x013:
                                SetHighByte(ref _video!._BG0VOFS, value);
                                break;

                            case 0x014:
                                SetLowByte(ref _video!._BG1HOFS, value);
                                break;
                            case 0x015:
                                SetHighByte(ref _video!._BG1HOFS, value);
                                break;

                            case 0x016:
                                SetLowByte(ref _video!._BG1VOFS, value);
                                break;
                            case 0x017:
                                SetHighByte(ref _video!._BG1VOFS, value);
                                break;

                            case 0x018:
                                SetLowByte(ref _video!._BG2HOFS, value);
                                break;
                            case 0x019:
                                SetHighByte(ref _video!._BG2HOFS, value);
                                break;

                            case 0x01a:
                                SetLowByte(ref _video!._BG2VOFS, value);
                                break;
                            case 0x01b:
                                SetHighByte(ref _video!._BG2VOFS, value);
                                break;

                            case 0x01c:
                                SetLowByte(ref _video!._BG3HOFS, value);
                                break;
                            case 0x01d:
                                SetHighByte(ref _video!._BG3HOFS, value);
                                break;

                            case 0x01e:
                                SetLowByte(ref _video!._BG3VOFS, value);
                                break;
                            case 0x01f:
                                SetHighByte(ref _video!._BG3VOFS, value);
                                break;

                            case 0x040:
                                SetLowByte(ref _video!._WIN0H, value);
                                break;
                            case 0x041:
                                SetHighByte(ref _video!._WIN0H, value);
                                break;

                            case 0x042:
                                SetLowByte(ref _video!._WIN1H, value);
                                break;
                            case 0x043:
                                SetHighByte(ref _video!._WIN1H, value);
                                break;

                            case 0x044:
                                SetLowByte(ref _video!._WIN0V, value);
                                break;
                            case 0x045:
                                SetHighByte(ref _video!._WIN0V, value);
                                break;

                            case 0x046:
                                SetLowByte(ref _video!._WIN1V, value);
                                break;
                            case 0x047:
                                SetHighByte(ref _video!._WIN1V, value);
                                break;

                            case 0x048:
                                SetLowByte(ref _video!._WININ, value);
                                break;
                            case 0x049:
                                SetHighByte(ref _video!._WININ, value);
                                break;

                            case 0x04a:
                                SetLowByte(ref _video!._WINOUT, value);
                                break;
                            case 0x04b:
                                SetHighByte(ref _video!._WINOUT, value);
                                break;

                            case 0x04c:
                                SetLowByte(ref _video!._MOSAIC, value);
                                break;
                            case 0x04d:
                                SetHighByte(ref _video!._MOSAIC, value);
                                break;

                            case 0x050:
                                SetLowByte(ref _video!._BLDCNT, value);
                                break;
                            case 0x051:
                                SetHighByte(ref _video!._BLDCNT, value);
                                break;

                            case 0x052:
                                SetLowByte(ref _video!._BLDALPHA, value);
                                break;
                            case 0x053:
                                SetHighByte(ref _video!._BLDALPHA, value);
                                break;

                            case 0x054:
                                SetLowByte(ref _video!._BLDY, value);
                                break;
                            case 0x055:
                                SetHighByte(ref _video!._BLDY, value);
                                break;

                            case 0x060:
                                SetLowByte(ref _sound!._SOUND1CNT_L, value);
                                break;
                            case 0x061:
                                SetHighByte(ref _sound!._SOUND1CNT_L, value);
                                break;

                            case 0x062:
                                SetLowByte(ref _sound!._SOUND1CNT_H, value);
                                break;
                            case 0x063:
                                SetHighByte(ref _sound!._SOUND1CNT_H, value);
                                break;

                            case 0x064:
                                SetLowByte(ref _sound!._SOUND1CNT_X, value);
                                break;
                            case 0x065:
                                SetHighByte(ref _sound!._SOUND1CNT_X, value);
                                break;

                            case 0x068:
                                SetLowByte(ref _sound!._SOUND2CNT_L, value);
                                break;
                            case 0x069:
                                SetHighByte(ref _sound!._SOUND2CNT_L, value);
                                break;

                            case 0x06c:
                                SetLowByte(ref _sound!._SOUND2CNT_H, value);
                                break;
                            case 0x06d:
                                SetHighByte(ref _sound!._SOUND2CNT_H, value);
                                break;

                            case 0x070:
                                SetLowByte(ref _sound!._SOUND3CNT_L, value);
                                break;
                            case 0x071:
                                SetHighByte(ref _sound!._SOUND3CNT_L, value);
                                break;

                            case 0x072:
                                SetLowByte(ref _sound!._SOUND3CNT_H, value);
                                break;
                            case 0x073:
                                SetHighByte(ref _sound!._SOUND3CNT_H, value);
                                break;

                            case 0x074:
                                SetLowByte(ref _sound!._SOUND3CNT_X, value);
                                break;
                            case 0x075:
                                SetHighByte(ref _sound!._SOUND3CNT_X, value);
                                break;

                            case 0x078:
                                SetLowByte(ref _sound!._SOUND4CNT_L, value);
                                break;
                            case 0x079:
                                SetHighByte(ref _sound!._SOUND4CNT_L, value);
                                break;

                            case 0x07c:
                                SetLowByte(ref _sound!._SOUND4CNT_H, value);
                                break;
                            case 0x07d:
                                SetHighByte(ref _sound!._SOUND4CNT_H, value);
                                break;

                            case 0x080:
                                SetLowByte(ref _sound!._SOUNDCNT_L, value);
                                break;
                            case 0x081:
                                SetHighByte(ref _sound!._SOUNDCNT_L, value);
                                break;

                            case 0x082:
                                SetLowByte(ref _sound!._SOUNDCNT_H, value);
                                break;
                            case 0x083:
                                SetHighByte(ref _sound!._SOUNDCNT_H, value);
                                break;

                            case 0x084:
                                SetLowByte(ref _sound!._SOUNDCNT_X, value);
                                break;
                            case 0x085:
                                SetHighByte(ref _sound!._SOUNDCNT_X, value);
                                break;

                            case 0x088:
                                SetLowByte(ref _sound!._SOUNDBIAS, value);
                                break;
                            case 0x089:
                                SetHighByte(ref _sound!._SOUNDBIAS, value);
                                break;

                            case 0x090:
                                SetLowByte(ref _sound!._WAVE_RAM0_L, value);
                                break;
                            case 0x091:
                                SetHighByte(ref _sound!._WAVE_RAM0_L, value);
                                break;

                            case 0x092:
                                SetLowByte(ref _sound!._WAVE_RAM0_H, value);
                                break;
                            case 0x093:
                                SetHighByte(ref _sound!._WAVE_RAM0_H, value);
                                break;

                            case 0x094:
                                SetLowByte(ref _sound!._WAVE_RAM1_L, value);
                                break;
                            case 0x095:
                                SetHighByte(ref _sound!._WAVE_RAM1_L, value);
                                break;

                            case 0x096:
                                SetLowByte(ref _sound!._WAVE_RAM1_H, value);
                                break;
                            case 0x097:
                                SetHighByte(ref _sound!._WAVE_RAM1_H, value);
                                break;

                            case 0x098:
                                SetLowByte(ref _sound!._WAVE_RAM2_L, value);
                                break;
                            case 0x099:
                                SetHighByte(ref _sound!._WAVE_RAM2_L, value);
                                break;

                            case 0x09a:
                                SetLowByte(ref _sound!._WAVE_RAM2_H, value);
                                break;
                            case 0x09b:
                                SetHighByte(ref _sound!._WAVE_RAM2_H, value);
                                break;

                            case 0x09c:
                                SetLowByte(ref _sound!._WAVE_RAM3_L, value);
                                break;
                            case 0x09d:
                                SetHighByte(ref _sound!._WAVE_RAM3_L, value);
                                break;

                            case 0x09e:
                                SetLowByte(ref _sound!._WAVE_RAM3_H, value);
                                break;
                            case 0x09f:
                                SetHighByte(ref _sound!._WAVE_RAM3_H, value);
                                break;

                            case 0x0b0:
                                SetLowByte(ref _dma!._DMA0SAD_L, value);
                                break;
                            case 0x0b1:
                                SetHighByte(ref _dma!._DMA0SAD_L, value);
                                break;

                            case 0x0b2:
                                SetLowByte(ref _dma!._DMA0SAD_H, value);
                                break;
                            case 0x0b3:
                                SetHighByte(ref _dma!._DMA0SAD_H, value);
                                break;

                            case 0x0b4:
                                SetLowByte(ref _dma!._DMA0DAD_L, value);
                                break;
                            case 0x0b5:
                                SetHighByte(ref _dma!._DMA0DAD_L, value);
                                break;

                            case 0x0b6:
                                SetLowByte(ref _dma!._DMA0DAD_H, value);
                                break;
                            case 0x0b7:
                                SetHighByte(ref _dma!._DMA0DAD_H, value);
                                break;

                            case 0x0b8:
                                SetLowByte(ref _dma!._DMA0CNT_L, value);
                                break;
                            case 0x0b9:
                                SetHighByte(ref _dma!._DMA0CNT_L, value);
                                break;

                            case 0x0ba:
                                SetLowByte(ref _dma!._DMA0CNT_H, value);
                                break;
                            case 0x0bb:
                                SetHighByte(ref _dma!._DMA0CNT_H, value);
                                break;

                            case 0x0bc:
                                SetLowByte(ref _dma!._DMA1SAD_L, value);
                                break;
                            case 0x0bd:
                                SetHighByte(ref _dma!._DMA1SAD_L, value);
                                break;

                            case 0x0be:
                                SetLowByte(ref _dma!._DMA1SAD_H, value);
                                break;
                            case 0x0bf:
                                SetHighByte(ref _dma!._DMA1SAD_H, value);
                                break;

                            case 0x0c0:
                                SetLowByte(ref _dma!._DMA1DAD_L, value);
                                break;
                            case 0x0c1:
                                SetHighByte(ref _dma!._DMA1DAD_L, value);
                                break;

                            case 0x0c2:
                                SetLowByte(ref _dma!._DMA1DAD_H, value);
                                break;
                            case 0x0c3:
                                SetHighByte(ref _dma!._DMA1DAD_H, value);
                                break;

                            case 0x0c4:
                                SetLowByte(ref _dma!._DMA1CNT_L, value);
                                break;
                            case 0x0c5:
                                SetHighByte(ref _dma!._DMA1CNT_L, value);
                                break;

                            case 0x0c6:
                                SetLowByte(ref _dma!._DMA1CNT_H, value);
                                break;
                            case 0x0c7:
                                SetHighByte(ref _dma!._DMA1CNT_H, value);
                                break;

                            case 0x0c8:
                                SetLowByte(ref _dma!._DMA2SAD_L, value);
                                break;
                            case 0x0c9:
                                SetHighByte(ref _dma!._DMA2SAD_L, value);
                                break;

                            case 0x0ca:
                                SetLowByte(ref _dma!._DMA2SAD_H, value);
                                break;
                            case 0x0cb:
                                SetHighByte(ref _dma!._DMA2SAD_H, value);
                                break;

                            case 0x0cc:
                                SetLowByte(ref _dma!._DMA2DAD_L, value);
                                break;
                            case 0x0cd:
                                SetHighByte(ref _dma!._DMA2DAD_L, value);
                                break;

                            case 0x0ce:
                                SetLowByte(ref _dma!._DMA2DAD_H, value);
                                break;
                            case 0x0cf:
                                SetHighByte(ref _dma!._DMA2DAD_H, value);
                                break;

                            case 0x0d0:
                                SetLowByte(ref _dma!._DMA2CNT_L, value);
                                break;
                            case 0x0d1:
                                SetHighByte(ref _dma!._DMA2CNT_L, value);
                                break;

                            case 0x0d2:
                                SetLowByte(ref _dma!._DMA2CNT_H, value);
                                break;
                            case 0x0d3:
                                SetHighByte(ref _dma!._DMA2CNT_H, value);
                                break;

                            case 0x0d4:
                                SetLowByte(ref _dma!._DMA3SAD_L, value);
                                break;
                            case 0x0d5:
                                SetHighByte(ref _dma!._DMA3SAD_L, value);
                                break;

                            case 0x0d6:
                                SetLowByte(ref _dma!._DMA3SAD_H, value);
                                break;
                            case 0x0d7:
                                SetHighByte(ref _dma!._DMA3SAD_H, value);
                                break;

                            case 0x0d8:
                                SetLowByte(ref _dma!._DMA3DAD_L, value);
                                break;
                            case 0x0d9:
                                SetHighByte(ref _dma!._DMA3DAD_L, value);
                                break;

                            case 0x0da:
                                SetLowByte(ref _dma!._DMA3DAD_H, value);
                                break;
                            case 0x0db:
                                SetHighByte(ref _dma!._DMA3DAD_H, value);
                                break;

                            case 0x0dc:
                                SetLowByte(ref _dma!._DMA3CNT_L, value);
                                break;
                            case 0x0dd:
                                SetHighByte(ref _dma!._DMA3CNT_L, value);
                                break;

                            case 0x0de:
                                SetLowByte(ref _dma!._DMA3CNT_H, value);
                                break;
                            case 0x0df:
                                SetHighByte(ref _dma!._DMA3CNT_H, value);
                                break;

                            case 0x100:
                                SetLowByte(ref _timer!._TM0CNT_L, value);
                                break;
                            case 0x101:
                                SetHighByte(ref _timer!._TM0CNT_L, value);
                                break;

                            case 0x102:
                                SetLowByte(ref _timer!._TM0CNT_H, value);
                                break;
                            case 0x103:
                                SetHighByte(ref _timer!._TM0CNT_H, value);
                                break;

                            case 0x104:
                                SetLowByte(ref _timer!._TM1CNT_L, value);
                                break;
                            case 0x105:
                                SetHighByte(ref _timer!._TM1CNT_L, value);
                                break;

                            case 0x106:
                                SetLowByte(ref _timer!._TM1CNT_H, value);
                                break;
                            case 0x107:
                                SetHighByte(ref _timer!._TM1CNT_H, value);
                                break;

                            case 0x108:
                                SetLowByte(ref _timer!._TM2CNT_L, value);
                                break;
                            case 0x109:
                                SetHighByte(ref _timer!._TM2CNT_L, value);
                                break;

                            case 0x10a:
                                SetLowByte(ref _timer!._TM2CNT_H, value);
                                break;
                            case 0x10b:
                                SetHighByte(ref _timer!._TM2CNT_H, value);
                                break;

                            case 0x10c:
                                SetLowByte(ref _timer!._TM3CNT_L, value);
                                break;
                            case 0x10d:
                                SetHighByte(ref _timer!._TM3CNT_L, value);
                                break;

                            case 0x10e:
                                SetLowByte(ref _timer!._TM3CNT_H, value);
                                break;
                            case 0x10f:
                                SetHighByte(ref _timer!._TM3CNT_H, value);
                                break;

                            case 0x120:
                                SetLowByte(ref _communication!._SIODATA0, value);
                                break;
                            case 0x121:
                                SetHighByte(ref _communication!._SIODATA0, value);
                                break;

                            case 0x122:
                                SetLowByte(ref _communication!._SIODATA1, value);
                                break;
                            case 0x123:
                                SetHighByte(ref _communication!._SIODATA1, value);
                                break;

                            case 0x124:
                                SetLowByte(ref _communication!._SIODATA2, value);
                                break;
                            case 0x125:
                                SetHighByte(ref _communication!._SIODATA2, value);
                                break;

                            case 0x126:
                                SetLowByte(ref _communication!._SIODATA3, value);
                                break;
                            case 0x127:
                                SetHighByte(ref _communication!._SIODATA3, value);
                                break;

                            case 0x128:
                                SetLowByte(ref _communication!._SIOCNT, value);
                                break;
                            case 0x129:
                                SetHighByte(ref _communication!._SIOCNT, value);
                                break;

                            case 0x12a:
                                SetLowByte(ref _communication!._SIODATA_SEND, value);
                                break;
                            case 0x12b:
                                SetHighByte(ref _communication!._SIODATA_SEND, value);
                                break;

                            case 0x130:
                                SetLowByte(ref _keyInput!._KEYINPUT, value);
                                break;
                            case 0x131:
                                SetHighByte(ref _keyInput!._KEYINPUT, value);
                                break;

                            case 0x132:
                                SetLowByte(ref _keyInput!._KEYCNT, value);
                                break;
                            case 0x133:
                                SetHighByte(ref _keyInput!._KEYCNT, value);
                                break;

                            case 0x134:
                                SetLowByte(ref _communication!._RCNT, value);
                                break;
                            case 0x135:
                                SetHighByte(ref _communication!._RCNT, value);
                                break;

                            case 0x140:
                                SetLowByte(ref _communication!._JOYCNT, value);
                                break;
                            case 0x141:
                                SetHighByte(ref _communication!._JOYCNT, value);
                                break;

                            case 0x200:
                                SetLowByte(ref _interruptControl!._IE, value);
                                _interruptControl.UpdateInterrupts();
                                break;
                            case 0x201:
                                SetHighByte(ref _interruptControl!._IE, value);
                                _interruptControl.UpdateInterrupts();
                                break;

                            case 0x202:
                                _interruptControl!._IF &= (UInt16)~value;
                                _interruptControl.UpdateInterrupts();
                                break;
                            case 0x203:
                                _interruptControl!._IF &= (UInt16)~(value << 8);
                                _interruptControl.UpdateInterrupts();
                                break;

                            case 0x204:
                                SetLowByte(ref _systemControl!._WAITCNT, value);
                                break;
                            case 0x205:
                                SetHighByte(ref _systemControl!._WAITCNT, value);
                                break;

                            case 0x208:
                                SetLowByte(ref _interruptControl!._IME, value);
                                _interruptControl.UpdateInterrupts();
                                break;
                            case 0x209:
                                SetHighByte(ref _interruptControl!._IME, value);
                                _interruptControl.UpdateInterrupts();
                                break;

                            case 0x300:
                                _systemControl!._POSTFLG = value;
                                break;

                            case 0x301:
                                _systemControl!._HALTCNT = value;
                                break;

                            case 0x410:
                                // undocumented
                                break;

                            default:
                                throw new Exception(string.Format("Iris.GBA.Memory: Unhandled write to address 0x{0:x8}", address));
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
                    throw new Exception(string.Format("Iris.GBA.Memory: Unhandled write to address 0x{0:x8}", address));
            }
        }

        internal void Write16(UInt32 address, UInt16 value)
        {
            address &= 0x0fff_fffe;

            IntPtr page = Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_write16PageTable), address >> 10);

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
                                _video!._DISPCNT = value;
                                break;
                            case 0x004:
                                _video!._DISPSTAT = value;
                                break;
                            case 0x008:
                                _video!._BG0CNT = value;
                                break;
                            case 0x00a:
                                _video!._BG1CNT = value;
                                break;
                            case 0x00c:
                                _video!._BG2CNT = value;
                                break;
                            case 0x00e:
                                _video!._BG3CNT = value;
                                break;
                            case 0x010:
                                _video!._BG0HOFS = value;
                                break;
                            case 0x012:
                                _video!._BG0VOFS = value;
                                break;
                            case 0x014:
                                _video!._BG1HOFS = value;
                                break;
                            case 0x016:
                                _video!._BG1VOFS = value;
                                break;
                            case 0x018:
                                _video!._BG2HOFS = value;
                                break;
                            case 0x01a:
                                _video!._BG2VOFS = value;
                                break;
                            case 0x01c:
                                _video!._BG3HOFS = value;
                                break;
                            case 0x01e:
                                _video!._BG3VOFS = value;
                                break;
                            case 0x020:
                                _video!._BG2PA = value;
                                break;
                            case 0x022:
                                _video!._BG2PB = value;
                                break;
                            case 0x024:
                                _video!._BG2PC = value;
                                break;
                            case 0x026:
                                _video!._BG2PD = value;
                                break;
                            case 0x028:
                                _video!._BG2X = (_video._BG2X & 0xffff_0000) | value;
                                break;
                            case 0x02a:
                                _video!._BG2X = (_video._BG2X & 0x0000_ffff) | ((UInt32)value << 16);
                                break;
                            case 0x02c:
                                _video!._BG2Y = (_video._BG2Y & 0xffff_0000) | value;
                                break;
                            case 0x02e:
                                _video!._BG2Y = (_video._BG2Y & 0x0000_ffff) | ((UInt32)value << 16);
                                break;
                            case 0x030:
                                _video!._BG3PA = value;
                                break;
                            case 0x032:
                                _video!._BG3PB = value;
                                break;
                            case 0x034:
                                _video!._BG3PC = value;
                                break;
                            case 0x036:
                                _video!._BG3PD = value;
                                break;
                            case 0x038:
                                _video!._BG3X = (_video._BG3X & 0xffff_0000) | value;
                                break;
                            case 0x03a:
                                _video!._BG3X = (_video._BG3X & 0x0000_ffff) | ((UInt32)value << 16);
                                break;
                            case 0x03c:
                                _video!._BG3Y = (_video._BG3Y & 0xffff_0000) | value;
                                break;
                            case 0x03e:
                                _video!._BG3Y = (_video._BG3Y & 0x0000_ffff) | ((UInt32)value << 16);
                                break;
                            case 0x040:
                                _video!._WIN0H = value;
                                break;
                            case 0x042:
                                _video!._WIN1H = value;
                                break;
                            case 0x044:
                                _video!._WIN0V = value;
                                break;
                            case 0x046:
                                _video!._WIN1V = value;
                                break;
                            case 0x048:
                                _video!._WININ = value;
                                break;
                            case 0x04a:
                                _video!._WINOUT = value;
                                break;
                            case 0x04c:
                                _video!._MOSAIC = value;
                                break;
                            case 0x050:
                                _video!._BLDCNT = value;
                                break;
                            case 0x052:
                                _video!._BLDALPHA = value;
                                break;
                            case 0x054:
                                _video!._BLDY = value;
                                break;
                            case 0x060:
                                _sound!._SOUND1CNT_L = value;
                                break;
                            case 0x062:
                                _sound!._SOUND1CNT_H = value;
                                break;
                            case 0x064:
                                _sound!._SOUND1CNT_X = value;
                                break;
                            case 0x068:
                                _sound!._SOUND2CNT_L = value;
                                break;
                            case 0x06c:
                                _sound!._SOUND2CNT_H = value;
                                break;
                            case 0x070:
                                _sound!._SOUND3CNT_L = value;
                                break;
                            case 0x072:
                                _sound!._SOUND3CNT_H = value;
                                break;
                            case 0x074:
                                _sound!._SOUND3CNT_X = value;
                                break;
                            case 0x078:
                                _sound!._SOUND4CNT_L = value;
                                break;
                            case 0x07c:
                                _sound!._SOUND4CNT_H = value;
                                break;
                            case 0x080:
                                _sound!._SOUNDCNT_L = value;
                                break;
                            case 0x082:
                                _sound!._SOUNDCNT_H = value;
                                break;
                            case 0x084:
                                _sound!._SOUNDCNT_X = value;
                                break;
                            case 0x088:
                                _sound!._SOUNDBIAS = value;
                                break;
                            case 0x090:
                                _sound!._WAVE_RAM0_L = value;
                                break;
                            case 0x092:
                                _sound!._WAVE_RAM0_H = value;
                                break;
                            case 0x094:
                                _sound!._WAVE_RAM1_L = value;
                                break;
                            case 0x096:
                                _sound!._WAVE_RAM1_H = value;
                                break;
                            case 0x098:
                                _sound!._WAVE_RAM2_L = value;
                                break;
                            case 0x09a:
                                _sound!._WAVE_RAM2_H = value;
                                break;
                            case 0x09c:
                                _sound!._WAVE_RAM3_L = value;
                                break;
                            case 0x09e:
                                _sound!._WAVE_RAM3_H = value;
                                break;
                            case 0x0b0:
                                _dma!._DMA0SAD_L = value;
                                break;
                            case 0x0b2:
                                _dma!._DMA0SAD_H = value;
                                break;
                            case 0x0b4:
                                _dma!._DMA0DAD_L = value;
                                break;
                            case 0x0b6:
                                _dma!._DMA0DAD_H = value;
                                break;
                            case 0x0b8:
                                _dma!._DMA0CNT_L = value;
                                break;
                            case 0x0ba:
                                _dma!._DMA0CNT_H = value;
                                break;
                            case 0x0bc:
                                _dma!._DMA1SAD_L = value;
                                break;
                            case 0x0be:
                                _dma!._DMA1SAD_H = value;
                                break;
                            case 0x0c0:
                                _dma!._DMA1DAD_L = value;
                                break;
                            case 0x0c2:
                                _dma!._DMA1DAD_H = value;
                                break;
                            case 0x0c4:
                                _dma!._DMA1CNT_L = value;
                                break;
                            case 0x0c6:
                                _dma!._DMA1CNT_H = value;
                                break;
                            case 0x0c8:
                                _dma!._DMA2SAD_L = value;
                                break;
                            case 0x0ca:
                                _dma!._DMA2SAD_H = value;
                                break;
                            case 0x0cc:
                                _dma!._DMA2DAD_L = value;
                                break;
                            case 0x0ce:
                                _dma!._DMA2DAD_H = value;
                                break;
                            case 0x0d0:
                                _dma!._DMA2CNT_L = value;
                                break;
                            case 0x0d2:
                                _dma!._DMA2CNT_H = value;
                                break;
                            case 0x0d4:
                                _dma!._DMA3SAD_L = value;
                                break;
                            case 0x0d6:
                                _dma!._DMA3SAD_H = value;
                                break;
                            case 0x0d8:
                                _dma!._DMA3DAD_L = value;
                                break;
                            case 0x0da:
                                _dma!._DMA3DAD_H = value;
                                break;
                            case 0x0dc:
                                _dma!._DMA3CNT_L = value;
                                break;
                            case 0x0de:
                                _dma!._DMA3CNT_H = value;
                                break;
                            case 0x100:
                                _timer!._TM0CNT_L = value;
                                break;
                            case 0x102:
                                _timer!._TM0CNT_H = value;
                                break;
                            case 0x104:
                                _timer!._TM1CNT_L = value;
                                break;
                            case 0x106:
                                _timer!._TM1CNT_H = value;
                                break;
                            case 0x108:
                                _timer!._TM2CNT_L = value;
                                break;
                            case 0x10a:
                                _timer!._TM2CNT_H = value;
                                break;
                            case 0x10c:
                                _timer!._TM3CNT_L = value;
                                break;
                            case 0x10e:
                                _timer!._TM3CNT_H = value;
                                break;
                            case 0x120:
                                _communication!._SIODATA0 = value;
                                break;
                            case 0x122:
                                _communication!._SIODATA1 = value;
                                break;
                            case 0x124:
                                _communication!._SIODATA2 = value;
                                break;
                            case 0x126:
                                _communication!._SIODATA3 = value;
                                break;
                            case 0x128:
                                _communication!._SIOCNT = value;
                                break;
                            case 0x12a:
                                _communication!._SIODATA_SEND = value;
                                break;
                            case 0x130:
                                _keyInput!._KEYINPUT = value;
                                break;
                            case 0x132:
                                _keyInput!._KEYCNT = value;
                                break;
                            case 0x134:
                                _communication!._RCNT = value;
                                break;
                            case 0x140:
                                _communication!._JOYCNT = value;
                                break;
                            case 0x158:
                                _communication!._JOYSTAT = value;
                                break;
                            case 0x200:
                                _interruptControl!._IE = value;
                                _interruptControl.UpdateInterrupts();
                                break;
                            case 0x202:
                                _interruptControl!._IF &= (UInt16)~value;
                                _interruptControl.UpdateInterrupts();
                                break;
                            case 0x204:
                                _systemControl!._WAITCNT = value;
                                break;
                            case 0x208:
                                _interruptControl!._IME = value;
                                _interruptControl.UpdateInterrupts();
                                break;
                            default:
                                throw new Exception(string.Format("Iris.GBA.Memory: Unhandled write to address 0x{0:x8}", address));
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
                    throw new Exception(string.Format("Iris.GBA.Memory: Unhandled write to address 0x{0:x8}", address));
            }
        }

        internal void Write32(UInt32 address, UInt32 value)
        {
            address &= 0x0fff_fffc;

            IntPtr page = Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_write32PageTable), address >> 10);

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
                        [MethodImpl(MethodImplOptions.AggressiveInlining)]
                        static UInt16 GetLowHalfword(UInt32 value) => (UInt16)value;

                        [MethodImpl(MethodImplOptions.AggressiveInlining)]
                        static UInt16 GetHighHalfword(UInt32 value) => (UInt16)(value >> 16);

                        UInt32 offset = address - 0x400_0000;

                        switch (offset)
                        {
                            case 0x000:
                                _video!._DISPCNT = GetLowHalfword(value);
                                // 16 upper bits are undocumented (green swap register)
                                break;
                            case 0x004:
                                _video!._DISPSTAT = GetLowHalfword(value);
                                break;
                            case 0x008:
                                _video!._BG0CNT = GetLowHalfword(value);
                                _video._BG1CNT = GetHighHalfword(value);
                                break;
                            case 0x00c:
                                _video!._BG2CNT = GetLowHalfword(value);
                                _video._BG3CNT = GetHighHalfword(value);
                                break;
                            case 0x010:
                                _video!._BG0HOFS = GetLowHalfword(value);
                                _video._BG0VOFS = GetHighHalfword(value);
                                break;
                            case 0x014:
                                _video!._BG1HOFS = GetLowHalfword(value);
                                _video._BG1VOFS = GetHighHalfword(value);
                                break;
                            case 0x018:
                                _video!._BG2HOFS = GetLowHalfword(value);
                                _video._BG2VOFS = GetHighHalfword(value);
                                break;
                            case 0x01c:
                                _video!._BG3HOFS = GetLowHalfword(value);
                                _video._BG3VOFS = GetHighHalfword(value);
                                break;
                            case 0x020:
                                _video!._BG2PA = GetLowHalfword(value);
                                _video._BG2PB = GetHighHalfword(value);
                                break;
                            case 0x024:
                                _video!._BG2PC = GetLowHalfword(value);
                                _video._BG2PD = GetHighHalfword(value);
                                break;
                            case 0x028:
                                _video!._BG2X = value;
                                break;
                            case 0x02c:
                                _video!._BG2Y = value;
                                break;
                            case 0x030:
                                _video!._BG3PA = GetLowHalfword(value);
                                _video._BG3PB = GetHighHalfword(value);
                                break;
                            case 0x034:
                                _video!._BG3PC = GetLowHalfword(value);
                                _video._BG3PD = GetHighHalfword(value);
                                break;
                            case 0x038:
                                _video!._BG3X = value;
                                break;
                            case 0x03c:
                                _video!._BG3Y = value;
                                break;
                            case 0x040:
                                _video!._WIN0H = GetLowHalfword(value);
                                _video._WIN1H = GetHighHalfword(value);
                                break;
                            case 0x044:
                                _video!._WIN0V = GetLowHalfword(value);
                                _video._WIN1V = GetHighHalfword(value);
                                break;
                            case 0x048:
                                _video!._WININ = GetLowHalfword(value);
                                _video._WINOUT = GetHighHalfword(value);
                                break;
                            case 0x04c:
                                _video!._MOSAIC = GetLowHalfword(value);
                                // 16 upper bits are unused
                                break;
                            case 0x050:
                                _video!._BLDCNT = GetLowHalfword(value);
                                _video._BLDALPHA = GetHighHalfword(value);
                                break;
                            case 0x054:
                                _video!._BLDY = GetLowHalfword(value);
                                // 16 upper bits are unused
                                break;
                            case 0x58:
                            case 0x5c:
                                // unused
                                break;
                            case 0x080:
                                _sound!._SOUNDCNT_L = GetLowHalfword(value);
                                _sound._SOUNDCNT_H = GetHighHalfword(value);
                                break;
                            case 0x090:
                                _sound!._WAVE_RAM0_L = GetLowHalfword(value);
                                _sound._WAVE_RAM0_H = GetHighHalfword(value);
                                break;
                            case 0x094:
                                _sound!._WAVE_RAM1_L = GetLowHalfword(value);
                                _sound._WAVE_RAM1_H = GetHighHalfword(value);
                                break;
                            case 0x098:
                                _sound!._WAVE_RAM2_L = GetLowHalfword(value);
                                _sound._WAVE_RAM2_H = GetHighHalfword(value);
                                break;
                            case 0x09c:
                                _sound!._WAVE_RAM3_L = GetLowHalfword(value);
                                _sound._WAVE_RAM3_H = GetHighHalfword(value);
                                break;
                            case 0x0a0:
                                _sound!._FIFO_A_L = GetLowHalfword(value);
                                _sound._FIFO_A_H = GetHighHalfword(value);
                                break;
                            case 0x0a4:
                                _sound!._FIFO_B_L = GetLowHalfword(value);
                                _sound._FIFO_B_H = GetHighHalfword(value);
                                break;
                            case 0x0a8:
                            case 0x0ac:
                                // unused
                                break;
                            case 0x0b0:
                                _dma!._DMA0SAD_L = GetLowHalfword(value);
                                _dma._DMA0SAD_H = GetHighHalfword(value);
                                break;
                            case 0x0b4:
                                _dma!._DMA0DAD_L = GetLowHalfword(value);
                                _dma._DMA0DAD_H = GetHighHalfword(value);
                                break;
                            case 0x0b8:
                                _dma!._DMA0CNT_L = GetLowHalfword(value);
                                _dma._DMA0CNT_H = GetHighHalfword(value);
                                break;
                            case 0x0bc:
                                _dma!._DMA1SAD_L = GetLowHalfword(value);
                                _dma._DMA1SAD_H = GetHighHalfword(value);
                                break;
                            case 0x0c0:
                                _dma!._DMA1DAD_L = GetLowHalfword(value);
                                _dma._DMA1DAD_H = GetHighHalfword(value);
                                break;
                            case 0x0c4:
                                _dma!._DMA1CNT_L = GetLowHalfword(value);
                                _dma._DMA1CNT_H = GetHighHalfword(value);
                                break;
                            case 0x0c8:
                                _dma!._DMA2SAD_L = GetLowHalfword(value);
                                _dma._DMA2SAD_H = GetHighHalfword(value);
                                break;
                            case 0x0cc:
                                _dma!._DMA2DAD_L = GetLowHalfword(value);
                                _dma._DMA2DAD_H = GetHighHalfword(value);
                                break;
                            case 0x0d0:
                                _dma!._DMA2CNT_L = GetLowHalfword(value);
                                _dma._DMA2CNT_H = GetHighHalfword(value);
                                break;
                            case 0x0d4:
                                _dma!._DMA3SAD_L = GetLowHalfword(value);
                                _dma._DMA3SAD_H = GetHighHalfword(value);
                                break;
                            case 0x0d8:
                                _dma!._DMA3DAD_L = GetLowHalfword(value);
                                _dma._DMA3DAD_H = GetHighHalfword(value);
                                break;
                            case 0x0dc:
                                _dma!._DMA3CNT_L = GetLowHalfword(value);
                                _dma._DMA3CNT_H = GetHighHalfword(value);
                                break;
                            case 0x0e0:
                            case 0x0e4:
                            case 0x0e8:
                            case 0x0ec:
                            case 0x0f0:
                            case 0x0f4:
                            case 0x0f8:
                            case 0x0fc:
                                // unused
                                break;
                            case 0x100:
                                _timer!._TM0CNT_L = GetLowHalfword(value);
                                _timer._TM0CNT_H = GetHighHalfword(value);
                                break;
                            case 0x104:
                                _timer!._TM1CNT_L = GetLowHalfword(value);
                                _timer._TM1CNT_H = GetHighHalfword(value);
                                break;
                            case 0x108:
                                _timer!._TM2CNT_L = GetLowHalfword(value);
                                _timer._TM2CNT_H = GetHighHalfword(value);
                                break;
                            case 0x10c:
                                _timer!._TM3CNT_L = GetLowHalfword(value);
                                _timer._TM3CNT_H = GetHighHalfword(value);
                                break;
                            case 0x110:
                            case 0x114:
                            case 0x118:
                            case 0x11c:
                                // unused
                                break;
                            case 0x120:
                                _communication!._SIODATA0 = GetLowHalfword(value);
                                _communication._SIODATA1 = GetHighHalfword(value);
                                break;
                            case 0x124:
                                _communication!._SIODATA2 = GetLowHalfword(value);
                                _communication._SIODATA3 = GetHighHalfword(value);
                                break;
                            case 0x128:
                                _communication!._SIOCNT = GetLowHalfword(value);
                                _communication._SIODATA_SEND = GetHighHalfword(value);
                                break;
                            case 0x12c:
                                // unused
                                break;
                            case 0x130:
                                _keyInput!._KEYINPUT = GetLowHalfword(value);
                                _keyInput._KEYCNT = GetHighHalfword(value);
                                break;
                            case 0x140:
                                _communication!._JOYCNT = GetLowHalfword(value);
                                // 16 upper bits are unused
                                break;
                            case 0x144:
                            case 0x148:
                            case 0x14c:
                                // unused
                                break;
                            case 0x150:
                                _communication!._JOY_RECV_L = GetLowHalfword(value);
                                _communication._JOY_RECV_H = GetHighHalfword(value);
                                break;
                            case 0x154:
                                _communication!._JOY_TRANS_L = GetLowHalfword(value);
                                _communication._JOY_TRANS_H = GetHighHalfword(value);
                                break;
                            case 0x158:
                                _communication!._JOYSTAT = GetLowHalfword(value);
                                // 16 upper bits are unused
                                break;
                            case 0x15c:
                                // unused
                                break;
                            case 0x200:
                                _interruptControl!._IE = GetLowHalfword(value);
                                _interruptControl._IF &= (UInt16)~GetHighHalfword(value);
                                _interruptControl.UpdateInterrupts();
                                break;
                            case 0x204:
                                _systemControl!._WAITCNT = GetLowHalfword(value);
                                // 16 upper bits are unused
                                break;
                            case 0x208:
                                _interruptControl!._IME = GetLowHalfword(value);
                                _interruptControl.UpdateInterrupts();
                                // 16 upper bits are unused
                                break;
                            case 0x20c:
                            case 0x210:
                            case 0x214:
                            case 0x218:
                            case 0x21c:
                                // unused
                                break;
                            default:
                                throw new Exception(string.Format("Iris.GBA.Memory: Unhandled write to address 0x{0:x8}", address));
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
                    throw new Exception(string.Format("Iris.GBA.Memory: Unhandled write to address 0x{0:x8}", address));
            }
        }
    }
}
