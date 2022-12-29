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

        private Byte[]? rom;
        private readonly Byte[] externalWRAM = new Byte[256 * KB];
        private readonly Byte[] internalWRAM = new Byte[32 * KB];

        private readonly CPU cpu;
        private readonly PPU ppu;

        private UInt16 keyinput = 0x03ff;

        private bool running = false;

        public GBA(IRenderer renderer)
        {
            this.cpu = new(this);
            this.ppu = new(renderer);
        }

        public void Init()
        {
            cpu.Init(0x0800_0000, 0b1101_1111); // flags cleared + IRQ & FIQ interrupts disabled + ARM state + system mode
            keyinput = 0x03ff;
        }

        public void LoadROM(string filename)
        {
            rom = File.ReadAllBytes(filename);
            Init();
        }

        public bool IsRunning()
        {
            return running;
        }

        public void Run()
        {
            running = true;
            while (running)
            {
                cpu.Step();
                ppu.Step();
            }
        }

        public void Pause()
        {
            running = false;
        }

        public void SetKeyStatus(Keys key, bool pressed)
        {
            int mask = 1 << (int)key;
            keyinput = pressed ? (UInt16)(keyinput & ~mask) : (UInt16)(keyinput | mask);
        }

        public Byte ReadMemory8(UInt32 address)
        {
            if (0x0200_0000 <= address && address < 0x0300_0000)
            {
                UInt32 offset = address - 0x0200_0000;
                if (offset < externalWRAM.Length)
                    return externalWRAM[offset];
            }
            else if (0x0300_0000 <= address && address < 0x0400_0000)
            {
                UInt32 offset = address - 0x0300_0000;
                if (offset < internalWRAM.Length)
                    return internalWRAM[offset];
            }
            else if (0x0400_0000 <= address && address < 0x0500_0000)
            {
                UInt32 offset = address - 0x0400_0000;
                switch (offset)
                {
                    case 0x004:
                        return (Byte)ppu.dispstat;
                    case 0x005:
                        return (Byte)(ppu.dispstat >> 8);

                    case 0x130:
                        return (Byte)keyinput;
                    case 0x131:
                        return (Byte)(keyinput >> 8);
                }
            }
            else if (0x0800_0000 <= address && address < 0x0A00_0000)
            {
                if (rom == null)
                    throw new Exception("GBA: No ROM loaded");

                UInt32 offset = address - 0x0800_0000;
                if (offset < rom.Length)
                    return rom[offset];
            }

            throw new Exception(string.Format("GBA: Invalid read from address 0x{0:x8}", address));
        }

        public UInt16 ReadMemory16(UInt32 address)
        {
            return (UInt16)((ReadMemory8(address + 1) << 8) | ReadMemory8(address));
        }

        public UInt32 ReadMemory32(UInt32 address)
        {
            return (UInt32)((ReadMemory16(address + 2) << 16) | ReadMemory16(address));
        }

        public void WriteMemory8(UInt32 address, Byte value)
        {
            if (0x0200_0000 <= address && address < 0x0300_0000)
            {
                UInt32 offset = address - 0x0200_0000;
                if (offset < externalWRAM.Length)
                    externalWRAM[offset] = value;
                else
                    throw new Exception(string.Format("GBA: Invalid write to address 0x{0:x8}", address));
            }
            else if (0x0300_0000 <= address && address < 0x0400_0000)
            {
                UInt32 offset = address - 0x0300_0000;
                if (offset < internalWRAM.Length)
                    internalWRAM[offset] = value;
                else
                    throw new Exception(string.Format("GBA: Invalid write to address 0x{0:x8}", address));
            }
            else if (0x0400_0000 <= address && address < 0x0500_0000)
            {
                UInt32 offset = address - 0x0400_0000;
                switch (offset)
                {
                    case 0x000:
                        ppu.dispcnt = (UInt16)((ppu.dispcnt & 0xff00) | value);
                        break;
                    case 0x001:
                        ppu.dispcnt = (UInt16)((ppu.dispcnt & 0x00ff) | (value << 8));
                        break;

                    case 0x002:
                    case 0x003:
                        // undocumented - green swap
                        break; // ignore

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
                if (offset < ppu.paletteRAM.Length)
                    ppu.paletteRAM[offset] = value;
                else
                    throw new Exception(string.Format("GBA: Invalid write to address 0x{0:x8}", address));
            }
            else if (0x0600_0000 <= address && address < 0x0700_0000)
            {
                UInt32 offset = address - 0x0600_0000;
                if (offset < ppu.VRAM.Length)
                    ppu.VRAM[offset] = value;
                else
                    throw new Exception(string.Format("GBA: Invalid write to address 0x{0:x8}", address));
            }
            else
            {
                throw new Exception(string.Format("GBA: Invalid write to address 0x{0:x8}", address));
            }
        }

        public void WriteMemory16(UInt32 address, UInt16 value)
        {
            WriteMemory8(address + 1, (Byte)(value >> 8));
            WriteMemory8(address, (Byte)value);
        }

        public void WriteMemory32(UInt32 address, UInt32 value)
        {
            WriteMemory16(address + 2, (UInt16)(value >> 16));
            WriteMemory16(address, (UInt16)value);
        }
    }
}
