using System.Numerics;

namespace Iris
{
    internal sealed partial class CPU
    {
        private delegate void ARM_InstructionHandler(CPU cpu, UInt32 instruction);
        private readonly record struct ARM_InstructionListEntry(UInt32 Mask, UInt32 Expected, ARM_InstructionHandler Handler);

        private static readonly ARM_InstructionListEntry[] ARM_InstructionList = new ARM_InstructionListEntry[]
        {
            // ADC
            new(0x0fe0_0000, 0x02a0_0000, ARM_ADC), // I bit is 1
            new(0x0fe0_0090, 0x00a0_0000, ARM_ADC), // I bit is 0, bit[7] is 0 and bit[4] is 0
            new(0x0fe0_0090, 0x00a0_0080, ARM_ADC), // I bit is 0, bit[7] is 1 and bit[4] is 0
            new(0x0fe0_0090, 0x00a0_0010, ARM_ADC), // I bit is 0, bit[7] is 0 and bit[4] is 1

            // ADD
            new(0x0fe0_0000, 0x0280_0000, ARM_ADD), // I bit is 1
            new(0x0fe0_0090, 0x0080_0000, ARM_ADD), // I bit is 0, bit[7] is 0 and bit[4] is 0
            new(0x0fe0_0090, 0x0080_0080, ARM_ADD), // I bit is 0, bit[7] is 1 and bit[4] is 0
            new(0x0fe0_0090, 0x0080_0010, ARM_ADD), // I bit is 0, bit[7] is 0 and bit[4] is 1

            // AND
            new(0x0fe0_0000, 0x0200_0000, ARM_AND), // I bit is 1
            new(0x0fe0_0090, 0x0000_0000, ARM_AND), // I bit is 0, bit[7] is 0 and bit[4] is 0
            new(0x0fe0_0090, 0x0000_0080, ARM_AND), // I bit is 0, bit[7] is 1 and bit[4] is 0
            new(0x0fe0_0090, 0x0000_0010, ARM_AND), // I bit is 0, bit[7] is 0 and bit[4] is 1

            // B
            new(0x0f00_0000, 0x0a00_0000, ARM_B),

            // BL
            new(0x0f00_0000, 0x0b00_0000, ARM_BL),

            // BIC
            new(0x0fe0_0000, 0x03c0_0000, ARM_BIC), // I bit is 1
            new(0x0fe0_0090, 0x01c0_0000, ARM_BIC), // I bit is 0, bit[7] is 0 and bit[4] is 0
            new(0x0fe0_0090, 0x01c0_0080, ARM_BIC), // I bit is 0, bit[7] is 1 and bit[4] is 0
            new(0x0fe0_0090, 0x01c0_0010, ARM_BIC), // I bit is 0, bit[7] is 0 and bit[4] is 1

            // BX
            new(0x0fff_fff0, 0x012f_ff10, ARM_BX),

            // CMN
            new(0x0ff0_f000, 0x0370_0000, ARM_CMN), // I bit is 1
            new(0x0ff0_f090, 0x0170_0000, ARM_CMN), // I bit is 0, bit[7] is 0 and bit[4] is 0
            new(0x0ff0_f090, 0x0170_0080, ARM_CMN), // I bit is 0, bit[7] is 1 and bit[4] is 0
            new(0x0ff0_f090, 0x0170_0010, ARM_CMN), // I bit is 0, bit[7] is 0 and bit[4] is 1
            new(0x0ff0_f000, 0x0370_f000, ARM_CMN), // I bit is 1
            new(0x0ff0_f090, 0x0170_f000, ARM_CMN), // I bit is 0, bit[7] is 0 and bit[4] is 0
            new(0x0ff0_f090, 0x0170_f080, ARM_CMN), // I bit is 0, bit[7] is 1 and bit[4] is 0
            new(0x0ff0_f090, 0x0170_f010, ARM_CMN), // I bit is 0, bit[7] is 0 and bit[4] is 1

            // CMP
            new(0x0ff0_f000, 0x0350_0000, ARM_CMP), // I bit is 1
            new(0x0ff0_f090, 0x0150_0000, ARM_CMP), // I bit is 0, bit[7] is 0 and bit[4] is 0
            new(0x0ff0_f090, 0x0150_0080, ARM_CMP), // I bit is 0, bit[7] is 1 and bit[4] is 0
            new(0x0ff0_f090, 0x0150_0010, ARM_CMP), // I bit is 0, bit[7] is 0 and bit[4] is 1
            new(0x0ff0_f000, 0x0350_f000, ARM_CMP), // I bit is 1
            new(0x0ff0_f090, 0x0150_f000, ARM_CMP), // I bit is 0, bit[7] is 0 and bit[4] is 0
            new(0x0ff0_f090, 0x0150_f080, ARM_CMP), // I bit is 0, bit[7] is 1 and bit[4] is 0
            new(0x0ff0_f090, 0x0150_f010, ARM_CMP), // I bit is 0, bit[7] is 0 and bit[4] is 1

            // EOR
            new(0x0fe0_0000, 0x0220_0000, ARM_EOR), // I bit is 1
            new(0x0fe0_0090, 0x0020_0000, ARM_EOR), // I bit is 0, bit[7] is 0 and bit[4] is 0
            new(0x0fe0_0090, 0x0020_0080, ARM_EOR), // I bit is 0, bit[7] is 1 and bit[4] is 0
            new(0x0fe0_0090, 0x0020_0010, ARM_EOR), // I bit is 0, bit[7] is 0 and bit[4] is 1

            // LDM
            new(0x0e50_0000, 0x0810_0000, ARM_LDM1),
            new(0x0e50_8000, 0x0850_0000, ARM_LDM2),

            // LDR
            new(0x0c50_0000, 0x0410_0000, ARM_LDR),

            // LDRB
            new(0x0c50_0000, 0x0450_0000, ARM_LDRB),

            // LDRH
            new(0x0e10_00f0, 0x0010_00b0, ARM_LDRH),

            // LDRSB
            new(0x0e10_00f0, 0x0010_00d0, ARM_LDRSB),

            // LDRSH
            new(0x0e10_00f0, 0x0010_00f0, ARM_LDRSH),

            // MLA
            new(0x0fe0_00f0, 0x0020_0090, ARM_MLA),

            // MOV
            new(0x0fef_0000, 0x03a0_0000, ARM_MOV), // I bit is 1
            new(0x0fef_0090, 0x01a0_0000, ARM_MOV), // I bit is 0, bit[7] is 0 and bit[4] is 0
            new(0x0fef_0090, 0x01a0_0080, ARM_MOV), // I bit is 0, bit[7] is 1 and bit[4] is 0
            new(0x0fef_0090, 0x01a0_0010, ARM_MOV), // I bit is 0, bit[7] is 0 and bit[4] is 1

            // MRS
            new(0x0fbf_0fff, 0x010f_0000, ARM_MRS),

            // MSR
            new(0x0fb0_f000, 0x0320_f000, ARM_MSR), // Immediate operand
            new(0x0fb0_fff0, 0x0120_f000, ARM_MSR), // Register operand

            // MUL
            new(0x0fe0_f0f0, 0x0000_0090, ARM_MUL),

            // MVN
            new(0x0fef_0000, 0x03e0_0000, ARM_MVN), // I bit is 1
            new(0x0fef_0090, 0x01e0_0000, ARM_MVN), // I bit is 0, bit[7] is 0 and bit[4] is 0
            new(0x0fef_0090, 0x01e0_0080, ARM_MVN), // I bit is 0, bit[7] is 1 and bit[4] is 0
            new(0x0fef_0090, 0x01e0_0010, ARM_MVN), // I bit is 0, bit[7] is 0 and bit[4] is 1

            // ORR
            new(0x0fe0_0000, 0x0380_0000, ARM_ORR), // I bit is 1
            new(0x0fe0_0090, 0x0180_0000, ARM_ORR), // I bit is 0, bit[7] is 0 and bit[4] is 0
            new(0x0fe0_0090, 0x0180_0080, ARM_ORR), // I bit is 0, bit[7] is 1 and bit[4] is 0
            new(0x0fe0_0090, 0x0180_0010, ARM_ORR), // I bit is 0, bit[7] is 0 and bit[4] is 1

            // RSB
            new(0x0fe0_0000, 0x0260_0000, ARM_RSB), // I bit is 1
            new(0x0fe0_0090, 0x0060_0000, ARM_RSB), // I bit is 0, bit[7] is 0 and bit[4] is 0
            new(0x0fe0_0090, 0x0060_0080, ARM_RSB), // I bit is 0, bit[7] is 1 and bit[4] is 0
            new(0x0fe0_0090, 0x0060_0010, ARM_RSB), // I bit is 0, bit[7] is 0 and bit[4] is 1

            // RSC
            new(0x0fe0_0000, 0x02e0_0000, ARM_RSC), // I bit is 1
            new(0x0fe0_0090, 0x00e0_0000, ARM_RSC), // I bit is 0, bit[7] is 0 and bit[4] is 0
            new(0x0fe0_0090, 0x00e0_0080, ARM_RSC), // I bit is 0, bit[7] is 1 and bit[4] is 0
            new(0x0fe0_0090, 0x00e0_0010, ARM_RSC), // I bit is 0, bit[7] is 0 and bit[4] is 1

            // SBC
            new(0x0fe0_0000, 0x02c0_0000, ARM_SBC), // I bit is 1
            new(0x0fe0_0090, 0x00c0_0000, ARM_SBC), // I bit is 0, bit[7] is 0 and bit[4] is 0
            new(0x0fe0_0090, 0x00c0_0080, ARM_SBC), // I bit is 0, bit[7] is 1 and bit[4] is 0
            new(0x0fe0_0090, 0x00c0_0010, ARM_SBC), // I bit is 0, bit[7] is 0 and bit[4] is 1

            // SMLAL
            new(0x0fe0_00f0, 0x00e0_0090, ARM_SMLAL),

            // SMULL
            new(0x0fe0_00f0, 0x00c0_0090, ARM_SMULL),

            // STM
            new(0x0e50_0000, 0x0800_0000, ARM_STM1),
            new(0x0e70_0000, 0x0840_0000, ARM_STM2),

            // STR
            new(0x0c50_0000, 0x0400_0000, ARM_STR),

            // STRB
            new(0x0c50_0000, 0x0440_0000, ARM_STRB),

            // STRH
            new(0x0e10_00f0, 0x0000_00b0, ARM_STRH),

            // SUB
            new(0x0fe0_0000, 0x0240_0000, ARM_SUB), // I bit is 1
            new(0x0fe0_0090, 0x0040_0000, ARM_SUB), // I bit is 0, bit[7] is 0 and bit[4] is 0
            new(0x0fe0_0090, 0x0040_0080, ARM_SUB), // I bit is 0, bit[7] is 1 and bit[4] is 0
            new(0x0fe0_0090, 0x0040_0010, ARM_SUB), // I bit is 0, bit[7] is 0 and bit[4] is 1

            // SWI
            new(0x0f00_0000, 0x0f00_0000, ARM_SWI),

            // SWP
            new(0x0ff0_0ff0, 0x0100_0090, ARM_SWP),

            // SWPB
            new(0x0ff0_0ff0, 0x0140_0090, ARM_SWPB),

            // TEQ
            new(0x0ff0_f000, 0x0330_0000, ARM_TEQ), // I bit is 1
            new(0x0ff0_f090, 0x0130_0000, ARM_TEQ), // I bit is 0, bit[7] is 0 and bit[4] is 0
            new(0x0ff0_f090, 0x0130_0080, ARM_TEQ), // I bit is 0, bit[7] is 1 and bit[4] is 0
            new(0x0ff0_f090, 0x0130_0010, ARM_TEQ), // I bit is 0, bit[7] is 0 and bit[4] is 1
            new(0x0ff0_f000, 0x0330_f000, ARM_TEQ), // I bit is 1
            new(0x0ff0_f090, 0x0130_f000, ARM_TEQ), // I bit is 0, bit[7] is 0 and bit[4] is 0
            new(0x0ff0_f090, 0x0130_f080, ARM_TEQ), // I bit is 0, bit[7] is 1 and bit[4] is 0
            new(0x0ff0_f090, 0x0130_f010, ARM_TEQ), // I bit is 0, bit[7] is 0 and bit[4] is 1

            // TST
            new(0x0ff0_f000, 0x0310_0000, ARM_TST), // I bit is 1
            new(0x0ff0_f090, 0x0110_0000, ARM_TST), // I bit is 0, bit[7] is 0 and bit[4] is 0
            new(0x0ff0_f090, 0x0110_0080, ARM_TST), // I bit is 0, bit[7] is 1 and bit[4] is 0
            new(0x0ff0_f090, 0x0110_0010, ARM_TST), // I bit is 0, bit[7] is 0 and bit[4] is 1
            new(0x0ff0_f000, 0x0310_f000, ARM_TST), // I bit is 1
            new(0x0ff0_f090, 0x0110_f000, ARM_TST), // I bit is 0, bit[7] is 0 and bit[4] is 0
            new(0x0ff0_f090, 0x0110_f080, ARM_TST), // I bit is 0, bit[7] is 1 and bit[4] is 0
            new(0x0ff0_f090, 0x0110_f010, ARM_TST), // I bit is 0, bit[7] is 0 and bit[4] is 1

            // UMLAL
            new(0x0fe0_00f0, 0x00a0_0090, ARM_UMLAL),

            // UMULL
            new(0x0fe0_00f0, 0x0080_0090, ARM_UMULL),
        };

