﻿using System.Numerics;

namespace Iris.CPU
{
    internal sealed class ARM_Interpreter
    {
        private readonly struct InstructionListEntry
        {
            internal readonly UInt32 Mask;
            internal readonly UInt32 Expected;
            internal unsafe readonly delegate*<CPU, UInt32, void> Handler;

            internal unsafe InstructionListEntry(UInt32 mask, UInt32 expected, delegate*<CPU, UInt32, void> handler)
            {
                Mask = mask;
                Expected = expected;
                Handler = handler;
            }
        }

        private unsafe readonly delegate*<CPU, UInt32, void>[] InstructionLUT = new delegate*<CPU, UInt32, void>[1 << 12];

        private static UInt32 InstructionLUTHash(UInt32 value)
        {
            return ((value >> 16) & 0xff0) | ((value >> 4) & 0x00f);
        }

        private unsafe void InitInstructionLUT()
        {
            InstructionListEntry[] InstructionList = new InstructionListEntry[]
            {
                // ADC
                new(0x0fe0_0000, 0x02a0_0000, &ADC), // I bit is 1
                new(0x0fe0_0090, 0x00a0_0000, &ADC), // I bit is 0, bit[7] is 0 and bit[4] is 0
                new(0x0fe0_0090, 0x00a0_0080, &ADC), // I bit is 0, bit[7] is 1 and bit[4] is 0
                new(0x0fe0_0090, 0x00a0_0010, &ADC), // I bit is 0, bit[7] is 0 and bit[4] is 1

                // ADD
                new(0x0fe0_0000, 0x0280_0000, &ADD), // I bit is 1
                new(0x0fe0_0090, 0x0080_0000, &ADD), // I bit is 0, bit[7] is 0 and bit[4] is 0
                new(0x0fe0_0090, 0x0080_0080, &ADD), // I bit is 0, bit[7] is 1 and bit[4] is 0
                new(0x0fe0_0090, 0x0080_0010, &ADD), // I bit is 0, bit[7] is 0 and bit[4] is 1

                // AND
                new(0x0fe0_0000, 0x0200_0000, &AND), // I bit is 1
                new(0x0fe0_0090, 0x0000_0000, &AND), // I bit is 0, bit[7] is 0 and bit[4] is 0
                new(0x0fe0_0090, 0x0000_0080, &AND), // I bit is 0, bit[7] is 1 and bit[4] is 0
                new(0x0fe0_0090, 0x0000_0010, &AND), // I bit is 0, bit[7] is 0 and bit[4] is 1

                // B
                new(0x0f00_0000, 0x0a00_0000, &B),

                // BL
                new(0x0f00_0000, 0x0b00_0000, &BL),

                // BIC
                new(0x0fe0_0000, 0x03c0_0000, &BIC), // I bit is 1
                new(0x0fe0_0090, 0x01c0_0000, &BIC), // I bit is 0, bit[7] is 0 and bit[4] is 0
                new(0x0fe0_0090, 0x01c0_0080, &BIC), // I bit is 0, bit[7] is 1 and bit[4] is 0
                new(0x0fe0_0090, 0x01c0_0010, &BIC), // I bit is 0, bit[7] is 0 and bit[4] is 1

                // BX
                new(0x0fff_fff0, 0x012f_ff10, &BX),

                // CMN
                new(0x0ff0_f000, 0x0370_0000, &CMN), // I bit is 1
                new(0x0ff0_f090, 0x0170_0000, &CMN), // I bit is 0, bit[7] is 0 and bit[4] is 0
                new(0x0ff0_f090, 0x0170_0080, &CMN), // I bit is 0, bit[7] is 1 and bit[4] is 0
                new(0x0ff0_f090, 0x0170_0010, &CMN), // I bit is 0, bit[7] is 0 and bit[4] is 1
                new(0x0ff0_f000, 0x0370_f000, &CMN), // I bit is 1
                new(0x0ff0_f090, 0x0170_f000, &CMN), // I bit is 0, bit[7] is 0 and bit[4] is 0
                new(0x0ff0_f090, 0x0170_f080, &CMN), // I bit is 0, bit[7] is 1 and bit[4] is 0
                new(0x0ff0_f090, 0x0170_f010, &CMN), // I bit is 0, bit[7] is 0 and bit[4] is 1

                // CMP
                new(0x0ff0_f000, 0x0350_0000, &CMP), // I bit is 1
                new(0x0ff0_f090, 0x0150_0000, &CMP), // I bit is 0, bit[7] is 0 and bit[4] is 0
                new(0x0ff0_f090, 0x0150_0080, &CMP), // I bit is 0, bit[7] is 1 and bit[4] is 0
                new(0x0ff0_f090, 0x0150_0010, &CMP), // I bit is 0, bit[7] is 0 and bit[4] is 1
                new(0x0ff0_f000, 0x0350_f000, &CMP), // I bit is 1
                new(0x0ff0_f090, 0x0150_f000, &CMP), // I bit is 0, bit[7] is 0 and bit[4] is 0
                new(0x0ff0_f090, 0x0150_f080, &CMP), // I bit is 0, bit[7] is 1 and bit[4] is 0
                new(0x0ff0_f090, 0x0150_f010, &CMP), // I bit is 0, bit[7] is 0 and bit[4] is 1

                // EOR
                new(0x0fe0_0000, 0x0220_0000, &EOR), // I bit is 1
                new(0x0fe0_0090, 0x0020_0000, &EOR), // I bit is 0, bit[7] is 0 and bit[4] is 0
                new(0x0fe0_0090, 0x0020_0080, &EOR), // I bit is 0, bit[7] is 1 and bit[4] is 0
                new(0x0fe0_0090, 0x0020_0010, &EOR), // I bit is 0, bit[7] is 0 and bit[4] is 1

                // LDM
                new(0x0e50_0000, 0x0810_0000, &LDM1),
                new(0x0e50_8000, 0x0850_0000, &LDM2),
                //new(0x0e50_8000, 0x0850_8000, &LDM3),

                // LDR
                new(0x0c50_0000, 0x0410_0000, &LDR),

                // LDRB
                new(0x0c50_0000, 0x0450_0000, &LDRB),

                // LDRH
                new(0x0e10_00f0, 0x0010_00b0, &LDRH),

                // LDRSB
                new(0x0e10_00f0, 0x0010_00d0, &LDRSB),

                // LDRSH
                new(0x0e10_00f0, 0x0010_00f0, &LDRSH),

                // MLA
                new(0x0fe0_00f0, 0x0020_0090, &MLA),

                // MOV
                new(0x0fef_0000, 0x03a0_0000, &MOV), // I bit is 1
                new(0x0fef_0090, 0x01a0_0000, &MOV), // I bit is 0, bit[7] is 0 and bit[4] is 0
                new(0x0fef_0090, 0x01a0_0080, &MOV), // I bit is 0, bit[7] is 1 and bit[4] is 0
                new(0x0fef_0090, 0x01a0_0010, &MOV), // I bit is 0, bit[7] is 0 and bit[4] is 1

                // MRS
                new(0x0fbf_0fff, 0x010f_0000, &MRS),

                // MSR
                new(0x0fb0_f000, 0x0320_f000, &MSR), // Immediate operand
                new(0x0fb0_fff0, 0x0120_f000, &MSR), // Register operand

                // MUL
                new(0x0fe0_f0f0, 0x0000_0090, &MUL),

                // MVN
                new(0x0fef_0000, 0x03e0_0000, &MVN), // I bit is 1
                new(0x0fef_0090, 0x01e0_0000, &MVN), // I bit is 0, bit[7] is 0 and bit[4] is 0
                new(0x0fef_0090, 0x01e0_0080, &MVN), // I bit is 0, bit[7] is 1 and bit[4] is 0
                new(0x0fef_0090, 0x01e0_0010, &MVN), // I bit is 0, bit[7] is 0 and bit[4] is 1

                // ORR
                new(0x0fe0_0000, 0x0380_0000, &ORR), // I bit is 1
                new(0x0fe0_0090, 0x0180_0000, &ORR), // I bit is 0, bit[7] is 0 and bit[4] is 0
                new(0x0fe0_0090, 0x0180_0080, &ORR), // I bit is 0, bit[7] is 1 and bit[4] is 0
                new(0x0fe0_0090, 0x0180_0010, &ORR), // I bit is 0, bit[7] is 0 and bit[4] is 1

                // RSB
                new(0x0fe0_0000, 0x0260_0000, &RSB), // I bit is 1
                new(0x0fe0_0090, 0x0060_0000, &RSB), // I bit is 0, bit[7] is 0 and bit[4] is 0
                new(0x0fe0_0090, 0x0060_0080, &RSB), // I bit is 0, bit[7] is 1 and bit[4] is 0
                new(0x0fe0_0090, 0x0060_0010, &RSB), // I bit is 0, bit[7] is 0 and bit[4] is 1

                // RSC
                new(0x0fe0_0000, 0x02e0_0000, &RSC), // I bit is 1
                new(0x0fe0_0090, 0x00e0_0000, &RSC), // I bit is 0, bit[7] is 0 and bit[4] is 0
                new(0x0fe0_0090, 0x00e0_0080, &RSC), // I bit is 0, bit[7] is 1 and bit[4] is 0
                new(0x0fe0_0090, 0x00e0_0010, &RSC), // I bit is 0, bit[7] is 0 and bit[4] is 1

                // SBC
                new(0x0fe0_0000, 0x02c0_0000, &SBC), // I bit is 1
                new(0x0fe0_0090, 0x00c0_0000, &SBC), // I bit is 0, bit[7] is 0 and bit[4] is 0
                new(0x0fe0_0090, 0x00c0_0080, &SBC), // I bit is 0, bit[7] is 1 and bit[4] is 0
                new(0x0fe0_0090, 0x00c0_0010, &SBC), // I bit is 0, bit[7] is 0 and bit[4] is 1

                // SMLAL
                new(0x0fe0_00f0, 0x00e0_0090, &SMLAL),

                // SMULL
                new(0x0fe0_00f0, 0x00c0_0090, &SMULL),

                // STM
                new(0x0e50_0000, 0x0800_0000, &STM1),
                new(0x0e70_0000, 0x0840_0000, &STM2),

                // STR
                new(0x0c50_0000, 0x0400_0000, &STR),

                // STRB
                new(0x0c50_0000, 0x0440_0000, &STRB),

                // STRH
                new(0x0e10_00f0, 0x0000_00b0, &STRH),

                // SUB
                new(0x0fe0_0000, 0x0240_0000, &SUB), // I bit is 1
                new(0x0fe0_0090, 0x0040_0000, &SUB), // I bit is 0, bit[7] is 0 and bit[4] is 0
                new(0x0fe0_0090, 0x0040_0080, &SUB), // I bit is 0, bit[7] is 1 and bit[4] is 0
                new(0x0fe0_0090, 0x0040_0010, &SUB), // I bit is 0, bit[7] is 0 and bit[4] is 1

                // SWI
                new(0x0f00_0000, 0x0f00_0000, &SWI),

                // SWP
                new(0x0ff0_0ff0, 0x0100_0090, &SWP),

                // SWPB
                new(0x0ff0_0ff0, 0x0140_0090, &SWPB),

                // TEQ
                new(0x0ff0_f000, 0x0330_0000, &TEQ), // I bit is 1
                new(0x0ff0_f090, 0x0130_0000, &TEQ), // I bit is 0, bit[7] is 0 and bit[4] is 0
                new(0x0ff0_f090, 0x0130_0080, &TEQ), // I bit is 0, bit[7] is 1 and bit[4] is 0
                new(0x0ff0_f090, 0x0130_0010, &TEQ), // I bit is 0, bit[7] is 0 and bit[4] is 1
                new(0x0ff0_f000, 0x0330_f000, &TEQ), // I bit is 1
                new(0x0ff0_f090, 0x0130_f000, &TEQ), // I bit is 0, bit[7] is 0 and bit[4] is 0
                new(0x0ff0_f090, 0x0130_f080, &TEQ), // I bit is 0, bit[7] is 1 and bit[4] is 0
                new(0x0ff0_f090, 0x0130_f010, &TEQ), // I bit is 0, bit[7] is 0 and bit[4] is 1

                // TST
                new(0x0ff0_f000, 0x0310_0000, &TST), // I bit is 1
                new(0x0ff0_f090, 0x0110_0000, &TST), // I bit is 0, bit[7] is 0 and bit[4] is 0
                new(0x0ff0_f090, 0x0110_0080, &TST), // I bit is 0, bit[7] is 1 and bit[4] is 0
                new(0x0ff0_f090, 0x0110_0010, &TST), // I bit is 0, bit[7] is 0 and bit[4] is 1
                new(0x0ff0_f000, 0x0310_f000, &TST), // I bit is 1
                new(0x0ff0_f090, 0x0110_f000, &TST), // I bit is 0, bit[7] is 0 and bit[4] is 0
                new(0x0ff0_f090, 0x0110_f080, &TST), // I bit is 0, bit[7] is 1 and bit[4] is 0
                new(0x0ff0_f090, 0x0110_f010, &TST), // I bit is 0, bit[7] is 0 and bit[4] is 1

                // UMLAL
                new(0x0fe0_00f0, 0x00a0_0090, &UMLAL),

                // UMULL
                new(0x0fe0_00f0, 0x0080_0090, &UMULL),
            };

            for (UInt64 instruction = 0; instruction < (UInt64)InstructionLUT.LongLength; ++instruction)
            {
                bool unknownInstruction = true;

                foreach (InstructionListEntry entry in InstructionList)
                {
                    if ((instruction & InstructionLUTHash(entry.Mask)) == InstructionLUTHash(entry.Expected))
                    {
                        InstructionLUT[instruction] = entry.Handler;
                        unknownInstruction = false;
                        break;
                    }
                }

                if (unknownInstruction)
                    InstructionLUT[instruction] = &UNKNOWN;
            }
        }

