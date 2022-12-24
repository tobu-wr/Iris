using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Iris
{
    public class GBA
    {
        private Byte[]? rom;
        private readonly Byte[] externalWorkingRAM = new Byte[256 * 1024]; // 256 KB
        private readonly Byte[] internalWorkingRAM = new Byte[32 * 1024]; // 32 KB

        private readonly CPU cpu;
        private readonly PPU ppu;

        public GBA()
        {
            this.cpu = new(Read8, Read16, Read32, Write8, Write16, Write32, 0x0800_0000);
            this.ppu = new(DrawFrame);
        }

        public void LoadROM(string filename)
        {
            try
            {
                rom = File.ReadAllBytes(filename);
            }
            catch
            {
                Console.WriteLine("ROM file not found");
                Environment.Exit(1);
            }
        }

        public Byte Read8(UInt32 address)
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
                    Console.WriteLine("No ROM loaded");
                    Environment.Exit(1);
                }

                address -= 0x0800_0000;

                if ((address + 3) < rom.Length)
                {
                    return rom[address];
                }
            }

            Console.WriteLine("Invalid read from address 0x{0:x8}", address);
            Environment.Exit(1);
            return 0;
        }

        public UInt16 Read16(UInt32 address)
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
                        Console.WriteLine("Read {0:x4} from DISPSTAT register", ppu.dispstat);
                        return ppu.dispstat;

                    case 0x130:
                        Console.WriteLine("Read from KEYINPUT register (unimplemented)");
                        return 0xff;

                    default:
                        Console.WriteLine("Invalid IO register read");
                        Environment.Exit(1);
                        return 0;
                }
            }
            else if (0x0800_0000 <= address && (address + 1) < 0x0A00_0000)
            {
                if (rom == null)
                {
                    Console.WriteLine("No ROM loaded");
                    Environment.Exit(1);
                }

                address -= 0x0800_0000;

                if ((address + 1) < rom.Length)
                {
                    return (UInt16)((rom[address + 1] << 8)
                                  | (rom[address + 0] << 0));
                }
            }

            Console.WriteLine("Invalid read from address 0x{0:x8}", address);
            Environment.Exit(1);
            return 0;
        }

        public UInt32 Read32(UInt32 address)
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
                    Console.WriteLine("No ROM loaded");
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

            Console.WriteLine("Invalid read from address 0x{0:x8}", address);
            Environment.Exit(1);
            return 0;
        }

        public void Write8(UInt32 address, Byte value)
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
                    Console.WriteLine("Invalid write to internal working RAM");
                    Environment.Exit(1);
                }
            }
            else
            {
                Console.WriteLine("Invalid write to address 0x{0:x8}", address);
                Environment.Exit(1);
            }
        }

        public void Write16(UInt32 address, UInt16 value)
        {
            if (0x0300_0000 <= address && (address + 3) < 0x0400_0000)
            {
                address -= 0x0300_0000;

                if ((address + 1) < internalWorkingRAM.Length)
                {
                    internalWorkingRAM[address + 1] = (Byte)((value >> 8) & 0xff);
                    internalWorkingRAM[address + 0] = (Byte)((value >> 0) & 0xff);
                }
                else
                {
                    Console.WriteLine("Invalid write to internal working RAM");
                    Environment.Exit(1);
                }
            }
            else if (0x0500_0000 <= address && (address + 1) < 0x0600_0000)
            {
                address -= 0x0500_0000;

                if ((address + 1) < ppu.paletteRAM.Length)
                {
                    ppu.paletteRAM[address + 1] = (Byte)((value >> 8) & 0xff);
                    ppu.paletteRAM[address + 0] = (Byte)((value >> 0) & 0xff);
                }
                else
                {
                    Console.WriteLine("Invalid write to palette RAM");
                    Environment.Exit(1);
                }
            }
            else if (0x0600_0000 <= address && (address + 1) < 0x0700_0000)
            {
                address -= 0x0600_0000;

                if ((address + 1) < ppu.VRAM.Length)
                {
                    ppu.VRAM[address + 1] = (Byte)((value >> 8) & 0xff);
                    ppu.VRAM[address + 0] = (Byte)((value >> 0) & 0xff);
                }
                else
                {
                    Console.WriteLine("Invalid write to VRAM");
                    Environment.Exit(1);
                }
            }
            else
            {
                Console.WriteLine("Invalid write to address 0x{0:x8}", address);
                Environment.Exit(1);
            }
        }

        public void Write32(UInt32 address, UInt32 value)
        {
            if (0x0200_000 <= address && (address + 3) < 0x0300_0000)
            {
                address -= 0x0200_0000;

                if ((address + 3) < externalWorkingRAM.Length)
                {
                    externalWorkingRAM[address + 3] = (Byte)((value >> 24) & 0xff);
                    externalWorkingRAM[address + 2] = (Byte)((value >> 16) & 0xff);
                    externalWorkingRAM[address + 1] = (Byte)((value >> 8) & 0xff);
                    externalWorkingRAM[address + 0] = (Byte)((value >> 0) & 0xff);
                }
                else
                {
                    Console.WriteLine("Invalid write to external working RAM");
                    Environment.Exit(1);
                }
            }
            else if (0x0300_0000 <= address && (address + 3) < 0x0400_0000)
            {
                address -= 0x0300_0000;

                if ((address + 3) < internalWorkingRAM.Length)
                {
                    internalWorkingRAM[address + 3] = (Byte)((value >> 24) & 0xff);
                    internalWorkingRAM[address + 2] = (Byte)((value >> 16) & 0xff);
                    internalWorkingRAM[address + 1] = (Byte)((value >> 8) & 0xff);
                    internalWorkingRAM[address + 0] = (Byte)((value >> 0) & 0xff);
                }
                else
                {
                    Console.WriteLine("Invalid write to internal working RAM");
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
                        // TODO
                        Console.WriteLine("Write 0x{0:x4} to DISPCNT register (unimplemented)", (UInt16)value);
                        break;

                    // IME
                    case 0x208:
                        // TODO
                        Console.WriteLine("Write 0x{0:x4} to IME register (unimplemented)", (UInt16)value);
                        break;

                    default:
                        Console.WriteLine("Invalid IO register write");
                        Environment.Exit(1);
                        break;
                }
            }
            else
            {
                Console.WriteLine("Invalid write to address 0x{0:x8}", address);
                Environment.Exit(1);
            }
        }

        void DrawFrame()
        {
            // TODO
        }

        public void Run()
        {
            while (true)
            {
                cpu.Step();
                cpu.Step();
                cpu.Step();
                cpu.Step();

                ppu.Step();
            }
        }
    }
}
