namespace Iris
{
    internal sealed class GBA : CPU.ICallbacks
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

        private Byte[]? _rom;
        private readonly Byte[] _externalWRAM = new Byte[256 * KB];
        private readonly Byte[] _internalWRAM = new Byte[32 * KB];

        private readonly CPU _cpu;
        private readonly PPU _ppu;

        private UInt16 _KEYINPUT = 0x03ff;

        private bool _running = false;

        internal GBA(IRenderer renderer)
        {
            _cpu = new(this);
            _ppu = new(renderer);
        }

        internal void Init()
        {
            const UInt32 ROMAddress = 0x0800_0000;

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
            _cpu.InterruptPending = false;

            for (UInt32 address = 0x0300_7e00; address < 0x0300_8000; address += 4)
                WriteMemory32(address, 0);

            _KEYINPUT = 0x03ff;
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
            _KEYINPUT = (UInt16)(pressed ? (_KEYINPUT & ~mask) : (_KEYINPUT | mask));
        }

        public Byte ReadMemory8(UInt32 address)
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
                UInt32 offset = address - 0x0200_0000;
                return _externalWRAM[offset];
            }
            else if (address is >= 0x0300_0000 and < 0x0300_8000)
            {
                UInt32 offset = address - 0x0300_0000;
                return _internalWRAM[offset];
            }
            else if (address is >= 0x0300_8000 and < 0x0400_0000)
            {
                // unused
                return 0;
            }
            else if (address is >= 0x0400_0000 and < 0x0500_0000)
            {
                UInt32 offset = address - 0x0400_0000;
                switch (offset)
                {
                    case 0x000:
                        return (Byte)_ppu.DISPCNT;
                    case 0x001:
                        return (Byte)(_ppu.DISPCNT >> 8);

                    case 0x002:
                    case 0x003:
                        // undocumented - green swap
                        return 0;

                    case 0x004:
                        return (Byte)_ppu.DISPSTAT;
                    case 0x005:
                        return (Byte)(_ppu.DISPSTAT >> 8);

                    case 0x006:
                        return (Byte)_ppu.VCOUNT;
                    case 0x007:
                        return (Byte)(_ppu.VCOUNT >> 8);

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
                    case 0x089:
                        Console.WriteLine("GBA: Read from SOUNDBIAS register unimplemented");
                        return 0;

                    case 0x0ba:
                    case 0x0bb:
                        Console.WriteLine("GBA: Read from DMA0CNT_H register unimplemented");
                        return 0;

                    case 0x0c4:
                    case 0x0c5:
                        Console.WriteLine("GBA: Read from DMA1CNT_L register unimplemented");
                        return 0;

                    case 0x0c6:
                    case 0x0c7:
                        Console.WriteLine("GBA: Read from DMA1CNT_H register unimplemented");
                        return 0;

                    case 0x0d0:
                    case 0x0d1:
                        Console.WriteLine("GBA: Read from DMA2CNT_L register unimplemented");
                        return 0;

                    case 0x0d2:
                    case 0x0d3:
                        Console.WriteLine("GBA: Read from DMA2CNT_H register unimplemented");
                        return 0;

                    case 0x0de:
                    case 0x0df:
                        Console.WriteLine("GBA: Read from DMA3CNT_H register unimplemented");
                        return 0;

                    case 0x102:
                    case 0x103:
                        Console.WriteLine("GBA: Read from TM0CNT_H register unimplemented");
                        return 0;

                    case 0x106:
                    case 0x107:
                        Console.WriteLine("GBA: Read from TM1CNT_H register unimplemented");
                        return 0;

                    case 0x10a:
                    case 0x10b:
                        Console.WriteLine("GBA: Read from TM2CNT_H register unimplemented");
                        return 0;

                    case 0x10e:
                    case 0x10f:
                        Console.WriteLine("GBA: Read from TM3CNT_H register unimplemented");
                        return 0;

                    case 0x128:
                    case 0x129:
                        Console.WriteLine("GBA: Read from SIOCNT register unimplemented");
                        return 0;

                    case 0x130:
                        return (Byte)_KEYINPUT;
                    case 0x131:
                        return (Byte)(_KEYINPUT >> 8);

                    case 0x132:
                    case 0x133:
                        Console.WriteLine("GBA: Read from KEYCNT register unimplemented");
                        return 0;

                    case 0x200:
                    case 0x201:
                        Console.WriteLine("GBA: Read from IE register unimplemented");
                        return 0;

                    case 0x204:
                    case 0x205:
                        Console.WriteLine("GBA: Read from WAITCNT register unimplemented");
                        return 0;

                    case 0x208:
                    case 0x209:
                        Console.WriteLine("GBA: Read from IME register unimplemented");
                        return 0;
                }
            }
            else if (address is >= 0x0600_0000 and < 0x0601_8000)
            {
                UInt32 offset = address - 0x0600_0000;
                if (offset < _ppu.VRAM.Length)
                    return _ppu.VRAM[offset];
            }
            else if (address is >= 0x0800_0000 and < 0x0a00_0000)
            {
                if (_rom is null)
                    throw new Exception("GBA: No ROM loaded");

                UInt32 offset = address - 0x0800_0000;
                if (offset < _rom.Length)
                    return _rom[offset];
            }
            else if (address is >= 0x0e00_0000 and < 0x0e01_0000)
            {
                // TODO: implement SRAM
                return 0;
            }

            throw new Exception(string.Format("GBA: Invalid read from address 0x{0:x8}", address));
        }

        public UInt16 ReadMemory16(UInt32 address)
        {
            address &= 0x0fff_fffe;
            return (UInt16)((ReadMemory8(address + 1) << 8) | ReadMemory8(address));
        }

        public UInt32 ReadMemory32(UInt32 address)
        {
            address &= 0x0fff_fffc;
            return (UInt32)((ReadMemory16(address + 2) << 16) | ReadMemory16(address));
        }

        public void WriteMemory8(UInt32 address, Byte value)
        {
            if (address is >= 0x0200_0000 and < 0x0300_0000)
            {
                UInt32 offset = address - 0x0200_0000;
                if (offset < _externalWRAM.Length)
                    _externalWRAM[offset] = value;
                else
                    throw new Exception(string.Format("GBA: Invalid write to address 0x{0:x8}", address));
            }
            else if (address is >= 0x0300_0000 and < 0x0400_0000)
            {
                UInt32 offset = address - 0x0300_0000;
                if (offset < _internalWRAM.Length)
                    _internalWRAM[offset] = value;
                else
                    throw new Exception(string.Format("GBA: Invalid write to address 0x{0:x8}", address));
            }
            else if (address is >= 0x0400_0000 and < 0x0500_0000)
            {
                UInt32 offset = address - 0x0400_0000;
                switch (offset)
                {
                    case 0x000:
                        _ppu.DISPCNT = (UInt16)((_ppu.DISPCNT & 0xff00) | value);
                        break;
                    case 0x001:
                        _ppu.DISPCNT = (UInt16)((_ppu.DISPCNT & 0x00ff) | (value << 8));
                        break;

                    case 0x002:
                    case 0x003:
                        // undocumented - green swap
                        break; // ignore

                    case 0x004:
                        _ppu.DISPSTAT = (UInt16)((_ppu.DISPSTAT & 0xff00) | value);
                        break;
                    case 0x005:
                        _ppu.DISPSTAT = (UInt16)((_ppu.DISPSTAT & 0x00ff) | (value << 8));
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
                    case 0x083:
                        Console.WriteLine("GBA: Write to SOUNDCNT_H register unimplemented");
                        break;

                    case 0x084:
                    case 0x085:
                        Console.WriteLine("GBA: Write to SOUNDCNT_X register unimplemented");
                        break;

                    case 0x088:
                    case 0x089:
                        Console.WriteLine("GBA: Write to SOUNDBIAS register unimplemented");
                        break;

                    case 0x0ba:
                    case 0x0bb:
                        Console.WriteLine("GBA: Write to DMA0CNT_H register unimplemented");
                        break;

                    case 0x0bc:
                    case 0x0bd:
                        Console.WriteLine("GBA: Write to DMA1SAD_L register unimplemented");
                        break;

                    case 0x0be:
                    case 0x0bf:
                        Console.WriteLine("GBA: Write to DMA1SAD_H register unimplemented");
                        break;

                    case 0x0c0:
                    case 0x0c1:
                        Console.WriteLine("GBA: Write to DMA1DAD_L register unimplemented");
                        break;

                    case 0x0c2:
                    case 0x0c3:
                        Console.WriteLine("GBA: Write to DMA1DAD_H register unimplemented");
                        break;

                    case 0x0c6:
                    case 0x0c7:
                        Console.WriteLine("GBA: Write to DMA1CNT_H register unimplemented");
                        break;

                    case 0x0c8:
                    case 0x0c9:
                        Console.WriteLine("GBA: Write to DMA2SAD_L register unimplemented");
                        break;

                    case 0x0ca:
                    case 0x0cb:
                        Console.WriteLine("GBA: Write to DMA2SAD_H register unimplemented");
                        break;

                    case 0x0cc:
                    case 0x0cd:
                        Console.WriteLine("GBA: Write to DMA2DAD_L register unimplemented");
                        break;

                    case 0x0ce:
                    case 0x0cf:
                        Console.WriteLine("GBA: Write to DMA2DAD_H register unimplemented");
                        break;

                    case 0x0d2:
                    case 0x0d3:
                        Console.WriteLine("GBA: Write to DMA2CNT_H register unimplemented");
                        break;

                    case 0x0de:
                    case 0x0df:
                        Console.WriteLine("GBA: Write to DMA3CNT_H register unimplemented");
                        break;

                    case 0x100:
                    case 0x101:
                        Console.WriteLine("GBA: Write to TM0CNT_L register unimplemented");
                        break;

                    case 0x102:
                    case 0x103:
                        Console.WriteLine("GBA: Write to TM0CNT_H register unimplemented");
                        break;

                    case 0x104:
                    case 0x105:
                        Console.WriteLine("GBA: Write to TM1CNT_L register unimplemented");
                        break;

                    case 0x106:
                    case 0x107:
                        Console.WriteLine("GBA: Write to TM1CNT_H register unimplemented");
                        break;

                    case 0x108:
                    case 0x109:
                        Console.WriteLine("GBA: Write to TM2CNT_L register unimplemented");
                        break;

                    case 0x10a:
                    case 0x10b:
                        Console.WriteLine("GBA: Write to TM2CNT_H register unimplemented");
                        break;

                    case 0x10c:
                    case 0x10d:
                        Console.WriteLine("GBA: Write to TM3CNT_L register unimplemented");
                        break;

                    case 0x10e:
                    case 0x10f:
                        Console.WriteLine("GBA: Write to TM3CNT_H register unimplemented");
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
                    case 0x129:
                        Console.WriteLine("GBA: Write to SIOCNT register unimplemented");
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
                    case 0x133:
                        Console.WriteLine("GBA: Write to KEYCNT register unimplemented");
                        break;

                    case 0x134:
                    case 0x135:
                        Console.WriteLine("GBA: Write to RCNT register unimplemented");
                        break;

                    case 0x200:
                    case 0x201:
                        Console.WriteLine("GBA: Write to IE register unimplemented");
                        break;

                    case 0x202:
                    case 0x203:
                        Console.WriteLine("GBA: Write to IF register unimplemented");
                        break;

                    case 0x204:
                    case 0x205:
                        Console.WriteLine("GBA: Write to WAITCNT register unimplemented");
                        break;

                    case 0x208:
                    case 0x209:
                        Console.WriteLine("GBA: Write to IME register unimplemented");
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
                UInt32 offset = address - 0x0500_0000;
                if (offset < _ppu.PaletteRAM.Length)
                    _ppu.PaletteRAM[offset] = value;
                else
                    throw new Exception(string.Format("GBA: Invalid write to address 0x{0:x8}", address));
            }
            else if (address is >= 0x0600_0000 and < 0x0700_0000)
            {
                UInt32 offset = address - 0x0600_0000;
                if (offset < _ppu.VRAM.Length)
                    _ppu.VRAM[offset] = value;
                else
                    throw new Exception(string.Format("GBA: Invalid write to address 0x{0:x8}", address));
            }
            else if (address is >= 0x0e00_0000 and < 0x0e01_0000)
            {
                // TODO: implement SRAM
            }
            else
            {
                //throw new Exception(string.Format("GBA: Invalid write to address 0x{0:x8}", address));
            }
        }

        public void WriteMemory16(UInt32 address, UInt16 value)
        {
            address &= 0xffff_fffe;
            WriteMemory8(address + 1, (Byte)(value >> 8));
            WriteMemory8(address, (Byte)value);
        }

        public void WriteMemory32(UInt32 address, UInt32 value)
        {
            address &= 0xffff_fffc;
            WriteMemory16(address + 2, (UInt16)(value >> 16));
            WriteMemory16(address, (UInt16)value);
        }

        public void HandleSWI(UInt32 value)
        {
            Byte function = (Byte)((value >> 16) & 0xff);

            switch (function)
            {
                // Div
                case 0x06:
                    {
                        Int32 number = (Int32)_cpu.Reg[0];
                        Int32 denom = (Int32)_cpu.Reg[1];
                        _cpu.Reg[0] = (UInt32)(number / denom);
                        _cpu.Reg[1] = (UInt32)(number % denom);
                        _cpu.Reg[3] = (UInt32)Math.Abs((Int32)_cpu.Reg[0]);
                        break;
                    }

                default:
                    Console.WriteLine("GBA: Unknown BIOS function 0x{0:x2}", function);
                    break;
            }
        }

        public void HandleInterrupt()
        {
            throw new NotImplementedException("GBA: unhandled interrupt");
        }
    }
}