        private void ARM_Step()
        {
            UInt32 instruction = _callbacks.ReadMemory32(NextInstructionAddress);
            NextInstructionAddress += 4;

            UInt32 cond = (instruction >> 28) & 0b1111;

            if (ConditionPassed(cond))
            {
                Reg[PC] = NextInstructionAddress + 4;

                foreach (ARM_InstructionListEntry entry in ARM_InstructionList)
                {
                    if ((instruction & entry.Mask) == entry.Expected)
                    {
                        entry.Handler(this, instruction);
                        return;
                    }
                }

                throw new Exception(string.Format("CPU: Unknown ARM instruction 0x{0:x8} at address 0x{1:x8}", instruction, NextInstructionAddress - 4));
            }
        }

        private void ARM_SetPC(UInt32 value)
        {
            Reg[PC] = value;
            NextInstructionAddress = value;
        }

        private void ARM_SetReg(UInt32 i, UInt32 value)
        {
            if (i == PC)
                ARM_SetPC(value);
            else
                Reg[i] = value;
        }

        // Addressing mode 1
        private (UInt32 shifterOperand, UInt32 shifterCarryOut) GetShifterOperand(UInt32 instruction)
        {
            UInt32 i = (instruction >> 25) & 1;

            UInt32 shifterOperand = 0;
            UInt32 shifterCarryOut = 0;

            if (i == 1) // 32-bit immediate
            {
                UInt32 rotateImm = (instruction >> 8) & 0b1111;
                UInt32 imm = instruction & 0xff;

                shifterOperand = BitOperations.RotateRight(imm, (int)(rotateImm * 2));
                shifterCarryOut = (rotateImm == 0) ? GetFlag(Flags.C) : (shifterOperand >> 31);
            }
            else
            {
                UInt32 shift = (instruction >> 5) & 0b11;
                UInt32 r = (instruction >> 4) & 1;
                UInt32 rm = instruction & 0b1111;

                if (r == 0) // Immediate shifts
                {
                    UInt32 shiftImm = (instruction >> 7) & 0b1_1111;

                    UInt32 value = Reg[rm];
                    int shiftAmount = (int)shiftImm;

                    switch (shift)
                    {
                        case 0b00: // Logical shift left
                            if (shiftAmount == 0)
                            {
                                shifterOperand = value;
                                shifterCarryOut = GetFlag(Flags.C);
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
                                shifterOperand = ArithmeticShiftRight(value, shiftAmount);
                                shifterCarryOut = (value >> (shiftAmount - 1)) & 1;
                            }
                            break;
                        case 0b11: // Rotate right
                            if (shiftAmount == 0)
                            {
                                shifterOperand = (GetFlag(Flags.C) << 31) | (value >> 1);
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

                    UInt32 value = (rm == PC) ? (Reg[PC] + 4) : Reg[rm];
                    int shiftAmount = (int)(Reg[rs] & 0xff);

                    switch (shift)
                    {
                        case 0b00: // Logical shift left
                            if (shiftAmount == 0)
                            {
                                shifterOperand = value;
                                shifterCarryOut = GetFlag(Flags.C);
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
                                shifterCarryOut = GetFlag(Flags.C);
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
                                shifterCarryOut = GetFlag(Flags.C);
                            }
                            else if (shiftAmount < 32)
                            {
                                shifterOperand = ArithmeticShiftRight(value, shiftAmount);
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
                                shifterCarryOut = GetFlag(Flags.C);
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
        private UInt32 GetAddress(UInt32 instruction)
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
                    index = Reg[rm];
                }
                else // Scaled register
                {
                    switch (shift)
                    {
                        case 0b00: // LSL
                            index = Reg[rm] << (int)shiftImm;
                            break;
                        case 0b01: // LSR
                            if (shiftImm == 0)
                                index = 0;
                            else
                                index = Reg[rm] >> (int)shiftImm;
                            break;
                        case 0b10: // ASR
                            if (shiftImm == 0)
                                index = ((Reg[rm] >> 31) == 1) ? 0xffff_ffff : 0;
                            else
                                index = ArithmeticShiftRight(Reg[rm], (int)shiftImm);
                            break;
                        case 0b11:
                            if (shiftImm == 0) // RRX
                                index = (GetFlag(Flags.C) << 31) | (Reg[rm] >> 1);
                            else // ROR
                                index = BitOperations.RotateRight(Reg[rm], (int)shiftImm);
                            break;
                    }
                }
            }

            UInt32 regRnIndexed = (u == 1) ? (Reg[rn] + index) : (Reg[rn] - index);

            UInt32 address;

            if (p == 0) // Post-indexed
            {
                address = Reg[rn];
                ARM_SetReg(rn, regRnIndexed);
            }
            else if (w == 0) // Offset
            {
                address = regRnIndexed;
            }
            else // Pre-indexed
            {
                address = regRnIndexed;
                ARM_SetReg(rn, regRnIndexed);
            }

            return address;
        }

        // Addressing mode 3
        private UInt32 GetAddress_Misc(UInt32 instruction)
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

                index = Reg[rm];
            }

            UInt32 regRnIndexed = (u == 1) ? (Reg[rn] + index) : (Reg[rn] - index);

            UInt32 address;

            if (p == 0) // Post-indexed
            {
                address = Reg[rn];
                ARM_SetReg(rn, regRnIndexed);
            }
            else if (w == 0) // Offset
            {
                address = regRnIndexed;
            }
            else // Pre-indexed
            {
                address = regRnIndexed;
                ARM_SetReg(rn, regRnIndexed);
            }

            return address;
        }

        // Addressing mode 4
        private (UInt32 startAddress, UInt32 endAddress) GetAddress_Multiple(UInt32 instruction)
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
                value = Reg[rn] + increment;

                if (p == 0) // after
                {
                    startAddress = Reg[rn];
                    endAddress = value - 4;
                }
                else // before
                {
                    startAddress = Reg[rn] + 4;
                    endAddress = value;
                }
            }
            else // decrement
            {
                value = Reg[rn] - increment;

                if (p == 0) // after
                {
                    startAddress = value + 4;
                    endAddress = Reg[rn];
                }
                else // before
                {
                    startAddress = value;
                    endAddress = Reg[rn] - 4;
                }
            }

            if (w == 1)
                ARM_SetReg(rn, value);

            return (startAddress, endAddress);
        }

