namespace Iris.EmulationCore.NDS
{
    public sealed partial class Core
    {
        private const int KB = 1024;

        private Byte[]? _ROM;

        internal void LoadROM(string filename)
        {
            _ROM = File.ReadAllBytes(filename);
        }

        private Byte ReadMemory8(UInt32 address)
        {
            throw new NotImplementedException("Iris.EmulationCore.NDS.Core.Memory: ReadMemory8 unimplemented");
        }

        private UInt16 ReadMemory16(UInt32 address)
        {
            throw new NotImplementedException("Iris.EmulationCore.NDS.Core.Memory: ReadMemory16 unimplemented");
        }

        private UInt32 ReadMemory32(UInt32 address)
        {
            throw new NotImplementedException("Iris.EmulationCore.NDS.Core.Memory: ReadMemory32 unimplemented");
        }

        private void WriteMemory8(UInt32 address, Byte value)
        {
            throw new NotImplementedException("Iris.EmulationCore.NDS.Core.Memory: ReadMemory32 unimplemented");
        }

        private void WriteMemory16(UInt32 address, UInt16 value)
        {
            throw new NotImplementedException("Iris.EmulationCore.NDS.Core.Memory: ReadMemory32 unimplemented");
        }

        private void WriteMemory32(UInt32 address, UInt32 value)
        {
            throw new NotImplementedException("Iris.EmulationCore.NDS.Core.Memory: ReadMemory32 unimplemented");
        }
    }
}
