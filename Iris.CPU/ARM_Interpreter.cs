﻿using System.Numerics;
using static Iris.CPU.CPU_Core;

namespace Iris.CPU
{
    internal sealed class ARM_Interpreter
    {
        private readonly CPU_Core _cpu;
        private readonly InstructionLUTEntry<UInt32>[] _instructionLUT = new InstructionLUTEntry<UInt32>[1 << 12];

        internal ARM_Interpreter(CPU_Core cpu)
        {
            _cpu = cpu;

            unsafe
            {
                InstructionListEntry<UInt32>[] InstructionList =
                [
                    // ADC
                    new(0x0fe0_0000, 0x02a0_0000, &ADC, [Model.ARM7TDMI]), // I bit is 1
                    new(0x0fe0_0090, 0x00a0_0000, &ADC, [Model.ARM7TDMI]), // I bit is 0, bit[7] is 0 and bit[4] is 0
                    new(0x0fe0_0090, 0x00a0_0080, &ADC, [Model.ARM7TDMI]), // I bit is 0, bit[7] is 1 and bit[4] is 0
                    new(0x0fe0_0090, 0x00a0_0010, &ADC, [Model.ARM7TDMI]), // I bit is 0, bit[7] is 0 and bit[4] is 1

                    // ADD
                    new(0x0fe0_0000, 0x0280_0000, &ADD, [Model.ARM7TDMI]), // I bit is 1
                    new(0x0fe0_0090, 0x0080_0000, &ADD, [Model.ARM7TDMI]), // I bit is 0, bit[7] is 0 and bit[4] is 0
                    new(0x0fe0_0090, 0x0080_0080, &ADD, [Model.ARM7TDMI]), // I bit is 0, bit[7] is 1 and bit[4] is 0
                    new(0x0fe0_0090, 0x0080_0010, &ADD, [Model.ARM7TDMI]), // I bit is 0, bit[7] is 0 and bit[4] is 1

                    // AND
                    new(0x0fe0_0000, 0x0200_0000, &AND, [Model.ARM7TDMI]), // I bit is 1
                    new(0x0fe0_0090, 0x0000_0000, &AND, [Model.ARM7TDMI]), // I bit is 0, bit[7] is 0 and bit[4] is 0
                    new(0x0fe0_0090, 0x0000_0080, &AND, [Model.ARM7TDMI]), // I bit is 0, bit[7] is 1 and bit[4] is 0
                    new(0x0fe0_0090, 0x0000_0010, &AND, [Model.ARM7TDMI]), // I bit is 0, bit[7] is 0 and bit[4] is 1

                    // B
                    new(0x0f00_0000, 0x0a00_0000, &B, [Model.ARM7TDMI]),

                    // BL
                    new(0x0f00_0000, 0x0b00_0000, &BL, [Model.ARM7TDMI]),

                    // BIC
                    new(0x0fe0_0000, 0x03c0_0000, &BIC, [Model.ARM7TDMI]), // I bit is 1
                    new(0x0fe0_0090, 0x01c0_0000, &BIC, [Model.ARM7TDMI]), // I bit is 0, bit[7] is 0 and bit[4] is 0
                    new(0x0fe0_0090, 0x01c0_0080, &BIC, [Model.ARM7TDMI]), // I bit is 0, bit[7] is 1 and bit[4] is 0
                    new(0x0fe0_0090, 0x01c0_0010, &BIC, [Model.ARM7TDMI]), // I bit is 0, bit[7] is 0 and bit[4] is 1

                    // BX
                    new(0x0fff_fff0, 0x012f_ff10, &BX, [Model.ARM7TDMI]),

                    // CMN
                    new(0x0ff0_f000, 0x0370_0000, &CMN, [Model.ARM7TDMI]), // I bit is 1
                    new(0x0ff0_f090, 0x0170_0000, &CMN, [Model.ARM7TDMI]), // I bit is 0, bit[7] is 0 and bit[4] is 0
                    new(0x0ff0_f090, 0x0170_0080, &CMN, [Model.ARM7TDMI]), // I bit is 0, bit[7] is 1 and bit[4] is 0
                    new(0x0ff0_f090, 0x0170_0010, &CMN, [Model.ARM7TDMI]), // I bit is 0, bit[7] is 0 and bit[4] is 1
                    new(0x0ff0_f000, 0x0370_f000, &CMN, [Model.ARM7TDMI]), // I bit is 1
                    new(0x0ff0_f090, 0x0170_f000, &CMN, [Model.ARM7TDMI]), // I bit is 0, bit[7] is 0 and bit[4] is 0
                    new(0x0ff0_f090, 0x0170_f080, &CMN, [Model.ARM7TDMI]), // I bit is 0, bit[7] is 1 and bit[4] is 0
                    new(0x0ff0_f090, 0x0170_f010, &CMN, [Model.ARM7TDMI]), // I bit is 0, bit[7] is 0 and bit[4] is 1

                    // CMP
                    new(0x0ff0_f000, 0x0350_0000, &CMP, [Model.ARM7TDMI]), // I bit is 1
                    new(0x0ff0_f090, 0x0150_0000, &CMP, [Model.ARM7TDMI]), // I bit is 0, bit[7] is 0 and bit[4] is 0
                    new(0x0ff0_f090, 0x0150_0080, &CMP, [Model.ARM7TDMI]), // I bit is 0, bit[7] is 1 and bit[4] is 0
                    new(0x0ff0_f090, 0x0150_0010, &CMP, [Model.ARM7TDMI]), // I bit is 0, bit[7] is 0 and bit[4] is 1
                    new(0x0ff0_f000, 0x0350_f000, &CMP, [Model.ARM7TDMI]), // I bit is 1
                    new(0x0ff0_f090, 0x0150_f000, &CMP, [Model.ARM7TDMI]), // I bit is 0, bit[7] is 0 and bit[4] is 0
                    new(0x0ff0_f090, 0x0150_f080, &CMP, [Model.ARM7TDMI]), // I bit is 0, bit[7] is 1 and bit[4] is 0
                    new(0x0ff0_f090, 0x0150_f010, &CMP, [Model.ARM7TDMI]), // I bit is 0, bit[7] is 0 and bit[4] is 1

                    // EOR
                    new(0x0fe0_0000, 0x0220_0000, &EOR, [Model.ARM7TDMI]), // I bit is 1
                    new(0x0fe0_0090, 0x0020_0000, &EOR, [Model.ARM7TDMI]), // I bit is 0, bit[7] is 0 and bit[4] is 0
                    new(0x0fe0_0090, 0x0020_0080, &EOR, [Model.ARM7TDMI]), // I bit is 0, bit[7] is 1 and bit[4] is 0
                    new(0x0fe0_0090, 0x0020_0010, &EOR, [Model.ARM7TDMI]), // I bit is 0, bit[7] is 0 and bit[4] is 1

                    // LDM
                    new(0x0e50_0000, 0x0810_0000, &LDM1, [Model.ARM7TDMI]),
                    new(0x0e50_8000, 0x0850_0000, &LDM2, [Model.ARM7TDMI]),
                    //new(0x0e50_8000, 0x0850_8000, &LDM3, new List<Model>{ Model.ARM7TDMI }),

                    // LDR
                    new(0x0c50_0000, 0x0410_0000, &LDR, [Model.ARM7TDMI]),

                    // LDRB
                    new(0x0c50_0000, 0x0450_0000, &LDRB, [Model.ARM7TDMI]),

                    // LDRH
                    new(0x0e10_00f0, 0x0010_00b0, &LDRH, [Model.ARM7TDMI]),

                    // LDRSB
                    new(0x0e10_00f0, 0x0010_00d0, &LDRSB, [Model.ARM7TDMI]),

                    // LDRSH
                    new(0x0e10_00f0, 0x0010_00f0, &LDRSH, [Model.ARM7TDMI]),

                    // MLA
                    new(0x0fe0_00f0, 0x0020_0090, &MLA, [Model.ARM7TDMI]),

                    // MOV
                    new(0x0fef_0000, 0x03a0_0000, &MOV, [Model.ARM7TDMI]), // I bit is 1
                    new(0x0fef_0090, 0x01a0_0000, &MOV, [Model.ARM7TDMI]), // I bit is 0, bit[7] is 0 and bit[4] is 0
                    new(0x0fef_0090, 0x01a0_0080, &MOV, [Model.ARM7TDMI]), // I bit is 0, bit[7] is 1 and bit[4] is 0
                    new(0x0fef_0090, 0x01a0_0010, &MOV, [Model.ARM7TDMI]), // I bit is 0, bit[7] is 0 and bit[4] is 1

                    // MRS
                    new(0x0fbf_0fff, 0x010f_0000, &MRS, [Model.ARM7TDMI]),

                    // MSR
                    new(0x0fb0_f000, 0x0320_f000, &MSR, [Model.ARM7TDMI]), // Immediate operand
                    new(0x0fb0_fff0, 0x0120_f000, &MSR, [Model.ARM7TDMI]), // Register operand

                    // MUL
                    new(0x0fe0_f0f0, 0x0000_0090, &MUL, [Model.ARM7TDMI]),

                    // MVN
                    new(0x0fef_0000, 0x03e0_0000, &MVN, [Model.ARM7TDMI]), // I bit is 1
                    new(0x0fef_0090, 0x01e0_0000, &MVN, [Model.ARM7TDMI]), // I bit is 0, bit[7] is 0 and bit[4] is 0
                    new(0x0fef_0090, 0x01e0_0080, &MVN, [Model.ARM7TDMI]), // I bit is 0, bit[7] is 1 and bit[4] is 0
                    new(0x0fef_0090, 0x01e0_0010, &MVN, [Model.ARM7TDMI]), // I bit is 0, bit[7] is 0 and bit[4] is 1

                    // ORR
                    new(0x0fe0_0000, 0x0380_0000, &ORR, [Model.ARM7TDMI]), // I bit is 1
                    new(0x0fe0_0090, 0x0180_0000, &ORR, [Model.ARM7TDMI]), // I bit is 0, bit[7] is 0 and bit[4] is 0
                    new(0x0fe0_0090, 0x0180_0080, &ORR, [Model.ARM7TDMI]), // I bit is 0, bit[7] is 1 and bit[4] is 0
                    new(0x0fe0_0090, 0x0180_0010, &ORR, [Model.ARM7TDMI]), // I bit is 0, bit[7] is 0 and bit[4] is 1

                    // RSB
                    new(0x0fe0_0000, 0x0260_0000, &RSB, [Model.ARM7TDMI]), // I bit is 1
                    new(0x0fe0_0090, 0x0060_0000, &RSB, [Model.ARM7TDMI]), // I bit is 0, bit[7] is 0 and bit[4] is 0
                    new(0x0fe0_0090, 0x0060_0080, &RSB, [Model.ARM7TDMI]), // I bit is 0, bit[7] is 1 and bit[4] is 0
                    new(0x0fe0_0090, 0x0060_0010, &RSB, [Model.ARM7TDMI]), // I bit is 0, bit[7] is 0 and bit[4] is 1

                    // RSC
                    new(0x0fe0_0000, 0x02e0_0000, &RSC, [Model.ARM7TDMI]), // I bit is 1
                    new(0x0fe0_0090, 0x00e0_0000, &RSC, [Model.ARM7TDMI]), // I bit is 0, bit[7] is 0 and bit[4] is 0
                    new(0x0fe0_0090, 0x00e0_0080, &RSC, [Model.ARM7TDMI]), // I bit is 0, bit[7] is 1 and bit[4] is 0
                    new(0x0fe0_0090, 0x00e0_0010, &RSC, [Model.ARM7TDMI]), // I bit is 0, bit[7] is 0 and bit[4] is 1

                    // SBC
                    new(0x0fe0_0000, 0x02c0_0000, &SBC, [Model.ARM7TDMI]), // I bit is 1
                    new(0x0fe0_0090, 0x00c0_0000, &SBC, [Model.ARM7TDMI]), // I bit is 0, bit[7] is 0 and bit[4] is 0
                    new(0x0fe0_0090, 0x00c0_0080, &SBC, [Model.ARM7TDMI]), // I bit is 0, bit[7] is 1 and bit[4] is 0
                    new(0x0fe0_0090, 0x00c0_0010, &SBC, [Model.ARM7TDMI]), // I bit is 0, bit[7] is 0 and bit[4] is 1

                    // SMLAL
                    new(0x0fe0_00f0, 0x00e0_0090, &SMLAL, [Model.ARM7TDMI]),

                    // SMULL
                    new(0x0fe0_00f0, 0x00c0_0090, &SMULL, [Model.ARM7TDMI]),

                    // STM
                    new(0x0e50_0000, 0x0800_0000, &STM1, [Model.ARM7TDMI]),
                    new(0x0e70_0000, 0x0840_0000, &STM2, [Model.ARM7TDMI]),

                    // STR
                    new(0x0c50_0000, 0x0400_0000, &STR, [Model.ARM7TDMI]),

                    // STRB
                    new(0x0c50_0000, 0x0440_0000, &STRB, [Model.ARM7TDMI]),

                    // STRH
                    new(0x0e10_00f0, 0x0000_00b0, &STRH, [Model.ARM7TDMI]),

                    // SUB
                    new(0x0fe0_0000, 0x0240_0000, &SUB, [Model.ARM7TDMI]), // I bit is 1
                    new(0x0fe0_0090, 0x0040_0000, &SUB, [Model.ARM7TDMI]), // I bit is 0, bit[7] is 0 and bit[4] is 0
                    new(0x0fe0_0090, 0x0040_0080, &SUB, [Model.ARM7TDMI]), // I bit is 0, bit[7] is 1 and bit[4] is 0
                    new(0x0fe0_0090, 0x0040_0010, &SUB, [Model.ARM7TDMI]), // I bit is 0, bit[7] is 0 and bit[4] is 1

                    // SWI
                    new(0x0f00_0000, 0x0f00_0000, &SWI, [Model.ARM7TDMI]),

                    // SWP
                    new(0x0ff0_0ff0, 0x0100_0090, &SWP, [Model.ARM7TDMI]),

                    // SWPB
                    new(0x0ff0_0ff0, 0x0140_0090, &SWPB, [Model.ARM7TDMI]),

                    // TEQ
                    new(0x0ff0_f000, 0x0330_0000, &TEQ, [Model.ARM7TDMI]), // I bit is 1
                    new(0x0ff0_f090, 0x0130_0000, &TEQ, [Model.ARM7TDMI]), // I bit is 0, bit[7] is 0 and bit[4] is 0
                    new(0x0ff0_f090, 0x0130_0080, &TEQ, [Model.ARM7TDMI]), // I bit is 0, bit[7] is 1 and bit[4] is 0
                    new(0x0ff0_f090, 0x0130_0010, &TEQ, [Model.ARM7TDMI]), // I bit is 0, bit[7] is 0 and bit[4] is 1
                    new(0x0ff0_f000, 0x0330_f000, &TEQ, [Model.ARM7TDMI]), // I bit is 1
                    new(0x0ff0_f090, 0x0130_f000, &TEQ, [Model.ARM7TDMI]), // I bit is 0, bit[7] is 0 and bit[4] is 0
                    new(0x0ff0_f090, 0x0130_f080, &TEQ, [Model.ARM7TDMI]), // I bit is 0, bit[7] is 1 and bit[4] is 0
                    new(0x0ff0_f090, 0x0130_f010, &TEQ, [Model.ARM7TDMI]), // I bit is 0, bit[7] is 0 and bit[4] is 1

                    // TST
                    new(0x0ff0_f000, 0x0310_0000, &TST, [Model.ARM7TDMI]), // I bit is 1
                    new(0x0ff0_f090, 0x0110_0000, &TST, [Model.ARM7TDMI]), // I bit is 0, bit[7] is 0 and bit[4] is 0
                    new(0x0ff0_f090, 0x0110_0080, &TST, [Model.ARM7TDMI]), // I bit is 0, bit[7] is 1 and bit[4] is 0
                    new(0x0ff0_f090, 0x0110_0010, &TST, [Model.ARM7TDMI]), // I bit is 0, bit[7] is 0 and bit[4] is 1
                    new(0x0ff0_f000, 0x0310_f000, &TST, [Model.ARM7TDMI]), // I bit is 1
                    new(0x0ff0_f090, 0x0110_f000, &TST, [Model.ARM7TDMI]), // I bit is 0, bit[7] is 0 and bit[4] is 0
                    new(0x0ff0_f090, 0x0110_f080, &TST, [Model.ARM7TDMI]), // I bit is 0, bit[7] is 1 and bit[4] is 0
                    new(0x0ff0_f090, 0x0110_f010, &TST, [Model.ARM7TDMI]), // I bit is 0, bit[7] is 0 and bit[4] is 1

                    // UMLAL
                    new(0x0fe0_00f0, 0x00a0_0090, &UMLAL, [Model.ARM7TDMI]),

                    // UMULL
                    new(0x0fe0_00f0, 0x0080_0090, &UMULL, [Model.ARM7TDMI]),
                ];

                for (UInt32 instruction = 0; instruction < _instructionLUT.Length; ++instruction)
                {
                    bool unknownInstruction = true;

                    foreach (InstructionListEntry<UInt32> entry in InstructionList)
                    {
                        if (((instruction & InstructionLUTHash(entry._mask)) == InstructionLUTHash(entry._expected)) && (entry._modelList.Contains(_cpu._model)))
                        {
                            _instructionLUT[instruction] = new(entry._handler);
                            unknownInstruction = false;
                            break;
                        }
                    }

                    if (unknownInstruction)
                        _instructionLUT[instruction] = new(&UNKNOWN);
                }
            }
        }

