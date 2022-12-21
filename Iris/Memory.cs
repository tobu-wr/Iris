using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Iris
{
    public class Memory
    {
        private Byte[]? rom;
        private Byte[] externalWorkingRAM = new Byte[256 * 1024]; // 256 KB

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

        public UInt16 Read16(UInt32 address)
        {
            if (0x0800_0000 <= address && (address + 1) < 0x0A00_0000)
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
            if (0x0800_0000 <= address && (address + 3) < 0x0A00_0000)
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

        public void Write(UInt32 address, UInt32 value)
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
                    Console.WriteLine("Invalid write to address 0x{0:x8}", address);
                    Environment.Exit(1);
                }
            }
            else if (0x0400_0000 <= address && (address + 3) < 0x0500_0000)
            {
                address -= 0x0400_0000;
                switch (address)
                {
                    // IME
                    case 0x208:
                        // TODO
                        Console.WriteLine("Write 0x{0:x8} to IME register (unimplemented)", value, address);
                        break;

                    default:
                        Console.WriteLine("Invalid write to address 0x{0:x8}", address);
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
    }
}