        private readonly CPU _cpu;

        internal ARM_Interpreter(CPU cpu)
        {
            _cpu = cpu;
            InitInstructionLUT();
        }

        internal void Step()
        {
            UInt32 instruction = _cpu._callbackInterface.ReadMemory32(_cpu.NextInstructionAddress);
            _cpu.NextInstructionAddress += 4;

            UInt32 cond = (instruction >> 28) & 0b1111;

            if (_cpu.ConditionPassed(cond))
            {
                _cpu.Reg[CPU.PC] = _cpu.NextInstructionAddress + 4;

                unsafe
                {
                    InstructionLUT[InstructionLUTHash(instruction)](_cpu, instruction);
                }
            }
        }

        private static void SetPC(CPU cpu, UInt32 value)
        {
            cpu.NextInstructionAddress = value;
        }

        private static void SetReg(CPU cpu, UInt32 i, UInt32 value)
        {
            if (i == CPU.PC)
                SetPC(cpu, value);
            else
                cpu.Reg[i] = value;
        }

        // Addressing mode 1
        private static (UInt32 shifterOperand, UInt32 shifterCarryOut) GetShifterOperand(CPU cpu, UInt32 instruction)
        {
            UInt32 i = (instruction >> 25) & 1;

            UInt32 shifterOperand = 0;
            UInt32 shifterCarryOut = 0;

            if (i == 1) // 32-bit immediate
            {
                UInt32 rotateImm = (instruction >> 8) & 0b1111;
                UInt32 imm = instruction & 0xff;

                shifterOperand = BitOperations.RotateRight(imm, (int)(rotateImm * 2));
                shifterCarryOut = (rotateImm == 0) ? cpu.GetFlag(CPU.Flag.C) : (shifterOperand >> 31);
            }
            else
            {
                UInt32 shift = (instruction >> 5) & 0b11;
                UInt32 r = (instruction >> 4) & 1;
                UInt32 rm = instruction & 0b1111;

                if (r == 0) // Immediate shifts
                {
                    UInt32 shiftImm = (instruction >> 7) & 0b1_1111;

                    UInt32 value = cpu.Reg[rm];
                    int shiftAmount = (int)shiftImm;

                    switch (shift)
                    {
                        case 0b00: // Logical shift left
                            if (shiftAmount == 0)
                            {
                                shifterOperand = value;
                                shifterCarryOut = cpu.GetFlag(CPU.Flag.C);
                            }
                            else
                            {
                                shifterOperand = value << shiftAmount;
                                shifterCarryOut = (value >> (32 - shiftAmount)) & 1;
                            }
                            break;
                        case 0b01: // Logical shift right
                            if (shiftAmount == 0)
                            {
                                shifterOperand = 0;
                                shifterCarryOut = value >> 31;
                            }
                            else
                            {
                                shifterOperand = value >> shiftAmount;
                                shifterCarryOut = (value >> (shiftAmount - 1)) & 1;
                            }
                            break;
                        case 0b10: // Arithmetic shift right
                            if (shiftAmount == 0)
                            {
                                shifterOperand = ((value >> 31) == 0) ? 0 : 0xffff_ffff;
                                shifterCarryOut = value >> 31;
                            }
                            else
                            {
                                shifterOperand = CPU.ArithmeticShiftRight(value, shiftAmount);
                                shifterCarryOut = (value >> (shiftAmount - 1)) & 1;
                            }
                            break;
                        case 0b11: // Rotate right
                            if (shiftAmount == 0)
                            {
                                shifterOperand = (cpu.GetFlag(CPU.Flag.C) << 31) | (value >> 1);
                                shifterCarryOut = value & 1;
                            }
                            else
                            {
                                shifterOperand = BitOperations.RotateRight(value, shiftAmount);
                                shifterCarryOut = (value >> (shiftAmount - 1)) & 1;
                            }
                            break;
                    }
                }
                else // Register shifts
                {
                    UInt32 rs = (instruction >> 8) & 0b1111;

                    UInt32 value = (rm == CPU.PC) ? (cpu.Reg[CPU.PC] + 4) : cpu.Reg[rm];
                    int shiftAmount = (int)(cpu.Reg[rs] & 0xff);

                    switch (shift)
                    {
                        case 0b00: // Logical shift left
                            if (shiftAmount == 0)
                            {
                                shifterOperand = value;
                                shifterCarryOut = cpu.GetFlag(CPU.Flag.C);
                            }
                            else if (shiftAmount < 32)
                            {
                                shifterOperand = value << shiftAmount;
                                shifterCarryOut = (value >> (32 - shiftAmount)) & 1;
                            }
                            else if (shiftAmount == 32)
                            {
                                shifterOperand = 0;
                                shifterCarryOut = value & 1;
                            }
                            else
                            {
                                shifterOperand = 0;
                                shifterCarryOut = 0;
                            }
                            break;
                        case 0b01: // Logical shift right
                            if (shiftAmount == 0)
                            {
                                shifterOperand = value;
                                shifterCarryOut = cpu.GetFlag(CPU.Flag.C);
                            }
                            else if (shiftAmount < 32)
                            {
                                shifterOperand = value >> shiftAmount;
                                shifterCarryOut = (value >> (shiftAmount - 1)) & 1;
                            }
                            else if (shiftAmount == 32)
                            {
                                shifterOperand = 0;
                                shifterCarryOut = value >> 31;
                            }
                            else
                            {
                                shifterOperand = 0;
                                shifterCarryOut = 0;
                            }
                            break;
                        case 0b10: // Arithmetic shift right
                            if (shiftAmount == 0)
                            {
                                shifterOperand = value;
                                shifterCarryOut = cpu.GetFlag(CPU.Flag.C);
                            }
                            else if (shiftAmount < 32)
                            {
                                shifterOperand = CPU.ArithmeticShiftRight(value, shiftAmount);
                                shifterCarryOut = (value >> (shiftAmount - 1)) & 1;
                            }
                            else
                            {
                                shifterOperand = ((value >> 31) == 0) ? 0 : 0xffff_ffff;
                                shifterCarryOut = value >> 31;
                            }
                            break;
                        case 0b11: // Rotate right
                            if (shiftAmount == 0)
                            {
                                shifterOperand = value;
                                shifterCarryOut = cpu.GetFlag(CPU.Flag.C);
                            }
                            else if ((shiftAmount & 0b1_1111) == 0)
                            {
                                shifterOperand = value;
                                shifterCarryOut = value >> 31;
                            }
                            else
                            {
                                shifterOperand = BitOperations.RotateRight(value, shiftAmount & 0b1_1111);
                                shifterCarryOut = (value >> ((shiftAmount & 0b1_1111) - 1)) & 1;
                            }
                            break;
                    }
                }
            }

            return (shifterOperand, shifterCarryOut);
        }

