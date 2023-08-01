using Iris.CPU;

namespace Iris.NDS
{
    public sealed partial class NDS_System
    {
        private void BIOS_Reset()
        {
            const UInt32 ROMAddress = 0x0800_0000;

            // TODO

            _cpu.Reg[CPU_Core.PC] = ROMAddress;
            _cpu.NextInstructionAddress = ROMAddress;
        }

        private void HandleSWI(UInt32 value)
        {
            throw new NotImplementedException("Iris.NDS.Core.BIOS: HandleSWI unimplemented");
        }

        private void HandleIRQ()
        {
            throw new NotImplementedException("Iris.NDS.Core.BIOS: HandleIRQ unimplemented");
        }
    }
}
