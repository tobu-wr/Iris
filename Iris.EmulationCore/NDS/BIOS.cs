using Iris.EmulationCore.Common;

namespace Iris.EmulationCore.NDS
{
    public sealed partial class Core
    {
        private void BIOS_Reset()
        {
            const UInt32 ROMAddress = 0x0800_0000;

            // TODO

            _cpu.Reg[CPU.PC] = ROMAddress;
            _cpu.NextInstructionAddress = ROMAddress;
        }

        private void HandleSWI(UInt32 value)
        {
            throw new NotImplementedException("Iris.EmulationCore.NDS.Core.BIOS: HandleSWI unimplemented");
        }

        private void HandleIRQ()
        {
            throw new NotImplementedException("Iris.EmulationCore.NDS.Core.BIOS: HandleIRQ unimplemented");
        }
    }
}
