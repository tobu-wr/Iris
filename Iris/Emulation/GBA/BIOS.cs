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
            // end of IRQ handler
            if (address is >= 0x138 and < 0x13c)
            {
                // fallback to HLE (see ReturnFromIRQ())
                Byte[] data = { 0x00, 0x00, 0xff, 0xef }; // SWI 0xff
                return data[address - 0x138];
            }

            return 0;
        }

        private void HandleSWI(UInt32 value)
        {
            Byte function = (Byte)((value >> 16) & 0xff);

            switch (function)
            {
                case 0x05:
                    VBlankIntrWait();
                    break;
                case 0x06:
                    Div();
                    break;
                case 0x0b:
                    CpuSet();
                    break;
                case 0x0c:
                    CpuFastSet();
                    break;
                case 0x12:
                    LZ77UnCompReadNormalWrite16bit();
                    break;
                case 0xff:
                    ReturnFromIRQ();
                    break;
                default:
                    throw new Exception(string.Format("Emulation.GBA.Core: Unknown BIOS function 0x{0:x2}", function));
            }
        }

        private void HandleIRQ()
        {
            // start of IRQ handler

            _cpu.Reg14_irq = _cpu.NextInstructionAddress + 4;
            _cpu.SPSR_irq = _cpu.CPSR;
            _cpu.SetCPSR((_cpu.CPSR & ~0xbfu) | 0x92u);

            void PushToStack(UInt32 value)
            {
                _cpu.Reg[CPU.Core.SP] -= 4;
                WriteMemory32(_cpu.Reg[CPU.Core.SP], value);
            }

            PushToStack(_cpu.Reg[CPU.Core.LR]);
            PushToStack(_cpu.Reg[12]);
            PushToStack(_cpu.Reg[3]);
            PushToStack(_cpu.Reg[2]);
            PushToStack(_cpu.Reg[1]);
            PushToStack(_cpu.Reg[0]);

            _cpu.Reg[0] = 0x400_0000;
            _cpu.Reg[CPU.Core.LR] = 0x138;
            _cpu.NextInstructionAddress = ReadMemory32(0x300_7ffc);
        }

        private void VBlankIntrWait()
        {
            // TODO
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
                    while (destination < lastDestination)
                    {
                        WriteMemory16(destination, ReadMemory16(source));
                        destination += 2;
                        source += 2;
                    }
                }

                // fill
                else
                {
                    UInt16 value = ReadMemory16(source);

                    while (destination < lastDestination)
                    {
                        WriteMemory16(destination, value);
                        destination += 2;
                    }
                }
            }

            // 32 bit
            else
            {
                UInt32 lastDestination = destination + (length * 4);

                // copy
                if (fixedSource == 0)
                {
                    while (destination < lastDestination)
                    {
                        WriteMemory32(destination, ReadMemory32(source));
                        destination += 4;
                        source += 4;
                    }
                }

                // fill
                else
                {
                    UInt32 value = ReadMemory32(source);

                    while (destination < lastDestination)
                    {
                        WriteMemory32(destination, value);
                        destination += 4;
                    }
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
                while (destination < lastDestination)
                {
                    WriteMemory32(destination, ReadMemory32(source));
                    destination += 4;
                    source += 4;
                }
            }

            // fill
            else
            {
                UInt32 value = ReadMemory32(source);

                while (destination < lastDestination)
                {
                    WriteMemory32(destination, value);
                    destination += 4;
                }
            }
        }

        private void LZ77UnCompReadNormalWrite16bit()
        {
            UInt32 source = _cpu.Reg[0];
            UInt32 destination = _cpu.Reg[1];

            UInt32 dataHeader = ReadMemory32(source);
            source += 4;

            UInt32 decompressedDataSize = dataHeader >> 8;
            UInt32 lastDestination = destination + decompressedDataSize;

            while (destination < lastDestination)
            {
                Byte flag = ReadMemory8(source);
                ++source;

                for (int i = 0; i < 8; ++i)
                {
                    Byte blockType = (Byte)((flag << i) & 0x80);

                    // uncompressed
                    if (blockType == 0)
                    {
                        WriteMemory8(destination, ReadMemory8(source));
                        ++destination;
                        ++source;
                    }

                    // compressed
                    else
                    {
                        UInt16 blockHeader = ReadMemory16(source);
                        source += 2;

                        UInt16 disp = (UInt16)((((blockHeader & 0xf) << 8) | (blockHeader >> 8)) + 1);
                        UInt16 blockSize = (UInt16)(((blockHeader >> 4) & 0xf) + 3);

                        for (int j = 0; j < blockSize; ++j)
                        {
                            WriteMemory8(destination, ReadMemory8(destination - disp));
                            ++destination;
                        }
                    }
                }
            }
        }

        private void ReturnFromIRQ()
        {
            // end of IRQ handler

            UInt32 PopFromStack()
            {
                UInt32 value = ReadMemory32(_cpu.Reg[CPU.Core.SP]);
                _cpu.Reg[CPU.Core.SP] += 4;
                return value;
            }

            _cpu.Reg[0] = PopFromStack();
            _cpu.Reg[1] = PopFromStack();
            _cpu.Reg[2] = PopFromStack();
            _cpu.Reg[3] = PopFromStack();
            _cpu.Reg[12] = PopFromStack();
            _cpu.Reg[CPU.Core.LR] = PopFromStack();

            _cpu.NextInstructionAddress = _cpu.Reg[CPU.Core.LR] - 4;
            _cpu.SetCPSR(_cpu.SPSR);
        }
    }
}