        // Addressing mode 2
        private static UInt32 GetAddress(CPU cpu, UInt32 instruction)
        {
            UInt32 i = (instruction >> 25) & 1;
            UInt32 p = (instruction >> 24) & 1;
            UInt32 u = (instruction >> 23) & 1;
            UInt32 w = (instruction >> 21) & 1;
            UInt32 rn = (instruction >> 16) & 0b1111;

            UInt32 index = 0;

            if (i == 0) // Immediate
            {
                UInt32 offset = instruction & 0xfff;

                index = offset;
            }
            else
            {
                UInt32 shiftImm = (instruction >> 7) & 0b1_1111;
                UInt32 shift = (instruction >> 5) & 0b11;
                UInt32 rm = instruction & 0b1111;

                if ((shiftImm == 0) && (shift == 0)) // Register
                {
                    index = cpu.Reg[rm];
                }
                else // Scaled register
                {
                    switch (shift)
                    {
                        case 0b00: // LSL
                            index = cpu.Reg[rm] << (int)shiftImm;
                            break;
                        case 0b01: // LSR
                            if (shiftImm == 0)
                                index = 0;
                            else
                                index = cpu.Reg[rm] >> (int)shiftImm;
                            break;
                        case 0b10: // ASR
                            if (shiftImm == 0)
                                index = ((cpu.Reg[rm] >> 31) == 1) ? 0xffff_ffff : 0;
                            else
                                index = CPU.ArithmeticShiftRight(cpu.Reg[rm], (int)shiftImm);
                            break;
                        case 0b11:
                            if (shiftImm == 0) // RRX
                                index = (cpu.GetFlag(CPU.Flag.C) << 31) | (cpu.Reg[rm] >> 1);
                            else // ROR
                                index = BitOperations.RotateRight(cpu.Reg[rm], (int)shiftImm);
                            break;
                    }
                }
            }

            UInt32 regRnIndexed = (u == 1) ? (cpu.Reg[rn] + index) : (cpu.Reg[rn] - index);

            UInt32 address;

            if (p == 0) // Post-indexed
            {
                address = cpu.Reg[rn];
                SetReg(cpu, rn, regRnIndexed);
            }
            else if (w == 0) // Offset
            {
                address = regRnIndexed;
            }
            else // Pre-indexed
            {
                address = regRnIndexed;
                SetReg(cpu, rn, regRnIndexed);
            }

            return address;
        }

        // Addressing mode 3
        private static UInt32 GetAddress_Misc(CPU cpu, UInt32 instruction)
        {
            UInt32 p = (instruction >> 24) & 1;
            UInt32 u = (instruction >> 23) & 1;
            UInt32 i = (instruction >> 22) & 1;
            UInt32 w = (instruction >> 21) & 1;
            UInt32 rn = (instruction >> 16) & 0b1111;

            UInt32 index;

            if (i == 1) // Immediate
            {
                UInt32 immH = (instruction >> 8) & 0b1111;
                UInt32 immL = instruction & 0b1111;

                UInt32 offset = (immH << 4) | immL;
                index = offset;
            }
            else // Register
            {
                UInt32 rm = instruction & 0b1111;

                index = cpu.Reg[rm];
            }

            UInt32 regRnIndexed = (u == 1) ? (cpu.Reg[rn] + index) : (cpu.Reg[rn] - index);

            UInt32 address;

            if (p == 0) // Post-indexed
            {
                address = cpu.Reg[rn];
                SetReg(cpu, rn, regRnIndexed);
            }
            else if (w == 0) // Offset
            {
                address = regRnIndexed;
            }
            else // Pre-indexed
            {
                address = regRnIndexed;
                SetReg(cpu, rn, regRnIndexed);
            }

            return address;
        }

