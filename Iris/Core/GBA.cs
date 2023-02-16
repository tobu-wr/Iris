namespace Iris.Core
{
    internal sealed class GBA
    {
        internal enum Keys
        {
            A = 0,
            B = 1,
            Select = 2,
            Start = 3,
            Right = 4,
            Left = 5,
            Up = 6,
            Down = 7,
            R = 8,
            L = 9,
        };

        private const int KB = 1024;

        private byte[]? _rom;
        private readonly byte[] _sram = new byte[64 * KB];
        private readonly byte[] _externalWRAM = new byte[256 * KB];
        private readonly byte[] _internalWRAM = new byte[32 * KB];

        private readonly CPU _cpu;
        private readonly PPU _ppu;

        private ushort _SOUNDCNT_H = 0;
        private ushort _SOUNDCNT_X = 0;
        private ushort _SOUNDBIAS = 0;
        private ushort _DMA0CNT_H = 0;
        private ushort _DMA1SAD_L = 0;
        private ushort _DMA1SAD_H = 0;
        private ushort _DMA1DAD_L = 0;
        private ushort _DMA1DAD_H = 0;
        private ushort _DMA1CNT_L = 0;
        private ushort _DMA1CNT_H = 0;
        private ushort _DMA2SAD_H = 0;
        private ushort _DMA2CNT_L = 0;
        private ushort _DMA2CNT_H = 0;
        private ushort _DMA3CNT_H = 0;
        private ushort _TM0CNT_H = 0;
        private ushort _TM1CNT_H = 0;
        private ushort _TM2CNT_H = 0;
        private ushort _TM3CNT_H = 0;
        private ushort _SIOCNT = 0;
        private ushort _KEYINPUT = 0x03ff;
        private ushort _KEYCNT = 0;
        private ushort _IE = 0;
        private ushort _WAITCNT = 0;
        private ushort _IME = 0;

        private bool _running = false;

        internal GBA(IRenderer renderer)
        {
            CPU.CallbackInterface cpuCallbackInterface = new()
            {
                ReadMemory8 = ReadMemory8,
                ReadMemory16 = ReadMemory16,
                ReadMemory32 = ReadMemory32,
                WriteMemory8 = WriteMemory8,
                WriteMemory16 = WriteMemory16,
                WriteMemory32 = WriteMemory32,
                HandleSWI = HandleSWI,
                HandleIRQ = HandleIRQ
            };

            _cpu = new(CPU.Architecture.ARMv4T, cpuCallbackInterface);
            _ppu = new(renderer);
        }

        internal void Init()
        {
            const uint ROMAddress = 0x0800_0000;

            for (int i = 0; i <= 12; ++i)
                _cpu.Reg[i] = 0;

            _cpu.Reg[CPU.SP] = 0x0300_7f00;
            _cpu.Reg[CPU.LR] = ROMAddress;
            _cpu.Reg[CPU.PC] = ROMAddress;

            _cpu.Reg13_svc = 0x0300_7fe0;
            _cpu.Reg13_irq = 0x0300_7fa0;

            _cpu.Reg14_svc = 0;
            _cpu.Reg14_irq = 0;

            _cpu.SPSR_svc = 0;
            _cpu.SPSR_irq = 0;

            _cpu.CPSR = 0x1f;

            _cpu.NextInstructionAddress = ROMAddress;
            _cpu.IRQPending = false;

            for (uint address = 0x0300_7e00; address < 0x0300_8000; address += 4)
                WriteMemory32(address, 0);

            _SOUNDCNT_H = 0;
            _SOUNDCNT_X = 0;
            _SOUNDBIAS = 0;
            _DMA0CNT_H = 0;
            _DMA1SAD_L = 0;
            _DMA1SAD_H = 0;
            _DMA1DAD_L = 0;
            _DMA1DAD_H = 0;
            _DMA1CNT_L = 0;
            _DMA1CNT_H = 0;
            _DMA2SAD_H = 0;
            _DMA2CNT_L = 0;
            _DMA2CNT_H = 0;
            _DMA3CNT_H = 0;
            _TM0CNT_H = 0;
            _TM1CNT_H = 0;
            _TM2CNT_H = 0;
            _TM3CNT_H = 0;
            _SIOCNT = 0;
            _KEYINPUT = 0x03ff;
            _KEYCNT = 0;
            _IE = 0;
            _WAITCNT = 0;
            _IME = 0;
        }

        internal void LoadROM(string filename)
        {
            _rom = File.ReadAllBytes(filename);
        }

        internal bool IsRunning()
        {
            return _running;
        }

        internal void Run()
        {
            _running = true;
            while (_running)
            {
                _cpu.Step();
                _ppu.Step();
            }
        }

        internal void Pause()
        {
            _running = false;
        }

        internal void SetKeyStatus(Keys key, bool pressed)
        {
            int mask = 1 << (int)key;
            _KEYINPUT = (ushort)(pressed ? _KEYINPUT & ~mask : _KEYINPUT | mask);
        }

        public byte ReadMemory8(uint address)
        {
            address &= 0x0fff_ffff;
            if (address is >= 0x0000_0000 and < 0x0000_4000)
            {
                // BIOS
                return 0;
            }
            else if (address is >= 0x0000_4000 and < 0x0200_0000)
            {
                // unused
                return 0;
            }
            else if (address is >= 0x0200_0000 and < 0x0204_0000)
            {
                uint offset = address - 0x0200_0000;
                return _externalWRAM[offset];
            }
            else if (address is >= 0x0300_0000 and < 0x0300_8000)
            {
                uint offset = address - 0x0300_0000;
                return _internalWRAM[offset];
            }
            else if (address is >= 0x0300_8000 and < 0x0400_0000)
            {
                // unused
                return 0;
            }
            else if (address is >= 0x0400_0000 and < 0x0500_0000)
            {
                uint offset = address - 0x0400_0000;
                switch (offset)
                {
                    case 0x000:
                        return (byte)_ppu.DISPCNT;
                    case 0x001:
                        return (byte)(_ppu.DISPCNT >> 8);

                    case 0x002:
                    case 0x003:
                        // undocumented - green swap
                        return 0;

                    case 0x004:
                        return (byte)_ppu.DISPSTAT;
                    case 0x005:
                        return (byte)(_ppu.DISPSTAT >> 8);

                    case 0x006:
                        return (byte)_ppu.VCOUNT;
                    case 0x007:
                        return (byte)(_ppu.VCOUNT >> 8);

                    case 0x008:
                    case 0x009:
                        Console.WriteLine("GBA: Read from BG0CNT register unimplemented");
                        return 0;

                    case 0x00a:
                    case 0x00b:
                        Console.WriteLine("GBA: Read from BG1CNT register unimplemented");
                        return 0;

                    case 0x00c:
                    case 0x00d:
                        Console.WriteLine("GBA: Read from BG2CNT register unimplemented");
                        return 0;

                    case 0x00e:
                    case 0x00f:
                        Console.WriteLine("GBA: Read from BG3CNT register unimplemented");
                        return 0;

                    case 0x050:
                    case 0x051:
                        Console.WriteLine("GBA: Read from BLDCNT register unimplemented");
                        return 0;

                    case 0x088:
                        return (byte)_SOUNDBIAS;
                    case 0x089:
                        return (byte)(_SOUNDBIAS >> 8);

                    case 0x0ba:
                        return (byte)_DMA0CNT_H;
                    case 0x0bb:
                        return (byte)(_DMA0CNT_H >> 8);

                    case 0x0c4:
                        return (byte)_DMA1CNT_L;
                    case 0x0c5:
                        return (byte)(_DMA1CNT_L >> 8);

                    case 0x0c6:
                        return (byte)_DMA1CNT_H;
                    case 0x0c7:
                        return (byte)(_DMA1CNT_H >> 8);

                    case 0x0d0:
                        return (byte)_DMA2CNT_L;
                    case 0x0d1:
                        return (byte)(_DMA2CNT_L >> 8);

                    case 0x0d2:
                        return (byte)_DMA2CNT_H;
                    case 0x0d3:
                        return (byte)(_DMA2CNT_H >> 8);

                    case 0x0de:
                        return (byte)_DMA3CNT_H;
                    case 0x0df:
                        return (byte)(_DMA3CNT_H >> 8);

                    case 0x102:
                        return (byte)_TM0CNT_H;
                    case 0x103:
                        return (byte)(_TM0CNT_H >> 8);

                    case 0x106:
                        return (byte)_TM1CNT_H;
                    case 0x107:
                        return (byte)(_TM1CNT_H >> 8);

                    case 0x10a:
                        return (byte)_TM2CNT_H;
                    case 0x10b:
                        return (byte)(_TM2CNT_H >> 8);

                    case 0x10e:
                        return (byte)_TM3CNT_H;
                    case 0x10f:
                        return (byte)(_TM3CNT_H >> 8);

                    case 0x128:
                        return (byte)_SIOCNT;
                    case 0x129:
                        return (byte)(_SIOCNT >> 8);

                    case 0x130:
                        return (byte)_KEYINPUT;
                    case 0x131:
                        return (byte)(_KEYINPUT >> 8);

                    case 0x132:
                        return (byte)_KEYCNT;
                    case 0x133:
                        return (byte)(_KEYCNT >> 8);

                    case 0x200:
                        return (byte)_IE;
                    case 0x201:
                        return (byte)(_IE >> 8);

                    case 0x204:
                        return (byte)_WAITCNT;
                    case 0x205:
                        return (byte)(_WAITCNT >> 8);

                    case 0x208:
                        return (byte)_IME;
                    case 0x209:
                        return (byte)(_IME >> 8);
                }
            }
            else if (address is >= 0x0600_0000 and < 0x0601_8000)
            {
                uint offset = address - 0x0600_0000;
                if (offset < _ppu.VRAM.Length)
                    return _ppu.VRAM[offset];
            }
            else if (address is >= 0x0800_0000 and < 0x0a00_0000)
            {
                if (_rom is null)
                    throw new Exception("GBA: No ROM loaded");

                uint offset = address - 0x0800_0000;
                if (offset < _rom.Length)
                    return _rom[offset];
            }
            else if (address is >= 0x0e00_0000 and < 0x0e01_0000)
            {
                uint offset = address - 0x0e00_0000;
                return _sram[offset];
            }

            throw new Exception(string.Format("GBA: Invalid read from address 0x{0:x8}", address));
        }

        public ushort ReadMemory16(uint address)
        {
            address &= 0x0fff_fffe;
            return (ushort)(ReadMemory8(address + 1) << 8 | ReadMemory8(address));
        }

        public uint ReadMemory32(uint address)
        {
            address &= 0x0fff_fffc;
            return (uint)(ReadMemory16(address + 2) << 16 | ReadMemory16(address));
        }

        public void WriteMemory8(uint address, byte value)
        {
            if (address is >= 0x0200_0000 and < 0x0300_0000)
            {
                uint offset = address - 0x0200_0000;
                if (offset < _externalWRAM.Length)
                    _externalWRAM[offset] = value;
                else
                    throw new Exception(string.Format("GBA: Invalid write to address 0x{0:x8}", address));
            }
            else if (address is >= 0x0300_0000 and < 0x0400_0000)
            {
                uint offset = address - 0x0300_0000;
                if (offset < _internalWRAM.Length)
                    _internalWRAM[offset] = value;
                else
                    throw new Exception(string.Format("GBA: Invalid write to address 0x{0:x8}", address));
            }
            else if (address is >= 0x0400_0000 and < 0x0500_0000)
            {
                uint offset = address - 0x0400_0000;
                switch (offset)
                {
                    case 0x000:
                        _ppu.DISPCNT = (ushort)(_ppu.DISPCNT & 0xff00 | value);
                        break;
                    case 0x001:
                        _ppu.DISPCNT = (ushort)(_ppu.DISPCNT & 0x00ff | value << 8);
                        break;

                    case 0x002:
                    case 0x003:
                        // undocumented - green swap
                        break; // ignore

                    case 0x004:
                        _ppu.DISPSTAT = (ushort)(_ppu.DISPSTAT & 0xff00 | value);
                        break;
                    case 0x005:
                        _ppu.DISPSTAT = (ushort)(_ppu.DISPSTAT & 0x00ff | value << 8);
                        break;

                    case 0x008:
                    case 0x009:
                        Console.WriteLine("GBA: Write to BG0CNT register unimplemented");
                        break;

                    case 0x00a:
                    case 0x00b:
                        Console.WriteLine("GBA: Write to BG1CNT register unimplemented");
                        break;

                    case 0x00c:
                    case 0x00d:
                        Console.WriteLine("GBA: Write to BG2CNT register unimplemented");
                        break;

                    case 0x00e:
                    case 0x00f:
                        Console.WriteLine("GBA: Write to BG3CNT register unimplemented");
                        break;

                    case 0x010:
                    case 0x011:
                        Console.WriteLine("GBA: Write to BG0HOFS register unimplemented");
                        break;

                    case 0x012:
                    case 0x013:
                        Console.WriteLine("GBA: Write to BG0VOFS register unimplemented");
                        break;

                    case 0x014:
                    case 0x015:
                        Console.WriteLine("GBA: Write to BG1HOFS register unimplemented");
                        break;

                    case 0x016:
                    case 0x017:
                        Console.WriteLine("GBA: Write to BG1VOFS register unimplemented");
                        break;

                    case 0x018:
                    case 0x019:
                        Console.WriteLine("GBA: Write to BG2HOFS register unimplemented");
                        break;

                    case 0x01a:
                    case 0x01b:
                        Console.WriteLine("GBA: Write to BG2VOFS register unimplemented");
                        break;

                    case 0x01c:
                    case 0x01d:
                        Console.WriteLine("GBA: Write to BG3HOFS register unimplemented");
                        break;

                    case 0x01e:
                    case 0x01f:
                        Console.WriteLine("GBA: Write to BG3VOFS register unimplemented");
                        break;

                    case 0x040:
                    case 0x041:
                        Console.WriteLine("GBA: Write to WIN0H register unimplemented");
                        break;

                    case 0x042:
                    case 0x043:
                        Console.WriteLine("GBA: Write to WIN1H register unimplemented");
                        break;

                    case 0x044:
                    case 0x045:
                        Console.WriteLine("GBA: Write to WIN0V register unimplemented");
                        break;

                    case 0x046:
                    case 0x047:
                        Console.WriteLine("GBA: Write to WIN1V register unimplemented");
                        break;

                    case 0x048:
                    case 0x049:
                        Console.WriteLine("GBA: Write to WININ register unimplemented");
                        break;

                    case 0x04a:
                    case 0x04b:
                        Console.WriteLine("GBA: Write to WINOUT register unimplemented");
                        break;

                    case 0x04c:
                    case 0x04d:
                        Console.WriteLine("GBA: Write to MOSAIC register unimplemented");
                        break;

                    case 0x050:
                    case 0x051:
                        Console.WriteLine("GBA: Write to BLDCNT register unimplemented");
                        break;

                    case 0x052:
                    case 0x053:
                        Console.WriteLine("GBA: Write to BLDALPHA register unimplemented");
                        break;

                    case 0x054:
                    case 0x055:
                        Console.WriteLine("GBA: Write to BLDY register unimplemented");
                        break;

                    case 0x062:
                    case 0x063:
                        Console.WriteLine("GBA: Write to SOUND1CNT_H register unimplemented");
                        break;

                    case 0x064:
                    case 0x065:
                        Console.WriteLine("GBA: Write to SOUND1CNT_X register unimplemented");
                        break;

                    case 0x068:
                    case 0x069:
                        Console.WriteLine("GBA: Write to SOUND2CNT_L register unimplemented");
                        break;

                    case 0x06c:
                    case 0x06d:
                        Console.WriteLine("GBA: Write to SOUND2CNT_H register unimplemented");
                        break;

                    case 0x070:
                    case 0x071:
                        Console.WriteLine("GBA: Write to SOUND3CNT_L register unimplemented");
                        break;

                    case 0x078:
                    case 0x079:
                        Console.WriteLine("GBA: Write to SOUND4CNT_L register unimplemented");
                        break;

                    case 0x07c:
                    case 0x07d:
                        Console.WriteLine("GBA: Write to SOUND4CNT_H register unimplemented");
                        break;

                    case 0x080:
                    case 0x081:
                        Console.WriteLine("GBA: Write to SOUNDCNT_L register unimplemented");
                        break;

                    case 0x082:
                        _SOUNDCNT_H = (ushort)(_SOUNDCNT_H & 0xff00 | value);
                        break;
                    case 0x083:
                        _SOUNDCNT_H = (ushort)(_SOUNDCNT_H & 0x00ff | value << 8);
                        break;

                    case 0x084:
                        _SOUNDCNT_X = (ushort)(_SOUNDCNT_X & 0xff00 | value);
                        break;
                    case 0x085:
                        _SOUNDCNT_X = (ushort)(_SOUNDCNT_X & 0x00ff | value << 8);
                        break;

                    case 0x088:
                        _SOUNDBIAS = (ushort)(_SOUNDBIAS & 0xff00 | value);
                        break;
                    case 0x089:
                        _SOUNDBIAS = (ushort)(_SOUNDBIAS & 0x00ff | value << 8);
                        break;

                    case 0x0ba:
                        _DMA0CNT_H = (ushort)(_DMA0CNT_H & 0xff00 | value);
                        break;
                    case 0x0bb:
                        _DMA0CNT_H = (ushort)(_DMA0CNT_H & 0x00ff | value << 8);
                        break;

                    case 0x0bc:
                        _DMA1SAD_L = (ushort)(_DMA1SAD_L & 0xff00 | value);
                        break;
                    case 0x0bd:
                        _DMA1SAD_L = (ushort)(_DMA1SAD_L & 0x00ff | value << 8);
                        break;

                    case 0x0be:
                        _DMA1SAD_H = (ushort)(_DMA1SAD_H & 0xff00 | value);
                        break;
                    case 0x0bf:
                        _DMA1SAD_H = (ushort)(_DMA1SAD_H & 0x00ff | value << 8);
                        break;

                    case 0x0c0:
                        _DMA1DAD_L = (ushort)(_DMA1DAD_L & 0xff00 | value);
                        break;
                    case 0x0c1:
                        _DMA1DAD_L = (ushort)(_DMA1DAD_L & 0x00ff | value << 8);
                        break;

                    case 0x0c2:
                        _DMA1DAD_H = (ushort)(_DMA1DAD_H & 0xff00 | value);
                        break;
                    case 0x0c3:
                        _DMA1DAD_H = (ushort)(_DMA1DAD_H & 0x00ff | value << 8);
                        break;

                    case 0x0c4:
                        _DMA1CNT_L = (ushort)(_DMA1CNT_L & 0xff00 | value);
                        break;
                    case 0x0c5:
                        _DMA1CNT_L = (ushort)(_DMA1CNT_L & 0x00ff | value << 8);
                        break;

                    case 0x0c6:
                        _DMA1CNT_H = (ushort)(_DMA1CNT_H & 0xff00 | value);
                        break;
                    case 0x0c7:
                        _DMA1CNT_H = (ushort)(_DMA1CNT_H & 0x00ff | value << 8);
                        break;

                    case 0x0c8:
                    case 0x0c9:
                        Console.WriteLine("GBA: Write to DMA2SAD_L register unimplemented");
                        break;

                    case 0x0ca:
                        _DMA2SAD_H = (ushort)(_DMA2SAD_H & 0xff00 | value);
                        break;
                    case 0x0cb:
                        _DMA2SAD_H = (ushort)(_DMA2SAD_H & 0x00ff | value << 8);
                        break;

                    case 0x0cc:
                    case 0x0cd:
                        Console.WriteLine("GBA: Write to DMA2DAD_L register unimplemented");
                        break;

                    case 0x0ce:
                    case 0x0cf:
                        Console.WriteLine("GBA: Write to DMA2DAD_H register unimplemented");
                        break;

                    case 0x0d0:
                        _DMA2CNT_L = (ushort)(_DMA2CNT_L & 0xff00 | value);
                        break;
                    case 0x0d1:
                        _DMA2CNT_L = (ushort)(_DMA2CNT_L & 0x00ff | value << 8);
                        break;

                    case 0x0d2:
                        _DMA2CNT_H = (ushort)(_DMA2CNT_H & 0xff00 | value);
                        break;
                    case 0x0d3:
                        _DMA2CNT_H = (ushort)(_DMA2CNT_H & 0x00ff | value << 8);
                        break;

                    case 0x0de:
                        _DMA3CNT_H = (ushort)(_DMA3CNT_H & 0xff00 | value);
                        break;
                    case 0x0df:
                        _DMA3CNT_H = (ushort)(_DMA3CNT_H & 0x00ff | value << 8);
                        break;

                    case 0x100:
                    case 0x101:
                        Console.WriteLine("GBA: Write to TM0CNT_L register unimplemented");
                        break;

                    case 0x102:
                        _TM0CNT_H = (ushort)(_TM0CNT_H & 0xff00 | value);
                        break;
                    case 0x103:
                        _TM0CNT_H = (ushort)(_TM0CNT_H & 0x00ff | value << 8);
                        break;

                    case 0x104:
                    case 0x105:
                        Console.WriteLine("GBA: Write to TM1CNT_L register unimplemented");
                        break;

                    case 0x106:
                        _TM1CNT_H = (ushort)(_TM1CNT_H & 0xff00 | value);
                        break;
                    case 0x107:
                        _TM1CNT_H = (ushort)(_TM1CNT_H & 0x00ff | value << 8);
                        break;

                    case 0x108:
                    case 0x109:
                        Console.WriteLine("GBA: Write to TM2CNT_L register unimplemented");
                        break;

                    case 0x10a:
                        _TM2CNT_H = (ushort)(_TM2CNT_H & 0xff00 | value);
                        break;
                    case 0x10b:
                        _TM2CNT_H = (ushort)(_TM2CNT_H & 0x00ff | value << 8);
                        break;

                    case 0x10c:
                    case 0x10d:
                        Console.WriteLine("GBA: Write to TM3CNT_L register unimplemented");
                        break;

                    case 0x10e:
                        _TM3CNT_H = (ushort)(_TM3CNT_H & 0xff00 | value);
                        break;
                    case 0x10f:
                        _TM3CNT_H = (ushort)(_TM3CNT_H & 0x00ff | value << 8);
                        break;

                    case 0x120:
                    case 0x121:
                        Console.WriteLine("GBA: Write to SIODATA32_L/SIOMULTI0 register unimplemented");
                        break;

                    case 0x122:
                    case 0x123:
                        Console.WriteLine("GBA: Write to SIODATA32_H/SIOMULTI1 register unimplemented");
                        break;

                    case 0x124:
                    case 0x125:
                        Console.WriteLine("GBA: Write to SIOMULTI2 register unimplemented");
                        break;

                    case 0x126:
                    case 0x127:
                        Console.WriteLine("GBA: Write to SIOMULTI3 register unimplemented");
                        break;

                    case 0x128:
                        _SIOCNT = (ushort)(_SIOCNT & 0xff00 | value);
                        break;
                    case 0x129:
                        _SIOCNT = (ushort)(_SIOCNT & 0x00ff | value << 8);
                        break;

                    case 0x12a:
                    case 0x12b:
                        Console.WriteLine("GBA: Write to SIODATA8 register unimplemented");
                        break;

                    case 0x130:
                    case 0x131:
                        Console.WriteLine("GBA: Write to KEYINPUT register unimplemented");
                        break;

                    case 0x132:
                        _KEYCNT = (ushort)(_KEYCNT & 0xff00 | value);
                        break;
                    case 0x133:
                        _KEYCNT = (ushort)(_KEYCNT & 0x00ff | value << 8);
                        break;

                    case 0x134:
                    case 0x135:
                        Console.WriteLine("GBA: Write to RCNT register unimplemented");
                        break;

                    case 0x200:
                        _IE = (ushort)(_IE & 0xff00 | value);
                        break;
                    case 0x201:
                        _IE = (ushort)(_IE & 0x00ff | value << 8);
                        break;

                    case 0x202:
                    case 0x203:
                        Console.WriteLine("GBA: Write to IF register unimplemented");
                        break;

                    case 0x204:
                        _WAITCNT = (ushort)(_WAITCNT & 0xff00 | value);
                        break;
                    case 0x205:
                        _WAITCNT = (ushort)(_WAITCNT & 0x00ff | value << 8);
                        break;

                    case 0x208:
                        _IME = (ushort)(_IME & 0xff00 | value);
                        break;
                    case 0x209:
                        _IME = (ushort)(_IME & 0x00ff | value << 8);
                        break;

                    case 0x20a:
                    case 0x20b:
                        // unused
                        break; // ignore

                    default:
                        throw new Exception(string.Format("GBA: Invalid write to address 0x{0:x8}", address));
                }
            }
            else if (address is >= 0x0500_0000 and < 0x0600_0000)
            {
                uint offset = address - 0x0500_0000;
                if (offset < _ppu.PaletteRAM.Length)
                    _ppu.PaletteRAM[offset] = value;
                else
                    throw new Exception(string.Format("GBA: Invalid write to address 0x{0:x8}", address));
            }
            else if (address is >= 0x0600_0000 and < 0x0700_0000)
            {
                uint offset = address - 0x0600_0000;
                if (offset < _ppu.VRAM.Length)
                    _ppu.VRAM[offset] = value;
                else
                    throw new Exception(string.Format("GBA: Invalid write to address 0x{0:x8}", address));
            }
            else if (address is >= 0x0e00_0000 and < 0x0e01_0000)
            {
                uint offset = address - 0x0e00_0000;
                _sram[offset] = value;
            }
            else
            {
                throw new Exception(string.Format("GBA: Invalid write to address 0x{0:x8}", address));
            }
        }

        public void WriteMemory16(uint address, ushort value)
        {
            address &= 0xffff_fffe;
            WriteMemory8(address + 1, (byte)(value >> 8));
            WriteMemory8(address, (byte)value);
        }

        public void WriteMemory32(uint address, uint value)
        {
            address &= 0xffff_fffc;
            WriteMemory16(address + 2, (ushort)(value >> 16));
            WriteMemory16(address, (ushort)value);
        }

        public void HandleSWI(uint value)
        {
            byte function = (byte)(value >> 16 & 0xff);

            switch (function)
            {
                // Div
                case 0x06:
                    {
                        int number = (int)_cpu.Reg[0];
                        int denom = (int)_cpu.Reg[1];
                        _cpu.Reg[0] = (uint)(number / denom);
                        _cpu.Reg[1] = (uint)(number % denom);
                        _cpu.Reg[3] = (uint)Math.Abs((int)_cpu.Reg[0]);
                        break;
                    }

                default:
                    Console.WriteLine("GBA: Unknown BIOS function 0x{0:x2}", function);
                    break;
            }
        }

        public void HandleIRQ()
        {
            throw new NotImplementedException("GBA: Unhandled interrupt");
        }
    }
}
