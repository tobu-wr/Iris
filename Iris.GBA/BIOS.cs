using Iris.CPU;

namespace Iris.GBA
{
    internal abstract class BIOS
    {
        internal abstract void Initialize(CPU_Core cpu, Memory memory);
        internal abstract void Reset();
        internal abstract Byte Read8(UInt32 address);
        internal abstract UInt16 Read16(UInt32 address);
        internal abstract UInt32 Read32(UInt32 address);
        internal abstract void HandleSWI(UInt32 value);
        internal abstract void HandleIRQ();
    }
}