        // Addressing mode 4
        private static (UInt32 startAddress, UInt32 endAddress) GetAddress_Multiple(CPU cpu, UInt32 instruction)
        {
            UInt32 p = (instruction >> 24) & 1;
            UInt32 u = (instruction >> 23) & 1;
            UInt32 w = (instruction >> 21) & 1;
            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 registerList = instruction & 0xffff;

            UInt32 increment = (registerList == 0) ? 0x40 : ((UInt32)BitOperations.PopCount(registerList) * 4);

            UInt32 startAddress, endAddress;
            UInt32 value;

            if (u == 1) // increment
            {
                value = cpu.Reg[rn] + increment;

                if (p == 0) // after
                {
                    startAddress = cpu.Reg[rn];
                    endAddress = value - 4;
                }
                else // before
                {
                    startAddress = cpu.Reg[rn] + 4;
                    endAddress = value;
                }
            }
            else // decrement
            {
                value = cpu.Reg[rn] - increment;

                if (p == 0) // after
                {
                    startAddress = value + 4;
                    endAddress = cpu.Reg[rn];
                }
                else // before
                {
                    startAddress = value;
                    endAddress = cpu.Reg[rn] - 4;
                }
            }

            if (w == 1)
                SetReg(cpu, rn, value);

            return (startAddress, endAddress);
        }

        private static void UNKNOWN(CPU cpu, UInt32 instruction)
        {
            throw new Exception(string.Format("Iris.CPU.ARM_Interpreter: Unknown ARM instruction 0x{0:x8} at address 0x{1:x8}", instruction, cpu.NextInstructionAddress - 4));
        }