        private static UInt32 InstructionLUTHash(UInt32 value)
        {
            return ((value >> 16) & 0xff0) | ((value >> 4) & 0x00f);
        }

        internal UInt64 Step()
        {
            UInt32 instruction = _cpu._callbackInterface.Read32(_cpu.NextInstructionAddress);
            _cpu.NextInstructionAddress += 4;

            UInt32 cond = (instruction >> 28) & 0b1111;

            if (_cpu.ConditionPassed(cond))
            {
                _cpu.Reg[PC] = _cpu.NextInstructionAddress + 4;

                unsafe
                {
                    return _instructionLUT[InstructionLUTHash(instruction)]._handler(_cpu, instruction);
                }
            }
            else
            {
                return 1;
            }
        }

        private static void SetPC(CPU_Core cpu, UInt32 value)
        {
            cpu.NextInstructionAddress = value;
        }

        private static void SetReg(CPU_Core cpu, UInt32 i, UInt32 value)
        {
            if (i == PC)
                SetPC(cpu, value);
            else
                cpu.Reg[i] = value;
        }

        // Addressing mode 1
        private static (UInt32 shifterOperand, UInt32 shifterCarryOut) GetShifterOperand(CPU_Core cpu, UInt32 instruction)
        {
            UInt32 i = (instruction >> 25) & 1;

            UInt32 shifterOperand = 0;
            UInt32 shifterCarryOut = 0;

            if (i == 1) // 32-bit immediate
            {
                UInt32 rotateImm = (instruction >> 8) & 0b1111;
                UInt32 imm = instruction & 0xff;

                shifterOperand = BitOperations.RotateRight(imm, (int)(rotateImm * 2));
                shifterCarryOut = (rotateImm == 0) ? cpu.GetFlag(Flag.C) : (shifterOperand >> 31);
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
                                shifterCarryOut = cpu.GetFlag(Flag.C);
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
                                shifterOperand = (cpu.GetFlag(Flag.C) << 31) | (value >> 1);
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

                    UInt32 value = (rm == PC) ? (cpu.Reg[PC] + 4) : cpu.Reg[rm];
                    int shiftAmount = (int)(cpu.Reg[rs] & 0xff);

                    switch (shift)
                    {
                        case 0b00: // Logical shift left
                            if (shiftAmount == 0)
                            {
                                shifterOperand = value;
                                shifterCarryOut = cpu.GetFlag(Flag.C);
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
                                shifterCarryOut = cpu.GetFlag(Flag.C);
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
                                shifterCarryOut = cpu.GetFlag(Flag.C);
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
                                shifterCarryOut = cpu.GetFlag(Flag.C);
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
        private static UInt32 GetAddress(CPU_Core cpu, UInt32 instruction)
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
                                index = ArithmeticShiftRight(cpu.Reg[rm], (int)shiftImm);
                            break;
                        case 0b11:
                            if (shiftImm == 0) // RRX
                                index = (cpu.GetFlag(Flag.C) << 31) | (cpu.Reg[rm] >> 1);
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
        private static UInt32 GetAddress_Misc(CPU_Core cpu, UInt32 instruction)
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
        private static (UInt32 startAddress, UInt32 endAddress) GetAddress_Multiple(CPU_Core cpu, UInt32 instruction)
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

        private static UInt64 UNKNOWN(CPU_Core cpu, UInt32 instruction)
        {
            throw new Exception(string.Format("Iris.CPU.ARM_Interpreter: Unknown ARM instruction 0x{0:x8} at address 0x{1:x8}", instruction, cpu.NextInstructionAddress - 4));
        }

        private static UInt64 ADC(CPU_Core cpu, UInt32 instruction)
        {
            UInt32 i = (instruction >> 25) & 1;
            UInt32 s = (instruction >> 20) & 1;
            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 rd = (instruction >> 12) & 0b1111;
            UInt32 r = (instruction >> 4) & 1;

            (UInt32 shifterOperand, _) = GetShifterOperand(cpu, instruction);

            bool shiftRs = (i == 0) && (r == 1);

            UInt32 leftOperand = ((rn == PC) && shiftRs) ? (cpu.Reg[PC] + 4) : cpu.Reg[rn];
            UInt32 rightOperand = shifterOperand;

            UInt64 result = (UInt64)leftOperand + (UInt64)rightOperand + (UInt64)cpu.GetFlag(Flag.C);
            SetReg(cpu, rd, (UInt32)result);

            if (s == 1)
            {
                if (rd == PC)
                {
                    cpu.SetCPSR(cpu.SPSR);

                    return shiftRs ? 4u : 3u;
                }
                else
                {
                    cpu.SetFlag(Flag.N, cpu.Reg[rd] >> 31);
                    cpu.SetFlag(Flag.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
                    cpu.SetFlag(Flag.C, CarryFrom(result));
                    cpu.SetFlag(Flag.V, OverflowFrom_Addition(leftOperand, rightOperand, cpu.Reg[rd]));

                    return shiftRs ? 2u : 1u;
                }
            }
            else
            {
                return (shiftRs ? 2u : 1u) + ((rd == PC) ? 2u : 0u);
            }
        }

        private static UInt64 ADD(CPU_Core cpu, UInt32 instruction)
        {
            UInt32 i = (instruction >> 25) & 1;
            UInt32 s = (instruction >> 20) & 1;
            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 rd = (instruction >> 12) & 0b1111;
            UInt32 r = (instruction >> 4) & 1;

            (UInt32 shifterOperand, _) = GetShifterOperand(cpu, instruction);

            bool shiftRs = (i == 0) && (r == 1);

            UInt32 leftOperand = ((rn == PC) && shiftRs) ? (cpu.Reg[PC] + 4) : cpu.Reg[rn];
            UInt32 rightOperand = shifterOperand;

            UInt64 result = (UInt64)leftOperand + (UInt64)rightOperand;
            SetReg(cpu, rd, (UInt32)result);

            if (s == 1)
            {
                if (rd == PC)
                {
                    cpu.SetCPSR(cpu.SPSR);

                    return shiftRs ? 4u : 3u;
                }
                else
                {
                    cpu.SetFlag(Flag.N, cpu.Reg[rd] >> 31);
                    cpu.SetFlag(Flag.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
                    cpu.SetFlag(Flag.C, CarryFrom(result));
                    cpu.SetFlag(Flag.V, OverflowFrom_Addition(leftOperand, rightOperand, cpu.Reg[rd]));

                    return shiftRs ? 2u : 1u;
                }
            }
            else
            {
                return (shiftRs ? 2u : 1u) + ((rd == PC) ? 2u : 0u);
            }
        }

        private static UInt64 AND(CPU_Core cpu, UInt32 instruction)
        {
            UInt32 i = (instruction >> 25) & 1;
            UInt32 s = (instruction >> 20) & 1;
            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 rd = (instruction >> 12) & 0b1111;
            UInt32 r = (instruction >> 4) & 1;

            (UInt32 shifterOperand, UInt32 shifterCarryOut) = GetShifterOperand(cpu, instruction);

            bool shiftRs = (i == 0) && (r == 1);

            UInt32 leftOperand = ((rn == PC) && shiftRs) ? (cpu.Reg[PC] + 4) : cpu.Reg[rn];
            UInt32 rightOperand = shifterOperand;

            SetReg(cpu, rd, leftOperand & rightOperand);

            if (s == 1)
            {
                if (rd == PC)
                {
                    cpu.SetCPSR(cpu.SPSR);

                    return shiftRs ? 4u : 3u;
                }
                else
                {
                    cpu.SetFlag(Flag.N, cpu.Reg[rd] >> 31);
                    cpu.SetFlag(Flag.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
                    cpu.SetFlag(Flag.C, shifterCarryOut);

                    return shiftRs ? 2u : 1u;
                }
            }
            else
            {
                return (shiftRs ? 2u : 1u) + ((rd == PC) ? 2u : 0u);
            }
        }

        private static UInt64 B(CPU_Core cpu, UInt32 instruction)
        {
            UInt32 imm = instruction & 0xff_ffff;

            SetPC(cpu, cpu.Reg[PC] + (SignExtend(imm, 24) << 2));

            return 3;
        }

        private static UInt64 BL(CPU_Core cpu, UInt32 instruction)
        {
            UInt32 imm = instruction & 0xff_ffff;

            cpu.Reg[LR] = cpu.NextInstructionAddress;
            SetPC(cpu, cpu.Reg[PC] + (SignExtend(imm, 24) << 2));

            return 3;
        }

        private static UInt64 BIC(CPU_Core cpu, UInt32 instruction)
        {
            UInt32 i = (instruction >> 25) & 1;
            UInt32 s = (instruction >> 20) & 1;
            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 rd = (instruction >> 12) & 0b1111;
            UInt32 r = (instruction >> 4) & 1;

            (UInt32 shifterOperand, UInt32 shifterCarryOut) = GetShifterOperand(cpu, instruction);

            bool shiftRs = (i == 0) && (r == 1);

            UInt32 leftOperand = ((rn == PC) && shiftRs) ? (cpu.Reg[PC] + 4) : cpu.Reg[rn];
            UInt32 rightOperand = shifterOperand;

            SetReg(cpu, rd, leftOperand & ~rightOperand);

            if (s == 1)
            {
                if (rd == PC)
                {
                    cpu.SetCPSR(cpu.SPSR);

                    return shiftRs ? 4u : 3u;
                }
                else
                {
                    cpu.SetFlag(Flag.N, cpu.Reg[rd] >> 31);
                    cpu.SetFlag(Flag.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
                    cpu.SetFlag(Flag.C, shifterCarryOut);

                    return shiftRs ? 2u : 1u;
                }
            }
            else
            {
                return (shiftRs ? 2u : 1u) + ((rd == PC) ? 2u : 0u);
            }
        }

        private static UInt64 BX(CPU_Core cpu, UInt32 instruction)
        {
            UInt32 rm = instruction & 0b1111;

            cpu.CPSR = (cpu.CPSR & ~(1u << 5)) | ((cpu.Reg[rm] & 1) << 5);
            SetPC(cpu, cpu.Reg[rm] & 0xffff_fffe);

            return 3;
        }

        private static UInt64 CMN(CPU_Core cpu, UInt32 instruction)
        {
            UInt32 i = (instruction >> 25) & 1;
            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 rd = (instruction >> 12) & 0b1111;
            UInt32 r = (instruction >> 4) & 1;

            (UInt32 shifterOperand, _) = GetShifterOperand(cpu, instruction);

            bool shiftRs = (i == 0) && (r == 1);

            UInt32 leftOperand = ((rn == PC) && shiftRs) ? (cpu.Reg[PC] + 4) : cpu.Reg[rn];
            UInt32 rightOperand = shifterOperand;

            UInt64 result = (UInt64)leftOperand + (UInt64)rightOperand;
            UInt32 aluOut = (UInt32)result;

            if (rd == PC)
            {
                if ((cpu.CPSR & ModeMask) == UserMode)
                    cpu.CPSR = (cpu.CPSR & ~0xf000_0000) | (aluOut & 0xf000_0000);
                else
                    cpu.SetCPSR((cpu.CPSR & ~0xf000_00c3) | (aluOut & 0xf000_0003) | (((aluOut >> 26) & 0b11) << 6));

                return shiftRs ? 4u : 3u;
            }
            else
            {
                cpu.SetFlag(Flag.N, aluOut >> 31);
                cpu.SetFlag(Flag.Z, (aluOut == 0) ? 1u : 0u);
                cpu.SetFlag(Flag.C, CarryFrom(result));
                cpu.SetFlag(Flag.V, OverflowFrom_Addition(leftOperand, rightOperand, aluOut));

                return shiftRs ? 2u : 1u;
            }
        }

        private static UInt64 CMP(CPU_Core cpu, UInt32 instruction)
        {
            UInt32 i = (instruction >> 25) & 1;
            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 rd = (instruction >> 12) & 0b1111;
            UInt32 r = (instruction >> 4) & 1;

            (UInt32 shifterOperand, _) = GetShifterOperand(cpu, instruction);

            bool shiftRs = (i == 0) && (r == 1);

            UInt32 leftOperand = ((rn == PC) && shiftRs) ? (cpu.Reg[PC] + 4) : cpu.Reg[rn];
            UInt32 rightOperand = shifterOperand;

            UInt64 result = (UInt64)leftOperand - (UInt64)rightOperand;
            UInt32 aluOut = (UInt32)result;

            if (rd == PC)
            {
                if ((cpu.CPSR & ModeMask) == UserMode)
                    cpu.CPSR = (cpu.CPSR & ~0xf000_0000) | (aluOut & 0xf000_0000);
                else
                    cpu.SetCPSR((cpu.CPSR & ~0xf000_00c3) | (aluOut & 0xf000_0003) | (((aluOut >> 26) & 0b11) << 6));

                return shiftRs ? 4u : 3u;
            }
            else
            {
                cpu.SetFlag(Flag.N, aluOut >> 31);
                cpu.SetFlag(Flag.Z, (aluOut == 0) ? 1u : 0u);
                cpu.SetFlag(Flag.C, Not(BorrowFrom(result)));
                cpu.SetFlag(Flag.V, OverflowFrom_Subtraction(leftOperand, rightOperand, aluOut));

                return shiftRs ? 2u : 1u;
            }
        }

        private static UInt64 EOR(CPU_Core cpu, UInt32 instruction)
        {
            UInt32 i = (instruction >> 25) & 1;
            UInt32 s = (instruction >> 20) & 1;
            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 rd = (instruction >> 12) & 0b1111;
            UInt32 r = (instruction >> 4) & 1;

            (UInt32 shifterOperand, UInt32 shifterCarryOut) = GetShifterOperand(cpu, instruction);

            bool shiftRs = (i == 0) && (r == 1);

            UInt32 leftOperand = ((rn == PC) && shiftRs) ? (cpu.Reg[PC] + 4) : cpu.Reg[rn];
            UInt32 rightOperand = shifterOperand;

            SetReg(cpu, rd, leftOperand ^ rightOperand);

            if (s == 1)
            {
                if (rd == PC)
                {
                    cpu.SetCPSR(cpu.SPSR);

                    return shiftRs ? 4u : 3u;
                }
                else
                {
                    cpu.SetFlag(Flag.N, cpu.Reg[rd] >> 31);
                    cpu.SetFlag(Flag.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
                    cpu.SetFlag(Flag.C, shifterCarryOut);

                    return shiftRs ? 2u : 1u;
                }
            }
            else
            {
                return (shiftRs ? 2u : 1u) + ((rd == PC) ? 2u : 0u);
            }
        }

        private static UInt64 LDM1(CPU_Core cpu, UInt32 instruction)
        {
            UInt32 registerList = instruction & 0xffff;

            (UInt32 startAddress, _) = GetAddress_Multiple(cpu, instruction);

            UInt32 address = startAddress;

            if (registerList == 0)
            {
                SetPC(cpu, cpu._callbackInterface.Read32(address));

                return 5;
            }
            else
            {
                for (int i = 0; i <= 14; ++i)
                {
                    if (((registerList >> i) & 1) == 1)
                    {
                        cpu.Reg[i] = cpu._callbackInterface.Read32(address);
                        address += 4;
                    }
                }

                UInt32 n = (UInt32)BitOperations.PopCount(registerList);

                if (((registerList >> 15) & 1) == 1)
                {
                    SetPC(cpu, cpu._callbackInterface.Read32(address) & 0xffff_fffc);

                    return n + 4;
                }
                else
                {
                    return n + 2;
                }
            }
        }

        private static UInt64 LDM2(CPU_Core cpu, UInt32 instruction)
        {
            UInt32 registerList = instruction & 0x7fff;

            (UInt32 startAddress, _) = GetAddress_Multiple(cpu, instruction);

            UInt32 address = startAddress;

            if (registerList == 0)
            {
                SetPC(cpu, cpu._callbackInterface.Read32(address));

                return 5;
            }
            else
            {
                for (int i = 0; i <= 14; ++i)
                {
                    if (((registerList >> i) & 1) == 1)
                    {
                        UInt32 value = cpu._callbackInterface.Read32(address);

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

                UInt32 n = (UInt32)BitOperations.PopCount(registerList);
                return n + 2;
            }
        }

        private static UInt64 LDR(CPU_Core cpu, UInt32 instruction)
        {
            UInt32 rd = (instruction >> 12) & 0b1111;

            UInt32 address = GetAddress(cpu, instruction);
            UInt32 data = BitOperations.RotateRight(cpu._callbackInterface.Read32(address), (int)(8 * (address & 0b11)));

            if (rd == PC)
            {
                SetPC(cpu, data & 0xffff_fffc);

                return 5;
            }
            else
            {
                cpu.Reg[rd] = data;

                return 3;
            }
        }

        private static UInt64 LDRB(CPU_Core cpu, UInt32 instruction)
        {
            UInt32 rd = (instruction >> 12) & 0b1111;

            UInt32 address = GetAddress(cpu, instruction);
            Byte data = cpu._callbackInterface.Read8(address);
            SetReg(cpu, rd, data);

            return (rd == PC) ? 5u : 3u;
        }

        private static UInt64 LDRH(CPU_Core cpu, UInt32 instruction)
        {
            UInt32 rd = (instruction >> 12) & 0b1111;

            UInt32 address = GetAddress_Misc(cpu, instruction);
            UInt32 data = BitOperations.RotateRight(cpu._callbackInterface.Read16(address), (int)(8 * (address & 1)));
            SetReg(cpu, rd, data);

            return (rd == PC) ? 5u : 3u;
        }

        private static UInt64 LDRSB(CPU_Core cpu, UInt32 instruction)
        {
            UInt32 rd = (instruction >> 12) & 0b1111;

            UInt32 address = GetAddress_Misc(cpu, instruction);
            Byte data = cpu._callbackInterface.Read8(address);
            SetReg(cpu, rd, SignExtend(data, 8));

            return (rd == PC) ? 5u : 3u;
        }

        private static UInt64 LDRSH(CPU_Core cpu, UInt32 instruction)
        {
            UInt32 rd = (instruction >> 12) & 0b1111;

            UInt32 address = GetAddress_Misc(cpu, instruction);

            if ((address & 1) == 1)
            {
                Byte data = cpu._callbackInterface.Read8(address);
                SetReg(cpu, rd, SignExtend(data, 8));
            }
            else
            {
                UInt16 data = cpu._callbackInterface.Read16(address);
                SetReg(cpu, rd, SignExtend(data, 16));
            }

            return (rd == PC) ? 5u : 3u;
        }

        private static UInt64 MLA(CPU_Core cpu, UInt32 instruction)
        {
            UInt32 s = (instruction >> 20) & 1;
            UInt32 rd = (instruction >> 16) & 0b1111;
            UInt32 rn = (instruction >> 12) & 0b1111;
            UInt32 rs = (instruction >> 8) & 0b1111;
            UInt32 rm = instruction & 0b1111;

            UInt64 m = ComputeMultiplicationCycleCount(cpu.Reg[rm], cpu.Reg[rs]);
            SetReg(cpu, rd, (cpu.Reg[rm] * cpu.Reg[rs]) + cpu.Reg[rn]);

            if (s == 1)
            {
                cpu.SetFlag(Flag.N, cpu.Reg[rd] >> 31);
                cpu.SetFlag(Flag.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
            }

            return m + 2;
        }

        private static UInt64 MOV(CPU_Core cpu, UInt32 instruction)
        {
            UInt32 i = (instruction >> 25) & 1;
            UInt32 s = (instruction >> 20) & 1;
            UInt32 rd = (instruction >> 12) & 0b1111;
            UInt32 r = (instruction >> 4) & 1;

            (UInt32 shifterOperand, UInt32 shifterCarryOut) = GetShifterOperand(cpu, instruction);

            bool shiftRs = (i == 0) && (r == 1);

            SetReg(cpu, rd, shifterOperand);

            if (s == 1)
            {
                if (rd == PC)
                {
                    cpu.SetCPSR(cpu.SPSR);

                    return shiftRs ? 4u : 3u;
                }
                else
                {
                    cpu.SetFlag(Flag.N, cpu.Reg[rd] >> 31);
                    cpu.SetFlag(Flag.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
                    cpu.SetFlag(Flag.C, shifterCarryOut);

                    return shiftRs ? 2u : 1u;
                }
            }
            else
            {
                return (shiftRs ? 2u : 1u) + ((rd == PC) ? 2u : 0u);
            }
        }

        private static UInt64 MRS(CPU_Core cpu, UInt32 instruction)
        {
            UInt32 r = (instruction >> 22) & 1;
            UInt32 rd = (instruction >> 12) & 0b1111;

            SetReg(cpu, rd, (r == 1) ? cpu.SPSR : cpu.CPSR);

            return 1;
        }

        private static UInt64 MSR(CPU_Core cpu, UInt32 instruction)
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
                if ((cpu.CPSR & ModeMask) == UserMode)
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

            return 1;
        }

        private static UInt64 MUL(CPU_Core cpu, UInt32 instruction)
        {
            UInt32 s = (instruction >> 20) & 1;
            UInt32 rd = (instruction >> 16) & 0b1111;
            UInt32 rs = (instruction >> 8) & 0b1111;
            UInt32 rm = instruction & 0b1111;

            UInt64 m = ComputeMultiplicationCycleCount(cpu.Reg[rm], cpu.Reg[rs]);
            SetReg(cpu, rd, cpu.Reg[rm] * cpu.Reg[rs]);

            if (s == 1)
            {
                cpu.SetFlag(Flag.N, cpu.Reg[rd] >> 31);
                cpu.SetFlag(Flag.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
            }

            return m + 1;
        }

        private static UInt64 MVN(CPU_Core cpu, UInt32 instruction)
        {
            UInt32 i = (instruction >> 25) & 1;
            UInt32 s = (instruction >> 20) & 1;
            UInt32 rd = (instruction >> 12) & 0b1111;
            UInt32 r = (instruction >> 4) & 1;

            (UInt32 shifterOperand, UInt32 shifterCarryOut) = GetShifterOperand(cpu, instruction);

            bool shiftRs = (i == 0) && (r == 1);

            SetReg(cpu, rd, ~shifterOperand);

            if (s == 1)
            {
                if (rd == PC)
                {
                    cpu.SetCPSR(cpu.SPSR);

                    return shiftRs ? 4u : 3u;
                }
                else
                {
                    cpu.SetFlag(Flag.N, cpu.Reg[rd] >> 31);
                    cpu.SetFlag(Flag.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
                    cpu.SetFlag(Flag.C, shifterCarryOut);

                    return shiftRs ? 2u : 1u;
                }
            }
            else
            {
                return (shiftRs ? 2u : 1u) + ((rd == PC) ? 2u : 0u);
            }
        }

        private static UInt64 ORR(CPU_Core cpu, UInt32 instruction)
        {
            UInt32 i = (instruction >> 25) & 1;
            UInt32 s = (instruction >> 20) & 1;
            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 rd = (instruction >> 12) & 0b1111;
            UInt32 r = (instruction >> 4) & 1;

            (UInt32 shifterOperand, UInt32 shifterCarryOut) = GetShifterOperand(cpu, instruction);

            bool shiftRs = (i == 0) && (r == 1);

            UInt32 leftOperand = ((rn == PC) && shiftRs) ? (cpu.Reg[PC] + 4) : cpu.Reg[rn];
            UInt32 rightOperand = shifterOperand;

            SetReg(cpu, rd, leftOperand | rightOperand);

            if (s == 1)
            {
                if (rd == PC)
                {
                    cpu.SetCPSR(cpu.SPSR);

                    return shiftRs ? 4u : 3u;
                }
                else
                {
                    cpu.SetFlag(Flag.N, cpu.Reg[rd] >> 31);
                    cpu.SetFlag(Flag.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
                    cpu.SetFlag(Flag.C, shifterCarryOut);

                    return shiftRs ? 2u : 1u;
                }
            }
            else
            {
                return (shiftRs ? 2u : 1u) + ((rd == PC) ? 2u : 0u);
            }
        }

        private static UInt64 RSB(CPU_Core cpu, UInt32 instruction)
        {
            UInt32 i = (instruction >> 25) & 1;
            UInt32 s = (instruction >> 20) & 1;
            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 rd = (instruction >> 12) & 0b1111;
            UInt32 r = (instruction >> 4) & 1;

            (UInt32 shifterOperand, _) = GetShifterOperand(cpu, instruction);

            bool shiftRs = (i == 0) && (r == 1);

            UInt32 leftOperand = shifterOperand;
            UInt32 rightOperand = ((rn == PC) && shiftRs) ? (cpu.Reg[PC] + 4) : cpu.Reg[rn];

            UInt64 result = (UInt64)leftOperand - (UInt64)rightOperand;
            SetReg(cpu, rd, (UInt32)result);

            if (s == 1)
            {
                if (rd == PC)
                {
                    cpu.SetCPSR(cpu.SPSR);

                    return shiftRs ? 4u : 3u;
                }
                else
                {
                    cpu.SetFlag(Flag.N, cpu.Reg[rd] >> 31);
                    cpu.SetFlag(Flag.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
                    cpu.SetFlag(Flag.C, Not(BorrowFrom(result)));
                    cpu.SetFlag(Flag.V, OverflowFrom_Subtraction(leftOperand, rightOperand, cpu.Reg[rd]));

                    return shiftRs ? 2u : 1u;
                }
            }
            else
            {
                return (shiftRs ? 2u : 1u) + ((rd == PC) ? 2u : 0u);
            }
        }

        private static UInt64 RSC(CPU_Core cpu, UInt32 instruction)
        {
            UInt32 i = (instruction >> 25) & 1;
            UInt32 s = (instruction >> 20) & 1;
            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 rd = (instruction >> 12) & 0b1111;
            UInt32 r = (instruction >> 4) & 1;

            (UInt32 shifterOperand, _) = GetShifterOperand(cpu, instruction);

            bool shiftRs = (i == 0) && (r == 1);

            UInt32 leftOperand = shifterOperand;
            UInt32 rightOperand = ((rn == PC) && shiftRs) ? (cpu.Reg[PC] + 4) : cpu.Reg[rn];

            UInt64 result = (UInt64)leftOperand - (UInt64)rightOperand - (UInt64)Not(cpu.GetFlag(Flag.C));
            SetReg(cpu, rd, (UInt32)result);

            if (s == 1)
            {
                if (rd == PC)
                {
                    cpu.SetCPSR(cpu.SPSR);

                    return shiftRs ? 4u : 3u;
                }
                else
                {
                    cpu.SetFlag(Flag.N, cpu.Reg[rd] >> 31);
                    cpu.SetFlag(Flag.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
                    cpu.SetFlag(Flag.C, Not(BorrowFrom(result)));
                    cpu.SetFlag(Flag.V, OverflowFrom_Subtraction(leftOperand, rightOperand, cpu.Reg[rd]));

                    return shiftRs ? 2u : 1u;
                }
            }
            else
            {
                return (shiftRs ? 2u : 1u) + ((rd == PC) ? 2u : 0u);
            }
        }

        private static UInt64 SBC(CPU_Core cpu, UInt32 instruction)
        {
            UInt32 i = (instruction >> 25) & 1;
            UInt32 s = (instruction >> 20) & 1;
            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 rd = (instruction >> 12) & 0b1111;
            UInt32 r = (instruction >> 4) & 1;

            (UInt32 shifterOperand, _) = GetShifterOperand(cpu, instruction);

            bool shiftRs = (i == 0) && (r == 1);

            UInt32 leftOperand = ((rn == PC) && shiftRs) ? (cpu.Reg[PC] + 4) : cpu.Reg[rn];
            UInt32 rightOperand = shifterOperand;

            UInt64 result = (UInt64)leftOperand - (UInt64)rightOperand - (UInt64)Not(cpu.GetFlag(Flag.C));
            SetReg(cpu, rd, (UInt32)result);

            if (s == 1)
            {
                if (rd == PC)
                {
                    cpu.SetCPSR(cpu.SPSR);

                    return shiftRs ? 4u : 3u;
                }
                else
                {
                    cpu.SetFlag(Flag.N, cpu.Reg[rd] >> 31);
                    cpu.SetFlag(Flag.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
                    cpu.SetFlag(Flag.C, Not(BorrowFrom(result)));
                    cpu.SetFlag(Flag.V, OverflowFrom_Subtraction(leftOperand, rightOperand, cpu.Reg[rd]));

                    return shiftRs ? 2u : 1u;
                }
            }
            else
            {
                return (shiftRs ? 2u : 1u) + ((rd == PC) ? 2u : 0u);
            }
        }

        private static UInt64 SMLAL(CPU_Core cpu, UInt32 instruction)
        {
            UInt32 s = (instruction >> 20) & 1;
            UInt32 rdHi = (instruction >> 16) & 0b1111;
            UInt32 rdLo = (instruction >> 12) & 0b1111;
            UInt32 rs = (instruction >> 8) & 0b1111;
            UInt32 rm = instruction & 0b1111;

            UInt64 m = ComputeMultiplicationCycleCount(cpu.Reg[rm], cpu.Reg[rs]);
            Int64 result = (Int64)(Int32)cpu.Reg[rm] * (Int64)(Int32)cpu.Reg[rs];
            UInt64 resultLo = (UInt64)(UInt32)result + (UInt64)cpu.Reg[rdLo];
            UInt32 resultHi = (UInt32)(result >> 32) + cpu.Reg[rdHi] + CarryFrom(resultLo);
            SetReg(cpu, rdLo, (UInt32)resultLo);
            SetReg(cpu, rdHi, resultHi);

            if (s == 1)
            {
                cpu.SetFlag(Flag.N, cpu.Reg[rdHi] >> 31);
                cpu.SetFlag(Flag.Z, ((cpu.Reg[rdHi] == 0) && (cpu.Reg[rdLo] == 0)) ? 1u : 0u);
            }

            return m + 3;
        }

        private static UInt64 SMULL(CPU_Core cpu, UInt32 instruction)
        {
            UInt32 s = (instruction >> 20) & 1;
            UInt32 rdHi = (instruction >> 16) & 0b1111;
            UInt32 rdLo = (instruction >> 12) & 0b1111;
            UInt32 rs = (instruction >> 8) & 0b1111;
            UInt32 rm = instruction & 0b1111;

            UInt64 m = ComputeMultiplicationCycleCount(cpu.Reg[rm], cpu.Reg[rs]);
            Int64 result = (Int64)(Int32)cpu.Reg[rm] * (Int64)(Int32)cpu.Reg[rs];
            SetReg(cpu, rdLo, (UInt32)result);
            SetReg(cpu, rdHi, (UInt32)(result >> 32));

            if (s == 1)
            {
                cpu.SetFlag(Flag.N, cpu.Reg[rdHi] >> 31);
                cpu.SetFlag(Flag.Z, ((cpu.Reg[rdHi] == 0) && (cpu.Reg[rdLo] == 0)) ? 1u : 0u);
            }

            return m + 2;
        }

        private static UInt64 STM1(CPU_Core cpu, UInt32 instruction)
        {
            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 registerList = instruction & 0xffff;

            UInt32 oldRegRn = cpu.Reg[rn];

            (UInt32 startAddress, _) = GetAddress_Multiple(cpu, instruction);

            UInt32 address = startAddress;

            if (registerList == 0)
            {
                cpu._callbackInterface.Write32(address, cpu.Reg[PC] + 4);

                return 2;
            }
            else
            {
                for (int i = 0; i <= 14; ++i)
                {
                    if (((registerList >> i) & 1) == 1)
                    {
                        if ((i == rn) && ((registerList & ~(0xffff << i)) == 0))
                            cpu._callbackInterface.Write32(address, oldRegRn);
                        else
                            cpu._callbackInterface.Write32(address, cpu.Reg[i]);

                        address += 4;
                    }
                }

                if (((registerList >> 15) & 1) == 1)
                    cpu._callbackInterface.Write32(address, cpu.Reg[PC] + 4);

                UInt32 n = (UInt32)BitOperations.PopCount(registerList);
                return n + 1;
            }
        }

        private static UInt64 STM2(CPU_Core cpu, UInt32 instruction)
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
                cpu._callbackInterface.Write32(address, cpu.Reg[PC] + 4);

                return 2;
            }
            else
            {
                for (int i = 0; i <= 14; ++i)
                {
                    if (((registerList >> i) & 1) == 1)
                    {
                        if ((i == rn) && ((registerList & ~(0xffff << i)) == 0))
                        {
                            cpu._callbackInterface.Write32(address, oldRegRn);
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

                            cpu._callbackInterface.Write32(address, value);
                        }

                        address += 4;
                    }
                }

                if (((registerList >> 15) & 1) == 1)
                    cpu._callbackInterface.Write32(address, cpu.Reg[PC] + 4);

                UInt32 n = (UInt32)BitOperations.PopCount(registerList);
                return n + 1;
            }
        }

        private static UInt64 STR(CPU_Core cpu, UInt32 instruction)
        {
            UInt32 rd = (instruction >> 12) & 0b1111;

            UInt32 data = (rd == PC) ? (cpu.Reg[PC] + 4) : cpu.Reg[rd];
            UInt32 address = GetAddress(cpu, instruction);
            cpu._callbackInterface.Write32(address, data);

            return 2;
        }

        private static UInt64 STRB(CPU_Core cpu, UInt32 instruction)
        {
            UInt32 rd = (instruction >> 12) & 0b1111;

            UInt32 data = (rd == PC) ? (cpu.Reg[PC] + 4) : cpu.Reg[rd];
            UInt32 address = GetAddress(cpu, instruction);
            cpu._callbackInterface.Write8(address, (Byte)data);

            return 2;
        }

        private static UInt64 STRH(CPU_Core cpu, UInt32 instruction)
        {
            UInt32 rd = (instruction >> 12) & 0b1111;

            UInt32 data = (rd == PC) ? (cpu.Reg[PC] + 4) : cpu.Reg[rd];
            UInt32 address = GetAddress_Misc(cpu, instruction);
            cpu._callbackInterface.Write16(address, (UInt16)data);

            return 2;
        }

        private static UInt64 SUB(CPU_Core cpu, UInt32 instruction)
        {
            UInt32 i = (instruction >> 25) & 1;
            UInt32 s = (instruction >> 20) & 1;
            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 rd = (instruction >> 12) & 0b1111;
            UInt32 r = (instruction >> 4) & 1;

            (UInt32 shifterOperand, _) = GetShifterOperand(cpu, instruction);

            bool shiftRs = (i == 0) && (r == 1);

            UInt32 leftOperand = ((rn == PC) && shiftRs) ? (cpu.Reg[PC] + 4) : cpu.Reg[rn];
            UInt32 rightOperand = shifterOperand;

            UInt64 result = (UInt64)leftOperand - (UInt64)rightOperand;
            SetReg(cpu, rd, (UInt32)result);

            if (s == 1)
            {
                if (rd == PC)
                {
                    cpu.SetCPSR(cpu.SPSR);

                    return shiftRs ? 4u : 3u;
                }
                else
                {
                    cpu.SetFlag(Flag.N, cpu.Reg[rd] >> 31);
                    cpu.SetFlag(Flag.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
                    cpu.SetFlag(Flag.C, Not(BorrowFrom(result)));
                    cpu.SetFlag(Flag.V, OverflowFrom_Subtraction(leftOperand, rightOperand, cpu.Reg[rd]));

                    return shiftRs ? 2u : 1u;
                }
            }
            else
            {
                return (shiftRs ? 2u : 1u) + ((rd == PC) ? 2u : 0u);
            }
        }

        private static UInt64 SWI(CPU_Core cpu, UInt32 instruction)
        {
            return cpu._callbackInterface.HandleSWI();
        }

        private static UInt64 SWP(CPU_Core cpu, UInt32 instruction)
        {
            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 rd = (instruction >> 12) & 0b1111;
            UInt32 rm = instruction & 0b1111;

            UInt32 temp = BitOperations.RotateRight(cpu._callbackInterface.Read32(cpu.Reg[rn]), (int)(8 * (cpu.Reg[rn] & 0b11)));
            cpu._callbackInterface.Write32(cpu.Reg[rn], cpu.Reg[rm]);
            SetReg(cpu, rd, temp);

            return 4;
        }

        private static UInt64 SWPB(CPU_Core cpu, UInt32 instruction)
        {
            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 rd = (instruction >> 12) & 0b1111;
            UInt32 rm = instruction & 0b1111;

            Byte temp = cpu._callbackInterface.Read8(cpu.Reg[rn]);
            cpu._callbackInterface.Write8(cpu.Reg[rn], (Byte)cpu.Reg[rm]);
            SetReg(cpu, rd, temp);

            return 4;
        }

        private static UInt64 TEQ(CPU_Core cpu, UInt32 instruction)
        {
            UInt32 i = (instruction >> 25) & 1;
            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 rd = (instruction >> 12) & 0b1111;
            UInt32 r = (instruction >> 4) & 1;

            (UInt32 shifterOperand, UInt32 shifterCarryOut) = GetShifterOperand(cpu, instruction);

            bool shiftRs = (i == 0) && (r == 1);

            UInt32 leftOperand = ((rn == PC) && shiftRs) ? (cpu.Reg[PC] + 4) : cpu.Reg[rn];
            UInt32 rightOperand = shifterOperand;

            UInt32 aluOut = leftOperand ^ rightOperand;

            if (rd == PC)
            {
                if ((cpu.CPSR & ModeMask) == UserMode)
                    cpu.CPSR = (cpu.CPSR & ~0xf000_0000) | (aluOut & 0xf000_0000);
                else
                    cpu.SetCPSR((cpu.CPSR & ~0xf000_00c3) | (aluOut & 0xf000_0003) | (((aluOut >> 26) & 0b11) << 6));

                return shiftRs ? 4u : 3u;
            }
            else
            {
                cpu.SetFlag(Flag.N, aluOut >> 31);
                cpu.SetFlag(Flag.Z, (aluOut == 0) ? 1u : 0u);
                cpu.SetFlag(Flag.C, shifterCarryOut);

                return shiftRs ? 2u : 1u;
            }
        }

        private static UInt64 TST(CPU_Core cpu, UInt32 instruction)
        {
            UInt32 i = (instruction >> 25) & 1;
            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 rd = (instruction >> 12) & 0b1111;
            UInt32 r = (instruction >> 4) & 1;

            (UInt32 shifterOperand, UInt32 shifterCarryOut) = GetShifterOperand(cpu, instruction);

            bool shiftRs = (i == 0) && (r == 1);

            UInt32 leftOperand = ((rn == PC) && shiftRs) ? (cpu.Reg[PC] + 4) : cpu.Reg[rn];
            UInt32 rightOperand = shifterOperand;

            UInt32 aluOut = leftOperand & rightOperand;

            if (rd == PC)
            {
                if ((cpu.CPSR & ModeMask) == UserMode)
                    cpu.CPSR = (cpu.CPSR & ~0xf000_0000) | (aluOut & 0xf000_0000);
                else
                    cpu.SetCPSR((cpu.CPSR & ~0xf000_00c3) | (aluOut & 0xf000_0003) | (((aluOut >> 26) & 0b11) << 6));

                return shiftRs ? 4u : 3u;
            }
            else
            {
                cpu.SetFlag(Flag.N, aluOut >> 31);
                cpu.SetFlag(Flag.Z, (aluOut == 0) ? 1u : 0u);
                cpu.SetFlag(Flag.C, shifterCarryOut);

                return shiftRs ? 2u : 1u;
            }
        }

        private static UInt64 UMLAL(CPU_Core cpu, UInt32 instruction)
        {
            UInt32 s = (instruction >> 20) & 1;
            UInt32 rdHi = (instruction >> 16) & 0b1111;
            UInt32 rdLo = (instruction >> 12) & 0b1111;
            UInt32 rs = (instruction >> 8) & 0b1111;
            UInt32 rm = instruction & 0b1111;

            UInt64 m = ComputeMultiplicationCycleCount(cpu.Reg[rm], cpu.Reg[rs]);
            UInt64 result = (UInt64)cpu.Reg[rm] * (UInt64)cpu.Reg[rs];
            UInt64 resultLo = (UInt64)(UInt32)result + (UInt64)cpu.Reg[rdLo];
            UInt32 resultHi = (UInt32)(result >> 32) + cpu.Reg[rdHi] + CarryFrom(resultLo);
            SetReg(cpu, rdLo, (UInt32)resultLo);
            SetReg(cpu, rdHi, resultHi);

            if (s == 1)
            {
                cpu.SetFlag(Flag.N, cpu.Reg[rdHi] >> 31);
                cpu.SetFlag(Flag.Z, ((cpu.Reg[rdHi] == 0) && (cpu.Reg[rdLo] == 0)) ? 1u : 0u);
            }

            return m + 3;
        }

        private static UInt64 UMULL(CPU_Core cpu, UInt32 instruction)
        {
            UInt32 s = (instruction >> 20) & 1;
            UInt32 rdHi = (instruction >> 16) & 0b1111;
            UInt32 rdLo = (instruction >> 12) & 0b1111;
            UInt32 rs = (instruction >> 8) & 0b1111;
            UInt32 rm = instruction & 0b1111;

            UInt64 m = ComputeMultiplicationCycleCount(cpu.Reg[rm], cpu.Reg[rs]);
            UInt64 result = (UInt64)cpu.Reg[rm] * (UInt64)cpu.Reg[rs];
            SetReg(cpu, rdLo, (UInt32)result);
            SetReg(cpu, rdHi, (UInt32)(result >> 32));

            if (s == 1)
            {
                cpu.SetFlag(Flag.N, cpu.Reg[rdHi] >> 31);
                cpu.SetFlag(Flag.Z, ((cpu.Reg[rdHi] == 0) && (cpu.Reg[rdLo] == 0)) ? 1u : 0u);
            }

            return m + 2;
        }
    }
}
