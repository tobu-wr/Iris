using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Iris
{
    public class GBA : CPU.ICallbacks
    {
        private const int KB = 1024;

        private Byte[]? rom;
        private readonly Byte[] externalWorkingRAM = new Byte[256 * KB];
        private readonly Byte[] internalWorkingRAM = new Byte[32 * KB];
        private readonly CPU cpu;
        private readonly PPU ppu;
        private bool running;

        public GBA(IRenderer renderer)
        {
            this.cpu = new(this);
            this.ppu = new(renderer);
        }

        public void LoadROM(string filename)
        {
            rom = File.ReadAllBytes(filename);
            cpu.Reset(0x0800_0000, 0b1101_1111); // flags cleared + IRQ & FIQ interrupts disabled + ARM state + system mode
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

        public Byte ReadMemory8(UInt32 address)
        {
            if (0x0300_0000 <= address && (address + 3) < 0x0400_0000)
            {
                address -= 0x0300_0000;

                if (address < internalWorkingRAM.Length)
                {
                    return internalWorkingRAM[address];
                }
            }
            else if (0x0800_0000 <= address && address < 0x0A00_0000)
            {
                if (rom == null)
                {
                    Console.WriteLine("GBA: No ROM loaded");
                    Environment.Exit(1);
                }

                address -= 0x0800_0000;

                if ((address + 3) < rom.Length)
                {
                    return rom[address];
                }
            }

            Console.WriteLine("GBA: Invalid read from address 0x{0:x8}", address);
            Environment.Exit(1);
            return 0;
        }

        public UInt16 ReadMemory16(UInt32 address)
        {
            if (0x0300_0000 <= address && (address + 3) < 0x0400_0000)
            {
                address -= 0x0300_0000;

                if ((address + 3) < internalWorkingRAM.Length)
                {
                    return (UInt16)((internalWorkingRAM[address + 1] << 8)
                                  | (internalWorkingRAM[address + 0] << 0));
                }
            }
            else if (0x0400_0000 <= address && (address + 1) < 0x0500_0000)
            {
                address -= 0x0400_0000;

                switch (address)
                {
                    case 0x004:
                        return ppu.dispstat;

                    case 0x130:
                        Console.WriteLine("GBA: Read from KEYINPUT register (unimplemented)");
                        return 0xff;

                    default:
                        Console.WriteLine("GBA: Invalid IO register read at {0}", address);
                        Environment.Exit(1);
                        return 0;
                }
            }
            else if (0x0800_0000 <= address && (address + 1) < 0x0A00_0000)
            {
                if (rom == null)
                {
                    Console.WriteLine("GBA: No ROM loaded");
                    Environment.Exit(1);
                }

                address -= 0x0800_0000;

                if ((address + 1) < rom.Length)
                {
                    return (UInt16)((rom[address + 1] << 8)
                                  | (rom[address + 0] << 0));
                }
            }

            Console.WriteLine("GBA: Invalid read from address 0x{0:x8}", address);
            Environment.Exit(1);
            return 0;
        }

        public UInt32 ReadMemory32(UInt32 address)
        {
            if (0x0300_0000 <= address && (address + 3) < 0x0400_0000)
            {
                address -= 0x0300_0000;

                if ((address + 3) < internalWorkingRAM.Length)
                {
                    return (UInt32)((internalWorkingRAM[address + 3] << 24)
                                  | (internalWorkingRAM[address + 2] << 16)
                                  | (internalWorkingRAM[address + 1] << 8)
                                  | (internalWorkingRAM[address + 0] << 0));
                }
            }
            else if (0x0800_0000 <= address && (address + 3) < 0x0A00_0000)
            {
                if (rom == null)
                {
                    Console.WriteLine("GBA: No ROM loaded");
                    Environment.Exit(1);
                }

                address -= 0x0800_0000;

                if ((address + 3) < rom.Length)
                {
                    return (UInt32)((rom[address + 3] << 24)
                                  | (rom[address + 2] << 16)
                                  | (rom[address + 1] << 8)
                                  | (rom[address + 0] << 0));
                }
            }

            Console.WriteLine("GBA: Invalid read from address 0x{0:x8}", address);
            Environment.Exit(1);
            return 0;
        }

        public void WriteMemory8(UInt32 address, Byte value)
        {
            if (0x0300_0000 <= address && (address + 3) < 0x0400_0000)
            {
                address -= 0x0300_0000;

                if (address < internalWorkingRAM.Length)
                {
                    internalWorkingRAM[address] = value;
                }
                else
                {
                    Console.WriteLine("GBA: Invalid write to internal working RAM");
                    Environment.Exit(1);
                }
            }
            else
            {
                Console.WriteLine("GBA: Invalid write to address 0x{0:x8}", address);
                Environment.Exit(1);
            }
        }

        public void WriteMemory16(UInt32 address, UInt16 value)
        {
            if (0x0300_0000 <= address && (address + 3) < 0x0400_0000)
            {
                address -= 0x0300_0000;

                if ((address + 1) < internalWorkingRAM.Length)
                {
                    internalWorkingRAM[address + 1] = (Byte)(value >> 8);
                    internalWorkingRAM[address + 0] = (Byte)(value >> 0);
                }
                else
                {
                    Console.WriteLine("GBA: Invalid write to internal working RAM");
                    Environment.Exit(1);
                }
            }
            else if (0x0500_0000 <= address && (address + 1) < 0x0600_0000)
            {
                address -= 0x0500_0000;

                if ((address + 1) < ppu.paletteRAM.Length)
                {
                    ppu.paletteRAM[address + 1] = (Byte)(value >> 8);
                    ppu.paletteRAM[address + 0] = (Byte)(value >> 0);
                }
                else
                {
                    Console.WriteLine("GBA: Invalid write to palette RAM");
                    Environment.Exit(1);
                }
            }
            else if (0x0600_0000 <= address && (address + 1) < 0x0700_0000)
            {
                address -= 0x0600_0000;

                if ((address + 1) < ppu.VRAM.Length)
                {
                    ppu.VRAM[address + 1] = (Byte)(value >> 8);
                    ppu.VRAM[address + 0] = (Byte)(value >> 0);
                }
                else
                {
                    Console.WriteLine("GBA: Invalid write to VRAM");
                    Environment.Exit(1);
                }
            }
            else
            {
                Console.WriteLine("GBA: Invalid write to address 0x{0:x8}", address);
                Environment.Exit(1);
            }
        }

        public void WriteMemory32(UInt32 address, UInt32 value)
        {
            if (0x0200_000 <= address && (address + 3) < 0x0300_0000)
            {
                address -= 0x0200_0000;

                if ((address + 3) < externalWorkingRAM.Length)
                {
                    externalWorkingRAM[address + 3] = (Byte)(value >> 24);
                    externalWorkingRAM[address + 2] = (Byte)(value >> 16);
                    externalWorkingRAM[address + 1] = (Byte)(value >> 8);
                    externalWorkingRAM[address + 0] = (Byte)(value >> 0);
                }
                else
                {
                    Console.WriteLine("GBA: Invalid write to external working RAM");
                    Environment.Exit(1);
                }
            }
            else if (0x0300_0000 <= address && (address + 3) < 0x0400_0000)
            {
                address -= 0x0300_0000;

                if ((address + 3) < internalWorkingRAM.Length)
                {
                    internalWorkingRAM[address + 3] = (Byte)(value >> 24);
                    internalWorkingRAM[address + 2] = (Byte)(value >> 16);
                    internalWorkingRAM[address + 1] = (Byte)(value >> 8);
                    internalWorkingRAM[address + 0] = (Byte)(value >> 0);
                }
                else
                {
                    Console.WriteLine("GBA: Invalid write to internal working RAM");
                    Environment.Exit(1);
                }
            }
            else if (0x0400_0000 <= address && (address + 3) < 0x0500_0000)
            {
                address -= 0x0400_0000;

                switch (address)
                {
                    // DISPCNT
                    case 0x000:
                        ppu.dispcnt = (UInt16)value;
                        break;

                    // IME
                    case 0x208:
                        if ((value & 1) == 1)
                        {
                            Console.WriteLine("GBA: IME register unimplemented");
                        }
                        break;

                    default:
                        Console.WriteLine("GBA: Invalid IO register write at {0}", address);
                        Environment.Exit(1);
                        break;
                }
            }
            else if (0x0600_0000 <= address && (address + 3) < 0x0700_0000)
            {
                address -= 0x0600_0000;

                if ((address + 3) < ppu.VRAM.Length)
                {
                    ppu.VRAM[address + 3] = (Byte)(value >> 24);
                    ppu.VRAM[address + 2] = (Byte)(value >> 16);
                    ppu.VRAM[address + 1] = (Byte)(value >> 8);
                    ppu.VRAM[address + 0] = (Byte)(value >> 0);
                }
                else
                {
                    Console.WriteLine("GBA: Invalid write to VRAM");
                    Environment.Exit(1);
                }
            }
            else
            {
                Console.WriteLine("GBA: Invalid write to address 0x{0:x8}", address);
                Environment.Exit(1);
            }
        }
    }
}
