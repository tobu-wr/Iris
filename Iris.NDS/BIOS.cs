namespace Iris.NDS
{
    public sealed partial class NDS_System
    {
        private void BIOS_Reset()
        {
            const UInt32 ROMAddress = 0x0800_0000;

            // TODO

            _cpu.Reg[CPU.CPU_Core.PC] = ROMAddress;
            _cpu.NextInstructionAddress = ROMAddress;
        }
    }
}
