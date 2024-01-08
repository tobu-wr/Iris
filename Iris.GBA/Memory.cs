using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Iris.GBA
{
    internal sealed class Memory
    {
        private Communication _communication;
        private Timer _timer;
        private Sound _sound;
        private DMA _dma;
        private KeyInput _keyInput;
        private SystemControl _systemControl;
        private InterruptControl _interruptControl;
        private Video _video;
        private BIOS _bios;

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

        internal void Initialize(Communication communication, Timer timer, Sound sound, DMA dma, KeyInput keyInput, SystemControl systemControl, InterruptControl interruptControl, Video video, BIOS bios)
        {
            _communication = communication;
            _timer = timer;
            _sound = sound;
            _dma = dma;
            _keyInput = keyInput;
            _systemControl = systemControl;
            _interruptControl = interruptControl;
            _video = video;
            _bios = bios;

            InitPageTables();
        }

        internal void ResetState()
        {
            byte[] sramData = new byte[SRAMSize];
            byte[] ewramData = new byte[EWRAMSize];
            byte[] iwramData = new byte[IWRAMSize];

            Marshal.Copy(sramData, 0, _SRAM, SRAMSize);
            Marshal.Copy(ewramData, 0, _eWRAM, EWRAMSize);
            Marshal.Copy(iwramData, 0, _iWRAM, IWRAMSize);
        }

        internal void LoadState(BinaryReader reader)
        {
            byte[] sramData = reader.ReadBytes(SRAMSize);
            byte[] ewramData = reader.ReadBytes(EWRAMSize);
            byte[] iwramData = reader.ReadBytes(IWRAMSize);

            Marshal.Copy(sramData, 0, _SRAM, SRAMSize);
            Marshal.Copy(ewramData, 0, _eWRAM, EWRAMSize);
            Marshal.Copy(iwramData, 0, _iWRAM, IWRAMSize);
        }

        internal void SaveState(BinaryWriter writer)
        {
            byte[] sramData = new byte[SRAMSize];
            byte[] ewramData = new byte[EWRAMSize];
            byte[] iwramData = new byte[IWRAMSize];

            Marshal.Copy(_SRAM, sramData, 0, SRAMSize);
            Marshal.Copy(_eWRAM, ewramData, 0, EWRAMSize);
            Marshal.Copy(_iWRAM, iwramData, 0, IWRAMSize);

            writer.Write(sramData);
            writer.Write(ewramData);
            writer.Write(iwramData);
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
                            0x000 => GetLowByte(_video!.ReadRegister(Video.Register.DISPCNT)),
                            0x001 => GetHighByte(_video!.ReadRegister(Video.Register.DISPCNT)),

                            0x004 => GetLowByte(_video!.ReadRegister(Video.Register.DISPSTAT)),
                            0x005 => GetHighByte(_video!.ReadRegister(Video.Register.DISPSTAT)),

                            0x006 => GetLowByte(_video!.ReadRegister(Video.Register.VCOUNT)),
                            0x007 => GetHighByte(_video!.ReadRegister(Video.Register.VCOUNT)),

                            0x008 => GetLowByte(_video!.ReadRegister(Video.Register.BG0CNT)),
                            0x009 => GetHighByte(_video!.ReadRegister(Video.Register.BG0CNT)),

                            0x00a => GetLowByte(_video!.ReadRegister(Video.Register.BG1CNT)),
                            0x00b => GetHighByte(_video!.ReadRegister(Video.Register.BG1CNT)),

                            0x00c => GetLowByte(_video!.ReadRegister(Video.Register.BG2CNT)),
                            0x00d => GetHighByte(_video!.ReadRegister(Video.Register.BG2CNT)),

                            0x00e => GetLowByte(_video!.ReadRegister(Video.Register.BG3CNT)),
                            0x00f => GetHighByte(_video!.ReadRegister(Video.Register.BG3CNT)),

                            0x048 => GetLowByte(_video!.ReadRegister(Video.Register.WININ)),
                            0x049 => GetHighByte(_video!.ReadRegister(Video.Register.WININ)),

                            0x04a => GetLowByte(_video!.ReadRegister(Video.Register.WINOUT)),
                            0x04b => GetHighByte(_video!.ReadRegister(Video.Register.WINOUT)),

                            0x050 => GetLowByte(_video!.ReadRegister(Video.Register.BLDCNT)),
                            0x051 => GetHighByte(_video!.ReadRegister(Video.Register.BLDCNT)),

                            0x052 => GetLowByte(_video!.ReadRegister(Video.Register.BLDALPHA)),
                            0x053 => GetHighByte(_video!.ReadRegister(Video.Register.BLDALPHA)),

                            0x060 => GetLowByte(_sound!.ReadRegister(Sound.Register.SOUND1CNT_L)),
                            0x061 => GetHighByte(_sound!.ReadRegister(Sound.Register.SOUND1CNT_L)),

                            0x062 => GetLowByte(_sound!.ReadRegister(Sound.Register.SOUND1CNT_H)),
                            0x063 => GetHighByte(_sound!.ReadRegister(Sound.Register.SOUND1CNT_H)),

                            0x064 => GetLowByte(_sound!.ReadRegister(Sound.Register.SOUND1CNT_X)),
                            0x065 => GetHighByte(_sound!.ReadRegister(Sound.Register.SOUND1CNT_X)),

                            0x068 => GetLowByte(_sound!.ReadRegister(Sound.Register.SOUND2CNT_L)),
                            0x069 => GetHighByte(_sound!.ReadRegister(Sound.Register.SOUND2CNT_L)),

                            0x06c => GetLowByte(_sound!.ReadRegister(Sound.Register.SOUND2CNT_H)),
                            0x06d => GetHighByte(_sound!.ReadRegister(Sound.Register.SOUND2CNT_H)),

                            0x070 => GetLowByte(_sound!.ReadRegister(Sound.Register.SOUND3CNT_L)),
                            0x071 => GetHighByte(_sound!.ReadRegister(Sound.Register.SOUND3CNT_L)),

                            0x072 => GetLowByte(_sound!.ReadRegister(Sound.Register.SOUND3CNT_H)),
                            0x073 => GetHighByte(_sound!.ReadRegister(Sound.Register.SOUND3CNT_H)),

                            0x074 => GetLowByte(_sound!.ReadRegister(Sound.Register.SOUND3CNT_X)),
                            0x075 => GetHighByte(_sound!.ReadRegister(Sound.Register.SOUND3CNT_X)),

                            0x078 => GetLowByte(_sound!.ReadRegister(Sound.Register.SOUND4CNT_L)),
                            0x079 => GetHighByte(_sound!.ReadRegister(Sound.Register.SOUND4CNT_L)),

                            0x07c => GetLowByte(_sound!.ReadRegister(Sound.Register.SOUND4CNT_H)),
                            0x07d => GetHighByte(_sound!.ReadRegister(Sound.Register.SOUND4CNT_H)),

                            0x080 => GetLowByte(_sound!.ReadRegister(Sound.Register.SOUNDCNT_L)),
                            0x081 => GetHighByte(_sound!.ReadRegister(Sound.Register.SOUNDCNT_L)),

                            0x082 => GetLowByte(_sound!.ReadRegister(Sound.Register.SOUNDCNT_H)),
                            0x083 => GetHighByte(_sound!.ReadRegister(Sound.Register.SOUNDCNT_H)),

                            0x084 => GetLowByte(_sound!.ReadRegister(Sound.Register.SOUNDCNT_X)),
                            0x085 => GetHighByte(_sound!.ReadRegister(Sound.Register.SOUNDCNT_X)),

                            0x088 => GetLowByte(_sound!.ReadRegister(Sound.Register.SOUNDBIAS)),
                            0x089 => GetHighByte(_sound!.ReadRegister(Sound.Register.SOUNDBIAS)),

                            0x090 => GetLowByte(_sound!.ReadRegister(Sound.Register.WAVE_RAM0_L)),
                            0x091 => GetHighByte(_sound!.ReadRegister(Sound.Register.WAVE_RAM0_L)),

                            0x092 => GetLowByte(_sound!.ReadRegister(Sound.Register.WAVE_RAM0_H)),
                            0x093 => GetHighByte(_sound!.ReadRegister(Sound.Register.WAVE_RAM0_H)),

                            0x094 => GetLowByte(_sound!.ReadRegister(Sound.Register.WAVE_RAM1_L)),
                            0x095 => GetHighByte(_sound!.ReadRegister(Sound.Register.WAVE_RAM1_L)),

                            0x096 => GetLowByte(_sound!.ReadRegister(Sound.Register.WAVE_RAM1_H)),
                            0x097 => GetHighByte(_sound!.ReadRegister(Sound.Register.WAVE_RAM1_H)),

                            0x098 => GetLowByte(_sound!.ReadRegister(Sound.Register.WAVE_RAM2_L)),
                            0x099 => GetHighByte(_sound!.ReadRegister(Sound.Register.WAVE_RAM2_L)),

                            0x09a => GetLowByte(_sound!.ReadRegister(Sound.Register.WAVE_RAM2_H)),
                            0x09b => GetHighByte(_sound!.ReadRegister(Sound.Register.WAVE_RAM2_H)),

                            0x09c => GetLowByte(_sound!.ReadRegister(Sound.Register.WAVE_RAM3_L)),
                            0x09d => GetHighByte(_sound!.ReadRegister(Sound.Register.WAVE_RAM3_L)),

                            0x09e => GetLowByte(_sound!.ReadRegister(Sound.Register.WAVE_RAM3_H)),
                            0x09f => GetHighByte(_sound!.ReadRegister(Sound.Register.WAVE_RAM3_H)),

                            0x0ba => GetLowByte(_dma!._DMA0CNT_H),
                            0x0bb => GetHighByte(_dma!._DMA0CNT_H),

                            0x0c6 => GetLowByte(_dma!._DMA1CNT_H),
                            0x0c7 => GetHighByte(_dma!._DMA1CNT_H),

                            0x0d2 => GetLowByte(_dma!._DMA2CNT_H),
                            0x0d3 => GetHighByte(_dma!._DMA2CNT_H),

                            0x0de => GetLowByte(_dma!._DMA3CNT_H),
                            0x0df => GetHighByte(_dma!._DMA3CNT_H),

                            0x100 => GetLowByte(_timer!.ReadRegister(Timer.Register.TM0CNT_L)),
                            0x101 => GetHighByte(_timer!.ReadRegister(Timer.Register.TM0CNT_L)),

                            0x102 => GetLowByte(_timer!.ReadRegister(Timer.Register.TM0CNT_H)),
                            0x103 => GetHighByte(_timer!.ReadRegister(Timer.Register.TM0CNT_H)),

                            0x104 => GetLowByte(_timer!.ReadRegister(Timer.Register.TM1CNT_L)),
                            0x105 => GetHighByte(_timer!.ReadRegister(Timer.Register.TM1CNT_L)),

                            0x106 => GetLowByte(_timer!.ReadRegister(Timer.Register.TM1CNT_H)),
                            0x107 => GetHighByte(_timer!.ReadRegister(Timer.Register.TM1CNT_H)),

                            0x108 => GetLowByte(_timer!.ReadRegister(Timer.Register.TM2CNT_L)),
                            0x109 => GetHighByte(_timer!.ReadRegister(Timer.Register.TM2CNT_L)),

                            0x10a => GetLowByte(_timer!.ReadRegister(Timer.Register.TM2CNT_H)),
                            0x10b => GetHighByte(_timer!.ReadRegister(Timer.Register.TM2CNT_H)),

                            0x10c => GetLowByte(_timer!.ReadRegister(Timer.Register.TM3CNT_L)),
                            0x10d => GetHighByte(_timer!.ReadRegister(Timer.Register.TM3CNT_L)),

                            0x10e => GetLowByte(_timer!.ReadRegister(Timer.Register.TM3CNT_H)),
                            0x10f => GetHighByte(_timer!.ReadRegister(Timer.Register.TM3CNT_H)),

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

                            0x130 => GetLowByte(_keyInput!.ReadRegister(KeyInput.Register.KEYINPUT)),
                            0x131 => GetHighByte(_keyInput!.ReadRegister(KeyInput.Register.KEYINPUT)),

                            0x132 => GetLowByte(_keyInput!.ReadRegister(KeyInput.Register.KEYCNT)),
                            0x133 => GetHighByte(_keyInput!.ReadRegister(KeyInput.Register.KEYCNT)),

                            0x134 => GetLowByte(_communication!._RCNT),
                            0x135 => GetHighByte(_communication!._RCNT),

                            0x200 => GetLowByte(_interruptControl!._IE),
                            0x201 => GetHighByte(_interruptControl!._IE),

                            0x202 => GetLowByte(_interruptControl!._IF),
                            0x203 => GetHighByte(_interruptControl!._IF),

                            0x204 => GetLowByte(_systemControl!.ReadRegister(SystemControl.Register.WAITCNT)),
                            0x205 => GetHighByte(_systemControl!.ReadRegister(SystemControl.Register.WAITCNT)),

                            0x208 => GetLowByte(_interruptControl!._IME),
                            0x209 => GetHighByte(_interruptControl!._IME),

                            0x300 => GetLowByte(_systemControl!.ReadRegister(SystemControl.Register.UNKNOWN_0)),
                            0x301 => GetHighByte(_systemControl!.ReadRegister(SystemControl.Register.UNKNOWN_0)),

                            _ => 0,
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

            return 0;
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
                            0x000 => _video!.ReadRegister(Video.Register.DISPCNT),
                            0x004 => _video!.ReadRegister(Video.Register.DISPSTAT),
                            0x006 => _video!.ReadRegister(Video.Register.VCOUNT),
                            0x008 => _video!.ReadRegister(Video.Register.BG0CNT),
                            0x00a => _video!.ReadRegister(Video.Register.BG1CNT),
                            0x00c => _video!.ReadRegister(Video.Register.BG2CNT),
                            0x00e => _video!.ReadRegister(Video.Register.BG3CNT),
                            0x048 => _video!.ReadRegister(Video.Register.WININ),
                            0x04a => _video!.ReadRegister(Video.Register.WINOUT),
                            0x050 => _video!.ReadRegister(Video.Register.BLDCNT),
                            0x052 => _video!.ReadRegister(Video.Register.BLDALPHA),
                            0x060 => _sound!.ReadRegister(Sound.Register.SOUND1CNT_L),
                            0x062 => _sound!.ReadRegister(Sound.Register.SOUND1CNT_H),
                            0x064 => _sound!.ReadRegister(Sound.Register.SOUND1CNT_X),
                            0x068 => _sound!.ReadRegister(Sound.Register.SOUND2CNT_L),
                            0x06c => _sound!.ReadRegister(Sound.Register.SOUND2CNT_H),
                            0x070 => _sound!.ReadRegister(Sound.Register.SOUND3CNT_L),
                            0x072 => _sound!.ReadRegister(Sound.Register.SOUND3CNT_H),
                            0x074 => _sound!.ReadRegister(Sound.Register.SOUND3CNT_X),
                            0x078 => _sound!.ReadRegister(Sound.Register.SOUND4CNT_L),
                            0x07c => _sound!.ReadRegister(Sound.Register.SOUND4CNT_H),
                            0x080 => _sound!.ReadRegister(Sound.Register.SOUNDCNT_L),
                            0x082 => _sound!.ReadRegister(Sound.Register.SOUNDCNT_H),
                            0x084 => _sound!.ReadRegister(Sound.Register.SOUNDCNT_X),
                            0x088 => _sound!.ReadRegister(Sound.Register.SOUNDBIAS),
                            0x090 => _sound!.ReadRegister(Sound.Register.WAVE_RAM0_L),
                            0x092 => _sound!.ReadRegister(Sound.Register.WAVE_RAM0_H),
                            0x094 => _sound!.ReadRegister(Sound.Register.WAVE_RAM1_L),
                            0x096 => _sound!.ReadRegister(Sound.Register.WAVE_RAM1_H),
                            0x098 => _sound!.ReadRegister(Sound.Register.WAVE_RAM2_L),
                            0x09a => _sound!.ReadRegister(Sound.Register.WAVE_RAM2_H),
                            0x09c => _sound!.ReadRegister(Sound.Register.WAVE_RAM3_L),
                            0x09e => _sound!.ReadRegister(Sound.Register.WAVE_RAM3_H),
                            0x0ba => _dma!._DMA0CNT_H,
                            0x0c6 => _dma!._DMA1CNT_H,
                            0x0d2 => _dma!._DMA2CNT_H,
                            0x0de => _dma!._DMA3CNT_H,
                            0x100 => _timer!.ReadRegister(Timer.Register.TM0CNT_L),
                            0x102 => _timer!.ReadRegister(Timer.Register.TM0CNT_H),
                            0x104 => _timer!.ReadRegister(Timer.Register.TM1CNT_L),
                            0x106 => _timer!.ReadRegister(Timer.Register.TM1CNT_H),
                            0x108 => _timer!.ReadRegister(Timer.Register.TM2CNT_L),
                            0x10a => _timer!.ReadRegister(Timer.Register.TM2CNT_H),
                            0x10c => _timer!.ReadRegister(Timer.Register.TM3CNT_L),
                            0x10e => _timer!.ReadRegister(Timer.Register.TM3CNT_H),
                            0x120 => _communication!._SIODATA0,
                            0x122 => _communication!._SIODATA1,
                            0x124 => _communication!._SIODATA2,
                            0x126 => _communication!._SIODATA3,
                            0x128 => _communication!._SIOCNT,
                            0x12a => _communication!._SIODATA_SEND,
                            0x130 => _keyInput!.ReadRegister(KeyInput.Register.KEYINPUT),
                            0x132 => _keyInput!.ReadRegister(KeyInput.Register.KEYCNT),
                            0x134 => _communication!._RCNT,
                            0x200 => _interruptControl!._IE,
                            0x202 => _interruptControl!._IF,
                            0x204 => _systemControl!.ReadRegister(SystemControl.Register.WAITCNT),
                            0x208 => _interruptControl!._IME,
                            0x300 => _systemControl!.ReadRegister(SystemControl.Register.UNKNOWN_0),
                            _ => 0,
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

            return 0;
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
                            0x000 => _video!.ReadRegister(Video.Register.DISPCNT),
                            0x004 => (UInt32)((_video!.ReadRegister(Video.Register.VCOUNT) << 16) | _video.ReadRegister(Video.Register.DISPSTAT)),
                            0x008 => (UInt32)((_video!.ReadRegister(Video.Register.BG1CNT) << 16) | _video.ReadRegister(Video.Register.BG0CNT)),
                            0x00c => (UInt32)((_video!.ReadRegister(Video.Register.BG3CNT) << 16) | _video.ReadRegister(Video.Register.BG2CNT)),
                            0x0b8 => (UInt32)(_dma!._DMA0CNT_H << 16),
                            0x0c4 => (UInt32)(_dma!._DMA1CNT_H << 16),
                            0x0d0 => (UInt32)(_dma!._DMA2CNT_H << 16),
                            0x0dc => (UInt32)(_dma!._DMA3CNT_H << 16),
                            0x150 => (UInt32)((_communication._JOY_RECV_H << 16) | _communication._JOY_RECV_L),
                            0x200 => (UInt32)((_interruptControl!._IF << 16) | _interruptControl._IE),
                            0x208 => _interruptControl!._IME,
                            _ => 0,
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

            return 0;
        }

        internal enum RegisterWriteMode
        {
            LowByte,
            HighByte,
            HalfWord
        }

        internal static void WriteRegisterHelper(ref UInt16 registerValue, UInt16 value, RegisterWriteMode mode)
        {
            switch (mode)
            {
                case RegisterWriteMode.LowByte:
                    registerValue = (UInt16)((registerValue & 0xff00) | value);
                    break;
                case RegisterWriteMode.HighByte:
                    registerValue = (UInt16)((registerValue & 0x00ff) | (value << 8));
                    break;
                case RegisterWriteMode.HalfWord:
                    registerValue = value;
                    break;
            }
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
                                _video!.WriteRegister(Video.Register.DISPCNT, value, RegisterWriteMode.LowByte);
                                break;
                            case 0x001:
                                _video!.WriteRegister(Video.Register.DISPCNT, value, RegisterWriteMode.HighByte);
                                break;

                            case 0x004:
                                _video!.WriteRegister(Video.Register.DISPSTAT, value, RegisterWriteMode.LowByte);
                                break;
                            case 0x005:
                                _video!.WriteRegister(Video.Register.DISPSTAT, value, RegisterWriteMode.HighByte);
                                break;

                            case 0x008:
                                _video!.WriteRegister(Video.Register.BG0CNT, value, RegisterWriteMode.LowByte);
                                break;
                            case 0x009:
                                _video!.WriteRegister(Video.Register.BG0CNT, value, RegisterWriteMode.HighByte);
                                break;

                            case 0x00a:
                                _video!.WriteRegister(Video.Register.BG1CNT, value, RegisterWriteMode.LowByte);
                                break;
                            case 0x00b:
                                _video!.WriteRegister(Video.Register.BG1CNT, value, RegisterWriteMode.HighByte);
                                break;

                            case 0x00c:
                                _video!.WriteRegister(Video.Register.BG2CNT, value, RegisterWriteMode.LowByte);
                                break;
                            case 0x00d:
                                _video!.WriteRegister(Video.Register.BG2CNT, value, RegisterWriteMode.HighByte);
                                break;

                            case 0x00e:
                                _video!.WriteRegister(Video.Register.BG3CNT, value, RegisterWriteMode.LowByte);
                                break;
                            case 0x00f:
                                _video!.WriteRegister(Video.Register.BG3CNT, value, RegisterWriteMode.HighByte);
                                break;

                            case 0x010:
                                _video!.WriteRegister(Video.Register.BG0HOFS, value, RegisterWriteMode.LowByte);
                                break;
                            case 0x011:
                                _video!.WriteRegister(Video.Register.BG0HOFS, value, RegisterWriteMode.HighByte);
                                break;

                            case 0x012:
                                _video!.WriteRegister(Video.Register.BG0VOFS, value, RegisterWriteMode.LowByte);
                                break;
                            case 0x013:
                                _video!.WriteRegister(Video.Register.BG0VOFS, value, RegisterWriteMode.HighByte);
                                break;

                            case 0x014:
                                _video!.WriteRegister(Video.Register.BG1HOFS, value, RegisterWriteMode.LowByte);
                                break;
                            case 0x015:
                                _video!.WriteRegister(Video.Register.BG1HOFS, value, RegisterWriteMode.HighByte);
                                break;

                            case 0x016:
                                _video!.WriteRegister(Video.Register.BG1VOFS, value, RegisterWriteMode.LowByte);
                                break;
                            case 0x017:
                                _video!.WriteRegister(Video.Register.BG1VOFS, value, RegisterWriteMode.HighByte);
                                break;

                            case 0x018:
                                _video!.WriteRegister(Video.Register.BG2HOFS, value, RegisterWriteMode.LowByte);
                                break;
                            case 0x019:
                                _video!.WriteRegister(Video.Register.BG2HOFS, value, RegisterWriteMode.HighByte);
                                break;

                            case 0x01a:
                                _video!.WriteRegister(Video.Register.BG2VOFS, value, RegisterWriteMode.LowByte);
                                break;
                            case 0x01b:
                                _video!.WriteRegister(Video.Register.BG2VOFS, value, RegisterWriteMode.HighByte);
                                break;

                            case 0x01c:
                                _video!.WriteRegister(Video.Register.BG3HOFS, value, RegisterWriteMode.LowByte);
                                break;
                            case 0x01d:
                                _video!.WriteRegister(Video.Register.BG3HOFS, value, RegisterWriteMode.HighByte);
                                break;

                            case 0x01e:
                                _video!.WriteRegister(Video.Register.BG3VOFS, value, RegisterWriteMode.LowByte);
                                break;
                            case 0x01f:
                                _video!.WriteRegister(Video.Register.BG3VOFS, value, RegisterWriteMode.HighByte);
                                break;

                            case 0x040:
                                _video!.WriteRegister(Video.Register.WIN0H, value, RegisterWriteMode.LowByte);
                                break;
                            case 0x041:
                                _video!.WriteRegister(Video.Register.WIN0H, value, RegisterWriteMode.HighByte);
                                break;

                            case 0x042:
                                _video!.WriteRegister(Video.Register.WIN1H, value, RegisterWriteMode.LowByte);
                                break;
                            case 0x043:
                                _video!.WriteRegister(Video.Register.WIN1H, value, RegisterWriteMode.HighByte);
                                break;

                            case 0x044:
                                _video!.WriteRegister(Video.Register.WIN0V, value, RegisterWriteMode.LowByte);
                                break;
                            case 0x045:
                                _video!.WriteRegister(Video.Register.WIN0V, value, RegisterWriteMode.HighByte);
                                break;

                            case 0x046:
                                _video!.WriteRegister(Video.Register.WIN1V, value, RegisterWriteMode.LowByte);
                                break;
                            case 0x047:
                                _video!.WriteRegister(Video.Register.WIN1V, value, RegisterWriteMode.HighByte);
                                break;

                            case 0x048:
                                _video!.WriteRegister(Video.Register.WININ, value, RegisterWriteMode.LowByte);
                                break;
                            case 0x049:
                                _video!.WriteRegister(Video.Register.WININ, value, RegisterWriteMode.HighByte);
                                break;

                            case 0x04a:
                                _video!.WriteRegister(Video.Register.WINOUT, value, RegisterWriteMode.LowByte);
                                break;
                            case 0x04b:
                                _video!.WriteRegister(Video.Register.WINOUT, value, RegisterWriteMode.HighByte);
                                break;

                            case 0x04c:
                                _video!.WriteRegister(Video.Register.MOSAIC, value, RegisterWriteMode.LowByte);
                                break;
                            case 0x04d:
                                _video!.WriteRegister(Video.Register.MOSAIC, value, RegisterWriteMode.HighByte);
                                break;

                            case 0x050:
                                _video!.WriteRegister(Video.Register.BLDCNT, value, RegisterWriteMode.LowByte);
                                break;
                            case 0x051:
                                _video!.WriteRegister(Video.Register.BLDCNT, value, RegisterWriteMode.HighByte);
                                break;

                            case 0x052:
                                _video!.WriteRegister(Video.Register.BLDALPHA, value, RegisterWriteMode.LowByte);
                                break;
                            case 0x053:
                                _video!.WriteRegister(Video.Register.BLDALPHA, value, RegisterWriteMode.HighByte);
                                break;

                            case 0x054:
                                _video!.WriteRegister(Video.Register.BLDY, value, RegisterWriteMode.LowByte);
                                break;
                            case 0x055:
                                _video!.WriteRegister(Video.Register.BLDY, value, RegisterWriteMode.HighByte);
                                break;

                            case 0x060:
                                _sound!.WriteRegister(Sound.Register.SOUND1CNT_L, value, RegisterWriteMode.LowByte);
                                break;
                            case 0x061:
                                _sound!.WriteRegister(Sound.Register.SOUND1CNT_L, value, RegisterWriteMode.HighByte);
                                break;

                            case 0x062:
                                _sound!.WriteRegister(Sound.Register.SOUND1CNT_H, value, RegisterWriteMode.LowByte);
                                break;
                            case 0x063:
                                _sound!.WriteRegister(Sound.Register.SOUND1CNT_H, value, RegisterWriteMode.HighByte);
                                break;

                            case 0x064:
                                _sound!.WriteRegister(Sound.Register.SOUND1CNT_X, value, RegisterWriteMode.LowByte);
                                break;
                            case 0x065:
                                _sound!.WriteRegister(Sound.Register.SOUND1CNT_X, value, RegisterWriteMode.HighByte);
                                break;

                            case 0x068:
                                _sound!.WriteRegister(Sound.Register.SOUND2CNT_L, value, RegisterWriteMode.LowByte);
                                break;
                            case 0x069:
                                _sound!.WriteRegister(Sound.Register.SOUND2CNT_L, value, RegisterWriteMode.HighByte);
                                break;

                            case 0x06c:
                                _sound!.WriteRegister(Sound.Register.SOUND2CNT_H, value, RegisterWriteMode.LowByte);
                                break;
                            case 0x06d:
                                _sound!.WriteRegister(Sound.Register.SOUND2CNT_H, value, RegisterWriteMode.HighByte);
                                break;

                            case 0x070:
                                _sound!.WriteRegister(Sound.Register.SOUND3CNT_L, value, RegisterWriteMode.LowByte);
                                break;
                            case 0x071:
                                _sound!.WriteRegister(Sound.Register.SOUND3CNT_L, value, RegisterWriteMode.HighByte);
                                break;

                            case 0x072:
                                _sound!.WriteRegister(Sound.Register.SOUND3CNT_H, value, RegisterWriteMode.LowByte);
                                break;
                            case 0x073:
                                _sound!.WriteRegister(Sound.Register.SOUND3CNT_H, value, RegisterWriteMode.HighByte);
                                break;

                            case 0x074:
                                _sound!.WriteRegister(Sound.Register.SOUND3CNT_X, value, RegisterWriteMode.LowByte);
                                break;
                            case 0x075:
                                _sound!.WriteRegister(Sound.Register.SOUND3CNT_X, value, RegisterWriteMode.HighByte);
                                break;

                            case 0x078:
                                _sound!.WriteRegister(Sound.Register.SOUND4CNT_L, value, RegisterWriteMode.LowByte);
                                break;
                            case 0x079:
                                _sound!.WriteRegister(Sound.Register.SOUND4CNT_L, value, RegisterWriteMode.HighByte);
                                break;

                            case 0x07c:
                                _sound!.WriteRegister(Sound.Register.SOUND4CNT_H, value, RegisterWriteMode.LowByte);
                                break;
                            case 0x07d:
                                _sound!.WriteRegister(Sound.Register.SOUND4CNT_H, value, RegisterWriteMode.HighByte);
                                break;

                            case 0x080:
                                _sound!.WriteRegister(Sound.Register.SOUNDCNT_L, value, RegisterWriteMode.LowByte);
                                break;
                            case 0x081:
                                _sound!.WriteRegister(Sound.Register.SOUNDCNT_L, value, RegisterWriteMode.HighByte);
                                break;

                            case 0x082:
                                _sound!.WriteRegister(Sound.Register.SOUNDCNT_H, value, RegisterWriteMode.LowByte);
                                break;
                            case 0x083:
                                _sound!.WriteRegister(Sound.Register.SOUNDCNT_H, value, RegisterWriteMode.HighByte);
                                break;

                            case 0x084:
                                _sound!.WriteRegister(Sound.Register.SOUNDCNT_X, value, RegisterWriteMode.LowByte);
                                break;
                            case 0x085:
                                _sound!.WriteRegister(Sound.Register.SOUNDCNT_X, value, RegisterWriteMode.HighByte);
                                break;

                            case 0x088:
                                _sound!.WriteRegister(Sound.Register.SOUNDBIAS, value, RegisterWriteMode.LowByte);
                                break;
                            case 0x089:
                                _sound!.WriteRegister(Sound.Register.SOUNDBIAS, value, RegisterWriteMode.HighByte);
                                break;

                            case 0x090:
                                _sound!.WriteRegister(Sound.Register.WAVE_RAM0_L, value, RegisterWriteMode.LowByte);
                                break;
                            case 0x091:
                                _sound!.WriteRegister(Sound.Register.WAVE_RAM0_L, value, RegisterWriteMode.HighByte);
                                break;

                            case 0x092:
                                _sound!.WriteRegister(Sound.Register.WAVE_RAM0_H, value, RegisterWriteMode.LowByte);
                                break;
                            case 0x093:
                                _sound!.WriteRegister(Sound.Register.WAVE_RAM0_H, value, RegisterWriteMode.HighByte);
                                break;

                            case 0x094:
                                _sound!.WriteRegister(Sound.Register.WAVE_RAM1_L, value, RegisterWriteMode.LowByte);
                                break;
                            case 0x095:
                                _sound!.WriteRegister(Sound.Register.WAVE_RAM1_L, value, RegisterWriteMode.HighByte);
                                break;

                            case 0x096:
                                _sound!.WriteRegister(Sound.Register.WAVE_RAM1_H, value, RegisterWriteMode.LowByte);
                                break;
                            case 0x097:
                                _sound!.WriteRegister(Sound.Register.WAVE_RAM1_H, value, RegisterWriteMode.HighByte);
                                break;

                            case 0x098:
                                _sound!.WriteRegister(Sound.Register.WAVE_RAM2_L, value, RegisterWriteMode.LowByte);
                                break;
                            case 0x099:
                                _sound!.WriteRegister(Sound.Register.WAVE_RAM2_L, value, RegisterWriteMode.HighByte);
                                break;

                            case 0x09a:
                                _sound!.WriteRegister(Sound.Register.WAVE_RAM2_H, value, RegisterWriteMode.LowByte);
                                break;
                            case 0x09b:
                                _sound!.WriteRegister(Sound.Register.WAVE_RAM2_H, value, RegisterWriteMode.HighByte);
                                break;

                            case 0x09c:
                                _sound!.WriteRegister(Sound.Register.WAVE_RAM3_L, value, RegisterWriteMode.LowByte);
                                break;
                            case 0x09d:
                                _sound!.WriteRegister(Sound.Register.WAVE_RAM3_L, value, RegisterWriteMode.HighByte);
                                break;

                            case 0x09e:
                                _sound!.WriteRegister(Sound.Register.WAVE_RAM3_H, value, RegisterWriteMode.LowByte);
                                break;
                            case 0x09f:
                                _sound!.WriteRegister(Sound.Register.WAVE_RAM3_H, value, RegisterWriteMode.HighByte);
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
                                _dma.UpdateChannel0();
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
                                _dma.UpdateChannel1();
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
                                _dma.UpdateChannel2();
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
                                _dma.UpdateChannel3();
                                break;

                            case 0x100:
                                _timer!.WriteRegister(Timer.Register.TM0CNT_L, value, RegisterWriteMode.LowByte);
                                break;
                            case 0x101:
                                _timer!.WriteRegister(Timer.Register.TM0CNT_L, value, RegisterWriteMode.HighByte);
                                break;

                            case 0x102:
                                _timer!.WriteRegister(Timer.Register.TM0CNT_H, value, RegisterWriteMode.LowByte);
                                break;
                            case 0x103:
                                _timer!.WriteRegister(Timer.Register.TM0CNT_H, value, RegisterWriteMode.HighByte);
                                break;

                            case 0x104:
                                _timer!.WriteRegister(Timer.Register.TM1CNT_L, value, RegisterWriteMode.LowByte);
                                break;
                            case 0x105:
                                _timer!.WriteRegister(Timer.Register.TM1CNT_L, value, RegisterWriteMode.HighByte);
                                break;

                            case 0x106:
                                _timer!.WriteRegister(Timer.Register.TM1CNT_H, value, RegisterWriteMode.LowByte);
                                break;
                            case 0x107:
                                _timer!.WriteRegister(Timer.Register.TM1CNT_H, value, RegisterWriteMode.HighByte);
                                break;

                            case 0x108:
                                _timer!.WriteRegister(Timer.Register.TM2CNT_L, value, RegisterWriteMode.LowByte);
                                break;
                            case 0x109:
                                _timer!.WriteRegister(Timer.Register.TM2CNT_L, value, RegisterWriteMode.HighByte);
                                break;

                            case 0x10a:
                                _timer!.WriteRegister(Timer.Register.TM2CNT_H, value, RegisterWriteMode.LowByte);
                                break;
                            case 0x10b:
                                _timer!.WriteRegister(Timer.Register.TM2CNT_H, value, RegisterWriteMode.HighByte);
                                break;

                            case 0x10c:
                                _timer!.WriteRegister(Timer.Register.TM3CNT_L, value, RegisterWriteMode.LowByte);
                                break;
                            case 0x10d:
                                _timer!.WriteRegister(Timer.Register.TM3CNT_L, value, RegisterWriteMode.HighByte);
                                break;

                            case 0x10e:
                                _timer!.WriteRegister(Timer.Register.TM3CNT_H, value, RegisterWriteMode.LowByte);
                                break;
                            case 0x10f:
                                _timer!.WriteRegister(Timer.Register.TM3CNT_H, value, RegisterWriteMode.HighByte);
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
                            case 0x131:
                                // KEYINPUT (read-only)
                                break;

                            case 0x132:
                                _keyInput!.WriteRegister(KeyInput.Register.KEYCNT, value, RegisterWriteMode.LowByte);
                                break;
                            case 0x133:
                                _keyInput!.WriteRegister(KeyInput.Register.KEYCNT, value, RegisterWriteMode.HighByte);
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
                                _interruptControl.CheckForInterrupts();
                                break;
                            case 0x201:
                                SetHighByte(ref _interruptControl!._IE, value);
                                _interruptControl.CheckForInterrupts();
                                break;

                            case 0x202:
                                _interruptControl!._IF &= (UInt16)~value;
                                _interruptControl.CheckForInterrupts();
                                break;
                            case 0x203:
                                _interruptControl!._IF &= (UInt16)~(value << 8);
                                _interruptControl.CheckForInterrupts();
                                break;

                            case 0x204:
                                _systemControl!.WriteRegister(SystemControl.Register.WAITCNT, value, RegisterWriteMode.LowByte);
                                break;
                            case 0x205:
                                _systemControl!.WriteRegister(SystemControl.Register.WAITCNT, value, RegisterWriteMode.HighByte);
                                break;

                            case 0x208:
                                SetLowByte(ref _interruptControl!._IME, value);
                                _interruptControl.CheckForInterrupts();
                                break;
                            case 0x209:
                                SetHighByte(ref _interruptControl!._IME, value);
                                _interruptControl.CheckForInterrupts();
                                break;

                            case 0x300:
                                _systemControl!.WriteRegister(SystemControl.Register.UNKNOWN_0, value, RegisterWriteMode.LowByte);
                                break;
                            case 0x301:
                                _systemControl!.WriteRegister(SystemControl.Register.UNKNOWN_0, value, RegisterWriteMode.HighByte);
                                break;

                            case 0x410:
                                // undocumented
                                break;

                            default:
                                Console.WriteLine("[Iris.GBA.Memory] Unhandled write to address 0x{0:x8}", address);
                                break;
                        }
                    }
                    break;

                // Palette RAM
                case 0x5:
                    _video!.Write8_PaletteRAM(address, value);
                    break;

                // VRAM
                case 0x6:
                    _video!.Write8_VRAM(address, value);
                    break;

                // OAM
                case 0x7:
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
                    Console.WriteLine("[Iris.GBA.Memory] Unhandled write to address 0x{0:x8}", address);
                    break;
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
                                _video!.WriteRegister(Video.Register.DISPCNT, value, RegisterWriteMode.HalfWord);
                                break;
                            case 0x004:
                                _video!.WriteRegister(Video.Register.DISPSTAT, value, RegisterWriteMode.HalfWord);
                                break;
                            case 0x008:
                                _video!.WriteRegister(Video.Register.BG0CNT, value, RegisterWriteMode.HalfWord);
                                break;
                            case 0x00a:
                                _video!.WriteRegister(Video.Register.BG1CNT, value, RegisterWriteMode.HalfWord);
                                break;
                            case 0x00c:
                                _video!.WriteRegister(Video.Register.BG2CNT, value, RegisterWriteMode.HalfWord);
                                break;
                            case 0x00e:
                                _video!.WriteRegister(Video.Register.BG3CNT, value, RegisterWriteMode.HalfWord);
                                break;
                            case 0x010:
                                _video!.WriteRegister(Video.Register.BG0HOFS, value, RegisterWriteMode.HalfWord);
                                break;
                            case 0x012:
                                _video!.WriteRegister(Video.Register.BG0VOFS, value, RegisterWriteMode.HalfWord);
                                break;
                            case 0x014:
                                _video!.WriteRegister(Video.Register.BG1HOFS, value, RegisterWriteMode.HalfWord);
                                break;
                            case 0x016:
                                _video!.WriteRegister(Video.Register.BG1VOFS, value, RegisterWriteMode.HalfWord);
                                break;
                            case 0x018:
                                _video!.WriteRegister(Video.Register.BG2HOFS, value, RegisterWriteMode.HalfWord);
                                break;
                            case 0x01a:
                                _video!.WriteRegister(Video.Register.BG2VOFS, value, RegisterWriteMode.HalfWord);
                                break;
                            case 0x01c:
                                _video!.WriteRegister(Video.Register.BG3HOFS, value, RegisterWriteMode.HalfWord);
                                break;
                            case 0x01e:
                                _video!.WriteRegister(Video.Register.BG3VOFS, value, RegisterWriteMode.HalfWord);
                                break;
                            case 0x020:
                                _video!.WriteRegister(Video.Register.BG2PA, value, RegisterWriteMode.HalfWord);
                                break;
                            case 0x022:
                                _video!.WriteRegister(Video.Register.BG2PB, value, RegisterWriteMode.HalfWord);
                                break;
                            case 0x024:
                                _video!.WriteRegister(Video.Register.BG2PC, value, RegisterWriteMode.HalfWord);
                                break;
                            case 0x026:
                                _video!.WriteRegister(Video.Register.BG2PD, value, RegisterWriteMode.HalfWord);
                                break;
                            case 0x028:
                                _video!.WriteRegister(Video.Register.BG2X_L, value, RegisterWriteMode.HalfWord);
                                break;
                            case 0x02a:
                                _video!.WriteRegister(Video.Register.BG2X_H, value, RegisterWriteMode.HalfWord);
                                break;
                            case 0x02c:
                                _video!.WriteRegister(Video.Register.BG2Y_L, value, RegisterWriteMode.HalfWord);
                                break;
                            case 0x02e:
                                _video!.WriteRegister(Video.Register.BG2Y_H, value, RegisterWriteMode.HalfWord);
                                break;
                            case 0x030:
                                _video!.WriteRegister(Video.Register.BG3PA, value, RegisterWriteMode.HalfWord);
                                break;
                            case 0x032:
                                _video!.WriteRegister(Video.Register.BG3PB, value, RegisterWriteMode.HalfWord);
                                break;
                            case 0x034:
                                _video!.WriteRegister(Video.Register.BG3PC, value, RegisterWriteMode.HalfWord);
                                break;
                            case 0x036:
                                _video!.WriteRegister(Video.Register.BG3PD, value, RegisterWriteMode.HalfWord);
                                break;
                            case 0x038:
                                _video!.WriteRegister(Video.Register.BG3X_L, value, RegisterWriteMode.HalfWord);
                                break;
                            case 0x03a:
                                _video!.WriteRegister(Video.Register.BG3X_H, value, RegisterWriteMode.HalfWord);
                                break;
                            case 0x03c:
                                _video!.WriteRegister(Video.Register.BG3Y_L, value, RegisterWriteMode.HalfWord);
                                break;
                            case 0x03e:
                                _video!.WriteRegister(Video.Register.BG3Y_H, value, RegisterWriteMode.HalfWord);
                                break;
                            case 0x040:
                                _video!.WriteRegister(Video.Register.WIN0H, value, RegisterWriteMode.HalfWord);
                                break;
                            case 0x042:
                                _video!.WriteRegister(Video.Register.WIN1H, value, RegisterWriteMode.HalfWord);
                                break;
                            case 0x044:
                                _video!.WriteRegister(Video.Register.WIN0V, value, RegisterWriteMode.HalfWord);
                                break;
                            case 0x046:
                                _video!.WriteRegister(Video.Register.WIN1V, value, RegisterWriteMode.HalfWord);
                                break;
                            case 0x048:
                                _video!.WriteRegister(Video.Register.WININ, value, RegisterWriteMode.HalfWord);
                                break;
                            case 0x04a:
                                _video!.WriteRegister(Video.Register.WINOUT, value, RegisterWriteMode.HalfWord);
                                break;
                            case 0x04c:
                                _video!.WriteRegister(Video.Register.MOSAIC, value, RegisterWriteMode.HalfWord);
                                break;
                            case 0x050:
                                _video!.WriteRegister(Video.Register.BLDCNT, value, RegisterWriteMode.HalfWord);
                                break;
                            case 0x052:
                                _video!.WriteRegister(Video.Register.BLDALPHA, value, RegisterWriteMode.HalfWord);
                                break;
                            case 0x054:
                                _video!.WriteRegister(Video.Register.BLDY, value, RegisterWriteMode.HalfWord);
                                break;
                            case 0x060:
                                _sound!.WriteRegister(Sound.Register.SOUND1CNT_L, value, RegisterWriteMode.HalfWord);
                                break;
                            case 0x062:
                                _sound!.WriteRegister(Sound.Register.SOUND1CNT_H, value, RegisterWriteMode.HalfWord);
                                break;
                            case 0x064:
                                _sound!.WriteRegister(Sound.Register.SOUND1CNT_X, value, RegisterWriteMode.HalfWord);
                                break;
                            case 0x068:
                                _sound!.WriteRegister(Sound.Register.SOUND2CNT_L, value, RegisterWriteMode.HalfWord);
                                break;
                            case 0x06c:
                                _sound!.WriteRegister(Sound.Register.SOUND2CNT_H, value, RegisterWriteMode.HalfWord);
                                break;
                            case 0x070:
                                _sound!.WriteRegister(Sound.Register.SOUND3CNT_L, value, RegisterWriteMode.HalfWord);
                                break;
                            case 0x072:
                                _sound!.WriteRegister(Sound.Register.SOUND3CNT_H, value, RegisterWriteMode.HalfWord);
                                break;
                            case 0x074:
                                _sound!.WriteRegister(Sound.Register.SOUND3CNT_X, value, RegisterWriteMode.HalfWord);
                                break;
                            case 0x078:
                                _sound!.WriteRegister(Sound.Register.SOUND4CNT_L, value, RegisterWriteMode.HalfWord);
                                break;
                            case 0x07c:
                                _sound!.WriteRegister(Sound.Register.SOUND4CNT_H, value, RegisterWriteMode.HalfWord);
                                break;
                            case 0x080:
                                _sound!.WriteRegister(Sound.Register.SOUNDCNT_L, value, RegisterWriteMode.HalfWord);
                                break;
                            case 0x082:
                                _sound!.WriteRegister(Sound.Register.SOUNDCNT_H, value, RegisterWriteMode.HalfWord);
                                break;
                            case 0x084:
                                _sound!.WriteRegister(Sound.Register.SOUNDCNT_X, value, RegisterWriteMode.HalfWord);
                                break;
                            case 0x088:
                                _sound!.WriteRegister(Sound.Register.SOUNDBIAS, value, RegisterWriteMode.HalfWord);
                                break;
                            case 0x090:
                                _sound!.WriteRegister(Sound.Register.WAVE_RAM0_L, value, RegisterWriteMode.HalfWord);
                                break;
                            case 0x092:
                                _sound!.WriteRegister(Sound.Register.WAVE_RAM0_H, value, RegisterWriteMode.HalfWord);
                                break;
                            case 0x094:
                                _sound!.WriteRegister(Sound.Register.WAVE_RAM1_L, value, RegisterWriteMode.HalfWord);
                                break;
                            case 0x096:
                                _sound!.WriteRegister(Sound.Register.WAVE_RAM1_H, value, RegisterWriteMode.HalfWord);
                                break;
                            case 0x098:
                                _sound!.WriteRegister(Sound.Register.WAVE_RAM2_L, value, RegisterWriteMode.HalfWord);
                                break;
                            case 0x09a:
                                _sound!.WriteRegister(Sound.Register.WAVE_RAM2_H, value, RegisterWriteMode.HalfWord);
                                break;
                            case 0x09c:
                                _sound!.WriteRegister(Sound.Register.WAVE_RAM3_L, value, RegisterWriteMode.HalfWord);
                                break;
                            case 0x09e:
                                _sound!.WriteRegister(Sound.Register.WAVE_RAM3_H, value, RegisterWriteMode.HalfWord);
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
                                _dma.UpdateChannel0();
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
                                _dma.UpdateChannel1();
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
                                _dma.UpdateChannel2();
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
                                _dma.UpdateChannel3();
                                break;
                            case 0x100:
                                _timer!.WriteRegister(Timer.Register.TM0CNT_L, value, RegisterWriteMode.HalfWord);
                                break;
                            case 0x102:
                                _timer!.WriteRegister(Timer.Register.TM0CNT_H, value, RegisterWriteMode.HalfWord);
                                break;
                            case 0x104:
                                _timer!.WriteRegister(Timer.Register.TM1CNT_L, value, RegisterWriteMode.HalfWord);
                                break;
                            case 0x106:
                                _timer!.WriteRegister(Timer.Register.TM1CNT_H, value, RegisterWriteMode.HalfWord);
                                break;
                            case 0x108:
                                _timer!.WriteRegister(Timer.Register.TM2CNT_L, value, RegisterWriteMode.HalfWord);
                                break;
                            case 0x10a:
                                _timer!.WriteRegister(Timer.Register.TM2CNT_H, value, RegisterWriteMode.HalfWord);
                                break;
                            case 0x10c:
                                _timer!.WriteRegister(Timer.Register.TM3CNT_L, value, RegisterWriteMode.HalfWord);
                                break;
                            case 0x10e:
                                _timer!.WriteRegister(Timer.Register.TM3CNT_H, value, RegisterWriteMode.HalfWord);
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
                                // KEYINPUT (read-only)
                                break;
                            case 0x132:
                                _keyInput!.WriteRegister(KeyInput.Register.KEYCNT, value, RegisterWriteMode.HalfWord);
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
                                _interruptControl.CheckForInterrupts();
                                break;
                            case 0x202:
                                _interruptControl!._IF &= (UInt16)~value;
                                _interruptControl.CheckForInterrupts();
                                break;
                            case 0x204:
                                _systemControl!.WriteRegister(SystemControl.Register.WAITCNT, value, RegisterWriteMode.HalfWord);
                                break;
                            case 0x208:
                                _interruptControl!._IME = value;
                                _interruptControl.CheckForInterrupts();
                                break;
                            case 0x300:
                                _systemControl!.WriteRegister(SystemControl.Register.UNKNOWN_0, value, RegisterWriteMode.HalfWord);
                                break;
                            default:
                                Console.WriteLine("[Iris.GBA.Memory] Unhandled write to address 0x{0:x8}", address);
                                break;
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
                    Console.WriteLine("[Iris.GBA.Memory] Unhandled write to address 0x{0:x8}", address);
                    break;
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
                                _video!.WriteRegister(Video.Register.DISPCNT, GetLowHalfword(value), RegisterWriteMode.HalfWord);
                                // 16 upper bits are undocumented (green swap register)
                                break;
                            case 0x004:
                                _video!.WriteRegister(Video.Register.DISPSTAT, GetLowHalfword(value), RegisterWriteMode.HalfWord);
                                break;
                            case 0x008:
                                _video!.WriteRegister(Video.Register.BG0CNT, GetLowHalfword(value), RegisterWriteMode.HalfWord);
                                _video!.WriteRegister(Video.Register.BG1CNT, GetHighHalfword(value), RegisterWriteMode.HalfWord);
                                break;
                            case 0x00c:
                                _video!.WriteRegister(Video.Register.BG2CNT, GetLowHalfword(value), RegisterWriteMode.HalfWord);
                                _video!.WriteRegister(Video.Register.BG3CNT, GetHighHalfword(value), RegisterWriteMode.HalfWord);
                                break;
                            case 0x010:
                                _video!.WriteRegister(Video.Register.BG0HOFS, GetLowHalfword(value), RegisterWriteMode.HalfWord);
                                _video!.WriteRegister(Video.Register.BG0VOFS, GetHighHalfword(value), RegisterWriteMode.HalfWord);
                                break;
                            case 0x014:
                                _video!.WriteRegister(Video.Register.BG1HOFS, GetLowHalfword(value), RegisterWriteMode.HalfWord);
                                _video!.WriteRegister(Video.Register.BG1VOFS, GetHighHalfword(value), RegisterWriteMode.HalfWord);
                                break;
                            case 0x018:
                                _video!.WriteRegister(Video.Register.BG2HOFS, GetLowHalfword(value), RegisterWriteMode.HalfWord);
                                _video!.WriteRegister(Video.Register.BG2VOFS, GetHighHalfword(value), RegisterWriteMode.HalfWord);
                                break;
                            case 0x01c:
                                _video!.WriteRegister(Video.Register.BG3HOFS, GetLowHalfword(value), RegisterWriteMode.HalfWord);
                                _video!.WriteRegister(Video.Register.BG3VOFS, GetHighHalfword(value), RegisterWriteMode.HalfWord);
                                break;
                            case 0x020:
                                _video!.WriteRegister(Video.Register.BG2PA, GetLowHalfword(value), RegisterWriteMode.HalfWord);
                                _video!.WriteRegister(Video.Register.BG2PB, GetHighHalfword(value), RegisterWriteMode.HalfWord);
                                break;
                            case 0x024:
                                _video!.WriteRegister(Video.Register.BG2PC, GetLowHalfword(value), RegisterWriteMode.HalfWord);
                                _video!.WriteRegister(Video.Register.BG2PD, GetHighHalfword(value), RegisterWriteMode.HalfWord);
                                break;
                            case 0x028:
                                _video!.WriteRegister(Video.Register.BG2X_L, GetLowHalfword(value), RegisterWriteMode.HalfWord);
                                _video!.WriteRegister(Video.Register.BG2X_H, GetHighHalfword(value), RegisterWriteMode.HalfWord);
                                break;
                            case 0x02c:
                                _video!.WriteRegister(Video.Register.BG2Y_L, GetLowHalfword(value), RegisterWriteMode.HalfWord);
                                _video!.WriteRegister(Video.Register.BG2Y_H, GetHighHalfword(value), RegisterWriteMode.HalfWord);
                                break;
                            case 0x030:
                                _video!.WriteRegister(Video.Register.BG3PA, GetLowHalfword(value), RegisterWriteMode.HalfWord);
                                _video!.WriteRegister(Video.Register.BG3PB, GetHighHalfword(value), RegisterWriteMode.HalfWord);
                                break;
                            case 0x034:
                                _video!.WriteRegister(Video.Register.BG3PC, GetLowHalfword(value), RegisterWriteMode.HalfWord);
                                _video!.WriteRegister(Video.Register.BG3PD, GetHighHalfword(value), RegisterWriteMode.HalfWord);
                                break;
                            case 0x038:
                                _video!.WriteRegister(Video.Register.BG3X_L, GetLowHalfword(value), RegisterWriteMode.HalfWord);
                                _video!.WriteRegister(Video.Register.BG3X_H, GetHighHalfword(value), RegisterWriteMode.HalfWord);
                                break;
                            case 0x03c:
                                _video!.WriteRegister(Video.Register.BG3Y_L, GetLowHalfword(value), RegisterWriteMode.HalfWord);
                                _video!.WriteRegister(Video.Register.BG3Y_H, GetHighHalfword(value), RegisterWriteMode.HalfWord);
                                break;
                            case 0x040:
                                _video!.WriteRegister(Video.Register.WIN0H, GetLowHalfword(value), RegisterWriteMode.HalfWord);
                                _video!.WriteRegister(Video.Register.WIN1H, GetHighHalfword(value), RegisterWriteMode.HalfWord);
                                break;
                            case 0x044:
                                _video!.WriteRegister(Video.Register.WIN0V, GetLowHalfword(value), RegisterWriteMode.HalfWord);
                                _video!.WriteRegister(Video.Register.WIN1V, GetHighHalfword(value), RegisterWriteMode.HalfWord);
                                break;
                            case 0x048:
                                _video!.WriteRegister(Video.Register.WININ, GetLowHalfword(value), RegisterWriteMode.HalfWord);
                                _video!.WriteRegister(Video.Register.WINOUT, GetHighHalfword(value), RegisterWriteMode.HalfWord);
                                break;
                            case 0x04c:
                                _video!.WriteRegister(Video.Register.MOSAIC, GetLowHalfword(value), RegisterWriteMode.HalfWord);
                                // 16 upper bits are unused
                                break;
                            case 0x050:
                                _video!.WriteRegister(Video.Register.BLDCNT, GetLowHalfword(value), RegisterWriteMode.HalfWord);
                                _video!.WriteRegister(Video.Register.BLDALPHA, GetHighHalfword(value), RegisterWriteMode.HalfWord);
                                break;
                            case 0x054:
                                _video!.WriteRegister(Video.Register.BLDY, GetLowHalfword(value), RegisterWriteMode.HalfWord);
                                // 16 upper bits are unused
                                break;
                            case 0x58:
                            case 0x5c:
                                // unused
                                break;
                            case 0x080:
                                _sound!.WriteRegister(Sound.Register.SOUNDCNT_L, GetLowHalfword(value), RegisterWriteMode.HalfWord);
                                _sound.WriteRegister(Sound.Register.SOUNDCNT_H, GetHighHalfword(value), RegisterWriteMode.HalfWord);
                                break;
                            case 0x090:
                                _sound!.WriteRegister(Sound.Register.WAVE_RAM0_L, GetLowHalfword(value), RegisterWriteMode.HalfWord);
                                _sound.WriteRegister(Sound.Register.WAVE_RAM0_H, GetHighHalfword(value), RegisterWriteMode.HalfWord);
                                break;
                            case 0x094:
                                _sound!.WriteRegister(Sound.Register.WAVE_RAM1_L, GetLowHalfword(value), RegisterWriteMode.HalfWord);
                                _sound.WriteRegister(Sound.Register.WAVE_RAM1_H, GetHighHalfword(value), RegisterWriteMode.HalfWord);
                                break;
                            case 0x098:
                                _sound!.WriteRegister(Sound.Register.WAVE_RAM2_L, GetLowHalfword(value), RegisterWriteMode.HalfWord);
                                _sound.WriteRegister(Sound.Register.WAVE_RAM2_H, GetHighHalfword(value), RegisterWriteMode.HalfWord);
                                break;
                            case 0x09c:
                                _sound!.WriteRegister(Sound.Register.WAVE_RAM3_L, GetLowHalfword(value), RegisterWriteMode.HalfWord);
                                _sound.WriteRegister(Sound.Register.WAVE_RAM3_H, GetHighHalfword(value), RegisterWriteMode.HalfWord);
                                break;
                            case 0x0a0:
                                _sound!.WriteRegister(Sound.Register.FIFO_A_L, GetLowHalfword(value), RegisterWriteMode.HalfWord);
                                _sound.WriteRegister(Sound.Register.FIFO_A_H, GetHighHalfword(value), RegisterWriteMode.HalfWord);
                                break;
                            case 0x0a4:
                                _sound!.WriteRegister(Sound.Register.FIFO_B_L, GetLowHalfword(value), RegisterWriteMode.HalfWord);
                                _sound.WriteRegister(Sound.Register.FIFO_B_H, GetHighHalfword(value), RegisterWriteMode.HalfWord);
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
                                _dma.UpdateChannel0();
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
                                _dma.UpdateChannel1();
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
                                _dma.UpdateChannel2();
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
                                _dma.UpdateChannel3();
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
                                _timer!.WriteRegister(Timer.Register.TM0CNT_L, GetLowHalfword(value), RegisterWriteMode.HalfWord);
                                _timer!.WriteRegister(Timer.Register.TM0CNT_H, GetHighHalfword(value), RegisterWriteMode.HalfWord);
                                break;
                            case 0x104:
                                _timer!.WriteRegister(Timer.Register.TM1CNT_L, GetLowHalfword(value), RegisterWriteMode.HalfWord);
                                _timer!.WriteRegister(Timer.Register.TM1CNT_H, GetHighHalfword(value), RegisterWriteMode.HalfWord);
                                break;
                            case 0x108:
                                _timer!.WriteRegister(Timer.Register.TM2CNT_L, GetLowHalfword(value), RegisterWriteMode.HalfWord);
                                _timer!.WriteRegister(Timer.Register.TM2CNT_H, GetHighHalfword(value), RegisterWriteMode.HalfWord);
                                break;
                            case 0x10c:
                                _timer!.WriteRegister(Timer.Register.TM3CNT_L, GetLowHalfword(value), RegisterWriteMode.HalfWord);
                                _timer!.WriteRegister(Timer.Register.TM3CNT_H, GetHighHalfword(value), RegisterWriteMode.HalfWord);
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
                                // 16 lower bits are read-only (KEYINPUT)
                                _keyInput!.WriteRegister(KeyInput.Register.KEYCNT, GetHighHalfword(value), RegisterWriteMode.HalfWord);
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
                                _interruptControl.CheckForInterrupts();
                                break;
                            case 0x204:
                                _systemControl!.WriteRegister(SystemControl.Register.WAITCNT, GetLowHalfword(value), RegisterWriteMode.HalfWord);
                                // 16 upper bits are unused
                                break;
                            case 0x208:
                                _interruptControl!._IME = GetLowHalfword(value);
                                _interruptControl.CheckForInterrupts();
                                // 16 upper bits are unused
                                break;
                            case 0x20c:
                            case 0x210:
                            case 0x214:
                            case 0x218:
                            case 0x21c:
                                // unused
                                break;
                            case 0x300:
                                _systemControl!.WriteRegister(SystemControl.Register.UNKNOWN_0, GetLowHalfword(value), RegisterWriteMode.HalfWord);
                                // 16 upper bits are unused
                                break;
                            case 0x800:
                                // 16 lower bits are undocumented (internal memory control)
                                // 16 upper bits are unused
                                break;
                            default:
                                Console.WriteLine("[Iris.GBA.Memory] Unhandled write to address 0x{0:x8}", address);
                                break;
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
                    Console.WriteLine("[Iris.GBA.Memory] Unhandled write to address 0x{0:x8}", address);
                    break;
            }
        }
    }
}
