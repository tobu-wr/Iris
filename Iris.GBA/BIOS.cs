using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Iris.GBA
{
    internal sealed class BIOS : IDisposable
    {
        private const int KB = 1024;
        private const int Size = 16 * KB;
        private readonly IntPtr _data = Marshal.AllocHGlobal(Size);

        private const UInt32 StartAddress = 0x0000_0000;
        private const UInt32 EndAddress = 0x0000_4000;

        private CPU.CPU_Core _cpu;
        private bool _disposed;

        internal BIOS()
        {
            Byte[] data;

            try
            {
                data = File.ReadAllBytes("gba_data.bin");
            }
            catch (FileNotFoundException)
            {
                throw new Exception("Iris.GBA.BIOS: Could not find dump file");
            }
            catch
            {
                throw new Exception("Iris.GBA.BIOS: Could not read dump file");
            }

            if (data.Length != Size)
                throw new Exception("Iris.GBA.BIOS: Wrong size");

            if (Convert.ToHexString(MD5.HashData(data)) != "A860E8C0B6D573D191E4EC7DB1B1E4F6")
                throw new Exception("Iris.GBA.BIOS: Wrong hash");

            Marshal.Copy(data, 0, _data, Size);
        }

        ~BIOS()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            Marshal.FreeHGlobal(_data);

            GC.SuppressFinalize(this);
            _disposed = true;
        }

        internal void Initialize(CPU.CPU_Core cpu, Memory memory)
        {
            _cpu = cpu;

            memory.Map(_data, Size, StartAddress, EndAddress, Memory.Flag.AllRead);
        }

        internal void Reset(bool skipIntro)
        {
            if (skipIntro)
            {
                _cpu.Reg[CPU.CPU_Core.SP] = 0x300_7f00;
                _cpu.Reg[CPU.CPU_Core.LR] = 0x800_0000;

                _cpu.CPSR = 0x1f;

                _cpu.Reg13_svc = 0x300_7fe0;
                _cpu.Reg13_irq = 0x300_7fa0;

                _cpu.NextInstructionAddress = 0x800_0000;
            }
            else
            {
                _cpu.CPSR = 0xd3;
                _cpu.NextInstructionAddress = 0;
            }
        }

        internal UInt64 HandleSWI()
        {
            _cpu.Reg14_svc = _cpu.NextInstructionAddress;
            _cpu.SPSR_svc = _cpu.CPSR;
            _cpu.SetCPSR((_cpu.CPSR & ~0xbfu) | 0x93u);
            _cpu.NextInstructionAddress = 0x08;
            return 3;
        }

        internal UInt64 HandleIRQ()
        {
            _cpu.Reg14_irq = _cpu.NextInstructionAddress + 4;
            _cpu.SPSR_irq = _cpu.CPSR;
            _cpu.SetCPSR((_cpu.CPSR & ~0xbfu) | 0x92u);
            _cpu.NextInstructionAddress = 0x18;
            return 3;
        }
    }
}
