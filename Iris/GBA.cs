using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Iris
{
    public class GBA : CPU.ICallbacks
    {
        public enum Keys
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

        public GBA(IRenderer renderer)
        {
            _cpu = new(this);
            _ppu = new(renderer);
        }

        public void Init()
        {
            _cpu.Init(0x0800_0000, 0b1101_1111); // flags cleared + IRQ & FIQ interrupts disabled + ARM state + system mode
            _KEYINPUT = 0x03ff;
        }

        public void LoadROM(string filename)
        {
            _rom = File.ReadAllBytes(filename);
            Init();
        }

        public bool IsRunning()
        {
            return _running;
        }

        public void Run()
        {
            _running = true;
            while (_running)
            {
                _cpu.Step();
                _ppu.Step();
            }
        }

        public void Pause()
        {
            _running = false;
        }

        public void SetKeyStatus(Keys key, bool pressed)
        {
            int mask = 1 << (int)key;
            _KEYINPUT = pressed ? (UInt16)(_KEYINPUT & ~mask) : (UInt16)(_KEYINPUT | mask);
        }

        public Byte ReadMemory8(UInt32 address)
        {
            if (0x0200_0000 <= address && address < 0x0300_0000)
            {
                UInt32 offset = address - 0x0200_0000;
                if (offset < _externalWRAM.Length)
                    return _externalWRAM[offset];
            }
            else if (0x0300_0000 <= address && address < 0x0400_0000)
            {
                UInt32 offset = address - 0x0300_0000;
                if (offset < _internalWRAM.Length)
                    return _internalWRAM[offset];
            }
            else if (0x0400_0000 <= address && address < 0x0500_0000)
            {
                UInt32 offset = address - 0x0400_0000;
                switch (offset)
                {
                    case 0x004:
                        return (Byte)_ppu.DISPSTAT;
                    case 0x005:
                        return (Byte)(_ppu.DISPSTAT >> 8);

                    case 0x0ba:
                    case 0x0bb:
                        Console.WriteLine("GBA: Read from DMA0CNT_H register unimplemented");
                        return 0;

                    case 0x0c6:
                    case 0x0c7:
                        Console.WriteLine("GBA: Read from DMA1CNT_H register unimplemented");
                        return 0;

                    case 0x0d2:
                    case 0x0d3:
                        Console.WriteLine("GBA: Read from DMA2CNT_H register unimplemented");
                        return 0;

                    case 0x0de:
                    case 0x0df:
                        Console.WriteLine("GBA: Read from DMA3CNT_H register unimplemented");
                        return 0;

                    case 0x130:
                        return (Byte)_KEYINPUT;
                    case 0x131:
                        return (Byte)(_KEYINPUT >> 8);
                }
            }
            else if (0x0800_0000 <= address && address < 0x0A00_0000)
            {
                if (_rom == null)
                    throw new Exception("GBA: No ROM loaded");

                UInt32 offset = address - 0x0800_0000;
                if (offset < _rom.Length)
                    return _rom[offset];
            }

            throw new Exception(string.Format("GBA: Invalid read from address 0x{0:x8}", address));
        }

        public UInt16 ReadMemory16(UInt32 address)
        {
            address &= 0xffff_fffe;
            return (UInt16)((ReadMemory8(address + 1) << 8) | ReadMemory8(address));
        }

        public UInt32 ReadMemory32(UInt32 address)
        {
            address &= 0xffff_fffc;
            return (UInt32)((ReadMemory16(address + 2) << 16) | ReadMemory16(address));
        }

        public void WriteMemory8(UInt32 address, Byte value)
        {
            if (0x0200_0000 <= address && address < 0x0300_0000)
            {
                UInt32 offset = address - 0x0200_0000;
                if (offset < _externalWRAM.Length)
                    _externalWRAM[offset] = value;
                else
                    throw new Exception(string.Format("GBA: Invalid write to address 0x{0:x8}", address));
            }
            else if (0x0300_0000 <= address && address < 0x0400_0000)
            {
                UInt32 offset = address - 0x0300_0000;
                if (offset < _internalWRAM.Length)
                    _internalWRAM[offset] = value;
                else
                    throw new Exception(string.Format("GBA: Invalid write to address 0x{0:x8}", address));
            }
            else if (0x0400_0000 <= address && address < 0x0500_0000)
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

                    case 0x0ba:
                    case 0x0bb:
                        Console.WriteLine("GBA: Write to DMA0CNT_H register unimplemented");
                        break;

                    case 0x0c6:
                    case 0x0c7:
                        Console.WriteLine("GBA: Write to DMA1CNT_H register unimplemented");
                        break;

                    case 0x0d2:
                    case 0x0d3:
                        Console.WriteLine("GBA: Write to DMA2CNT_H register unimplemented");
                        break;

                    case 0x0de:
                    case 0x0df:
                        Console.WriteLine("GBA: Write to DMA3CNT_H register unimplemented");
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
            else if (0x0500_0000 <= address && address < 0x0600_0000)
            {
                UInt32 offset = address - 0x0500_0000;
                if (offset < _ppu.PaletteRAM.Length)
                    _ppu.PaletteRAM[offset] = value;
                else
                    throw new Exception(string.Format("GBA: Invalid write to address 0x{0:x8}", address));
            }
            else if (0x0600_0000 <= address && address < 0x0700_0000)
            {
                UInt32 offset = address - 0x0600_0000;
                if (offset < _ppu.VRAM.Length)
                    _ppu.VRAM[offset] = value;
                else
                    throw new Exception(string.Format("GBA: Invalid write to address 0x{0:x8}", address));
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
    }
}
