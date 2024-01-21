using Iris.CPU;

namespace Iris.GBA
{
    internal sealed class BIOS_HLE : BIOS
    {
        private CPU_Core? _cpu;
        private Communication _communication;
        private Memory? _memory;

        internal override void Initialize(CPU_Core cpu, Communication communication, Memory memory)
        {
            _cpu = cpu;
            _communication = communication;
            _memory = memory;
        }

        internal override void Reset()
        {
            const UInt32 ROMAddress = 0x800_0000;

            for (int i = 0; i <= 12; ++i)
                _cpu!.Reg[i] = 0;

            _cpu!.Reg[CPU_Core.SP] = 0x300_7f00;
            _cpu.Reg[CPU_Core.LR] = ROMAddress;

            _cpu.Reg13_svc = 0x300_7fe0;
            _cpu.Reg14_svc = 0;
            _cpu.SPSR_svc = 0;

            _cpu.Reg13_irq = 0x300_7fa0;
            _cpu.Reg14_irq = 0;
            _cpu.SPSR_irq = 0;

            _cpu.CPSR = 0x1f;

            _cpu.NextInstructionAddress = ROMAddress;

            for (UInt32 address = 0x300_7e00; address < 0x300_8000; address += 4)
                _memory!.Write32(address, 0);

            // TODO: check register values after bios intro
            _communication.WriteRegister(Communication.Register.RCNT, 0x8000, Memory.RegisterWriteMode.HalfWord);
        }

        internal override Byte Read8(UInt32 address)
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

        internal override UInt16 Read16(UInt32 address)
        {
            return address switch
            {
                // SWI 0xff (fallback to HLE, see ReturnFromIRQ)
                0x138 => 0x0000,
                0x13a => 0xefff,

                _ => 0,
            };
        }

        internal override UInt32 Read32(UInt32 address)
        {
            // SWI 0xff (fallback to HLE, see ReturnFromIRQ)
            return (address == 0x138) ? 0xefff_0000 : 0;
        }

        internal override UInt32 HandleSWI()
        {
            Byte function = _memory.Read8(_cpu.NextInstructionAddress - 2);

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
                    throw new Exception(string.Format("Iris.GBA.BIOS: Unknown BIOS function 0x{0:x2}", function));
            }

            // TODO: fix cycle count
            return 3;
        }

        // IRQ handler start
        internal override UInt32 HandleIRQ()
        {
            _cpu!.Reg14_irq = _cpu.NextInstructionAddress + 4;
            _cpu.SPSR_irq = _cpu.CPSR;
            _cpu.SetCPSR((_cpu.CPSR & ~0xbfu) | 0x92u);

            void PushToStack(UInt32 value)
            {
                _cpu.Reg[CPU_Core.SP] -= 4;
                _memory!.Write32(_cpu.Reg[CPU_Core.SP], value);
            }

            PushToStack(_cpu.Reg[CPU_Core.LR]);
            PushToStack(_cpu.Reg[12]);
            PushToStack(_cpu.Reg[3]);
            PushToStack(_cpu.Reg[2]);
            PushToStack(_cpu.Reg[1]);
            PushToStack(_cpu.Reg[0]);

            _cpu.Reg[0] = 0x400_0000;
            _cpu.Reg[CPU_Core.LR] = 0x138;
            _cpu.NextInstructionAddress = _memory!.Read32(0x300_7ffc);

            // TODO: fix cycle count
            return 3;
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
            Int32 number = (Int32)_cpu!.Reg[0];
            Int32 divisor = (Int32)_cpu.Reg[1];
            _cpu.Reg[0] = (UInt32)(number / divisor);
            _cpu.Reg[1] = (UInt32)(number % divisor);
            _cpu.Reg[3] = (UInt32)Math.Abs((Int32)_cpu.Reg[0]);
        }

        private void CpuSet()
        {
            UInt32 source = _cpu!.Reg[0];
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
                        _memory!.Write16(destination, _memory.Read16(source));
                        destination += 2;
                        source += 2;
                    }
                }

                // fill
                else
                {
                    UInt16 value = _memory!.Read16(source);

                    while (destination < lastDestination)
                    {
                        _memory.Write16(destination, value);
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
                        _memory!.Write32(destination, _memory.Read32(source));
                        destination += 4;
                        source += 4;
                    }
                }

                // fill
                else
                {
                    UInt32 value = _memory!.Read32(source);

                    while (destination < lastDestination)
                    {
                        _memory.Write32(destination, value);
                        destination += 4;
                    }
                }
            }
        }

        private void CpuFastSet()
        {
            UInt32 source = _cpu!.Reg[0];
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
                    _memory!.Write32(destination, _memory.Read32(source));
                    destination += 4;
                    source += 4;
                }
            }

            // fill
            else
            {
                UInt32 value = _memory!.Read32(source);

                while (destination < lastDestination)
                {
                    _memory.Write32(destination, value);
                    destination += 4;
                }
            }
        }

        private void LZ77UnCompReadNormalWrite16bit()
        {
            UInt32 source = _cpu!.Reg[0];
            UInt32 destination = _cpu.Reg[1];

            UInt32 dataHeader = _memory!.Read32(source);
            source += 4;

            UInt32 dataSize = dataHeader >> 8;
            UInt32 lastDestination = destination + dataSize;

            while (destination < lastDestination)
            {
                Byte flags = _memory.Read8(source);
                ++source;

                for (int i = 7; i >= 0; --i)
                {
                    Byte blockType = (Byte)((flags >> i) & 1);

                    // uncompressed
                    if (blockType == 0)
                    {
                        _memory.Write16(destination, _memory.Read16(source));
                        destination += 2;
                        source += 2;
                    }

                    // compressed
                    else
                    {
                        UInt16 blockHeader = _memory.Read16(source);
                        source += 2;

                        UInt16 offset = (UInt16)((((blockHeader & 0xf) << 8) | (blockHeader >> 8)) + 1);
                        UInt16 blockSize = (UInt16)(((blockHeader >> 4) & 0xf) + 3);

                        for (int j = 0; j < blockSize; j += 2)
                        {
                            _memory.Write16(destination, _memory.Read16(destination - offset));
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
                UInt32 value = _memory!.Read32(_cpu.Reg[CPU_Core.SP]);
                _cpu.Reg[CPU_Core.SP] += 4;
                return value;
            }

            _cpu!.Reg[0] = PopFromStack();
            _cpu.Reg[1] = PopFromStack();
            _cpu.Reg[2] = PopFromStack();
            _cpu.Reg[3] = PopFromStack();
            _cpu.Reg[12] = PopFromStack();
            _cpu.Reg[CPU_Core.LR] = PopFromStack();

            _cpu.NextInstructionAddress = _cpu.Reg[CPU_Core.LR] - 4;
            _cpu.SetCPSR(_cpu.SPSR);
        }
    }
}