        private static void ADC(CPU cpu, UInt32 instruction)
        {
            UInt32 i = (instruction >> 25) & 1;
            UInt32 s = (instruction >> 20) & 1;
            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 rd = (instruction >> 12) & 0b1111;
            UInt32 r = (instruction >> 4) & 1;

            (UInt32 shifterOperand, _) = GetShifterOperand(cpu, instruction);

            UInt32 leftOperand = ((rn == CPU.PC) && (i == 0) && (r == 1)) ? (cpu.Reg[CPU.PC] + 4) : cpu.Reg[rn];
            UInt64 rightOperand = (UInt64)shifterOperand + (UInt64)cpu.GetFlag(CPU.Flag.C);

            UInt64 result = (UInt64)leftOperand + rightOperand;
            SetReg(cpu, rd, (UInt32)result);

            if (s == 1)
            {
                if (rd == CPU.PC)
                {
                    cpu.SetCPSR(cpu.SPSR);
                }
                else
                {
                    cpu.SetFlag(CPU.Flag.N, cpu.Reg[rd] >> 31);
                    cpu.SetFlag(CPU.Flag.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
                    cpu.SetFlag(CPU.Flag.C, CPU.CarryFrom(result));
                    cpu.SetFlag(CPU.Flag.V, CPU.OverflowFrom_Addition(leftOperand, (UInt32)rightOperand, cpu.Reg[rd]));
                }
            }
        }

        private static void ADD(CPU cpu, UInt32 instruction)
        {
            UInt32 i = (instruction >> 25) & 1;
            UInt32 s = (instruction >> 20) & 1;
            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 rd = (instruction >> 12) & 0b1111;
            UInt32 r = (instruction >> 4) & 1;

            (UInt32 shifterOperand, _) = GetShifterOperand(cpu, instruction);

            UInt32 leftOperand = ((rn == CPU.PC) && (i == 0) && (r == 1)) ? (cpu.Reg[CPU.PC] + 4) : cpu.Reg[rn];
            UInt32 rightOperand = shifterOperand;

            UInt64 result = (UInt64)leftOperand + (UInt64)rightOperand;
            SetReg(cpu, rd, (UInt32)result);

            if (s == 1)
            {
                if (rd == CPU.PC)
                {
                    cpu.SetCPSR(cpu.SPSR);
                }
                else
                {
                    cpu.SetFlag(CPU.Flag.N, cpu.Reg[rd] >> 31);
                    cpu.SetFlag(CPU.Flag.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
                    cpu.SetFlag(CPU.Flag.C, CPU.CarryFrom(result));
                    cpu.SetFlag(CPU.Flag.V, CPU.OverflowFrom_Addition(leftOperand, rightOperand, cpu.Reg[rd]));
                }
            }
        }

        private static void AND(CPU cpu, UInt32 instruction)
        {
            UInt32 i = (instruction >> 25) & 1;
            UInt32 s = (instruction >> 20) & 1;
            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 rd = (instruction >> 12) & 0b1111;
            UInt32 r = (instruction >> 4) & 1;

            (UInt32 shifterOperand, UInt32 shifterCarryOut) = GetShifterOperand(cpu, instruction);

            UInt32 leftOperand = ((rn == CPU.PC) && (i == 0) && (r == 1)) ? (cpu.Reg[CPU.PC] + 4) : cpu.Reg[rn];
            UInt32 rightOperand = shifterOperand;

            SetReg(cpu, rd, leftOperand & rightOperand);

            if (s == 1)
            {
                if (rd == CPU.PC)
                {
                    cpu.SetCPSR(cpu.SPSR);
                }
                else
                {
                    cpu.SetFlag(CPU.Flag.N, cpu.Reg[rd] >> 31);
                    cpu.SetFlag(CPU.Flag.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
                    cpu.SetFlag(CPU.Flag.C, shifterCarryOut);
                }
            }
        }

        private static void B(CPU cpu, UInt32 instruction)
        {
            UInt32 imm = instruction & 0xff_ffff;

            SetPC(cpu, cpu.Reg[CPU.PC] + (CPU.SignExtend(imm, 24) << 2));
        }

        private static void BL(CPU cpu, UInt32 instruction)
        {
            UInt32 imm = instruction & 0xff_ffff;

            cpu.Reg[CPU.LR] = cpu.NextInstructionAddress;
            SetPC(cpu, cpu.Reg[CPU.PC] + (CPU.SignExtend(imm, 24) << 2));
        }

        private static void BIC(CPU cpu, UInt32 instruction)
        {
            UInt32 i = (instruction >> 25) & 1;
            UInt32 s = (instruction >> 20) & 1;
            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 rd = (instruction >> 12) & 0b1111;
            UInt32 r = (instruction >> 4) & 1;

            (UInt32 shifterOperand, UInt32 shifterCarryOut) = GetShifterOperand(cpu, instruction);

            UInt32 leftOperand = ((rn == CPU.PC) && (i == 0) && (r == 1)) ? (cpu.Reg[CPU.PC] + 4) : cpu.Reg[rn];
            UInt32 rightOperand = shifterOperand;

            SetReg(cpu, rd, leftOperand & ~rightOperand);

            if (s == 1)
            {
                if (rd == CPU.PC)
                {
                    cpu.SetCPSR(cpu.SPSR);
                }
                else
                {
                    cpu.SetFlag(CPU.Flag.N, cpu.Reg[rd] >> 31);
                    cpu.SetFlag(CPU.Flag.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
                    cpu.SetFlag(CPU.Flag.C, shifterCarryOut);
                }
            }
        }

        private static void BX(CPU cpu, UInt32 instruction)
        {
            UInt32 rm = instruction & 0b1111;

            cpu.CPSR = (cpu.CPSR & ~(1u << 5)) | ((cpu.Reg[rm] & 1) << 5);
            SetPC(cpu, cpu.Reg[rm] & 0xffff_fffe);
        }

        private static void CMN(CPU cpu, UInt32 instruction)
        {
            UInt32 i = (instruction >> 25) & 1;
            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 rd = (instruction >> 12) & 0b1111;
            UInt32 r = (instruction >> 4) & 1;

            (UInt32 shifterOperand, _) = GetShifterOperand(cpu, instruction);

            UInt32 leftOperand = ((rn == CPU.PC) && (i == 0) && (r == 1)) ? (cpu.Reg[CPU.PC] + 4) : cpu.Reg[rn];
            UInt32 rightOperand = shifterOperand;

            UInt64 result = (UInt64)leftOperand + (UInt64)rightOperand;
            UInt32 aluOut = (UInt32)result;

            if (rd == CPU.PC)
            {
                if ((cpu.CPSR & CPU.ModeMask) == CPU.UserMode)
                    cpu.CPSR = (cpu.CPSR & ~0xf000_0000) | (aluOut & 0xf000_0000);
                else
                    cpu.SetCPSR((cpu.CPSR & ~0xf000_00c3) | (aluOut & 0xf000_0003) | (((aluOut >> 26) & 0b11) << 6));
            }
            else
            {
                cpu.SetFlag(CPU.Flag.N, aluOut >> 31);
                cpu.SetFlag(CPU.Flag.Z, (aluOut == 0) ? 1u : 0u);
                cpu.SetFlag(CPU.Flag.C, CPU.CarryFrom(result));
                cpu.SetFlag(CPU.Flag.V, CPU.OverflowFrom_Addition(leftOperand, rightOperand, aluOut));
            }
        }

        private static void CMP(CPU cpu, UInt32 instruction)
        {
            UInt32 i = (instruction >> 25) & 1;
            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 rd = (instruction >> 12) & 0b1111;
            UInt32 r = (instruction >> 4) & 1;

            (UInt32 shifterOperand, _) = GetShifterOperand(cpu, instruction);

            UInt32 leftOperand = ((rn == CPU.PC) && (i == 0) && (r == 1)) ? (cpu.Reg[CPU.PC] + 4) : cpu.Reg[rn];
            UInt32 rightOperand = shifterOperand;

            UInt32 aluOut = leftOperand - rightOperand;

            if (rd == CPU.PC)
            {
                if ((cpu.CPSR & CPU.ModeMask) == CPU.UserMode)
                    cpu.CPSR = (cpu.CPSR & ~0xf000_0000) | (aluOut & 0xf000_0000);
                else
                    cpu.SetCPSR((cpu.CPSR & ~0xf000_00c3) | (aluOut & 0xf000_0003) | (((aluOut >> 26) & 0b11) << 6));
            }
            else
            {
                cpu.SetFlag(CPU.Flag.N, aluOut >> 31);
                cpu.SetFlag(CPU.Flag.Z, (aluOut == 0) ? 1u : 0u);
                cpu.SetFlag(CPU.Flag.C, CPU.Not(CPU.BorrowFrom(leftOperand, rightOperand)));
                cpu.SetFlag(CPU.Flag.V, CPU.OverflowFrom_Subtraction(leftOperand, rightOperand, aluOut));
            }
        }

        private static void EOR(CPU cpu, UInt32 instruction)
        {
            UInt32 i = (instruction >> 25) & 1;
            UInt32 s = (instruction >> 20) & 1;
            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 rd = (instruction >> 12) & 0b1111;
            UInt32 r = (instruction >> 4) & 1;

            (UInt32 shifterOperand, UInt32 shifterCarryOut) = GetShifterOperand(cpu, instruction);

            UInt32 leftOperand = ((rn == CPU.PC) && (i == 0) && (r == 1)) ? (cpu.Reg[CPU.PC] + 4) : cpu.Reg[rn];
            UInt32 rightOperand = shifterOperand;

            SetReg(cpu, rd, leftOperand ^ rightOperand);

            if (s == 1)
            {
                if (rd == CPU.PC)
                {
                    cpu.SetCPSR(cpu.SPSR);
                }
                else
                {
                    cpu.SetFlag(CPU.Flag.N, cpu.Reg[rd] >> 31);
                    cpu.SetFlag(CPU.Flag.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
                    cpu.SetFlag(CPU.Flag.C, shifterCarryOut);
                }
            }
        }

        private static void LDM1(CPU cpu, UInt32 instruction)
        {
            UInt32 registerList = instruction & 0xffff;

            (UInt32 startAddress, _) = GetAddress_Multiple(cpu, instruction);

            UInt32 address = startAddress;

            if (registerList == 0)
            {
                SetPC(cpu, cpu._callbackInterface.ReadMemory32(address));
            }
            else
            {
                for (int i = 0; i <= 14; ++i)
                {
                    if (((registerList >> i) & 1) == 1)
                    {
                        cpu.Reg[i] = cpu._callbackInterface.ReadMemory32(address);
                        address += 4;
                    }
                }

                if (((registerList >> 15) & 1) == 1)
                    SetPC(cpu, cpu._callbackInterface.ReadMemory32(address) & 0xffff_fffc);
            }
        }

        private static void LDM2(CPU cpu, UInt32 instruction)
        {
            UInt32 registerList = instruction & 0x7fff;

            (UInt32 startAddress, _) = GetAddress_Multiple(cpu, instruction);

            UInt32 address = startAddress;

            if (registerList == 0)
            {
                SetPC(cpu, cpu._callbackInterface.ReadMemory32(address));
            }
            else
            {
                for (int i = 0; i <= 14; ++i)
                {
                    if (((registerList >> i) & 1) == 1)
                    {
                        UInt32 value = cpu._callbackInterface.ReadMemory32(address);

                        switch (i)
                        {
                            case 8:
                                cpu.Reg8_usr = value;
                                break;
                            case 9:
                                cpu.Reg9_usr = value;
                                break;
                            case 10:
                                cpu.Reg10_usr = value;
                                break;
                            case 11:
                                cpu.Reg11_usr = value;
                                break;
                            case 12:
                                cpu.Reg12_usr = value;
                                break;
                            case 13:
                                cpu.Reg13_usr = value;
                                break;
                            case 14:
                                cpu.Reg14_usr = value;
                                break;
                            default:
                                cpu.Reg[i] = value;
                                break;
                        }

                        address += 4;
                    }
                }
            }
        }

        private static void LDR(CPU cpu, UInt32 instruction)
        {
            UInt32 rd = (instruction >> 12) & 0b1111;

            UInt32 address = GetAddress(cpu, instruction);
            UInt32 data = BitOperations.RotateRight(cpu._callbackInterface.ReadMemory32(address), (int)(8 * (address & 0b11)));

            if (rd == CPU.PC)
                SetPC(cpu, data & 0xffff_fffc);
            else
                cpu.Reg[rd] = data;
        }

        private static void LDRB(CPU cpu, UInt32 instruction)
        {
            UInt32 rd = (instruction >> 12) & 0b1111;

            UInt32 address = GetAddress(cpu, instruction);
            Byte data = cpu._callbackInterface.ReadMemory8(address);
            SetReg(cpu, rd, data);
        }

        private static void LDRH(CPU cpu, UInt32 instruction)
        {
            UInt32 rd = (instruction >> 12) & 0b1111;

            UInt32 address = GetAddress_Misc(cpu, instruction);
            UInt32 data = BitOperations.RotateRight(cpu._callbackInterface.ReadMemory16(address), (int)(8 * (address & 1)));
            SetReg(cpu, rd, data);
        }

        private static void LDRSB(CPU cpu, UInt32 instruction)
        {
            UInt32 rd = (instruction >> 12) & 0b1111;

            UInt32 address = GetAddress_Misc(cpu, instruction);
            Byte data = cpu._callbackInterface.ReadMemory8(address);
            SetReg(cpu, rd, CPU.SignExtend(data, 8));
        }

        private static void LDRSH(CPU cpu, UInt32 instruction)
        {
            UInt32 rd = (instruction >> 12) & 0b1111;

            UInt32 address = GetAddress_Misc(cpu, instruction);

            if ((address & 1) == 1)
            {
                Byte data = cpu._callbackInterface.ReadMemory8(address);
                SetReg(cpu, rd, CPU.SignExtend(data, 8));
            }
            else
            {
                UInt16 data = cpu._callbackInterface.ReadMemory16(address);
                SetReg(cpu, rd, CPU.SignExtend(data, 16));
            }
        }

        private static void MLA(CPU cpu, UInt32 instruction)
        {
            UInt32 s = (instruction >> 20) & 1;
            UInt32 rd = (instruction >> 16) & 0b1111;
            UInt32 rn = (instruction >> 12) & 0b1111;
            UInt32 rs = (instruction >> 8) & 0b1111;
            UInt32 rm = instruction & 0b1111;

            SetReg(cpu, rd, (cpu.Reg[rm] * cpu.Reg[rs]) + cpu.Reg[rn]);

            if (s == 1)
            {
                cpu.SetFlag(CPU.Flag.N, cpu.Reg[rd] >> 31);
                cpu.SetFlag(CPU.Flag.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
            }
        }

        private static void MOV(CPU cpu, UInt32 instruction)
        {
            UInt32 s = (instruction >> 20) & 1;
            UInt32 rd = (instruction >> 12) & 0b1111;

            (UInt32 shifterOperand, UInt32 shifterCarryOut) = GetShifterOperand(cpu, instruction);

            SetReg(cpu, rd, shifterOperand);

            if (s == 1)
            {
                if (rd == CPU.PC)
                {
                    cpu.SetCPSR(cpu.SPSR);
                }
                else
                {
                    cpu.SetFlag(CPU.Flag.N, cpu.Reg[rd] >> 31);
                    cpu.SetFlag(CPU.Flag.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
                    cpu.SetFlag(CPU.Flag.C, shifterCarryOut);
                }
            }
        }

        private static void MRS(CPU cpu, UInt32 instruction)
        {
            UInt32 r = (instruction >> 22) & 1;
            UInt32 rd = (instruction >> 12) & 0b1111;

            SetReg(cpu, rd, (r == 1) ? cpu.SPSR : cpu.CPSR);
        }

        private static void MSR(CPU cpu, UInt32 instruction)
        {
            UInt32 i = (instruction >> 25) & 1;
            UInt32 r = (instruction >> 22) & 1;
            UInt32 fieldMask = (instruction >> 16) & 0b1111;

            UInt32 operand;

            if (i == 1)
            {
                UInt32 rotateImm = (instruction >> 8) & 0b1111;
                UInt32 imm = instruction & 0xff;

                operand = BitOperations.RotateRight(imm, (int)(2 * rotateImm));
            }
            else
            {
                UInt32 rm = instruction & 0b1111;

                operand = cpu.Reg[rm];
            }

            UInt32 mask = (UInt32)((((fieldMask >> 0) & 1) == 1) ? 0x0000_00ff : 0)
                        | (UInt32)((((fieldMask >> 1) & 1) == 1) ? 0x0000_ff00 : 0)
                        | (UInt32)((((fieldMask >> 2) & 1) == 1) ? 0x00ff_0000 : 0)
                        | (UInt32)((((fieldMask >> 3) & 1) == 1) ? 0xff00_0000 : 0);

            if (r == 0)
            {
                if ((cpu.CPSR & CPU.ModeMask) == CPU.UserMode)
                {
                    mask &= 0xff00_0000;
                    cpu.CPSR = (cpu.CPSR & ~mask) | (operand & mask);
                }
                else
                {
                    cpu.SetCPSR((cpu.CPSR & ~mask) | (operand & mask));
                }
            }
            else
            {
                cpu.SPSR = (cpu.SPSR & ~mask) | (operand & mask);
            }
        }

        private static void MUL(CPU cpu, UInt32 instruction)
        {
            UInt32 s = (instruction >> 20) & 1;
            UInt32 rd = (instruction >> 16) & 0b1111;
            UInt32 rs = (instruction >> 8) & 0b1111;
            UInt32 rm = instruction & 0b1111;

            SetReg(cpu, rd, cpu.Reg[rm] * cpu.Reg[rs]);

            if (s == 1)
            {
                cpu.SetFlag(CPU.Flag.N, cpu.Reg[rd] >> 31);
                cpu.SetFlag(CPU.Flag.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
            }
        }

        private static void MVN(CPU cpu, UInt32 instruction)
        {
            UInt32 s = (instruction >> 20) & 1;
            UInt32 rd = (instruction >> 12) & 0b1111;

            (UInt32 shifterOperand, UInt32 shifterCarryOut) = GetShifterOperand(cpu, instruction);

            SetReg(cpu, rd, ~shifterOperand);

            if (s == 1)
            {
                if (rd == CPU.PC)
                {
                    cpu.SetCPSR(cpu.SPSR);
                }
                else
                {
                    cpu.SetFlag(CPU.Flag.N, cpu.Reg[rd] >> 31);
                    cpu.SetFlag(CPU.Flag.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
                    cpu.SetFlag(CPU.Flag.C, shifterCarryOut);
                }
            }
        }

        private static void ORR(CPU cpu, UInt32 instruction)
        {
            UInt32 i = (instruction >> 25) & 1;
            UInt32 s = (instruction >> 20) & 1;
            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 rd = (instruction >> 12) & 0b1111;
            UInt32 r = (instruction >> 4) & 1;

            (UInt32 shifterOperand, UInt32 shifterCarryOut) = GetShifterOperand(cpu, instruction);

            UInt32 leftOperand = ((rn == CPU.PC) && (i == 0) && (r == 1)) ? (cpu.Reg[CPU.PC] + 4) : cpu.Reg[rn];
            UInt32 rightOperand = shifterOperand;

            SetReg(cpu, rd, leftOperand | rightOperand);

            if (s == 1)
            {
                if (rd == CPU.PC)
                {
                    cpu.SetCPSR(cpu.SPSR);
                }
                else
                {
                    cpu.SetFlag(CPU.Flag.N, cpu.Reg[rd] >> 31);
                    cpu.SetFlag(CPU.Flag.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
                    cpu.SetFlag(CPU.Flag.C, shifterCarryOut);
                }
            }
        }

        private static void RSB(CPU cpu, UInt32 instruction)
        {
            UInt32 i = (instruction >> 25) & 1;
            UInt32 s = (instruction >> 20) & 1;
            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 rd = (instruction >> 12) & 0b1111;
            UInt32 r = (instruction >> 4) & 1;

            (UInt32 shifterOperand, _) = GetShifterOperand(cpu, instruction);

            UInt32 leftOperand = shifterOperand;
            UInt32 rightOperand = ((rn == CPU.PC) && (i == 0) && (r == 1)) ? (cpu.Reg[CPU.PC] + 4) : cpu.Reg[rn];

            SetReg(cpu, rd, leftOperand - rightOperand);

            if (s == 1)
            {
                if (rd == CPU.PC)
                {
                    cpu.SetCPSR(cpu.SPSR);
                }
                else
                {
                    cpu.SetFlag(CPU.Flag.N, cpu.Reg[rd] >> 31);
                    cpu.SetFlag(CPU.Flag.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
                    cpu.SetFlag(CPU.Flag.C, CPU.Not(CPU.BorrowFrom(leftOperand, rightOperand)));
                    cpu.SetFlag(CPU.Flag.V, CPU.OverflowFrom_Subtraction(leftOperand, rightOperand, cpu.Reg[rd]));
                }
            }
        }

        private static void RSC(CPU cpu, UInt32 instruction)
        {
            UInt32 i = (instruction >> 25) & 1;
            UInt32 s = (instruction >> 20) & 1;
            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 rd = (instruction >> 12) & 0b1111;
            UInt32 r = (instruction >> 4) & 1;

            (UInt32 shifterOperand, _) = GetShifterOperand(cpu, instruction);

            UInt32 leftOperand = shifterOperand;
            UInt64 rightOperand = (UInt64)(((rn == CPU.PC) && (i == 0) && (r == 1)) ? (cpu.Reg[CPU.PC] + 4) : cpu.Reg[rn])
                                + (UInt64)CPU.Not(cpu.GetFlag(CPU.Flag.C));

            SetReg(cpu, rd, leftOperand - (UInt32)rightOperand);

            if (s == 1)
            {
                if (rd == CPU.PC)
                {
                    cpu.SetCPSR(cpu.SPSR);
                }
                else
                {
                    cpu.SetFlag(CPU.Flag.N, cpu.Reg[rd] >> 31);
                    cpu.SetFlag(CPU.Flag.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
                    cpu.SetFlag(CPU.Flag.C, CPU.Not(CPU.BorrowFrom(leftOperand, rightOperand)));
                    cpu.SetFlag(CPU.Flag.V, CPU.OverflowFrom_Subtraction(leftOperand, (UInt32)rightOperand, cpu.Reg[rd]));
                }
            }
        }

        private static void SBC(CPU cpu, UInt32 instruction)
        {
            UInt32 i = (instruction >> 25) & 1;
            UInt32 s = (instruction >> 20) & 1;
            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 rd = (instruction >> 12) & 0b1111;
            UInt32 r = (instruction >> 4) & 1;

            (UInt32 shifterOperand, _) = GetShifterOperand(cpu, instruction);

            UInt32 leftOperand = ((rn == CPU.PC) && (i == 0) && (r == 1)) ? (cpu.Reg[CPU.PC] + 4) : cpu.Reg[rn];
            UInt64 rightOperand = (UInt64)shifterOperand + (UInt64)CPU.Not(cpu.GetFlag(CPU.Flag.C));

            SetReg(cpu, rd, leftOperand - (UInt32)rightOperand);

            if (s == 1)
            {
                if (rd == CPU.PC)
                {
                    cpu.SetCPSR(cpu.SPSR);
                }
                else
                {
                    cpu.SetFlag(CPU.Flag.N, cpu.Reg[rd] >> 31);
                    cpu.SetFlag(CPU.Flag.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
                    cpu.SetFlag(CPU.Flag.C, CPU.Not(CPU.BorrowFrom(leftOperand, rightOperand)));
                    cpu.SetFlag(CPU.Flag.V, CPU.OverflowFrom_Subtraction(leftOperand, (UInt32)rightOperand, cpu.Reg[rd]));
                }
            }
        }

        private static void SMLAL(CPU cpu, UInt32 instruction)
        {
            UInt32 s = (instruction >> 20) & 1;
            UInt32 rdHi = (instruction >> 16) & 0b1111;
            UInt32 rdLo = (instruction >> 12) & 0b1111;
            UInt32 rs = (instruction >> 8) & 0b1111;
            UInt32 rm = instruction & 0b1111;

            Int64 result = (Int64)(Int32)cpu.Reg[rm] * (Int64)(Int32)cpu.Reg[rs];
            UInt64 resultLo = (UInt64)(UInt32)result + (UInt64)cpu.Reg[rdLo];
            UInt32 resultHi = (UInt32)(result >> 32) + cpu.Reg[rdHi] + CPU.CarryFrom(resultLo);
            SetReg(cpu, rdLo, (UInt32)resultLo);
            SetReg(cpu, rdHi, resultHi);

            if (s == 1)
            {
                cpu.SetFlag(CPU.Flag.N, cpu.Reg[rdHi] >> 31);
                cpu.SetFlag(CPU.Flag.Z, ((cpu.Reg[rdHi] == 0) && (cpu.Reg[rdLo] == 0)) ? 1u : 0u);
            }
        }

        private static void SMULL(CPU cpu, UInt32 instruction)
        {
            UInt32 s = (instruction >> 20) & 1;
            UInt32 rdHi = (instruction >> 16) & 0b1111;
            UInt32 rdLo = (instruction >> 12) & 0b1111;
            UInt32 rs = (instruction >> 8) & 0b1111;
            UInt32 rm = instruction & 0b1111;

            Int64 result = (Int64)(Int32)cpu.Reg[rm] * (Int64)(Int32)cpu.Reg[rs];
            SetReg(cpu, rdLo, (UInt32)result);
            SetReg(cpu, rdHi, (UInt32)(result >> 32));

            if (s == 1)
            {
                cpu.SetFlag(CPU.Flag.N, cpu.Reg[rdHi] >> 31);
                cpu.SetFlag(CPU.Flag.Z, ((cpu.Reg[rdHi] == 0) && (cpu.Reg[rdLo] == 0)) ? 1u : 0u);
            }
        }

        private static void STM1(CPU cpu, UInt32 instruction)
        {
            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 registerList = instruction & 0xffff;

            UInt32 oldRegRn = cpu.Reg[rn];

            (UInt32 startAddress, _) = GetAddress_Multiple(cpu, instruction);

            UInt32 address = startAddress;

            if (registerList == 0)
            {
                cpu._callbackInterface.WriteMemory32(address, cpu.Reg[CPU.PC] + 4);
            }
            else
            {
                for (int i = 0; i <= 14; ++i)
                {
                    if (((registerList >> i) & 1) == 1)
                    {
                        if ((i == rn) && ((registerList & ~(0xffff << i)) == 0))
                            cpu._callbackInterface.WriteMemory32(address, oldRegRn);
                        else
                            cpu._callbackInterface.WriteMemory32(address, cpu.Reg[i]);

                        address += 4;
                    }
                }

                if (((registerList >> 15) & 1) == 1)
                    cpu._callbackInterface.WriteMemory32(address, cpu.Reg[CPU.PC] + 4);
            }
        }

        private static void STM2(CPU cpu, UInt32 instruction)
        {
            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 registerList = instruction & 0xffff;

            UInt32 oldRegRn = rn switch
            {
                8 => cpu.Reg8_usr,
                9 => cpu.Reg9_usr,
                10 => cpu.Reg10_usr,
                11 => cpu.Reg11_usr,
                12 => cpu.Reg12_usr,
                13 => cpu.Reg13_usr,
                14 => cpu.Reg14_usr,
                _ => cpu.Reg[rn],
            };

            (UInt32 startAddress, _) = GetAddress_Multiple(cpu, instruction);

            UInt32 address = startAddress;

            if (registerList == 0)
            {
                cpu._callbackInterface.WriteMemory32(address, cpu.Reg[CPU.PC] + 4);
            }
            else
            {
                for (int i = 0; i <= 14; ++i)
                {
                    if (((registerList >> i) & 1) == 1)
                    {
                        if ((i == rn) && ((registerList & ~(0xffff << i)) == 0))
                        {
                            cpu._callbackInterface.WriteMemory32(address, oldRegRn);
                        }
                        else
                        {
                            UInt32 value = i switch
                            {
                                8 => cpu.Reg8_usr,
                                9 => cpu.Reg9_usr,
                                10 => cpu.Reg10_usr,
                                11 => cpu.Reg11_usr,
                                12 => cpu.Reg12_usr,
                                13 => cpu.Reg13_usr,
                                14 => cpu.Reg14_usr,
                                _ => cpu.Reg[i],
                            };

                            cpu._callbackInterface.WriteMemory32(address, value);
                        }

                        address += 4;
                    }
                }

                if (((registerList >> 15) & 1) == 1)
                    cpu._callbackInterface.WriteMemory32(address, cpu.Reg[CPU.PC] + 4);
            }
        }

        private static void STR(CPU cpu, UInt32 instruction)
        {
            UInt32 rd = (instruction >> 12) & 0b1111;

            UInt32 data = (rd == CPU.PC) ? (cpu.Reg[CPU.PC] + 4) : cpu.Reg[rd];
            UInt32 address = GetAddress(cpu, instruction);
            cpu._callbackInterface.WriteMemory32(address, data);
        }

        private static void STRB(CPU cpu, UInt32 instruction)
        {
            UInt32 rd = (instruction >> 12) & 0b1111;

            UInt32 data = (rd == CPU.PC) ? (cpu.Reg[CPU.PC] + 4) : cpu.Reg[rd];
            UInt32 address = GetAddress(cpu, instruction);
            cpu._callbackInterface.WriteMemory8(address, (Byte)data);
        }

        private static void STRH(CPU cpu, UInt32 instruction)
        {
            UInt32 rd = (instruction >> 12) & 0b1111;

            UInt32 data = (rd == CPU.PC) ? (cpu.Reg[CPU.PC] + 4) : cpu.Reg[rd];
            UInt32 address = GetAddress_Misc(cpu, instruction);
            cpu._callbackInterface.WriteMemory16(address, (UInt16)data);
        }

        private static void SUB(CPU cpu, UInt32 instruction)
        {
            UInt32 i = (instruction >> 25) & 1;
            UInt32 s = (instruction >> 20) & 1;
            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 rd = (instruction >> 12) & 0b1111;
            UInt32 r = (instruction >> 4) & 1;

            (UInt32 shifterOperand, _) = GetShifterOperand(cpu, instruction);

            UInt32 leftOperand = ((rn == CPU.PC) && (i == 0) && (r == 1)) ? (cpu.Reg[CPU.PC] + 4) : cpu.Reg[rn];
            UInt32 rightOperand = shifterOperand;

            SetReg(cpu, rd, leftOperand - rightOperand);

            if (s == 1)
            {
                if (rd == CPU.PC)
                {
                    cpu.SetCPSR(cpu.SPSR);
                }
                else
                {
                    cpu.SetFlag(CPU.Flag.N, cpu.Reg[rd] >> 31);
                    cpu.SetFlag(CPU.Flag.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
                    cpu.SetFlag(CPU.Flag.C, CPU.Not(CPU.BorrowFrom(leftOperand, rightOperand)));
                    cpu.SetFlag(CPU.Flag.V, CPU.OverflowFrom_Subtraction(leftOperand, rightOperand, cpu.Reg[rd]));
                }
            }
        }

        private static void SWI(CPU cpu, UInt32 instruction)
        {
            UInt32 imm = instruction & 0xff_ffff;

            cpu._callbackInterface.HandleSWI(imm);
        }

        private static void SWP(CPU cpu, UInt32 instruction)
        {
            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 rd = (instruction >> 12) & 0b1111;
            UInt32 rm = instruction & 0b1111;

            UInt32 temp = BitOperations.RotateRight(cpu._callbackInterface.ReadMemory32(cpu.Reg[rn]), (int)(8 * (cpu.Reg[rn] & 0b11)));
            cpu._callbackInterface.WriteMemory32(cpu.Reg[rn], cpu.Reg[rm]);
            SetReg(cpu, rd, temp);
        }

        private static void SWPB(CPU cpu, UInt32 instruction)
        {
            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 rd = (instruction >> 12) & 0b1111;
            UInt32 rm = instruction & 0b1111;

            Byte temp = cpu._callbackInterface.ReadMemory8(cpu.Reg[rn]);
            cpu._callbackInterface.WriteMemory8(cpu.Reg[rn], (Byte)cpu.Reg[rm]);
            SetReg(cpu, rd, temp);
        }

        private static void TEQ(CPU cpu, UInt32 instruction)
        {
            UInt32 i = (instruction >> 25) & 1;
            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 rd = (instruction >> 12) & 0b1111;
            UInt32 r = (instruction >> 4) & 1;

            (UInt32 shifterOperand, UInt32 shifterCarryOut) = GetShifterOperand(cpu, instruction);

            UInt32 leftOperand = ((rn == CPU.PC) && (i == 0) && (r == 1)) ? (cpu.Reg[CPU.PC] + 4) : cpu.Reg[rn];
            UInt32 rightOperand = shifterOperand;

            UInt32 aluOut = leftOperand ^ rightOperand;

            if (rd == CPU.PC)
            {
                if ((cpu.CPSR & CPU.ModeMask) == CPU.UserMode)
                    cpu.CPSR = (cpu.CPSR & ~0xf000_0000) | (aluOut & 0xf000_0000);
                else
                    cpu.SetCPSR((cpu.CPSR & ~0xf000_00c3) | (aluOut & 0xf000_0003) | (((aluOut >> 26) & 0b11) << 6));
            }
            else
            {
                cpu.SetFlag(CPU.Flag.N, aluOut >> 31);
                cpu.SetFlag(CPU.Flag.Z, (aluOut == 0) ? 1u : 0u);
                cpu.SetFlag(CPU.Flag.C, shifterCarryOut);
            }
        }

        private static void TST(CPU cpu, UInt32 instruction)
        {
            UInt32 i = (instruction >> 25) & 1;
            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 rd = (instruction >> 12) & 0b1111;
            UInt32 r = (instruction >> 4) & 1;

            (UInt32 shifterOperand, UInt32 shifterCarryOut) = GetShifterOperand(cpu, instruction);

            UInt32 leftOperand = ((rn == CPU.PC) && (i == 0) && (r == 1)) ? (cpu.Reg[CPU.PC] + 4) : cpu.Reg[rn];
            UInt32 rightOperand = shifterOperand;

            UInt32 aluOut = leftOperand & rightOperand;

            if (rd == CPU.PC)
            {
                if ((cpu.CPSR & CPU.ModeMask) == CPU.UserMode)
                    cpu.CPSR = (cpu.CPSR & ~0xf000_0000) | (aluOut & 0xf000_0000);
                else
                    cpu.SetCPSR((cpu.CPSR & ~0xf000_00c3) | (aluOut & 0xf000_0003) | (((aluOut >> 26) & 0b11) << 6));
            }
            else
            {
                cpu.SetFlag(CPU.Flag.N, aluOut >> 31);
                cpu.SetFlag(CPU.Flag.Z, (aluOut == 0) ? 1u : 0u);
                cpu.SetFlag(CPU.Flag.C, shifterCarryOut);
            }
        }

        private static void UMLAL(CPU cpu, UInt32 instruction)
        {
            UInt32 s = (instruction >> 20) & 1;
            UInt32 rdHi = (instruction >> 16) & 0b1111;
            UInt32 rdLo = (instruction >> 12) & 0b1111;
            UInt32 rs = (instruction >> 8) & 0b1111;
            UInt32 rm = instruction & 0b1111;

            UInt64 result = (UInt64)cpu.Reg[rm] * (UInt64)cpu.Reg[rs];
            UInt64 resultLo = (UInt64)(UInt32)result + (UInt64)cpu.Reg[rdLo];
            UInt32 resultHi = (UInt32)(result >> 32) + cpu.Reg[rdHi] + CPU.CarryFrom(resultLo);
            SetReg(cpu, rdLo, (UInt32)resultLo);
            SetReg(cpu, rdHi, resultHi);

            if (s == 1)
            {
                cpu.SetFlag(CPU.Flag.N, cpu.Reg[rdHi] >> 31);
                cpu.SetFlag(CPU.Flag.Z, ((cpu.Reg[rdHi] == 0) && (cpu.Reg[rdLo] == 0)) ? 1u : 0u);
            }
        }

        private static void UMULL(CPU cpu, UInt32 instruction)
        {
            UInt32 s = (instruction >> 20) & 1;
            UInt32 rdHi = (instruction >> 16) & 0b1111;
            UInt32 rdLo = (instruction >> 12) & 0b1111;
            UInt32 rs = (instruction >> 8) & 0b1111;
            UInt32 rm = instruction & 0b1111;

            UInt64 result = (UInt64)cpu.Reg[rm] * (UInt64)cpu.Reg[rs];
            SetReg(cpu, rdLo, (UInt32)result);
            SetReg(cpu, rdHi, (UInt32)(result >> 32));

            if (s == 1)
            {
                cpu.SetFlag(CPU.Flag.N, cpu.Reg[rdHi] >> 31);
                cpu.SetFlag(CPU.Flag.Z, ((cpu.Reg[rdHi] == 0) && (cpu.Reg[rdLo] == 0)) ? 1u : 0u);
            }
        }
    }
}
