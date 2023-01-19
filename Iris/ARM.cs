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

            // CMP
            new(0x0ff0_f000, 0x0350_0000, ARM_CMP), // I bit is 1
            new(0x0ff0_f090, 0x0150_0000, ARM_CMP), // I bit is 0, bit[7] is 0 and bit[4] is 0
            new(0x0ff0_f090, 0x0150_0080, ARM_CMP), // I bit is 0, bit[7] is 1 and bit[4] is 0
            new(0x0ff0_f090, 0x0150_0010, ARM_CMP), // I bit is 0, bit[7] is 0 and bit[4] is 1

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

            // TST
            new(0x0ff0_f000, 0x0310_0000, ARM_TST), // I bit is 1
            new(0x0ff0_f090, 0x0110_0000, ARM_TST), // I bit is 0, bit[7] is 0 and bit[4] is 0
            new(0x0ff0_f090, 0x0110_0080, ARM_TST), // I bit is 0, bit[7] is 1 and bit[4] is 0
            new(0x0ff0_f090, 0x0110_0010, ARM_TST), // I bit is 0, bit[7] is 0 and bit[4] is 1

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

        private bool CurrentModeHasSPSR()
        {
            return (CPSR & ModeMask) switch
            {
                UserMode or SystemMode => false,
                _ => true,
            };
        }

        private static UInt32 SignExtend30(UInt32 value, int size)
        {
            return SignExtend(value, size) & 0x3fff_ffff;
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
            UInt32 shifterOperand = 0;
            UInt32 shifterCarryOut = 0;

            if (((instruction >> 25) & 1) == 1) // 32-bit immediate
            {
                UInt32 rotateImm = (instruction >> 8) & 0b1111;
                UInt32 imm = instruction & 0xff;

                UInt32 rotateAmount = rotateImm * 2;
                shifterOperand = RotateRight(imm, rotateAmount);

                if (rotateImm == 0)
                    shifterCarryOut = GetFlag(Flags.C);
                else
                    shifterCarryOut = shifterOperand >> 31;
            }
            else
            {
                UInt32 shift = (instruction >> 5) & 0b11;
                UInt32 rm = instruction & 0b1111;
                if (((instruction >> 4) & 1) == 0) // Immediate shifts
                {
                    UInt32 shiftImm = (instruction >> 7) & 0b1_1111;
                    switch (shift)
                    {
                        case 0b00: // Logical shift left
                            if (shiftImm == 0)
                            {
                                shifterOperand = Reg[rm];
                                shifterCarryOut = GetFlag(Flags.C);
                            }
                            else
                            {
                                shifterOperand = Reg[rm] << (int)shiftImm;
                                shifterCarryOut = (Reg[rm] >> (32 - (int)shiftImm)) & 1;
                            }
                            break;
                        case 0b01: // Logical shift right
                            if (shiftImm == 0)
                            {
                                shifterOperand = 0;
                                shifterCarryOut = Reg[rm] >> 31;
                            }
                            else
                            {
                                shifterOperand = Reg[rm] >> (int)shiftImm;
                                shifterCarryOut = (Reg[rm] >> ((int)shiftImm - 1)) & 1;
                            }
                            break;
                        case 0b10: // Arithmetic shift right
                            if (shiftImm == 0)
                            {
                                if ((Reg[rm] >> 31) == 0)
                                    shifterOperand = 0;
                                else
                                    shifterOperand = 0xffff_ffff;

                                shifterCarryOut = Reg[rm] >> 31;
                            }
                            else
                            {
                                shifterOperand = ArithmeticShiftRight(Reg[rm], shiftImm);
                                shifterCarryOut = (Reg[rm] >> ((int)shiftImm - 1)) & 1;
                            }
                            break;
                        case 0b11: // Rotate right
                            if (shiftImm == 0)
                            {
                                shifterOperand = (GetFlag(Flags.C) << 31) | (Reg[rm] >> 1);
                                shifterCarryOut = Reg[rm] & 1;
                            }
                            else
                            {
                                shifterOperand = RotateRight(Reg[rm], shiftImm);
                                shifterCarryOut = (Reg[rm] >> ((int)shiftImm - 1)) & 1;
                            }
                            break;
                    }
                }
                else if (((instruction >> 7) & 1) == 0) // Register shifts
                {
                    UInt32 rs = (instruction >> 8) & 0b1111;
                    UInt32 regRm = (rm == PC) ? Reg[PC] + 4 : Reg[rm];
                    UInt32 regRs = Reg[rs] & 0xff;
                    switch (shift)
                    {
                        case 0b00: // Logical shift left
                            if (regRs == 0)
                            {
                                shifterOperand = regRm;
                                shifterCarryOut = GetFlag(Flags.C);
                            }
                            else if (regRs < 32)
                            {
                                shifterOperand = regRm << (int)regRs;
                                shifterCarryOut = (regRm >> (32 - (int)regRs)) & 1;
                            }
                            else if (regRs == 32)
                            {
                                shifterOperand = 0;
                                shifterCarryOut = regRm & 1;
                            }
                            else
                            {
                                shifterOperand = 0;
                                shifterCarryOut = 0;
                            }
                            break;
                        case 0b01: // Logical shift right
                            if (regRs == 0)
                            {
                                shifterOperand = regRm;
                                shifterCarryOut = GetFlag(Flags.C);
                            }
                            else if (regRs < 32)
                            {
                                shifterOperand = regRm >> (int)regRs;
                                shifterCarryOut = (regRm >> ((int)regRs - 1)) & 1;
                            }
                            else if (regRs == 32)
                            {
                                shifterOperand = 0;
                                shifterCarryOut = regRm >> 31;
                            }
                            else
                            {
                                shifterOperand = 0;
                                shifterCarryOut = 0;
                            }
                            break;
                        case 0b10: // Arithmetic shift right
                            if (regRs == 0)
                            {
                                shifterOperand = regRm;
                                shifterCarryOut = GetFlag(Flags.C);
                            }
                            else if (regRs < 32)
                            {
                                shifterOperand = ArithmeticShiftRight(regRm, regRs);
                                shifterCarryOut = (regRm >> ((int)regRs - 1)) & 1;
                            }
                            else
                            {
                                if ((regRm >> 31) == 0)
                                    shifterOperand = 0;
                                else
                                    shifterOperand = 0xffff_ffff;

                                shifterCarryOut = regRm >> 31;
                            }
                            break;
                        case 0b11: // Rotate right
                            if (regRs == 0)
                            {
                                shifterOperand = regRm;
                                shifterCarryOut = GetFlag(Flags.C);
                            }
                            else if ((regRs & 0b1_1111) == 0)
                            {
                                shifterOperand = regRm;
                                shifterCarryOut = regRm >> 31;
                            }
                            else
                            {
                                shifterOperand = RotateRight(regRm, regRs & 0b1_1111);
                                shifterCarryOut = (regRm >> ((int)(regRs & 0b1_1111) - 1)) & 1;
                            }
                            break;
                    }
                }
                else
                {
                    throw new Exception("CPU: Wrong addressing mode 1 encoding");
                }
            }

            return (shifterOperand, shifterCarryOut);
        }

        // Addressing mode 2
        private UInt32 GetAddress(UInt32 instruction)
        {
            UInt32 address;

            UInt32 p = (instruction >> 24) & 1;
            UInt32 u = (instruction >> 23) & 1;
            UInt32 w = (instruction >> 21) & 1;
            UInt32 rn = (instruction >> 16) & 0b1111;
            if (((instruction >> 25) & 1) == 0) // Immediate
            {
                UInt32 offset = instruction & 0xfff;
                if (p == 0) // Post-indexed
                {
                    address = Reg[rn];

                    if (u == 1)
                        ARM_SetReg(rn, Reg[rn] + offset);
                    else
                        ARM_SetReg(rn, Reg[rn] - offset);
                }
                else if (w == 0) // Offset
                {
                    if (u == 1)
                        address = Reg[rn] + offset;
                    else
                        address = Reg[rn] - offset;
                }
                else // Pre-indexed
                {
                    if (u == 1)
                        address = Reg[rn] + offset;
                    else
                        address = Reg[rn] - offset;

                    ARM_SetReg(rn, address);
                }
            }
            else if (((instruction >> 4) & 1) == 0)
            {
                UInt32 shiftImm = (instruction >> 7) & 0b1_1111;
                UInt32 shift = (instruction >> 5) & 0b11;
                UInt32 rm = instruction & 0b1111;
                if ((shiftImm == 0) && shift == 0) // Register
                {
                    if (p == 0) // Post-indexed
                    {
                        address = Reg[rn];

                        if (u == 1)
                            ARM_SetReg(rn, Reg[rn] + Reg[rm]);
                        else
                            ARM_SetReg(rn, Reg[rn] - Reg[rm]);
                    }
                    else if (w == 0) // Offset
                    {
                        if (u == 1)
                            address = Reg[rn] + Reg[rm];
                        else
                            address = Reg[rn] - Reg[rm];
                    }
                    else // Pre-indexed
                    {
                        if (u == 1)
                            address = Reg[rn] + Reg[rm];
                        else
                            address = Reg[rn] - Reg[rm];

                        ARM_SetReg(rn, address);
                    }
                }
                else // Scaled register
                {
                    if (p == 0) // Post-indexed
                    {
                        address = Reg[rn];

                        UInt32 index = 0;
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
                                {
                                    if ((Reg[rm] >> 31) == 1)
                                        index = 0xffff_ffff;
                                    else
                                        index = 0;
                                }
                                else
                                {
                                    index = ArithmeticShiftRight(Reg[rm], shiftImm);
                                }
                                break;
                            case 0b11:
                                if (shiftImm == 0) // RRX
                                    index = (GetFlag(Flags.C) << 31) | (Reg[rm] >> 1);
                                else // ROR
                                    index = RotateRight(Reg[rm], shiftImm);
                                break;
                        }

                        if (u == 1)
                            ARM_SetReg(rn, Reg[rn] + index);
                        else
                            ARM_SetReg(rn, Reg[rn] - index);
                    }
                    else if (w == 0) // Offset
                    {
                        UInt32 index = 0;
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
                                {
                                    if ((Reg[rm] >> 31) == 1)
                                        index = 0xffff_ffff;
                                    else
                                        index = 0;
                                }
                                else
                                {
                                    index = ArithmeticShiftRight(Reg[rm], shiftImm);
                                }
                                break;
                            case 0b11:
                                if (shiftImm == 0) // RRX
                                    index = (GetFlag(Flags.C) << 31) | (Reg[rm] >> 1);
                                else // ROR
                                    index = RotateRight(Reg[rm], shiftImm);
                                break;
                        }

                        if (u == 1)
                            address = Reg[rn] + index;
                        else
                            address = Reg[rn] - index;
                    }
                    else // Pre-indexed
                    {
                        UInt32 index = 0;
                        switch (shift)
                        {
                            case 0b00: // LSL
                                index = (Reg[rm] << (int)shiftImm);
                                break;
                            case 0b01: // LSR
                                if (shiftImm == 0)
                                    index = 0;
                                else
                                    index = (Reg[rm] >> (int)shiftImm);
                                break;
                            case 0b10: // ASR
                                if (shiftImm == 0)
                                {
                                    if ((Reg[rm] >> 31) == 1)
                                        index = 0xffff_ffff;
                                    else
                                        index = 0;
                                }
                                else
                                {
                                    index = ArithmeticShiftRight(Reg[rm], shiftImm);
                                }
                                break;
                            case 0b11:
                                if (shiftImm == 0) // RRX
                                    index = (GetFlag(Flags.C) << 31) | (Reg[rm] >> 1);
                                else // ROR
                                    index = RotateRight(Reg[rm], shiftImm);
                                break;
                        }

                        if (u == 1)
                            address = Reg[rn] + index;
                        else
                            address = Reg[rn] - index;

                        ARM_SetReg(rn, address);
                    }
                }
            }
            else
            {
                throw new Exception("CPU: Wrong addressing mode 2 encoding");
            }

            return address;
        }

        // Addressing mode 3
        private UInt32 GetAddress_Misc(UInt32 instruction)
        {
            UInt32 address = 0;

            UInt32 p = (instruction >> 24) & 1;
            UInt32 u = (instruction >> 23) & 1;
            UInt32 w = (instruction >> 21) & 1;
            UInt32 rn = (instruction >> 16) & 0b1111;
            if (((instruction >> 22) & 1) == 1) // Immediate
            {
                UInt32 immH = (instruction >> 8) & 0b1111;
                UInt32 immL = instruction & 0b1111;
                UInt32 offset = (immH << 4) | immL;
                if (p == 0) // Post-indexed
                {
                    address = Reg[rn];

                    if (u == 1)
                        ARM_SetReg(rn, Reg[rn] + offset);
                    else
                        ARM_SetReg(rn, Reg[rn] - offset);
                }
                else if (w == 0) // Offset
                {
                    if (u == 1)
                        address = Reg[rn] + offset;
                    else
                        address = Reg[rn] - offset;
                }
                else // Pre-indexed
                {
                    if (u == 1)
                        address = Reg[rn] + offset;
                    else
                        address = Reg[rn] - offset;

                    ARM_SetReg(rn, address);
                }
            }
            else if (((instruction >> 8) & 0b1111) == 0) // Register
            {
                if (p == 0) // Post-indexed
                {
                    Console.WriteLine("CPU: encoding unimplemented");
                    Environment.Exit(1);
                }
                else if (w == 0) // Offset
                {
                    UInt32 rm = instruction & 0b1111;
                    if (u == 1)
                        address = Reg[rn] + Reg[rm];
                    else
                        address = Reg[rn] - Reg[rm];
                }
                else // Pre-indexed
                {
                    Console.WriteLine("CPU: encoding unimplemented");
                    Environment.Exit(1);
                }
            }
            else
            {
                throw new Exception("CPU: Wrong addressing mode 3 encoding");
            }

            return address;
        }

        // Addressing mode 4
        private (UInt32 startAddress, UInt32 endAddress) GetAddress_Multiple(UInt32 instruction)
        {
            UInt32 startAddress;
            UInt32 endAddress;

            UInt32 p = (instruction >> 24) & 1;
            UInt32 u = (instruction >> 23) & 1;
            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 registerList = instruction & 0xffff;

            UInt32 value;
            if (u == 1) // increment
            {
                value = Reg[rn] + (NumberOfSetBitsIn(registerList, 16) * 4);

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
                value = Reg[rn] - (NumberOfSetBitsIn(registerList, 16) * 4);

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

            UInt32 w = (instruction >> 21) & 1;
            if (w == 1)
                ARM_SetReg(rn, value);

            return (startAddress, endAddress);
        }

        private static void ARM_ADC(CPU cpu, UInt32 instruction)
        {
            var (shifterOperand, _) = cpu.GetShifterOperand(instruction);

            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 rd = (instruction >> 12) & 0b1111;

            UInt32 regRn = cpu.Reg[rn];
            UInt64 rightOperand = (UInt64)shifterOperand + (UInt64)cpu.GetFlag(Flags.C);
            UInt64 result = (UInt64)regRn + rightOperand;
            cpu.ARM_SetReg(rd, (UInt32)result);

            UInt32 s = (instruction >> 20) & 1;
            if (s == 1)
            {
                if (rd == PC)
                {
                    Console.WriteLine("ADC: S field partially implemented");
                    Environment.Exit(1);
                }
                else
                {
                    cpu.SetFlag(Flags.N, cpu.Reg[rd] >> 31);
                    cpu.SetFlag(Flags.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
                    cpu.SetFlag(Flags.C, CarryFrom(result));
                    cpu.SetFlag(Flags.V, OverflowFrom_Addition(regRn, (UInt32)rightOperand, cpu.Reg[rd]));
                }
            }
        }

        private static void ARM_ADD(CPU cpu, UInt32 instruction)
        {
            var (shifterOperand, _) = cpu.GetShifterOperand(instruction);

            UInt32 i = (instruction >> 25) & 1;
            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 rd = (instruction >> 12) & 0b1111;
            UInt32 r = (instruction >> 4) & 1;

            UInt32 regRn = ((rn == PC) && (i == 0) && (r == 1)) ? cpu.Reg[PC] + 4 : cpu.Reg[rn];
            UInt64 result = (UInt64)regRn + (UInt64)shifterOperand;
            cpu.ARM_SetReg(rd, (UInt32)result);

            UInt32 s = (instruction >> 20) & 1;
            if (s == 1)
            {
                if (rd == PC)
                {
                    Console.WriteLine("ADD: S field partially implemented");
                    Environment.Exit(1);
                }
                else
                {
                    cpu.SetFlag(Flags.N, cpu.Reg[rd] >> 31);
                    cpu.SetFlag(Flags.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
                    cpu.SetFlag(Flags.C, CarryFrom(result));
                    cpu.SetFlag(Flags.V, OverflowFrom_Addition(regRn, shifterOperand, cpu.Reg[rd]));
                }
            }
        }

        private static void ARM_AND(CPU cpu, UInt32 instruction)
        {
            var (shifterOperand, shifterCarryOut) = cpu.GetShifterOperand(instruction);

            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 rd = (instruction >> 12) & 0b1111;
            cpu.ARM_SetReg(rd, cpu.Reg[rn] & shifterOperand);

            UInt32 s = (instruction >> 20) & 1;
            if (s == 1)
            {
                if (rd == PC)
                {
                    Console.WriteLine("AND: S field partially implemented");
                    Environment.Exit(1);
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
            cpu.ARM_SetPC(cpu.Reg[PC] + (SignExtend30(imm, 24) << 2));
        }

        private static void ARM_BL(CPU cpu, UInt32 instruction)
        {
            cpu.Reg[LR] = cpu.NextInstructionAddress;

            UInt32 imm = instruction & 0xff_ffff;
            cpu.ARM_SetPC(cpu.Reg[PC] + (SignExtend30(imm, 24) << 2));
        }

        private static void ARM_BIC(CPU cpu, UInt32 instruction)
        {
            var (shifterOperand, shifterCarryOut) = cpu.GetShifterOperand(instruction);

            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 rd = (instruction >> 12) & 0b1111;
            cpu.ARM_SetReg(rd, cpu.Reg[rn] & ~shifterOperand);

            UInt32 s = (instruction >> 20) & 1;
            if (s == 1)
            {
                if (rd == PC)
                {
                    Console.WriteLine("BIC: S field partially implemented");
                    Environment.Exit(1);
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
            cpu.SetCPSR((cpu.CPSR & ~(1u << 5)) | ((cpu.Reg[rm] & 1) << 5));
            cpu.ARM_SetPC(cpu.Reg[rm] & 0xffff_fffe);
        }

        private static void ARM_CMN(CPU cpu, UInt32 instruction)
        {
            var (shifterOperand, _) = cpu.GetShifterOperand(instruction);

            UInt32 rn = (instruction >> 16) & 0b1111;

            UInt64 result = (UInt64)cpu.Reg[rn] + (UInt64)shifterOperand;
            UInt32 aluOut = (UInt32)result;

            cpu.SetFlag(Flags.N, aluOut >> 31);
            cpu.SetFlag(Flags.Z, (aluOut == 0) ? 1u : 0u);
            cpu.SetFlag(Flags.C, CarryFrom(result));
            cpu.SetFlag(Flags.V, OverflowFrom_Addition(cpu.Reg[rn], shifterOperand, aluOut));
        }

        private static void ARM_CMP(CPU cpu, UInt32 instruction)
        {
            var (shifterOperand, _) = cpu.GetShifterOperand(instruction);

            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 aluOut = cpu.Reg[rn] - shifterOperand;

            cpu.SetFlag(Flags.N, aluOut >> 31);
            cpu.SetFlag(Flags.Z, (aluOut == 0) ? 1u : 0u);
            cpu.SetFlag(Flags.C, Not(BorrowFrom(cpu.Reg[rn], shifterOperand)));
            cpu.SetFlag(Flags.V, OverflowFrom_Subtraction(cpu.Reg[rn], shifterOperand, aluOut));
        }

        private static void ARM_EOR(CPU cpu, UInt32 instruction)
        {
            var (shifterOperand, shifterCarryOut) = cpu.GetShifterOperand(instruction);

            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 rd = (instruction >> 12) & 0b1111;
            cpu.ARM_SetReg(rd, cpu.Reg[rn] ^ shifterOperand);

            UInt32 s = (instruction >> 20) & 1;
            if (s == 1)
            {
                if (rd == PC)
                {
                    Console.WriteLine("EOR: S field partially implemented");
                    Environment.Exit(1);
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
            var (startAddress, _) = cpu.GetAddress_Multiple(instruction);

            UInt32 address = startAddress;

            UInt32 registerList = instruction & 0xffff;
            for (var i = 0; i <= 14; ++i)
            {
                if (((registerList >> i) & 1) == 1)
                {
                    cpu.Reg[i] = cpu._callbacks.ReadMemory32(address);
                    address += 4;
                }
            }

            if (((registerList >> 15) & 1) == 1)
            {
                UInt32 value = cpu._callbacks.ReadMemory32(address);
                cpu.ARM_SetPC(value & 0xffff_fffc);
            }
        }

        private static void ARM_LDM2(CPU cpu, UInt32 instruction)
        {
            var (startAddress, _) = cpu.GetAddress_Multiple(instruction);

            UInt32 address = startAddress;

            UInt32 registerList = instruction & 0x7fff;
            for (var i = 0; i <= 14; ++i)
            {
                if (((registerList >> i) & 1) == 1)
                {
                    UInt32 value = cpu._callbacks.ReadMemory32(address);
                    switch (i)
                    {
                        case 8:
                            cpu.Reg8 = value;
                            break;
                        case 9:
                            cpu.Reg9 = value;
                            break;
                        case 10:
                            cpu.Reg10 = value;
                            break;
                        case 11:
                            cpu.Reg11 = value;
                            break;
                        case 12:
                            cpu.Reg12 = value;
                            break;
                        case 13:
                            cpu.Reg13 = value;
                            break;
                        case 14:
                            cpu.Reg14 = value;
                            break;
                        default:
                            cpu.Reg[i] = value;
                            break;
                    }

                    address += 4;
                }
            }
        }

        private static void ARM_LDR(CPU cpu, UInt32 instruction)
        {
            UInt32 address = cpu.GetAddress(instruction);

            UInt32 data = RotateRight(cpu._callbacks.ReadMemory32(address), 8 * (address & 0b11));
            UInt32 rd = (instruction >> 12) & 0b1111;
            if (rd == PC)
                cpu.ARM_SetPC(data & 0xffff_fffc);
            else
                cpu.Reg[rd] = data;
        }

        private static void ARM_LDRB(CPU cpu, UInt32 instruction)
        {
            UInt32 address = cpu.GetAddress(instruction);

            UInt32 rd = (instruction >> 12) & 0b1111;
            cpu.ARM_SetReg(rd, cpu._callbacks.ReadMemory8(address));
        }

        private static void ARM_LDRH(CPU cpu, UInt32 instruction)
        {
            UInt32 address = cpu.GetAddress_Misc(instruction);

            UInt16 data = cpu._callbacks.ReadMemory16(address);
            UInt32 rd = (instruction >> 12) & 0b1111;
            cpu.ARM_SetReg(rd, data);
        }

        private static void ARM_LDRSB(CPU cpu, UInt32 instruction)
        {
            UInt32 address = cpu.GetAddress_Misc(instruction);

            Byte data = cpu._callbacks.ReadMemory8(address);
            UInt32 rd = (instruction >> 12) & 0b1111;
            cpu.ARM_SetReg(rd, SignExtend(data, 8));
        }

        private static void ARM_LDRSH(CPU cpu, UInt32 instruction)
        {
            UInt32 address = cpu.GetAddress_Misc(instruction);

            UInt16 data = cpu._callbacks.ReadMemory16(address);
            UInt32 rd = (instruction >> 12) & 0b1111;
            cpu.ARM_SetReg(rd, SignExtend(data, 16));
        }

        private static void ARM_MLA(CPU cpu, UInt32 instruction)
        {
            UInt32 rd = (instruction >> 16) & 0b1111;
            UInt32 rn = (instruction >> 12) & 0b1111;
            UInt32 rs = (instruction >> 8) & 0b1111;
            UInt32 rm = instruction & 0b1111;
            cpu.ARM_SetReg(rd, (cpu.Reg[rm] * cpu.Reg[rs]) + cpu.Reg[rn]);

            UInt32 s = (instruction >> 20) & 1;
            if (s == 1)
            {
                if (rd == PC)
                {
                    Console.WriteLine("MLA: S field partially implemented");
                    Environment.Exit(1);
                }
                else
                {
                    cpu.SetFlag(Flags.N, cpu.Reg[rd] >> 31);
                    cpu.SetFlag(Flags.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
                }
            }
        }

        private static void ARM_MOV(CPU cpu, UInt32 instruction)
        {
            var (shifterOperand, shifterCarryOut) = cpu.GetShifterOperand(instruction);

            UInt32 rd = (instruction >> 12) & 0b1111;
            cpu.ARM_SetReg(rd, shifterOperand);

            UInt32 s = (instruction >> 20) & 1;
            if (s == 1)
            {
                if (rd == PC)
                {
                    Console.WriteLine("MOV: S field partially implemented");
                    Environment.Exit(1);
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

            if (r == 1)
                cpu.ARM_SetReg(rd, cpu.SPSR);
            else
                cpu.ARM_SetReg(rd, cpu.CPSR);
        }

        private static void ARM_MSR(CPU cpu, UInt32 instruction)
        {
            UInt32 operand;
            if (((instruction >> 25) & 1) == 1)
            {
                UInt32 rotateImm = (instruction >> 8) & 0b1111;
                UInt32 imm = instruction & 0xff;

                UInt32 rotateAmount = 2 * rotateImm;
                operand = RotateRight(imm, rotateAmount);
            }
            else
            {
                UInt32 rm = instruction & 0b1111;
                operand = cpu.Reg[rm];
            }

            UInt32 fieldMask = (instruction >> 16) & 0b1111;
            UInt32 byteMask = (UInt32)((((fieldMask >> 0) & 1) == 1) ? 0x0000_00ff : 0)
                            | (UInt32)((((fieldMask >> 1) & 1) == 1) ? 0x0000_ff00 : 0)
                            | (UInt32)((((fieldMask >> 2) & 1) == 1) ? 0x00ff_0000 : 0)
                            | (UInt32)((((fieldMask >> 3) & 1) == 1) ? 0xff00_0000 : 0);

            const UInt32 UserMask = 0xf000_0000;
            const UInt32 PrivMask = 0x0000_000f;
            const UInt32 StateMask = 0x0000_0020;

            UInt32 r = (instruction >> 22) & 1;
            if (r == 0)
            {
                UInt32 mask;
                if ((cpu.CPSR & ModeMask) != UserMode)
                    mask = byteMask & (UserMask | PrivMask);
                else
                    mask = byteMask & UserMask;

                cpu.SetCPSR((cpu.CPSR & ~mask) | (operand & mask));
            }
            else if (cpu.CurrentModeHasSPSR())
            {
                UInt32 mask = byteMask & (UserMask | PrivMask | StateMask);
                cpu.SPSR = (cpu.SPSR & ~mask) | (operand & mask);
            }
        }

        private static void ARM_MUL(CPU cpu, UInt32 instruction)
        {
            UInt32 rd = (instruction >> 16) & 0b1111;
            UInt32 rs = (instruction >> 8) & 0b1111;
            UInt32 rm = instruction & 0b1111;
            cpu.ARM_SetReg(rd, cpu.Reg[rm] * cpu.Reg[rs]);

            UInt32 s = (instruction >> 20) & 1;
            if (s == 1)
            {
                if (rd == PC)
                {
                    Console.WriteLine("MUL: S field partially implemented");
                    Environment.Exit(1);
                }
                else
                {
                    cpu.SetFlag(Flags.N, cpu.Reg[rd] >> 31);
                    cpu.SetFlag(Flags.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
                }
            }
        }

        private static void ARM_MVN(CPU cpu, UInt32 instruction)
        {
            var (shifterOperand, shifterCarryOut) = cpu.GetShifterOperand(instruction);

            UInt32 rd = (instruction >> 12) & 0b1111;
            cpu.ARM_SetReg(rd, ~shifterOperand);

            UInt32 s = (instruction >> 20) & 1;
            if (s == 1)
            {
                if (rd == PC)
                {
                    Console.WriteLine("MVN: S field partially implemented");
                    Environment.Exit(1);
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
            var (shifterOperand, shifterCarryOut) = cpu.GetShifterOperand(instruction);

            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 rd = (instruction >> 12) & 0b1111;
            cpu.ARM_SetReg(rd, cpu.Reg[rn] | shifterOperand);

            UInt32 s = (instruction >> 20) & 1;
            if (s == 1)
            {
                if (rd == PC)
                {
                    Console.WriteLine("ORR: S field partially implemented");
                    Environment.Exit(1);
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
            var (shifterOperand, _) = cpu.GetShifterOperand(instruction);

            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 rd = (instruction >> 12) & 0b1111;

            UInt32 regRn = cpu.Reg[rn];
            cpu.ARM_SetReg(rd, shifterOperand - regRn);

            UInt32 s = (instruction >> 20) & 1;
            if (s == 1)
            {
                if (rd == PC)
                {
                    Console.WriteLine("RSC: S field partially implemented");
                    Environment.Exit(1);
                }
                else
                {
                    cpu.SetFlag(Flags.N, cpu.Reg[rd] >> 31);
                    cpu.SetFlag(Flags.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
                    cpu.SetFlag(Flags.C, Not(BorrowFrom(shifterOperand, regRn)));
                    cpu.SetFlag(Flags.V, OverflowFrom_Subtraction(shifterOperand, regRn, cpu.Reg[rd]));
                }
            }
        }

        private static void ARM_RSC(CPU cpu, UInt32 instruction)
        {
            var (shifterOperand, _) = cpu.GetShifterOperand(instruction);

            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 rd = (instruction >> 12) & 0b1111;

            UInt64 rightOperand = (UInt64)cpu.Reg[rn] + (UInt64)Not(cpu.GetFlag(Flags.C));
            cpu.ARM_SetReg(rd, shifterOperand - (UInt32)rightOperand);

            UInt32 s = (instruction >> 20) & 1;
            if (s == 1)
            {
                if (rd == PC)
                {
                    Console.WriteLine("RSC: S field partially implemented");
                    Environment.Exit(1);
                }
                else
                {
                    cpu.SetFlag(Flags.N, cpu.Reg[rd] >> 31);
                    cpu.SetFlag(Flags.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
                    cpu.SetFlag(Flags.C, Not(BorrowFrom(shifterOperand, rightOperand)));
                    cpu.SetFlag(Flags.V, OverflowFrom_Subtraction(shifterOperand, (UInt32)rightOperand, cpu.Reg[rd]));
                }
            }
        }

        private static void ARM_SBC(CPU cpu, UInt32 instruction)
        {
            var (shifterOperand, _) = cpu.GetShifterOperand(instruction);

            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 rd = (instruction >> 12) & 0b1111;

            UInt32 regRn = cpu.Reg[rn];
            UInt64 rightOperand = (UInt64)shifterOperand + (UInt64)Not(cpu.GetFlag(Flags.C));
            cpu.ARM_SetReg(rd, regRn - (UInt32)rightOperand);

            UInt32 s = (instruction >> 20) & 1;
            if (s == 1)
            {
                if (rd == PC)
                {
                    Console.WriteLine("SBC: S field partially implemented");
                    Environment.Exit(1);
                }
                else
                {
                    cpu.SetFlag(Flags.N, cpu.Reg[rd] >> 31);
                    cpu.SetFlag(Flags.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
                    cpu.SetFlag(Flags.C, Not(BorrowFrom(regRn, rightOperand)));
                    cpu.SetFlag(Flags.V, OverflowFrom_Subtraction(regRn, (UInt32)rightOperand, cpu.Reg[rd]));
                }
            }
        }

        private static void ARM_SMLAL(CPU cpu, UInt32 instruction)
        {
            UInt32 rdHi = (instruction >> 16) & 0b1111;
            UInt32 rdLo = (instruction >> 12) & 0b1111;
            UInt32 rs = (instruction >> 8) & 0b1111;
            UInt32 rm = instruction & 0b1111;

            Int64 result = (Int64)(Int32)cpu.Reg[rm] * (Int64)(Int32)cpu.Reg[rs];
            UInt64 resultLo = (UInt64)(UInt32)result + (UInt64)cpu.Reg[rdLo];
            cpu.ARM_SetReg(rdLo, (UInt32)resultLo);
            cpu.ARM_SetReg(rdHi, cpu.Reg[rdHi] + (UInt32)(result >> 32) + CarryFrom(resultLo));

            UInt32 s = (instruction >> 20) & 1;
            if (s == 1)
            {
                cpu.SetFlag(Flags.N, cpu.Reg[rdHi] >> 31);
                cpu.SetFlag(Flags.Z, (cpu.Reg[rdHi] == 0 && cpu.Reg[rdLo] == 0) ? 1u : 0u);
            }
        }

        private static void ARM_SMULL(CPU cpu, UInt32 instruction)
        {
            UInt32 rdHi = (instruction >> 16) & 0b1111;
            UInt32 rdLo = (instruction >> 12) & 0b1111;
            UInt32 rs = (instruction >> 8) & 0b1111;
            UInt32 rm = instruction & 0b1111;

            Int64 result = (Int64)(Int32)cpu.Reg[rm] * (Int64)(Int32)cpu.Reg[rs];
            cpu.ARM_SetReg(rdHi, (UInt32)(result >> 32));
            cpu.ARM_SetReg(rdLo, (UInt32)result);

            UInt32 s = (instruction >> 20) & 1;
            if (s == 1)
            {
                cpu.SetFlag(Flags.N, cpu.Reg[rdHi] >> 31);
                cpu.SetFlag(Flags.Z, (cpu.Reg[rdHi] == 0 && cpu.Reg[rdLo] == 0) ? 1u : 0u);
            }
        }

        private static void ARM_STM1(CPU cpu, UInt32 instruction)
        {
            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 regRn = cpu.Reg[rn];

            var (startAddress, _) = cpu.GetAddress_Multiple(instruction);

            UInt32 address = startAddress;

            UInt32 registerList = instruction & 0xffff;
            for (var i = 0; i <= 15; ++i)
            {
                if (((registerList >> i) & 1) == 1)
                {
                    cpu._callbacks.WriteMemory32(address, (i == rn) ? regRn : cpu.Reg[i]);
                    address += 4;
                }
            }
        }

        private static void ARM_STR(CPU cpu, UInt32 instruction)
        {
            UInt32 address = cpu.GetAddress(instruction);

            UInt32 rd = (instruction >> 12) & 0b1111;
            cpu._callbacks.WriteMemory32(address, cpu.Reg[rd]);
        }

        private static void ARM_STRB(CPU cpu, UInt32 instruction)
        {
            UInt32 address = cpu.GetAddress(instruction);

            UInt32 rd = (instruction >> 12) & 0b1111;
            cpu._callbacks.WriteMemory8(address, (Byte)cpu.Reg[rd]);
        }

        private static void ARM_STRH(CPU cpu, UInt32 instruction)
        {
            UInt32 address = cpu.GetAddress_Misc(instruction);

            UInt32 rd = (instruction >> 12) & 0b1111;
            cpu._callbacks.WriteMemory16(address, (UInt16)cpu.Reg[rd]);
        }

        private static void ARM_SUB(CPU cpu, UInt32 instruction)
        {
            var (shifterOperand, _) = cpu.GetShifterOperand(instruction);

            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 rd = (instruction >> 12) & 0b1111;

            UInt32 regRn = cpu.Reg[rn];
            cpu.ARM_SetReg(rd, regRn - shifterOperand);

            UInt32 s = (instruction >> 20) & 1;
            if (s == 1)
            {
                if (rd == PC)
                {
                    if (cpu.CurrentModeHasSPSR())
                        cpu.SetCPSR(cpu.SPSR);
                }
                else
                {
                    cpu.SetFlag(Flags.N, cpu.Reg[rd] >> 31);
                    cpu.SetFlag(Flags.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
                    cpu.SetFlag(Flags.C, Not(BorrowFrom(regRn, shifterOperand)));
                    cpu.SetFlag(Flags.V, OverflowFrom_Subtraction(regRn, shifterOperand, cpu.Reg[rd]));
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

            UInt32 address = cpu.Reg[rn];
            UInt32 temp = RotateRight(cpu._callbacks.ReadMemory32(address), 8 * (address & 0b11));
            cpu._callbacks.WriteMemory32(address, cpu.Reg[rm]);
            cpu.ARM_SetReg(rd, temp);
        }

        private static void ARM_SWPB(CPU cpu, UInt32 instruction)
        {
            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 rd = (instruction >> 12) & 0b1111;
            UInt32 rm = instruction & 0b1111;

            UInt32 address = cpu.Reg[rn];
            Byte temp = cpu._callbacks.ReadMemory8(address);
            cpu._callbacks.WriteMemory8(address, (Byte)cpu.Reg[rm]);
            cpu.ARM_SetReg(rd, temp);
        }

        private static void ARM_TEQ(CPU cpu, UInt32 instruction)
        {
            var (shifterOperand, shifterCarryOut) = cpu.GetShifterOperand(instruction);

            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 aluOut = cpu.Reg[rn] ^ shifterOperand;

            cpu.SetFlag(Flags.N, aluOut >> 31);
            cpu.SetFlag(Flags.Z, (aluOut == 0) ? 1u : 0u);
            cpu.SetFlag(Flags.C, shifterCarryOut);
        }

        private static void ARM_TST(CPU cpu, UInt32 instruction)
        {
            var (shifterOperand, shifterCarryOut) = cpu.GetShifterOperand(instruction);

            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 aluOut = cpu.Reg[rn] & shifterOperand;

            cpu.SetFlag(Flags.N, aluOut >> 31);
            cpu.SetFlag(Flags.Z, (aluOut == 0) ? 1u : 0u);
            cpu.SetFlag(Flags.C, shifterCarryOut);
        }

        private static void ARM_UMLAL(CPU cpu, UInt32 instruction)
        {
            UInt32 rdHi = (instruction >> 16) & 0b1111;
            UInt32 rdLo = (instruction >> 12) & 0b1111;
            UInt32 rs = (instruction >> 8) & 0b1111;
            UInt32 rm = instruction & 0b1111;

            UInt64 result = (UInt64)cpu.Reg[rm] * (UInt64)cpu.Reg[rs];
            UInt64 resultLo = (UInt64)(UInt32)result + (UInt64)cpu.Reg[rdLo];
            cpu.ARM_SetReg(rdLo, (UInt32)resultLo);
            cpu.ARM_SetReg(rdHi, cpu.Reg[rdHi] + (UInt32)(result >> 32) + CarryFrom(resultLo));

            UInt32 s = (instruction >> 20) & 1;
            if (s == 1)
            {
                cpu.SetFlag(Flags.N, cpu.Reg[rdHi] >> 31);
                cpu.SetFlag(Flags.Z, (cpu.Reg[rdHi] == 0 && cpu.Reg[rdLo] == 0) ? 1u : 0u);
            }
        }

        private static void ARM_UMULL(CPU cpu, UInt32 instruction)
        {
            UInt32 rdHi = (instruction >> 16) & 0b1111;
            UInt32 rdLo = (instruction >> 12) & 0b1111;
            UInt32 rs = (instruction >> 8) & 0b1111;
            UInt32 rm = instruction & 0b1111;

            UInt64 result = (UInt64)cpu.Reg[rm] * (UInt64)cpu.Reg[rs];
            cpu.ARM_SetReg(rdHi, (UInt32)(result >> 32));
            cpu.ARM_SetReg(rdLo, (UInt32)result);

            UInt32 s = (instruction >> 20) & 1;
            if (s == 1)
            {
                cpu.SetFlag(Flags.N, cpu.Reg[rdHi] >> 31);
                cpu.SetFlag(Flags.Z, (cpu.Reg[rdHi] == 0 && cpu.Reg[rdLo] == 0) ? 1u : 0u);
            }
        }
    }
}