        private static void ARM_ADC(CPU cpu, UInt32 instruction)
        {
            UInt32 i = (instruction >> 25) & 1;
            UInt32 s = (instruction >> 20) & 1;
            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 rd = (instruction >> 12) & 0b1111;
            UInt32 r = (instruction >> 4) & 1;

            var (shifterOperand, _) = cpu.GetShifterOperand(instruction);

            UInt32 leftOperand = ((rn == PC) && (i == 0) && (r == 1)) ? (cpu.Reg[PC] + 4) : cpu.Reg[rn];
            UInt64 rightOperand = (UInt64)shifterOperand + (UInt64)cpu.GetFlag(Flags.C);

            UInt64 result = (UInt64)leftOperand + rightOperand;
            cpu.ARM_SetReg(rd, (UInt32)result);

            if (s == 1)
            {
                if (rd == PC)
                {
                    cpu.SetCPSR(cpu.SPSR);
                }
                else
                {
                    cpu.SetFlag(Flags.N, cpu.Reg[rd] >> 31);
                    cpu.SetFlag(Flags.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
                    cpu.SetFlag(Flags.C, CarryFrom(result));
                    cpu.SetFlag(Flags.V, OverflowFrom_Addition(leftOperand, (UInt32)rightOperand, cpu.Reg[rd]));
                }
            }
        }

        private static void ARM_ADD(CPU cpu, UInt32 instruction)
        {
            UInt32 i = (instruction >> 25) & 1;
            UInt32 s = (instruction >> 20) & 1;
            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 rd = (instruction >> 12) & 0b1111;
            UInt32 r = (instruction >> 4) & 1;

            var (shifterOperand, _) = cpu.GetShifterOperand(instruction);

            UInt32 leftOperand = ((rn == PC) && (i == 0) && (r == 1)) ? (cpu.Reg[PC] + 4) : cpu.Reg[rn];
            UInt32 rightOperand = shifterOperand;

            UInt64 result = (UInt64)leftOperand + (UInt64)rightOperand;
            cpu.ARM_SetReg(rd, (UInt32)result);

            if (s == 1)
            {
                if (rd == PC)
                {
                    cpu.SetCPSR(cpu.SPSR);
                }
                else
                {
                    cpu.SetFlag(Flags.N, cpu.Reg[rd] >> 31);
                    cpu.SetFlag(Flags.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
                    cpu.SetFlag(Flags.C, CarryFrom(result));
                    cpu.SetFlag(Flags.V, OverflowFrom_Addition(leftOperand, rightOperand, cpu.Reg[rd]));
                }
            }
        }

        private static void ARM_AND(CPU cpu, UInt32 instruction)
        {
            UInt32 i = (instruction >> 25) & 1;
            UInt32 s = (instruction >> 20) & 1;
            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 rd = (instruction >> 12) & 0b1111;
            UInt32 r = (instruction >> 4) & 1;

            var (shifterOperand, shifterCarryOut) = cpu.GetShifterOperand(instruction);

            UInt32 leftOperand = ((rn == PC) && (i == 0) && (r == 1)) ? (cpu.Reg[PC] + 4) : cpu.Reg[rn];
            UInt32 rightOperand = shifterOperand;

            cpu.ARM_SetReg(rd, leftOperand & rightOperand);

            if (s == 1)
            {
                if (rd == PC)
                {
                    cpu.SetCPSR(cpu.SPSR);
                }
                else
                {
                    cpu.SetFlag(Flags.N, cpu.Reg[rd] >> 31);
                    cpu.SetFlag(Flags.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
                    cpu.SetFlag(Flags.C, shifterCarryOut);
                }
            }
        }

        private static void ARM_B(CPU cpu, UInt32 instruction)
        {
            UInt32 imm = instruction & 0xff_ffff;

            cpu.ARM_SetPC(cpu.Reg[PC] + (SignExtend(imm, 24) << 2));
        }

        private static void ARM_BL(CPU cpu, UInt32 instruction)
        {
            UInt32 imm = instruction & 0xff_ffff;

            cpu.Reg[LR] = cpu.NextInstructionAddress;
            cpu.ARM_SetPC(cpu.Reg[PC] + (SignExtend(imm, 24) << 2));
        }

        private static void ARM_BIC(CPU cpu, UInt32 instruction)
        {
            UInt32 i = (instruction >> 25) & 1;
            UInt32 s = (instruction >> 20) & 1;
            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 rd = (instruction >> 12) & 0b1111;
            UInt32 r = (instruction >> 4) & 1;

            var (shifterOperand, shifterCarryOut) = cpu.GetShifterOperand(instruction);

            UInt32 leftOperand = ((rn == PC) && (i == 0) && (r == 1)) ? (cpu.Reg[PC] + 4) : cpu.Reg[rn];
            UInt32 rightOperand = shifterOperand;

            cpu.ARM_SetReg(rd, leftOperand & ~rightOperand);

            if (s == 1)
            {
                if (rd == PC)
                {
                    cpu.SetCPSR(cpu.SPSR);
                }
                else
                {
                    cpu.SetFlag(Flags.N, cpu.Reg[rd] >> 31);
                    cpu.SetFlag(Flags.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
                    cpu.SetFlag(Flags.C, shifterCarryOut);
                }
            }
        }

        private static void ARM_BX(CPU cpu, UInt32 instruction)
        {
            UInt32 rm = instruction & 0b1111;

            cpu.CPSR = (cpu.CPSR & ~(1u << 5)) | ((cpu.Reg[rm] & 1) << 5);
            cpu.ARM_SetPC(cpu.Reg[rm] & 0xffff_fffe);
        }

        private static void ARM_CMN(CPU cpu, UInt32 instruction)
        {
            UInt32 i = (instruction >> 25) & 1;
            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 rd = (instruction >> 12) & 0b1111;
            UInt32 r = (instruction >> 4) & 1;

            var (shifterOperand, _) = cpu.GetShifterOperand(instruction);

            UInt32 leftOperand = ((rn == PC) && (i == 0) && (r == 1)) ? (cpu.Reg[PC] + 4) : cpu.Reg[rn];
            UInt32 rightOperand = shifterOperand;

            UInt64 result = (UInt64)leftOperand + (UInt64)rightOperand;
            UInt32 aluOut = (UInt32)result;

            if (rd == PC)
            {
                if ((cpu.CPSR & ModeMask) == UserMode)
                    cpu.CPSR = (cpu.CPSR & ~0xf000_0000) | (aluOut & 0xf000_0000);
                else
                    cpu.SetCPSR((cpu.CPSR & ~0xf000_00c3) | (aluOut & 0xf000_0003) | (((aluOut >> 26) & 0b11) << 6));
            }
            else
            {
                cpu.SetFlag(Flags.N, aluOut >> 31);
                cpu.SetFlag(Flags.Z, (aluOut == 0) ? 1u : 0u);
                cpu.SetFlag(Flags.C, CarryFrom(result));
                cpu.SetFlag(Flags.V, OverflowFrom_Addition(leftOperand, rightOperand, aluOut));
            }
        }

        private static void ARM_CMP(CPU cpu, UInt32 instruction)
        {
            UInt32 i = (instruction >> 25) & 1;
            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 rd = (instruction >> 12) & 0b1111;
            UInt32 r = (instruction >> 4) & 1;

            var (shifterOperand, _) = cpu.GetShifterOperand(instruction);

            UInt32 leftOperand = ((rn == PC) && (i == 0) && (r == 1)) ? (cpu.Reg[PC] + 4) : cpu.Reg[rn];
            UInt32 rightOperand = shifterOperand;

            UInt32 aluOut = leftOperand - rightOperand;

            if (rd == PC)
            {
                if ((cpu.CPSR & ModeMask) == UserMode)
                    cpu.CPSR = (cpu.CPSR & ~0xf000_0000) | (aluOut & 0xf000_0000);
                else
                    cpu.SetCPSR((cpu.CPSR & ~0xf000_00c3) | (aluOut & 0xf000_0003) | (((aluOut >> 26) & 0b11) << 6));
            }
            else
            {
                cpu.SetFlag(Flags.N, aluOut >> 31);
                cpu.SetFlag(Flags.Z, (aluOut == 0) ? 1u : 0u);
                cpu.SetFlag(Flags.C, Not(BorrowFrom(leftOperand, rightOperand)));
                cpu.SetFlag(Flags.V, OverflowFrom_Subtraction(leftOperand, rightOperand, aluOut));
            }
        }

        private static void ARM_EOR(CPU cpu, UInt32 instruction)
        {
            UInt32 i = (instruction >> 25) & 1;
            UInt32 s = (instruction >> 20) & 1;
            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 rd = (instruction >> 12) & 0b1111;
            UInt32 r = (instruction >> 4) & 1;

            var (shifterOperand, shifterCarryOut) = cpu.GetShifterOperand(instruction);

            UInt32 leftOperand = ((rn == PC) && (i == 0) && (r == 1)) ? (cpu.Reg[PC] + 4) : cpu.Reg[rn];
            UInt32 rightOperand = shifterOperand;

            cpu.ARM_SetReg(rd, leftOperand ^ rightOperand);

            if (s == 1)
            {
                if (rd == PC)
                {
                    cpu.SetCPSR(cpu.SPSR);
                }
                else
                {
                    cpu.SetFlag(Flags.N, cpu.Reg[rd] >> 31);
                    cpu.SetFlag(Flags.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
                    cpu.SetFlag(Flags.C, shifterCarryOut);
                }
            }
        }

        private static void ARM_LDM1(CPU cpu, UInt32 instruction)
        {
            UInt32 registerList = instruction & 0xffff;

            var (startAddress, _) = cpu.GetAddress_Multiple(instruction);

            UInt32 address = startAddress;

            if (registerList == 0)
            {
                cpu.ARM_SetPC(cpu._callbacks.ReadMemory32(address));
            }
            else
            {
                for (var i = 0; i <= 14; ++i)
                {
                    if (((registerList >> i) & 1) == 1)
                    {
                        cpu.Reg[i] = cpu._callbacks.ReadMemory32(address);
                        address += 4;
                    }
                }

                if (((registerList >> 15) & 1) == 1)
                    cpu.ARM_SetPC(cpu._callbacks.ReadMemory32(address) & 0xffff_fffc);
            }
        }

        private static void ARM_LDM2(CPU cpu, UInt32 instruction)
        {
            UInt32 registerList = instruction & 0x7fff;

            var (startAddress, _) = cpu.GetAddress_Multiple(instruction);

            UInt32 address = startAddress;

            if (registerList == 0)
            {
                cpu.ARM_SetPC(cpu._callbacks.ReadMemory32(address));
            }
            else
            {
                for (var i = 0; i <= 14; ++i)
                {
                    if (((registerList >> i) & 1) == 1)
                    {
                        UInt32 value = cpu._callbacks.ReadMemory32(address);

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

        private static void ARM_LDR(CPU cpu, UInt32 instruction)
        {
            UInt32 rd = (instruction >> 12) & 0b1111;

            UInt32 address = cpu.GetAddress(instruction);
            UInt32 data = BitOperations.RotateRight(cpu._callbacks.ReadMemory32(address), (int)(8 * (address & 0b11)));

            if (rd == PC)
                cpu.ARM_SetPC(data & 0xffff_fffc);
            else
                cpu.Reg[rd] = data;
        }

        private static void ARM_LDRB(CPU cpu, UInt32 instruction)
        {
            UInt32 rd = (instruction >> 12) & 0b1111;

            UInt32 address = cpu.GetAddress(instruction);
            Byte data = cpu._callbacks.ReadMemory8(address);
            cpu.ARM_SetReg(rd, data);
        }

        private static void ARM_LDRH(CPU cpu, UInt32 instruction)
        {
            UInt32 rd = (instruction >> 12) & 0b1111;

            UInt32 address = cpu.GetAddress_Misc(instruction);
            UInt32 data = BitOperations.RotateRight(cpu._callbacks.ReadMemory16(address), (int)(8 * (address & 1)));
            cpu.ARM_SetReg(rd, data);
        }

        private static void ARM_LDRSB(CPU cpu, UInt32 instruction)
        {
            UInt32 rd = (instruction >> 12) & 0b1111;

            UInt32 address = cpu.GetAddress_Misc(instruction);
            Byte data = cpu._callbacks.ReadMemory8(address);
            cpu.ARM_SetReg(rd, SignExtend(data, 8));
        }

        private static void ARM_LDRSH(CPU cpu, UInt32 instruction)
        {
            UInt32 rd = (instruction >> 12) & 0b1111;

            UInt32 address = cpu.GetAddress_Misc(instruction);

            if ((address & 1) == 1)
            {
                Byte data = cpu._callbacks.ReadMemory8(address);
                cpu.ARM_SetReg(rd, SignExtend(data, 8));
            }
            else
            {
                UInt16 data = cpu._callbacks.ReadMemory16(address);
                cpu.ARM_SetReg(rd, SignExtend(data, 16));
            }
        }

        private static void ARM_MLA(CPU cpu, UInt32 instruction)
        {
            UInt32 s = (instruction >> 20) & 1;
            UInt32 rd = (instruction >> 16) & 0b1111;
            UInt32 rn = (instruction >> 12) & 0b1111;
            UInt32 rs = (instruction >> 8) & 0b1111;
            UInt32 rm = instruction & 0b1111;

            cpu.ARM_SetReg(rd, (cpu.Reg[rm] * cpu.Reg[rs]) + cpu.Reg[rn]);

            if (s == 1)
            {
                cpu.SetFlag(Flags.N, cpu.Reg[rd] >> 31);
                cpu.SetFlag(Flags.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
            }
        }

        private static void ARM_MOV(CPU cpu, UInt32 instruction)
        {
            UInt32 s = (instruction >> 20) & 1;
            UInt32 rd = (instruction >> 12) & 0b1111;

            var (shifterOperand, shifterCarryOut) = cpu.GetShifterOperand(instruction);

            cpu.ARM_SetReg(rd, shifterOperand);

            if (s == 1)
            {
                if (rd == PC)
                {
                    cpu.SetCPSR(cpu.SPSR);
                }
                else
                {
                    cpu.SetFlag(Flags.N, cpu.Reg[rd] >> 31);
                    cpu.SetFlag(Flags.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
                    cpu.SetFlag(Flags.C, shifterCarryOut);
                }
            }
        }

        private static void ARM_MRS(CPU cpu, UInt32 instruction)
        {
            UInt32 r = (instruction >> 22) & 1;
            UInt32 rd = (instruction >> 12) & 0b1111;

            cpu.ARM_SetReg(rd, (r == 1) ? cpu.SPSR : cpu.CPSR);
        }

        private static void ARM_MSR(CPU cpu, UInt32 instruction)
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

            UInt32 byteMask = (UInt32)((((fieldMask >> 0) & 1) == 1) ? 0x0000_00ff : 0)
                            | (UInt32)((((fieldMask >> 1) & 1) == 1) ? 0x0000_ff00 : 0)
                            | (UInt32)((((fieldMask >> 2) & 1) == 1) ? 0x00ff_0000 : 0)
                            | (UInt32)((((fieldMask >> 3) & 1) == 1) ? 0xff00_0000 : 0);

            if (r == 0)
            {
                if ((cpu.CPSR & ModeMask) == UserMode)
                {
                    UInt32 mask = byteMask & 0xf000_0000;
                    cpu.CPSR = (cpu.CPSR & ~mask) | (operand & mask);
                }
                else
                {
                    UInt32 mask = byteMask & 0xf000_00cf;
                    cpu.SetCPSR((cpu.CPSR & ~mask) | (operand & mask));
                }
            }
            else
            {
                cpu.SPSR = operand;
            }
        }

        private static void ARM_MUL(CPU cpu, UInt32 instruction)
        {
            UInt32 s = (instruction >> 20) & 1;
            UInt32 rd = (instruction >> 16) & 0b1111;
            UInt32 rs = (instruction >> 8) & 0b1111;
            UInt32 rm = instruction & 0b1111;

            cpu.ARM_SetReg(rd, cpu.Reg[rm] * cpu.Reg[rs]);

            if (s == 1)
            {
                cpu.SetFlag(Flags.N, cpu.Reg[rd] >> 31);
                cpu.SetFlag(Flags.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
            }
        }

        private static void ARM_MVN(CPU cpu, UInt32 instruction)
        {
            UInt32 s = (instruction >> 20) & 1;
            UInt32 rd = (instruction >> 12) & 0b1111;

            var (shifterOperand, shifterCarryOut) = cpu.GetShifterOperand(instruction);

            cpu.ARM_SetReg(rd, ~shifterOperand);

            if (s == 1)
            {
                if (rd == PC)
                {
                    cpu.SetCPSR(cpu.SPSR);
                }
                else
                {
                    cpu.SetFlag(Flags.N, cpu.Reg[rd] >> 31);
                    cpu.SetFlag(Flags.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
                    cpu.SetFlag(Flags.C, shifterCarryOut);
                }
            }
        }

        private static void ARM_ORR(CPU cpu, UInt32 instruction)
        {
            UInt32 i = (instruction >> 25) & 1;
            UInt32 s = (instruction >> 20) & 1;
            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 rd = (instruction >> 12) & 0b1111;
            UInt32 r = (instruction >> 4) & 1;

            var (shifterOperand, shifterCarryOut) = cpu.GetShifterOperand(instruction);

            UInt32 leftOperand = ((rn == PC) && (i == 0) && (r == 1)) ? (cpu.Reg[PC] + 4) : cpu.Reg[rn];
            UInt32 rightOperand = shifterOperand;

            cpu.ARM_SetReg(rd, leftOperand | rightOperand);

            if (s == 1)
            {
                if (rd == PC)
                {
                    cpu.SetCPSR(cpu.SPSR);
                }
                else
                {
                    cpu.SetFlag(Flags.N, cpu.Reg[rd] >> 31);
                    cpu.SetFlag(Flags.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
                    cpu.SetFlag(Flags.C, shifterCarryOut);
                }
            }
        }

        private static void ARM_RSB(CPU cpu, UInt32 instruction)
        {
            UInt32 i = (instruction >> 25) & 1;
            UInt32 s = (instruction >> 20) & 1;
            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 rd = (instruction >> 12) & 0b1111;
            UInt32 r = (instruction >> 4) & 1;

            var (shifterOperand, _) = cpu.GetShifterOperand(instruction);

            UInt32 leftOperand = shifterOperand;
            UInt32 rightOperand = ((rn == PC) && (i == 0) && (r == 1)) ? (cpu.Reg[PC] + 4) : cpu.Reg[rn];

            cpu.ARM_SetReg(rd, leftOperand - rightOperand);

            if (s == 1)
            {
                if (rd == PC)
                {
                    cpu.SetCPSR(cpu.SPSR);
                }
                else
                {
                    cpu.SetFlag(Flags.N, cpu.Reg[rd] >> 31);
                    cpu.SetFlag(Flags.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
                    cpu.SetFlag(Flags.C, Not(BorrowFrom(leftOperand, rightOperand)));
                    cpu.SetFlag(Flags.V, OverflowFrom_Subtraction(leftOperand, rightOperand, cpu.Reg[rd]));
                }
            }
        }

        private static void ARM_RSC(CPU cpu, UInt32 instruction)
        {
            UInt32 i = (instruction >> 25) & 1;
            UInt32 s = (instruction >> 20) & 1;
            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 rd = (instruction >> 12) & 0b1111;
            UInt32 r = (instruction >> 4) & 1;

            var (shifterOperand, _) = cpu.GetShifterOperand(instruction);

            UInt32 leftOperand = shifterOperand;
            UInt64 rightOperand = (UInt64)(((rn == PC) && (i == 0) && (r == 1)) ? (cpu.Reg[PC] + 4) : cpu.Reg[rn])
                                + (UInt64)Not(cpu.GetFlag(Flags.C));

            cpu.ARM_SetReg(rd, leftOperand - (UInt32)rightOperand);

            if (s == 1)
            {
                if (rd == PC)
                {
                    cpu.SetCPSR(cpu.SPSR);
                }
                else
                {
                    cpu.SetFlag(Flags.N, cpu.Reg[rd] >> 31);
                    cpu.SetFlag(Flags.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
                    cpu.SetFlag(Flags.C, Not(BorrowFrom(leftOperand, rightOperand)));
                    cpu.SetFlag(Flags.V, OverflowFrom_Subtraction(leftOperand, (UInt32)rightOperand, cpu.Reg[rd]));
                }
            }
        }

        private static void ARM_SBC(CPU cpu, UInt32 instruction)
        {
            UInt32 i = (instruction >> 25) & 1;
            UInt32 s = (instruction >> 20) & 1;
            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 rd = (instruction >> 12) & 0b1111;
            UInt32 r = (instruction >> 4) & 1;

            var (shifterOperand, _) = cpu.GetShifterOperand(instruction);

            UInt32 leftOperand = ((rn == PC) && (i == 0) && (r == 1)) ? (cpu.Reg[PC] + 4) : cpu.Reg[rn];
            UInt64 rightOperand = (UInt64)shifterOperand + (UInt64)Not(cpu.GetFlag(Flags.C));

            cpu.ARM_SetReg(rd, leftOperand - (UInt32)rightOperand);

            if (s == 1)
            {
                if (rd == PC)
                {
                    cpu.SetCPSR(cpu.SPSR);
                }
                else
                {
                    cpu.SetFlag(Flags.N, cpu.Reg[rd] >> 31);
                    cpu.SetFlag(Flags.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
                    cpu.SetFlag(Flags.C, Not(BorrowFrom(leftOperand, rightOperand)));
                    cpu.SetFlag(Flags.V, OverflowFrom_Subtraction(leftOperand, (UInt32)rightOperand, cpu.Reg[rd]));
                }
            }
        }

        private static void ARM_SMLAL(CPU cpu, UInt32 instruction)
        {
            UInt32 s = (instruction >> 20) & 1;
            UInt32 rdHi = (instruction >> 16) & 0b1111;
            UInt32 rdLo = (instruction >> 12) & 0b1111;
            UInt32 rs = (instruction >> 8) & 0b1111;
            UInt32 rm = instruction & 0b1111;

            Int64 result = (Int64)(Int32)cpu.Reg[rm] * (Int64)(Int32)cpu.Reg[rs];
            UInt64 resultLo = (UInt64)(UInt32)result + (UInt64)cpu.Reg[rdLo];
            UInt32 resultHi = (UInt32)(result >> 32) + cpu.Reg[rdHi] + CarryFrom(resultLo);
            cpu.ARM_SetReg(rdLo, (UInt32)resultLo);
            cpu.ARM_SetReg(rdHi, resultHi);

            if (s == 1)
            {
                cpu.SetFlag(Flags.N, cpu.Reg[rdHi] >> 31);
                cpu.SetFlag(Flags.Z, ((cpu.Reg[rdHi] == 0) && (cpu.Reg[rdLo] == 0)) ? 1u : 0u);
            }
        }

        private static void ARM_SMULL(CPU cpu, UInt32 instruction)
        {
            UInt32 s = (instruction >> 20) & 1;
            UInt32 rdHi = (instruction >> 16) & 0b1111;
            UInt32 rdLo = (instruction >> 12) & 0b1111;
            UInt32 rs = (instruction >> 8) & 0b1111;
            UInt32 rm = instruction & 0b1111;

            Int64 result = (Int64)(Int32)cpu.Reg[rm] * (Int64)(Int32)cpu.Reg[rs];
            cpu.ARM_SetReg(rdLo, (UInt32)result);
            cpu.ARM_SetReg(rdHi, (UInt32)(result >> 32));

            if (s == 1)
            {
                cpu.SetFlag(Flags.N, cpu.Reg[rdHi] >> 31);
                cpu.SetFlag(Flags.Z, ((cpu.Reg[rdHi] == 0) && (cpu.Reg[rdLo] == 0)) ? 1u : 0u);
            }
        }

        private static void ARM_STM1(CPU cpu, UInt32 instruction)
        {
            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 registerList = instruction & 0xffff;

            UInt32 oldRegRn = cpu.Reg[rn];

            var (startAddress, _) = cpu.GetAddress_Multiple(instruction);

            UInt32 address = startAddress;

            if (registerList == 0)
            {
                cpu._callbacks.WriteMemory32(address, cpu.Reg[PC] + 4);
            }
            else
            {
                for (var i = 0; i <= 14; ++i)
                {
                    if (((registerList >> i) & 1) == 1)
                    {
                        if ((i == rn) && ((registerList & ~(0xffff << i)) == 0))
                            cpu._callbacks.WriteMemory32(address, oldRegRn);
                        else
                            cpu._callbacks.WriteMemory32(address, cpu.Reg[i]);

                        address += 4;
                    }
                }

                if (((registerList >> 15) & 1) == 1)
                    cpu._callbacks.WriteMemory32(address, cpu.Reg[PC] + 4);
            }
        }

        private static void ARM_STM2(CPU cpu, UInt32 instruction)
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

            var (startAddress, _) = cpu.GetAddress_Multiple(instruction);

            UInt32 address = startAddress;

            if (registerList == 0)
            {
                cpu._callbacks.WriteMemory32(address, cpu.Reg[PC] + 4);
            }
            else
            {
                for (var i = 0; i <= 14; ++i)
                {
                    if (((registerList >> i) & 1) == 1)
                    {
                        if ((i == rn) && ((registerList & ~(0xffff << i)) == 0))
                        {
                            cpu._callbacks.WriteMemory32(address, oldRegRn);
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

                            cpu._callbacks.WriteMemory32(address, value);
                        }

                        address += 4;
                    }
                }

                if (((registerList >> 15) & 1) == 1)
                    cpu._callbacks.WriteMemory32(address, cpu.Reg[PC] + 4);
            }
        }

        private static void ARM_STR(CPU cpu, UInt32 instruction)
        {
            UInt32 rd = (instruction >> 12) & 0b1111;

            UInt32 data = (rd == PC) ? (cpu.Reg[PC] + 4) : cpu.Reg[rd];
            UInt32 address = cpu.GetAddress(instruction);
            cpu._callbacks.WriteMemory32(address, data);
        }

        private static void ARM_STRB(CPU cpu, UInt32 instruction)
        {
            UInt32 rd = (instruction >> 12) & 0b1111;

            UInt32 data = (rd == PC) ? (cpu.Reg[PC] + 4) : cpu.Reg[rd];
            UInt32 address = cpu.GetAddress(instruction);
            cpu._callbacks.WriteMemory8(address, (Byte)data);
        }

        private static void ARM_STRH(CPU cpu, UInt32 instruction)
        {
            UInt32 rd = (instruction >> 12) & 0b1111;

            UInt32 data = (rd == PC) ? (cpu.Reg[PC] + 4) : cpu.Reg[rd];
            UInt32 address = cpu.GetAddress_Misc(instruction);
            cpu._callbacks.WriteMemory16(address, (UInt16)data);
        }

        private static void ARM_SUB(CPU cpu, UInt32 instruction)
        {
            UInt32 i = (instruction >> 25) & 1;
            UInt32 s = (instruction >> 20) & 1;
            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 rd = (instruction >> 12) & 0b1111;
            UInt32 r = (instruction >> 4) & 1;

            var (shifterOperand, _) = cpu.GetShifterOperand(instruction);

            UInt32 leftOperand = ((rn == PC) && (i == 0) && (r == 1)) ? (cpu.Reg[PC] + 4) : cpu.Reg[rn];
            UInt32 rightOperand = shifterOperand;

            cpu.ARM_SetReg(rd, leftOperand - rightOperand);

            if (s == 1)
            {
                if (rd == PC)
                {
                    cpu.SetCPSR(cpu.SPSR);
                }
                else
                {
                    cpu.SetFlag(Flags.N, cpu.Reg[rd] >> 31);
                    cpu.SetFlag(Flags.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
                    cpu.SetFlag(Flags.C, Not(BorrowFrom(leftOperand, rightOperand)));
                    cpu.SetFlag(Flags.V, OverflowFrom_Subtraction(leftOperand, rightOperand, cpu.Reg[rd]));
                }
            }
        }

        private static void ARM_SWI(CPU cpu, UInt32 instruction)
        {
            UInt32 imm = instruction & 0xff_ffff;

            cpu._callbacks.HandleSWI(imm);
        }

        private static void ARM_SWP(CPU cpu, UInt32 instruction)
        {
            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 rd = (instruction >> 12) & 0b1111;
            UInt32 rm = instruction & 0b1111;

            UInt32 temp = BitOperations.RotateRight(cpu._callbacks.ReadMemory32(cpu.Reg[rn]), (int)(8 * (cpu.Reg[rn] & 0b11)));
            cpu._callbacks.WriteMemory32(cpu.Reg[rn], cpu.Reg[rm]);
            cpu.ARM_SetReg(rd, temp);
        }

        private static void ARM_SWPB(CPU cpu, UInt32 instruction)
        {
            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 rd = (instruction >> 12) & 0b1111;
            UInt32 rm = instruction & 0b1111;

            Byte temp = cpu._callbacks.ReadMemory8(cpu.Reg[rn]);
            cpu._callbacks.WriteMemory8(cpu.Reg[rn], (Byte)cpu.Reg[rm]);
            cpu.ARM_SetReg(rd, temp);
        }

        private static void ARM_TEQ(CPU cpu, UInt32 instruction)
        {
            UInt32 i = (instruction >> 25) & 1;
            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 rd = (instruction >> 12) & 0b1111;
            UInt32 r = (instruction >> 4) & 1;

            var (shifterOperand, shifterCarryOut) = cpu.GetShifterOperand(instruction);

            UInt32 leftOperand = ((rn == PC) && (i == 0) && (r == 1)) ? (cpu.Reg[PC] + 4) : cpu.Reg[rn];
            UInt32 rightOperand = shifterOperand;

            UInt32 aluOut = leftOperand ^ rightOperand;

            if (rd == PC)
            {
                if ((cpu.CPSR & ModeMask) == UserMode)
                    cpu.CPSR = (cpu.CPSR & ~0xf000_0000) | (aluOut & 0xf000_0000);
                else
                    cpu.SetCPSR((cpu.CPSR & ~0xf000_00c3) | (aluOut & 0xf000_0003) | (((aluOut >> 26) & 0b11) << 6));
            }
            else
            {
                cpu.SetFlag(Flags.N, aluOut >> 31);
                cpu.SetFlag(Flags.Z, (aluOut == 0) ? 1u : 0u);
                cpu.SetFlag(Flags.C, shifterCarryOut);
            }
        }

        private static void ARM_TST(CPU cpu, UInt32 instruction)
        {
            UInt32 i = (instruction >> 25) & 1;
            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 rd = (instruction >> 12) & 0b1111;
            UInt32 r = (instruction >> 4) & 1;

            var (shifterOperand, shifterCarryOut) = cpu.GetShifterOperand(instruction);

            UInt32 leftOperand = ((rn == PC) && (i == 0) && (r == 1)) ? (cpu.Reg[PC] + 4) : cpu.Reg[rn];
            UInt32 rightOperand = shifterOperand;

            UInt32 aluOut = leftOperand & rightOperand;

            if (rd == PC)
            {
                if ((cpu.CPSR & ModeMask) == UserMode)
                    cpu.CPSR = (cpu.CPSR & ~0xf000_0000) | (aluOut & 0xf000_0000);
                else
                    cpu.SetCPSR((cpu.CPSR & ~0xf000_00c3) | (aluOut & 0xf000_0003) | (((aluOut >> 26) & 0b11) << 6));
            }
            else
            {
                cpu.SetFlag(Flags.N, aluOut >> 31);
                cpu.SetFlag(Flags.Z, (aluOut == 0) ? 1u : 0u);
                cpu.SetFlag(Flags.C, shifterCarryOut);
            }
        }

        private static void ARM_UMLAL(CPU cpu, UInt32 instruction)
        {
            UInt32 s = (instruction >> 20) & 1;
            UInt32 rdHi = (instruction >> 16) & 0b1111;
            UInt32 rdLo = (instruction >> 12) & 0b1111;
            UInt32 rs = (instruction >> 8) & 0b1111;
            UInt32 rm = instruction & 0b1111;

            UInt64 result = (UInt64)cpu.Reg[rm] * (UInt64)cpu.Reg[rs];
            UInt64 resultLo = (UInt64)(UInt32)result + (UInt64)cpu.Reg[rdLo];
            UInt32 resultHi = (UInt32)(result >> 32) + cpu.Reg[rdHi] + CarryFrom(resultLo);
            cpu.ARM_SetReg(rdLo, (UInt32)resultLo);
            cpu.ARM_SetReg(rdHi, resultHi);

            if (s == 1)
            {
                cpu.SetFlag(Flags.N, cpu.Reg[rdHi] >> 31);
                cpu.SetFlag(Flags.Z, ((cpu.Reg[rdHi] == 0) && (cpu.Reg[rdLo] == 0)) ? 1u : 0u);
            }
        }

        private static void ARM_UMULL(CPU cpu, UInt32 instruction)
        {
            UInt32 s = (instruction >> 20) & 1;
            UInt32 rdHi = (instruction >> 16) & 0b1111;
            UInt32 rdLo = (instruction >> 12) & 0b1111;
            UInt32 rs = (instruction >> 8) & 0b1111;
            UInt32 rm = instruction & 0b1111;

            UInt64 result = (UInt64)cpu.Reg[rm] * (UInt64)cpu.Reg[rs];
            cpu.ARM_SetReg(rdLo, (UInt32)result);
            cpu.ARM_SetReg(rdHi, (UInt32)(result >> 32));

            if (s == 1)
            {
                cpu.SetFlag(Flags.N, cpu.Reg[rdHi] >> 31);
                cpu.SetFlag(Flags.Z, ((cpu.Reg[rdHi] == 0) && (cpu.Reg[rdLo] == 0)) ? 1u : 0u);
            }
        }
    }
}
