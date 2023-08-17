using Iris.CPU;
using System.Runtime.InteropServices;

namespace Iris.GBA
{
    internal sealed class BIOS_LLE : BIOS, IDisposable
    {
        private const int KB = 1024;
        private const int BIOS_Size = 16 * KB;
        private readonly IntPtr _bios = Marshal.AllocHGlobal(BIOS_Size);

        private const UInt32 BIOS_StartAddress = 0x0000_0000;
        private const UInt32 BIOS_EndAddress = 0x0000_4000;

        private CPU_Core _cpu;
        private Memory _memory;

        private bool _initialized;
        private bool _disposed;

        internal BIOS_LLE(string filename)
        {
            Byte[] data;

            try
            {
                data = File.ReadAllBytes(filename);
            }
            catch
            {
                throw new Exception("Iris.GBA.BIOS_LLE: Could not load BIOS");
            }

            if (data.Length != BIOS_Size)
                throw new Exception("Iris.GBA.BIOS_LLE: Wrong BIOS size");

            Marshal.Copy(data, 0, _bios, BIOS_Size);
        }

        ~BIOS_LLE()
        {
            if (_initialized)
                _memory.Unmap(BIOS_StartAddress, BIOS_EndAddress);

            Marshal.FreeHGlobal(_bios);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            if (_initialized)
                _memory.Unmap(BIOS_StartAddress, BIOS_EndAddress);

            Marshal.FreeHGlobal(_bios);

            GC.SuppressFinalize(this);
            _disposed = true;
        }

        internal override void Initialize(CPU_Core cpu, Memory memory)
        {
            if (_initialized)
                return;

            _cpu = cpu;
            _memory = memory;

            _memory.Map(_bios, BIOS_Size / Memory.PageSize, BIOS_StartAddress, BIOS_EndAddress, Memory.Flag.AllRead);

            _initialized = true;
        }

        internal override void Reset()
        {
            _cpu.CPSR = 0xd3;
            _cpu.NextInstructionAddress = 0;
        }

        internal override Byte Read8(UInt32 address)
        {
            throw new Exception("Iris.GBA.BIOS_LLE: BIOS is not correctly mapped to memory");
        }

        internal override UInt16 Read16(UInt32 address)
        {
            throw new Exception("Iris.GBA.BIOS_LLE: BIOS is not correctly mapped to memory");
        }

        internal override UInt32 Read32(UInt32 address)
        {
            throw new Exception("Iris.GBA.BIOS_LLE: BIOS is not correctly mapped to memory");
        }

        internal override UInt32 HandleSWI()
        {
            _cpu.Reg14_svc = _cpu.NextInstructionAddress;
            _cpu.SPSR_svc = _cpu.CPSR;
            _cpu.SetCPSR((_cpu.CPSR & ~0xbfu) | 0x93u);
            _cpu.NextInstructionAddress = 0x08;
            return 3;
        }

        internal override UInt32 HandleIRQ()
        {
            _cpu.Reg14_irq = _cpu.NextInstructionAddress + 4;
            _cpu.SPSR_irq = _cpu.CPSR;
            _cpu.SetCPSR((_cpu.CPSR & ~0xbfu) | 0x92u);
            _cpu.NextInstructionAddress = 0x18;
            return 3;
        }
    }
}
