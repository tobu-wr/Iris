namespace Iris.Emulation.GBA
{
    internal sealed partial class Core
    {
        // TODO: SoftReset BIOS function is done atm but we should execute the reset exception handler
        private void BIOS_Reset()
        {
            const UInt32 ROMAddress = 0x800_0000;

            for (int i = 0; i <= 12; ++i)
                _cpu.Reg[i] = 0;

            _cpu.Reg[CPU.Core.SP] = 0x300_7f00;
            _cpu.Reg[CPU.Core.LR] = ROMAddress;

            _cpu.Reg13_svc = 0x300_7fe0;
            _cpu.Reg14_svc = 0;
            _cpu.SPSR_svc = 0;

            _cpu.Reg13_irq = 0x300_7fa0;
            _cpu.Reg14_irq = 0;
            _cpu.SPSR_irq = 0;

            _cpu.CPSR = 0x1f;

            _cpu.NextInstructionAddress = ROMAddress;

            for (UInt32 address = 0x300_7e00; address < 0x300_8000; address += 4)
                WriteMemory32(address, 0);
        }

        private Byte BIOS_Read(UInt32 address)
        {
            // end of BIOS IRQ handler
            if (address is >= 0x138 and < 0x140)
            {
                Byte[] data =
                {
                    0x0f, 0x50, 0xbd, 0xe8, // ldmia sp!,{r0,r1,r2,r3,r12,lr}
                    0x04, 0xf0, 0x5e, 0xe2  // subs pc,lr,#4
                };

                return data[address - 0x138];
            }

            return 0;
        }

        private void HandleSWI(UInt32 value)
        {
            Byte function = (Byte)((value >> 16) & 0xff);

            switch (function)
            {
                case 0x06:
                    Div();
                    break;
                case 0x0b:
                    CpuSet();
                    break;
                case 0x0c:
                    CpuFastSet();
                    break;
                default:
                    throw new Exception(string.Format("Emulation.GBA.Core: Unknown BIOS function 0x{0:x2}", function));
            }
        }

        private void HandleIRQ()
        {
            // start of BIOS IRQ handler

            _cpu.Reg14_irq = _cpu.NextInstructionAddress + 4;
            _cpu.SPSR_irq = _cpu.CPSR;
            _cpu.SetCPSR((_cpu.CPSR & ~0xbfu) | 0x92u);

            _cpu.Reg[CPU.Core.SP] -= 4;
            WriteMemory32(_cpu.Reg[CPU.Core.SP], _cpu.Reg[CPU.Core.LR]);
            _cpu.Reg[CPU.Core.SP] -= 4;
            WriteMemory32(_cpu.Reg[CPU.Core.SP], _cpu.Reg[12]);
            _cpu.Reg[CPU.Core.SP] -= 4;
            WriteMemory32(_cpu.Reg[CPU.Core.SP], _cpu.Reg[3]);
            _cpu.Reg[CPU.Core.SP] -= 4;
            WriteMemory32(_cpu.Reg[CPU.Core.SP], _cpu.Reg[2]);
            _cpu.Reg[CPU.Core.SP] -= 4;
            WriteMemory32(_cpu.Reg[CPU.Core.SP], _cpu.Reg[1]);
            _cpu.Reg[CPU.Core.SP] -= 4;
            WriteMemory32(_cpu.Reg[CPU.Core.SP], _cpu.Reg[0]);

            _cpu.Reg[0] = 0x400_0000;
            _cpu.Reg[CPU.Core.LR] = 0x138;
            _cpu.NextInstructionAddress = ReadMemory32(0x300_7ffc);
        }

        private void Div()
        {
            Int32 number = (Int32)_cpu.Reg[0];
            Int32 divisor = (Int32)_cpu.Reg[1];
            _cpu.Reg[0] = (UInt32)(number / divisor);
            _cpu.Reg[1] = (UInt32)(number % divisor);
            _cpu.Reg[3] = (UInt32)Math.Abs((Int32)_cpu.Reg[0]);
        }

        private void CpuSet()
        {
            UInt32 source = _cpu.Reg[0];
            UInt32 destination = _cpu.Reg[1];
            UInt32 length = _cpu.Reg[2] & 0xf_ffff;
            UInt32 fixedSource = (_cpu.Reg[2] >> 24) & 1;
            UInt32 dataSize = (_cpu.Reg[2] >> 26) & 1;

            // 16 bit
            if (dataSize == 0)
            {
                UInt32 lastDestination = destination + (length * 2);

                // copy
                if (fixedSource == 0)
                {
                    for (; destination != lastDestination; destination += 2, source += 2)
                        WriteMemory16(destination, ReadMemory16(source));
                }

                // fill
                else
                {
                    UInt16 value = ReadMemory16(source);

                    for (; destination != lastDestination; destination += 2)
                        WriteMemory16(destination, value);
                }
            }

            // 32 bit
            else
            {
                UInt32 lastDestination = destination + (length * 4);

                // copy
                if (fixedSource == 0)
                {
                    for (; destination != lastDestination; destination += 4, source += 4)
                        WriteMemory32(destination, ReadMemory32(source));
                }

                // fill
                else
                {
                    UInt32 value = ReadMemory32(source);

                    for (; destination != lastDestination; destination += 4)
                        WriteMemory32(destination, value);
                }
            }
        }

        private void CpuFastSet()
        {
            UInt32 source = _cpu.Reg[0];
            UInt32 destination = _cpu.Reg[1];
            UInt32 length = _cpu.Reg[2] & 0xf_ffff;
            UInt32 fixedSource = (_cpu.Reg[2] >> 24) & 1;

            // round-up length to multiple of 8
            if ((length & 7) != 0)
                length = (length & ~7u) + 8;

            UInt32 lastDestination = destination + (length * 4);

            // copy
            if (fixedSource == 0)
            {
                for (; destination != lastDestination; destination += 4, source += 4)
                    WriteMemory32(destination, ReadMemory32(source));
            }

            // fill
            else
            {
                UInt32 value = ReadMemory32(source);

                for (; destination != lastDestination; destination += 4)
                    WriteMemory32(destination, value);
            }
        }
    }
}
