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
            _memory.Unmap(0, BIOS_Size);
            Marshal.FreeHGlobal(_bios);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _memory.Unmap(0, BIOS_Size);
            Marshal.FreeHGlobal(_bios);
            GC.SuppressFinalize(this);
            _disposed = true;
        }

        internal override void Initialize(CPU_Core cpu, Memory memory)
        {
            _cpu = cpu;
            _memory = memory;

            int pageCount = BIOS_Size / Memory.PageSize;
            _memory.Map(_bios, pageCount, 0, BIOS_Size, Memory.Flag.AllRead);
        }

        internal override void Reset()
        {
            _cpu.CPSR = 0xd3;
            _cpu.NextInstructionAddress = 0;
        }

        internal override byte Read8(uint address)
        {
            throw new Exception("Iris.GBA.BIOS_LLE: Unhandled read from BIOS");
        }

        internal override ushort Read16(uint address)
        {
            throw new Exception("Iris.GBA.BIOS_LLE: Unhandled read from BIOS");
        }

        internal override uint Read32(uint address)
        {
            throw new Exception("Iris.GBA.BIOS_LLE: Unhandled read from BIOS");
        }

        internal override void HandleSWI(uint value)
        {
            // TODO
            throw new NotImplementedException();
        }

        internal override void HandleIRQ()
        {
            // TODO
            throw new NotImplementedException();
        }
    }
}
