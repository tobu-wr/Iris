namespace Iris.EmulationCore.GBA
{
    public sealed partial class Core
    {
        private void BIOS_Reset()
        {
            const UInt32 ROMAddress = 0x800_0000;

            for (int i = 0; i <= 12; ++i)
                _CPU.Reg[i] = 0;

            _CPU.Reg[CPU.CPU.SP] = 0x300_7f00;
            _CPU.Reg[CPU.CPU.LR] = ROMAddress;

            _CPU.Reg13_svc = 0x300_7fe0;
            _CPU.Reg14_svc = 0;
            _CPU.SPSR_svc = 0;

            _CPU.Reg13_irq = 0x300_7fa0;
            _CPU.Reg14_irq = 0;
            _CPU.SPSR_irq = 0;

            _CPU.CPSR = 0x1f;

            _CPU.NextInstructionAddress = ROMAddress;

            for (UInt32 address = 0x300_7e00; address < 0x300_8000; address += 4)
                WriteMemory32(address, 0);
        }

        private Byte BIOS_Read8(UInt32 address)
        {
            return address switch
            {
                // SWI 0xff (fallback to HLE, see ReturnFromIRQ)
                0x138 => 0x00,
                0x139 => 0x00,
                0x13a => 0xff,
                0x13b => 0xef,

                _ => 0,
            };
        }

        private UInt16 BIOS_Read16(UInt32 address)
        {
            return address switch
            {
                // SWI 0xff (fallback to HLE, see ReturnFromIRQ)
                0x138 => 0x0000,
                0x13a => 0xefff,

                _ => 0,
            };
        }

        private UInt32 BIOS_Read32(UInt32 address)
        {
            // SWI 0xff (fallback to HLE, see ReturnFromIRQ)
            return (address == 0x138) ? 0xefff_0000 : 0;
        }

        private void HandleSWI(UInt32 value)
        {
            Byte function = (Byte)((value >> 16) & 0xff);

            switch (function)
            {
                case 0x01:
                    RegisterRamReset();
                    break;
                case 0x02:
                    Halt();
                    break;
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
                case 0x28:
                    SoundDriverVSyncOff();
                    break;
                case 0xff:
                    ReturnFromIRQ();
                    break;
                default:
                    throw new Exception(string.Format("Iris.EmulationCore.GBA.Core.BIOS: Unknown BIOS function 0x{0:x2}", function));
            }
        }

        // IRQ handler start
        private void HandleIRQ()
        {
            _CPU.Reg14_irq = _CPU.NextInstructionAddress + 4;
            _CPU.SPSR_irq = _CPU.CPSR;
            _CPU.SetCPSR((_CPU.CPSR & ~0xbfu) | 0x92u);

            void PushToStack(UInt32 value)
            {
                _CPU.Reg[CPU.CPU.SP] -= 4;
                WriteMemory32(_CPU.Reg[CPU.CPU.SP], value);
            }

            PushToStack(_CPU.Reg[CPU.CPU.LR]);
            PushToStack(_CPU.Reg[12]);
            PushToStack(_CPU.Reg[3]);
            PushToStack(_CPU.Reg[2]);
            PushToStack(_CPU.Reg[1]);
            PushToStack(_CPU.Reg[0]);

            _CPU.Reg[0] = 0x400_0000;
            _CPU.Reg[CPU.CPU.LR] = 0x138;
            _CPU.NextInstructionAddress = ReadMemory32(0x300_7ffc);
        }

        private void RegisterRamReset()
        {
            // TODO
        }

        private void Halt()
        {
            // TODO
        }

        private void VBlankIntrWait()
        {
            // TODO
        }

        private void Div()
        {
            Int32 number = (Int32)_CPU.Reg[0];
            Int32 divisor = (Int32)_CPU.Reg[1];
            _CPU.Reg[0] = (UInt32)(number / divisor);
            _CPU.Reg[1] = (UInt32)(number % divisor);
            _CPU.Reg[3] = (UInt32)Math.Abs((Int32)_CPU.Reg[0]);
        }

        private void CpuSet()
        {
            UInt32 source = _CPU.Reg[0];
            UInt32 destination = _CPU.Reg[1];
            UInt32 length = _CPU.Reg[2] & 0xf_ffff;
            UInt32 fixedSource = (_CPU.Reg[2] >> 24) & 1;
            UInt32 dataSize = (_CPU.Reg[2] >> 26) & 1;

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
            UInt32 source = _CPU.Reg[0];
            UInt32 destination = _CPU.Reg[1];
            UInt32 length = _CPU.Reg[2] & 0xf_ffff;
            UInt32 fixedSource = (_CPU.Reg[2] >> 24) & 1;

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
            UInt32 source = _CPU.Reg[0];
            UInt32 destination = _CPU.Reg[1];

            UInt32 dataHeader = ReadMemory32(source);
            source += 4;

            UInt32 dataSize = dataHeader >> 8;
            UInt32 lastDestination = destination + dataSize;

            while (destination < lastDestination)
            {
                Byte flags = ReadMemory8(source);
                ++source;

                for (int i = 7; i >= 0; --i)
                {
                    Byte blockType = (Byte)((flags >> i) & 1);

                    // uncompressed
                    if (blockType == 0)
                    {
                        WriteMemory16(destination, ReadMemory16(source));
                        destination += 2;
                        source += 2;
                    }

                    // compressed
                    else
                    {
                        UInt16 blockHeader = ReadMemory16(source);
                        source += 2;

                        UInt16 offset = (UInt16)((((blockHeader & 0xf) << 8) | (blockHeader >> 8)) + 1);
                        UInt16 blockSize = (UInt16)(((blockHeader >> 4) & 0xf) + 3);

                        for (int j = 0; j < blockSize; j += 2)
                        {
                            WriteMemory16(destination, ReadMemory16(destination - offset));
                            destination += 2;
                        }
                    }
                }
            }
        }

        private void SoundDriverVSyncOff()
        {
            // TODO
        }

        // IRQ handler end
        private void ReturnFromIRQ()
        {
            UInt32 PopFromStack()
            {
                UInt32 value = ReadMemory32(_CPU.Reg[CPU.CPU.SP]);
                _CPU.Reg[CPU.CPU.SP] += 4;
                return value;
            }

            _CPU.Reg[0] = PopFromStack();
            _CPU.Reg[1] = PopFromStack();
            _CPU.Reg[2] = PopFromStack();
            _CPU.Reg[3] = PopFromStack();
            _CPU.Reg[12] = PopFromStack();
            _CPU.Reg[CPU.CPU.LR] = PopFromStack();

            _CPU.NextInstructionAddress = _CPU.Reg[CPU.CPU.LR] - 4;
            _CPU.SetCPSR(_CPU.SPSR);
        }
    }
}
