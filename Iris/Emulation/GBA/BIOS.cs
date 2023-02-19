namespace Iris.Emulation.GBA
{
    internal sealed partial class Core
    {
        private void BIOS_Reset()
        {
            // TODO: Hardware reset rather than software?

            const UInt32 ROMAddress = 0x0800_0000;

            for (int i = 0; i <= 12; ++i)
                _cpu.Reg[i] = 0;

            _cpu.Reg[CPU.Core.SP] = 0x0300_7f00;
            _cpu.Reg[CPU.Core.LR] = ROMAddress;
            _cpu.Reg[CPU.Core.PC] = ROMAddress;

            _cpu.Reg13_svc = 0x0300_7fe0;
            _cpu.Reg13_irq = 0x0300_7fa0;

            _cpu.Reg14_svc = 0;
            _cpu.Reg14_irq = 0;

            _cpu.SPSR_svc = 0;
            _cpu.SPSR_irq = 0;

            _cpu.CPSR = 0x1f;

            _cpu.NextInstructionAddress = ROMAddress;

            for (UInt32 address = 0x0300_7e00; address < 0x0300_8000; address += 4)
                WriteMemory32(address, 0);
        }

        private void HandleSWI(UInt32 value)
        {
            Byte function = (Byte)((value >> 16) & 0xff);

            switch (function)
            {
                case 0x06:
                    Div();
                    break;
                default:
                    throw new Exception(string.Format("Emulation.GBA.Core: Unknown BIOS function 0x{0:x2}", function));
            }
        }

        private void HandleIRQ()
        {
            _cpu.Reg14_irq = _cpu.NextInstructionAddress + 4;
            _cpu.SPSR_irq = _cpu.CPSR;

            _cpu.SetCPSR((_cpu.CPSR & 0xffff_ff40) | 0x92);

            _cpu.Reg[CPU.Core.SP] -= 4;
            WriteMemory32(_cpu.Reg[CPU.Core.SP], _cpu.Reg[0]);
            _cpu.Reg[CPU.Core.SP] -= 4;
            WriteMemory32(_cpu.Reg[CPU.Core.SP], _cpu.Reg[1]);
            _cpu.Reg[CPU.Core.SP] -= 4;
            WriteMemory32(_cpu.Reg[CPU.Core.SP], _cpu.Reg[2]);
            _cpu.Reg[CPU.Core.SP] -= 4;
            WriteMemory32(_cpu.Reg[CPU.Core.SP], _cpu.Reg[3]);
            _cpu.Reg[CPU.Core.SP] -= 4;
            WriteMemory32(_cpu.Reg[CPU.Core.SP], _cpu.Reg[12]);
            _cpu.Reg[CPU.Core.SP] -= 4;
            WriteMemory32(_cpu.Reg[CPU.Core.SP], _cpu.Reg[CPU.Core.LR]);

            const UInt32 UserHandlerAddress = 0x0300_7ffc;

            _cpu.Reg[0] = 0x0400_0000;

            _cpu.Reg[CPU.Core.LR] = 0x0000_0138;
            _cpu.Reg[CPU.Core.PC] = UserHandlerAddress;

            _cpu.NextInstructionAddress = UserHandlerAddress;
        }

        private void Div()
        {
            Int32 number = (Int32)_cpu.Reg[0];
            Int32 denom = (Int32)_cpu.Reg[1];
            _cpu.Reg[0] = (UInt32)(number / denom);
            _cpu.Reg[1] = (UInt32)(number % denom);
            _cpu.Reg[3] = (UInt32)Math.Abs((Int32)_cpu.Reg[0]);
        }
    }
}
