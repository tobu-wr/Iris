using Iris.CPU;
using System.Runtime.InteropServices;

namespace Iris.GBA
{
    internal sealed class BIOS_LLE : BIOS
    {
        private const int KB = 1024;
        // private const int BIOS_Size = 16 * KB;
        // private readonly IntPtr _mhh = Marshal.AllocHGlobal(BIOS_Size);

        internal BIOS_LLE(string filename)
        {
            throw new NotImplementedException();
        }

        internal override void Init(CPU_Core cpu, Memory memory)
        {
            throw new NotImplementedException();
        }

        internal override void Reset()
        {
            throw new NotImplementedException();
        }

        internal override byte Read8(uint address)
        {
            throw new NotImplementedException();
        }

        internal override ushort Read16(uint address)
        {
            throw new NotImplementedException();
        }

        internal override uint Read32(uint address)
        {
            throw new NotImplementedException();
        }

        internal override void HandleSWI(uint value)
        {
            throw new NotImplementedException();
        }

        internal override void HandleIRQ()
        {
            throw new NotImplementedException();
        }
    }
}
