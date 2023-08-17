using Iris.CPU;
using System.Runtime.InteropServices;

namespace Iris.GBA
{
    internal sealed class BIOS_LLE : BIOS, IDisposable
    {
        private const int KB = 1024;
        private const int BIOS_Size = 16 * KB;
        private readonly IntPtr _bios = Marshal.AllocHGlobal(BIOS_Size);

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
                _memory.Unmap(0, BIOS_Size);

            Marshal.FreeHGlobal(_bios);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            if (_initialized)
                _memory.Unmap(0, BIOS_Size);

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

            int pageCount = BIOS_Size / Memory.PageSize;
            _memory.Map(_bios, pageCount, 0, BIOS_Size, Memory.Flag.AllRead);

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
