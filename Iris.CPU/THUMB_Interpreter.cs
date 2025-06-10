using System.Numerics;
using static Iris.CPU.CPU_Core;

namespace Iris.CPU
{
    internal sealed class THUMB_Interpreter
    {
        private readonly CPU_Core _cpu;
        private readonly InstructionLUTEntry<UInt16>[] _instructionLUT = new InstructionLUTEntry<UInt16>[1 << 10];

        internal THUMB_Interpreter(CPU_Core cpu)
        {
            _cpu = cpu;

            unsafe
            {
                InstructionListEntry<UInt16>[] InstructionList =
                [
                    // ADC
                    new(0xffc0, 0x4140, &ADC, [Model.ARM7TDMI]),

                    // ADD
                    new(0xfe00, 0x1c00, &ADD1, [Model.ARM7TDMI]),
                    new(0xf800, 0x3000, &ADD2, [Model.ARM7TDMI]),
                    new(0xfe00, 0x1800, &ADD3, [Model.ARM7TDMI]),
                    new(0xff00, 0x4400, &ADD4, [Model.ARM7TDMI]),
                    new(0xf800, 0xa000, &ADD5, [Model.ARM7TDMI]),
                    new(0xf800, 0xa800, &ADD6, [Model.ARM7TDMI]),
                    new(0xff80, 0xb000, &ADD7, [Model.ARM7TDMI]),

                    // AND
                    new(0xffc0, 0x4000, &AND, [Model.ARM7TDMI]),

                    // ASR
                    new(0xf800, 0x1000, &ASR1, [Model.ARM7TDMI]),
                    new(0xffc0, 0x4100, &ASR2, [Model.ARM7TDMI]),

                    // B
                    new(0xff00, 0xd000, &B1, [Model.ARM7TDMI]), // condition field 0b0000
                    new(0xff00, 0xd100, &B1, [Model.ARM7TDMI]), // condition field 0b0001
                    new(0xff00, 0xd200, &B1, [Model.ARM7TDMI]), // condition field 0b0010
                    new(0xff00, 0xd300, &B1, [Model.ARM7TDMI]), // condition field 0b0011
                    new(0xff00, 0xd400, &B1, [Model.ARM7TDMI]), // condition field 0b0100
                    new(0xff00, 0xd500, &B1, [Model.ARM7TDMI]), // condition field 0b0101
                    new(0xff00, 0xd600, &B1, [Model.ARM7TDMI]), // condition field 0b0110
                    new(0xff00, 0xd700, &B1, [Model.ARM7TDMI]), // condition field 0b0111
                    new(0xff00, 0xd800, &B1, [Model.ARM7TDMI]), // condition field 0b1000
                    new(0xff00, 0xd900, &B1, [Model.ARM7TDMI]), // condition field 0b1001
                    new(0xff00, 0xda00, &B1, [Model.ARM7TDMI]), // condition field 0b1010
                    new(0xff00, 0xdb00, &B1, [Model.ARM7TDMI]), // condition field 0b1011
                    new(0xff00, 0xdc00, &B1, [Model.ARM7TDMI]), // condition field 0b1100
                    new(0xff00, 0xdd00, &B1, [Model.ARM7TDMI]), // condition field 0b1101
                    new(0xf800, 0xe000, &B2, [Model.ARM7TDMI]),

                    // BIC
                    new(0xffc0, 0x4380, &BIC, [Model.ARM7TDMI]),

                    // BL
                    new(0xf000, 0xf000, &BL, [Model.ARM7TDMI]),

                    // BX
                    new(0xff80, 0x4700, &BX, [Model.ARM7TDMI]),

                    // CMN
                    new(0xffc0, 0x42c0, &CMN, [Model.ARM7TDMI]),

                    // CMP
                    new(0xf800, 0x2800, &CMP1, [Model.ARM7TDMI]),
                    new(0xffc0, 0x4280, &CMP2, [Model.ARM7TDMI]),
                    new(0xff00, 0x4500, &CMP3, [Model.ARM7TDMI]),

                    // EOR
                    new(0xffc0, 0x4040, &EOR, [Model.ARM7TDMI]),

                    // LDMIA
                    new(0xf800, 0xc800, &LDMIA, [Model.ARM7TDMI]),

                    // LDR
                    new(0xf800, 0x6800, &LDR1, [Model.ARM7TDMI]),
                    new(0xfe00, 0x5800, &LDR2, [Model.ARM7TDMI]),
                    new(0xf800, 0x4800, &LDR3, [Model.ARM7TDMI]),
                    new(0xf800, 0x9800, &LDR4, [Model.ARM7TDMI]),

                    // LDRB
                    new(0xf800, 0x7800, &LDRB1, [Model.ARM7TDMI]),
                    new(0xfe00, 0x5c00, &LDRB2, [Model.ARM7TDMI]),

                    // LDRH
                    new(0xf800, 0x8800, &LDRH1, [Model.ARM7TDMI]),
                    new(0xfe00, 0x5a00, &LDRH2, [Model.ARM7TDMI]),

                    // LDRSB
                    new(0xfe00, 0x5600, &LDRSB, [Model.ARM7TDMI]),

                    // LDRSH
                    new(0xfe00, 0x5e00, &LDRSH, [Model.ARM7TDMI]),

                    // LSL
                    new(0xf800, 0x0000, &LSL1, [Model.ARM7TDMI]),
                    new(0xffc0, 0x4080, &LSL2, [Model.ARM7TDMI]),

                    // LSR
                    new(0xf800, 0x0800, &LSR1, [Model.ARM7TDMI]),
                    new(0xffc0, 0x40c0, &LSR2, [Model.ARM7TDMI]),

                    // MOV
                    new(0xf800, 0x2000, &MOV1, [Model.ARM7TDMI]),
                    //new(0xffc0, 0x1c00, &MOV2, new List<Model>{ Model.ARM7TDMI }),
                    new(0xff00, 0x4600, &MOV3, [Model.ARM7TDMI]),

                    // MUL
                    new(0xffc0, 0x4340, &MUL, [Model.ARM7TDMI]),

                    // MVN
                    new(0xffc0, 0x43c0, &MVN, [Model.ARM7TDMI]),

                    // NEG
                    new(0xffc0, 0x4240, &NEG, [Model.ARM7TDMI]),

                    // ORR
                    new(0xffc0, 0x4300, &ORR, [Model.ARM7TDMI]),

                    // POP
                    new(0xfe00, 0xbc00, &POP, [Model.ARM7TDMI]),

                    // PUSH
                    new(0xfe00, 0xb400, &PUSH, [Model.ARM7TDMI]),

                    // ROR
                    new(0xffc0, 0x41c0, &ROR, [Model.ARM7TDMI]),

                    // SBC
                    new(0xffc0, 0x4180, &SBC, [Model.ARM7TDMI]),

                    // STMIA
                    new(0xf800, 0xc000, &STMIA, [Model.ARM7TDMI]),

                    // STR
                    new(0xf800, 0x6000, &STR1, [Model.ARM7TDMI]),
                    new(0xfe00, 0x5000, &STR2, [Model.ARM7TDMI]),
                    new(0xf800, 0x9000, &STR3, [Model.ARM7TDMI]),

                    // STRB
                    new(0xf800, 0x7000, &STRB1, [Model.ARM7TDMI]),
                    new(0xfe00, 0x5400, &STRB2, [Model.ARM7TDMI]),

                    // STRH
                    new(0xf800, 0x8000, &STRH1, [Model.ARM7TDMI]),
                    new(0xfe00, 0x5200, &STRH2, [Model.ARM7TDMI]),

                    // SUB
                    new(0xfe00, 0x1e00, &SUB1, [Model.ARM7TDMI]),
                    new(0xf800, 0x3800, &SUB2, [Model.ARM7TDMI]),
                    new(0xfe00, 0x1a00, &SUB3, [Model.ARM7TDMI]),
                    new(0xff80, 0xb080, &SUB4, [Model.ARM7TDMI]),

                    // SWI
                    new(0xff00, 0xdf00, &SWI, [Model.ARM7TDMI]),

                    // TST
                    new(0xffc0, 0x4200, &TST, [Model.ARM7TDMI]),
                ];

                for (UInt16 instruction = 0; instruction < _instructionLUT.Length; ++instruction)
                {
                    bool unknownInstruction = true;

                    foreach (InstructionListEntry<UInt16> entry in InstructionList)
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

        private static UInt16 InstructionLUTHash(UInt16 value)
        {
            return (UInt16)(value >> 6);
        }

        internal UInt64 Step()
        {
            UInt16 instruction = _cpu._callbackInterface.Read16(_cpu.NextInstructionAddress);
            _cpu.NextInstructionAddress += 2;

            _cpu.Reg[PC] = _cpu.NextInstructionAddress + 2;

            unsafe
            {
                return _instructionLUT[InstructionLUTHash(instruction)]._handler(_cpu, instruction);
            }
        }

        private static void SetPC(CPU_Core cpu, UInt32 value)
        {
            cpu.NextInstructionAddress = value & 0xffff_fffe;
        }

        private static void SetReg(CPU_Core cpu, UInt32 index, UInt32 value)
        {
            if (index == PC)
                SetPC(cpu, value);
            else
                cpu.Reg[index] = value;
        }

        private static UInt64 UNKNOWN(CPU_Core cpu, UInt16 instruction)
        {
            throw new Exception($"Iris.CPU.THUMB_Interpreter: Unknown THUMB instruction 0x{instruction:x4} at address 0x{(cpu.NextInstructionAddress - 2):x8}");
        }

        private static UInt64 ADC(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 leftOperand = cpu.Reg[rd];
            UInt32 rightOperand = cpu.Reg[rm];

            UInt64 result = (UInt64)leftOperand + (UInt64)rightOperand + (UInt64)cpu.GetFlag(Flag.C);
            cpu.Reg[rd] = (UInt32)result;

            cpu.SetFlag(Flag.N, cpu.Reg[rd] >> 31);
            cpu.SetFlag(Flag.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
            cpu.SetFlag(Flag.C, CarryFrom(result));
            cpu.SetFlag(Flag.V, OverflowFrom_Addition(leftOperand, rightOperand, cpu.Reg[rd]));

            return 1;
        }

        private static UInt64 ADD1(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)((instruction >> 6) & 0b111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 leftOperand = cpu.Reg[rn];
            UInt32 rightOperand = imm;

            UInt64 result = (UInt64)leftOperand + (UInt64)rightOperand;
            cpu.Reg[rd] = (UInt32)result;

            cpu.SetFlag(Flag.N, cpu.Reg[rd] >> 31);
            cpu.SetFlag(Flag.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
            cpu.SetFlag(Flag.C, CarryFrom(result));
            cpu.SetFlag(Flag.V, OverflowFrom_Addition(leftOperand, rightOperand, cpu.Reg[rd]));

            return 1;
        }

        private static UInt64 ADD2(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 rd = (UInt16)((instruction >> 8) & 0b111);
            UInt16 imm = (UInt16)(instruction & 0xff);

            UInt32 leftOperand = cpu.Reg[rd];
            UInt32 rightOperand = imm;

            UInt64 result = (UInt64)leftOperand + (UInt64)rightOperand;
            cpu.Reg[rd] = (UInt32)result;

            cpu.SetFlag(Flag.N, cpu.Reg[rd] >> 31);
            cpu.SetFlag(Flag.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
            cpu.SetFlag(Flag.C, CarryFrom(result));
            cpu.SetFlag(Flag.V, OverflowFrom_Addition(leftOperand, rightOperand, cpu.Reg[rd]));

            return 1;
        }

        private static UInt64 ADD3(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 6) & 0b111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 leftOperand = cpu.Reg[rn];
            UInt32 rightOperand = cpu.Reg[rm];

            UInt64 result = (UInt64)leftOperand + (UInt64)rightOperand;
            cpu.Reg[rd] = (UInt32)result;

            cpu.SetFlag(Flag.N, cpu.Reg[rd] >> 31);
            cpu.SetFlag(Flag.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
            cpu.SetFlag(Flag.C, CarryFrom(result));
            cpu.SetFlag(Flag.V, OverflowFrom_Addition(leftOperand, rightOperand, cpu.Reg[rd]));

            return 1;
        }

        private static UInt64 ADD4(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 h1 = (UInt16)((instruction >> 7) & 1);
            UInt16 h2 = (UInt16)((instruction >> 6) & 1);
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            rd |= (UInt16)(h1 << 3);
            rm |= (UInt16)(h2 << 3);

            SetReg(cpu, rd, cpu.Reg[rd] + cpu.Reg[rm]);

            return (rd == PC) ? 3u : 1u;
        }

        private static UInt64 ADD5(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 rd = (UInt16)((instruction >> 8) & 0b111);
            UInt16 imm = (UInt16)(instruction & 0xff);

            cpu.Reg[rd] = (cpu.Reg[PC] & 0xffff_fffc) + (imm * 4u);

            return 1;
        }

        private static UInt64 ADD6(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 rd = (UInt16)((instruction >> 8) & 0b111);
            UInt16 imm = (UInt16)(instruction & 0xff);

            cpu.Reg[rd] = cpu.Reg[SP] + (imm * 4u);

            return 1;
        }

        private static UInt64 ADD7(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)(instruction & 0x7f);

            cpu.Reg[SP] += imm * 4u;

            return 1;
        }

        private static UInt64 AND(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            cpu.Reg[rd] &= cpu.Reg[rm];

            cpu.SetFlag(Flag.N, cpu.Reg[rd] >> 31);
            cpu.SetFlag(Flag.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);

            return 1;
        }

        private static UInt64 ASR1(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)((instruction >> 6) & 0b1_1111);
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            int shiftAmount = imm;

            if (shiftAmount == 0)
            {
                cpu.SetFlag(Flag.C, cpu.Reg[rm] >> 31);
                cpu.Reg[rd] = ((cpu.Reg[rm] >> 31) == 0) ? 0 : 0xffff_ffff;
            }
            else
            {
                cpu.SetFlag(Flag.C, (cpu.Reg[rm] >> (shiftAmount - 1)) & 1);
                cpu.Reg[rd] = ArithmeticShiftRight(cpu.Reg[rm], shiftAmount);
            }

            cpu.SetFlag(Flag.N, cpu.Reg[rd] >> 31);
            cpu.SetFlag(Flag.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);

            return 1;
        }

        private static UInt64 ASR2(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 rs = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            int shiftAmount = (int)(cpu.Reg[rs] & 0xff);

            if (shiftAmount == 0)
            {
                // nothing to do
            }
            else if (shiftAmount < 32)
            {
                cpu.SetFlag(Flag.C, (cpu.Reg[rd] >> (shiftAmount - 1)) & 1);
                cpu.Reg[rd] = ArithmeticShiftRight(cpu.Reg[rd], shiftAmount);
            }
            else
            {
                cpu.SetFlag(Flag.C, cpu.Reg[rd] >> 31);
                cpu.Reg[rd] = ((cpu.Reg[rd] >> 31) == 0) ? 0 : 0xffff_ffff;
            }

            cpu.SetFlag(Flag.N, cpu.Reg[rd] >> 31);
            cpu.SetFlag(Flag.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);

            return 2;
        }

        private static UInt64 B1(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 cond = (UInt16)((instruction >> 8) & 0b1111);
            UInt16 imm = (UInt16)(instruction & 0xff);

            if (cpu.ConditionPassed(cond))
            {
                SetPC(cpu, cpu.Reg[PC] + (SignExtend(imm, 8) << 1));

                return 3;
            }
            else
            {
                return 1;
            }
        }

        private static UInt64 B2(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)(instruction & 0x7ff);

            SetPC(cpu, cpu.Reg[PC] + (SignExtend(imm, 11) << 1));

            return 3;
        }

        private static UInt64 BIC(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            cpu.Reg[rd] &= ~cpu.Reg[rm];

            cpu.SetFlag(Flag.N, cpu.Reg[rd] >> 31);
            cpu.SetFlag(Flag.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);

            return 1;
        }

        private static UInt64 BL(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 h = (UInt16)((instruction >> 11) & 0b11);
            UInt16 offset = (UInt16)(instruction & 0x7ff);

            if (h == 0b10)
            {
                cpu.Reg[LR] = cpu.Reg[PC] + (SignExtend(offset, 11) << 12);
            }
            else if (h == 0b11)
            {
                // save NextInstructionAddress because it's invalidated by SetPC
                UInt32 nextInstructionAddress = cpu.NextInstructionAddress;

                SetPC(cpu, cpu.Reg[LR] + (UInt32)(offset << 1));
                cpu.Reg[LR] = nextInstructionAddress | 1;
            }

            return 4;
        }

        private static UInt64 BX(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 h2 = (UInt16)((instruction >> 6) & 1);
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);

            rm |= (UInt16)(h2 << 3);

            cpu.CPSR = (cpu.CPSR & ~(1u << 5)) | ((cpu.Reg[rm] & 1) << 5);
            SetPC(cpu, cpu.Reg[rm]);

            return 3;
        }

        private static UInt64 CMN(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rn = (UInt16)(instruction & 0b111);

            UInt32 leftOperand = cpu.Reg[rn];
            UInt32 rightOperand = cpu.Reg[rm];

            UInt64 result = (UInt64)leftOperand + (UInt64)rightOperand;
            UInt32 aluOut = (UInt32)result;

            cpu.SetFlag(Flag.N, aluOut >> 31);
            cpu.SetFlag(Flag.Z, (aluOut == 0) ? 1u : 0u);
            cpu.SetFlag(Flag.C, CarryFrom(result));
            cpu.SetFlag(Flag.V, OverflowFrom_Addition(leftOperand, rightOperand, aluOut));

            return 1;
        }

        private static UInt64 CMP1(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 rn = (UInt16)((instruction >> 8) & 0b111);
            UInt16 imm = (UInt16)(instruction & 0xff);

            UInt32 leftOperand = cpu.Reg[rn];
            UInt32 rightOperand = imm;

            UInt64 result = (UInt64)leftOperand - (UInt64)rightOperand;
            UInt32 aluOut = (UInt32)result;

            cpu.SetFlag(Flag.N, aluOut >> 31);
            cpu.SetFlag(Flag.Z, (aluOut == 0) ? 1u : 0u);
            cpu.SetFlag(Flag.C, Not(BorrowFrom(result)));
            cpu.SetFlag(Flag.V, OverflowFrom_Subtraction(leftOperand, rightOperand, aluOut));

            return 1;
        }

        private static UInt64 CMP2(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rn = (UInt16)(instruction & 0b111);

            UInt32 leftOperand = cpu.Reg[rn];
            UInt32 rightOperand = cpu.Reg[rm];

            UInt64 result = (UInt64)leftOperand - (UInt64)rightOperand;
            UInt32 aluOut = (UInt32)result;

            cpu.SetFlag(Flag.N, aluOut >> 31);
            cpu.SetFlag(Flag.Z, (aluOut == 0) ? 1u : 0u);
            cpu.SetFlag(Flag.C, Not(BorrowFrom(result)));
            cpu.SetFlag(Flag.V, OverflowFrom_Subtraction(leftOperand, rightOperand, aluOut));

            return 1;
        }

        private static UInt64 CMP3(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 h1 = (UInt16)((instruction >> 7) & 1);
            UInt16 h2 = (UInt16)((instruction >> 6) & 1);
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rn = (UInt16)(instruction & 0b111);

            rn |= (UInt16)(h1 << 3);
            rm |= (UInt16)(h2 << 3);

            UInt32 leftOperand = cpu.Reg[rn];
            UInt32 rightOperand = cpu.Reg[rm];

            UInt64 result = (UInt64)leftOperand - (UInt64)rightOperand;
            UInt32 aluOut = (UInt32)result;

            cpu.SetFlag(Flag.N, aluOut >> 31);
            cpu.SetFlag(Flag.Z, (aluOut == 0) ? 1u : 0u);
            cpu.SetFlag(Flag.C, Not(BorrowFrom(result)));
            cpu.SetFlag(Flag.V, OverflowFrom_Subtraction(leftOperand, rightOperand, aluOut));

            return 1;
        }

        private static UInt64 EOR(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            cpu.Reg[rd] ^= cpu.Reg[rm];

            cpu.SetFlag(Flag.N, cpu.Reg[rd] >> 31);
            cpu.SetFlag(Flag.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);

            return 1;
        }

        private static UInt64 LDMIA(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 rn = (UInt16)((instruction >> 8) & 0b111);
            UInt16 registerList = (UInt16)(instruction & 0xff);

            UInt32 address = cpu.Reg[rn];

            if (registerList == 0)
            {
                cpu.Reg[rn] += 0x40;
                SetPC(cpu, cpu._callbackInterface.Read32(address));

                return 5;
            }
            else
            {
                UInt32 n = (UInt32)BitOperations.PopCount(registerList);
                cpu.Reg[rn] += n * 4;

                for (int i = 0; i <= 7; ++i)
                {
                    if (((registerList >> i) & 1) == 1)
                    {
                        cpu.Reg[i] = cpu._callbackInterface.Read32(address);
                        address += 4;
                    }
                }

                return n + 2;
            }
        }

        private static UInt64 LDR1(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)((instruction >> 6) & 0b1_1111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 address = cpu.Reg[rn] + (imm * 4u);
            UInt32 data = BitOperations.RotateRight(cpu._callbackInterface.Read32(address), (int)(8 * (address & 0b11)));
            cpu.Reg[rd] = data;

            return 3;
        }

        private static UInt64 LDR2(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 6) & 0b111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 address = cpu.Reg[rn] + cpu.Reg[rm];
            UInt32 data = BitOperations.RotateRight(cpu._callbackInterface.Read32(address), (int)(8 * (address & 0b11)));
            cpu.Reg[rd] = data;

            return 3;
        }

        private static UInt64 LDR3(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 rd = (UInt16)((instruction >> 8) & 0b111);
            UInt16 imm = (UInt16)(instruction & 0xff);

            UInt32 address = cpu.Reg[PC] + (imm * 4u);
            UInt32 data = cpu._callbackInterface.Read32(address);
            cpu.Reg[rd] = data;

            return 3;
        }

        private static UInt64 LDR4(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 rd = (UInt16)((instruction >> 8) & 0b111);
            UInt16 imm = (UInt16)(instruction & 0xff);

            UInt32 address = cpu.Reg[SP] + (imm * 4u);
            UInt32 data = BitOperations.RotateRight(cpu._callbackInterface.Read32(address), (int)(8 * (address & 0b11)));
            cpu.Reg[rd] = data;

            return 3;
        }

        private static UInt64 LDRB1(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)((instruction >> 6) & 0b1_1111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 address = cpu.Reg[rn] + imm;
            Byte data = cpu._callbackInterface.Read8(address);
            cpu.Reg[rd] = data;

            return 3;
        }

        private static UInt64 LDRB2(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 6) & 0b111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 address = cpu.Reg[rn] + cpu.Reg[rm];
            Byte data = cpu._callbackInterface.Read8(address);
            cpu.Reg[rd] = data;

            return 3;
        }

        private static UInt64 LDRH1(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)((instruction >> 6) & 0b1_1111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 address = cpu.Reg[rn] + (imm * 2u);
            UInt32 data = BitOperations.RotateRight(cpu._callbackInterface.Read16(address), (int)(8 * (address & 1)));
            cpu.Reg[rd] = data;

            return 3;
        }

        private static UInt64 LDRH2(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 6) & 0b111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 address = cpu.Reg[rn] + cpu.Reg[rm];
            UInt32 data = BitOperations.RotateRight(cpu._callbackInterface.Read16(address), (int)(8 * (address & 1)));
            cpu.Reg[rd] = data;

            return 3;
        }

        private static UInt64 LDRSB(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 6) & 0b111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 address = cpu.Reg[rn] + cpu.Reg[rm];
            Byte data = cpu._callbackInterface.Read8(address);
            cpu.Reg[rd] = SignExtend(data, 8);

            return 3;
        }

        private static UInt64 LDRSH(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 6) & 0b111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 address = cpu.Reg[rn] + cpu.Reg[rm];

            if ((address & 1) == 1)
            {
                Byte data = cpu._callbackInterface.Read8(address);
                cpu.Reg[rd] = SignExtend(data, 8);
            }
            else
            {
                UInt16 data = cpu._callbackInterface.Read16(address);
                cpu.Reg[rd] = SignExtend(data, 16);
            }

            return 3;
        }

        private static UInt64 LSL1(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)((instruction >> 6) & 0b1_1111);
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            int shiftAmount = imm;

            if (shiftAmount == 0)
            {
                cpu.Reg[rd] = cpu.Reg[rm];
            }
            else
            {
                cpu.SetFlag(Flag.C, (cpu.Reg[rm] >> (32 - shiftAmount)) & 1);
                cpu.Reg[rd] = cpu.Reg[rm] << shiftAmount;
            }

            cpu.SetFlag(Flag.N, cpu.Reg[rd] >> 31);
            cpu.SetFlag(Flag.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);

            return 1;
        }

        private static UInt64 LSL2(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 rs = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            int shiftAmount = (int)(cpu.Reg[rs] & 0xff);

            if (shiftAmount == 0)
            {
                // nothing to do
            }
            else if (shiftAmount < 32)
            {
                cpu.SetFlag(Flag.C, (cpu.Reg[rd] >> (32 - shiftAmount)) & 1);
                cpu.Reg[rd] <<= shiftAmount;
            }
            else if (shiftAmount == 32)
            {
                cpu.SetFlag(Flag.C, cpu.Reg[rd] & 1);
                cpu.Reg[rd] = 0;
            }
            else
            {
                cpu.SetFlag(Flag.C, 0);
                cpu.Reg[rd] = 0;
            }

            cpu.SetFlag(Flag.N, cpu.Reg[rd] >> 31);
            cpu.SetFlag(Flag.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);

            return 2;
        }

        private static UInt64 LSR1(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)((instruction >> 6) & 0b1_1111);
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            int shiftAmount = imm;

            if (shiftAmount == 0)
            {
                cpu.SetFlag(Flag.C, cpu.Reg[rm] >> 31);
                cpu.Reg[rd] = 0;
            }
            else
            {
                cpu.SetFlag(Flag.C, (cpu.Reg[rm] >> (shiftAmount - 1)) & 1);
                cpu.Reg[rd] = cpu.Reg[rm] >> shiftAmount;
            }

            cpu.SetFlag(Flag.N, cpu.Reg[rd] >> 31);
            cpu.SetFlag(Flag.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);

            return 1;
        }

        private static UInt64 LSR2(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 rs = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            int shiftAmount = (int)(cpu.Reg[rs] & 0xff);

            if (shiftAmount == 0)
            {
                // nothing to do
            }
            else if (shiftAmount < 32)
            {
                cpu.SetFlag(Flag.C, (cpu.Reg[rd] >> (shiftAmount - 1)) & 1);
                cpu.Reg[rd] >>= shiftAmount;
            }
            else if (shiftAmount == 32)
            {
                cpu.SetFlag(Flag.C, cpu.Reg[rd] >> 31);
                cpu.Reg[rd] = 0;
            }
            else
            {
                cpu.SetFlag(Flag.C, 0);
                cpu.Reg[rd] = 0;
            }

            cpu.SetFlag(Flag.N, cpu.Reg[rd] >> 31);
            cpu.SetFlag(Flag.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);

            return 2;
        }

        private static UInt64 MOV1(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 rd = (UInt16)((instruction >> 8) & 0b111);
            UInt16 imm = (UInt16)(instruction & 0xff);

            cpu.Reg[rd] = imm;

            cpu.SetFlag(Flag.N, cpu.Reg[rd] >> 31);
            cpu.SetFlag(Flag.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);

            return 1;
        }

        private static UInt64 MOV3(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 h1 = (UInt16)((instruction >> 7) & 1);
            UInt16 h2 = (UInt16)((instruction >> 6) & 1);
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            rd |= (UInt16)(h1 << 3);
            rm |= (UInt16)(h2 << 3);

            SetReg(cpu, rd, cpu.Reg[rm]);

            return (rd == PC) ? 3u : 1u;
        }

        private static UInt64 MUL(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt64 m = ComputeMultiplicationCycleCount(cpu.Reg[rd], cpu.Reg[rm]);
            cpu.Reg[rd] *= cpu.Reg[rm];

            cpu.SetFlag(Flag.N, cpu.Reg[rd] >> 31);
            cpu.SetFlag(Flag.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);

            return m + 1;
        }

        private static UInt64 MVN(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            cpu.Reg[rd] = ~cpu.Reg[rm];

            cpu.SetFlag(Flag.N, cpu.Reg[rd] >> 31);
            cpu.SetFlag(Flag.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);

            return 1;
        }

        private static UInt64 NEG(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 leftOperand = 0;
            UInt32 rightOperand = cpu.Reg[rm];

            UInt64 result = (UInt64)leftOperand - (UInt64)rightOperand;
            cpu.Reg[rd] = (UInt32)result;

            cpu.SetFlag(Flag.N, cpu.Reg[rd] >> 31);
            cpu.SetFlag(Flag.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
            cpu.SetFlag(Flag.C, Not(BorrowFrom(result)));
            cpu.SetFlag(Flag.V, OverflowFrom_Subtraction(leftOperand, rightOperand, cpu.Reg[rd]));

            return 1;
        }

        private static UInt64 ORR(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            cpu.Reg[rd] |= cpu.Reg[rm];

            cpu.SetFlag(Flag.N, cpu.Reg[rd] >> 31);
            cpu.SetFlag(Flag.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);

            return 1;
        }

        private static UInt64 POP(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 r = (UInt16)((instruction >> 8) & 1);
            UInt16 registerList = (UInt16)(instruction & 0xff);

            UInt32 n = (UInt32)BitOperations.PopCount(registerList);
            UInt32 address = cpu.Reg[SP];
            cpu.Reg[SP] += 4 * (r + n);

            for (int i = 0; i <= 7; ++i)
            {
                if (((registerList >> i) & 1) == 1)
                {
                    cpu.Reg[i] = cpu._callbackInterface.Read32(address);
                    address += 4;
                }
            }

            if (r == 1)
            {
                SetPC(cpu, cpu._callbackInterface.Read32(address));

                return n + 5;
            }
            else
            {
                return n + 4;
            }
        }

        private static UInt64 PUSH(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 r = (UInt16)((instruction >> 8) & 1);
            UInt16 registerList = (UInt16)(instruction & 0xff);

            UInt32 n = (UInt32)BitOperations.PopCount(registerList);
            cpu.Reg[SP] -= 4 * (r + n);
            UInt32 address = cpu.Reg[SP];

            for (int i = 0; i <= 7; ++i)
            {
                if (((registerList >> i) & 1) == 1)
                {
                    cpu._callbackInterface.Write32(address, cpu.Reg[i]);
                    address += 4;
                }
            }

            if (r == 1)
            {
                cpu._callbackInterface.Write32(address, cpu.Reg[LR]);

                return n + 2;
            }
            else
            {
                return n + 1;
            }
        }

        private static UInt64 ROR(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 rs = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            if ((cpu.Reg[rs] & 0xff) == 0)
            {
                // nothing to do
            }
            else if ((cpu.Reg[rs] & 0b1_1111) == 0)
            {
                cpu.SetFlag(Flag.C, cpu.Reg[rd] >> 31);
            }
            else
            {
                cpu.SetFlag(Flag.C, (cpu.Reg[rd] >> (int)((cpu.Reg[rs] & 0b1_1111) - 1)) & 1);
                cpu.Reg[rd] = BitOperations.RotateRight(cpu.Reg[rd], (int)(cpu.Reg[rs] & 0b1_1111));
            }

            cpu.SetFlag(Flag.N, cpu.Reg[rd] >> 31);
            cpu.SetFlag(Flag.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);

            return 2;
        }

        private static UInt64 SBC(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 leftOperand = cpu.Reg[rd];
            UInt32 rightOperand = cpu.Reg[rm];

            UInt64 result = (UInt64)leftOperand - (UInt64)rightOperand - (UInt64)Not(cpu.GetFlag(Flag.C));
            cpu.Reg[rd] = (UInt32)result;

            cpu.SetFlag(Flag.N, cpu.Reg[rd] >> 31);
            cpu.SetFlag(Flag.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
            cpu.SetFlag(Flag.C, Not(BorrowFrom(result)));
            cpu.SetFlag(Flag.V, OverflowFrom_Subtraction(leftOperand, rightOperand, cpu.Reg[rd]));

            return 1;
        }

        private static UInt64 STMIA(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 rn = (UInt16)((instruction >> 8) & 0b111);
            UInt16 registerList = (UInt16)(instruction & 0xff);

            UInt32 address = cpu.Reg[rn];

            if (registerList == 0)
            {
                cpu.Reg[rn] += 0x40;
                cpu._callbackInterface.Write32(address, cpu.Reg[PC] + 2);

                return 2;
            }
            else
            {
                UInt32 n = (UInt32)BitOperations.PopCount(registerList);
                UInt32 oldRegRn = cpu.Reg[rn];
                cpu.Reg[rn] += n * 4;

                for (int i = 0; i <= 7; ++i)
                {
                    if (((registerList >> i) & 1) == 1)
                    {
                        if ((i == rn) && ((registerList & ~(0xff << i)) == 0))
                            cpu._callbackInterface.Write32(address, oldRegRn);
                        else
                            cpu._callbackInterface.Write32(address, cpu.Reg[i]);

                        address += 4;
                    }
                }

                return n + 1;
            }
        }

        private static UInt64 STR1(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)((instruction >> 6) & 0b1_1111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 address = cpu.Reg[rn] + (imm * 4u);
            cpu._callbackInterface.Write32(address, cpu.Reg[rd]);

            return 2;
        }

        private static UInt64 STR2(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 6) & 0b111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 address = cpu.Reg[rn] + cpu.Reg[rm];
            cpu._callbackInterface.Write32(address, cpu.Reg[rd]);

            return 2;
        }

        private static UInt64 STR3(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 rd = (UInt16)((instruction >> 8) & 0b111);
            UInt16 imm = (UInt16)(instruction & 0xff);

            UInt32 address = cpu.Reg[SP] + (imm * 4u);
            cpu._callbackInterface.Write32(address, cpu.Reg[rd]);

            return 2;
        }

        private static UInt64 STRB1(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)((instruction >> 6) & 0b1_1111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 address = cpu.Reg[rn] + imm;
            cpu._callbackInterface.Write8(address, (Byte)cpu.Reg[rd]);

            return 2;
        }

        private static UInt64 STRB2(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 6) & 0b111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 address = cpu.Reg[rn] + cpu.Reg[rm];
            cpu._callbackInterface.Write8(address, (Byte)cpu.Reg[rd]);

            return 2;
        }

        private static UInt64 STRH1(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)((instruction >> 6) & 0b1_1111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 address = cpu.Reg[rn] + (imm * 2u);
            cpu._callbackInterface.Write16(address, (UInt16)cpu.Reg[rd]);

            return 2;
        }

        private static UInt64 STRH2(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 6) & 0b111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 address = cpu.Reg[rn] + cpu.Reg[rm];
            cpu._callbackInterface.Write16(address, (UInt16)cpu.Reg[rd]);

            return 2;
        }

        private static UInt64 SUB1(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)((instruction >> 6) & 0b111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 leftOperand = cpu.Reg[rn];
            UInt32 rightOperand = imm;

            UInt64 result = (UInt64)leftOperand - (UInt64)rightOperand;
            cpu.Reg[rd] = (UInt32)result;

            cpu.SetFlag(Flag.N, cpu.Reg[rd] >> 31);
            cpu.SetFlag(Flag.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
            cpu.SetFlag(Flag.C, Not(BorrowFrom(result)));
            cpu.SetFlag(Flag.V, OverflowFrom_Subtraction(leftOperand, rightOperand, cpu.Reg[rd]));

            return 1;
        }

        private static UInt64 SUB2(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 rd = (UInt16)((instruction >> 8) & 0b111);
            UInt16 imm = (UInt16)(instruction & 0xff);

            UInt32 leftOperand = cpu.Reg[rd];
            UInt32 rightOperand = imm;

            UInt64 result = (UInt64)leftOperand - (UInt64)rightOperand;
            cpu.Reg[rd] = (UInt32)result;

            cpu.SetFlag(Flag.N, cpu.Reg[rd] >> 31);
            cpu.SetFlag(Flag.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
            cpu.SetFlag(Flag.C, Not(BorrowFrom(result)));
            cpu.SetFlag(Flag.V, OverflowFrom_Subtraction(leftOperand, rightOperand, cpu.Reg[rd]));

            return 1;
        }

        private static UInt64 SUB3(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 6) & 0b111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 leftOperand = cpu.Reg[rn];
            UInt32 rightOperand = cpu.Reg[rm];

            UInt64 result = (UInt64)leftOperand - (UInt64)rightOperand;
            cpu.Reg[rd] = (UInt32)result;

            cpu.SetFlag(Flag.N, cpu.Reg[rd] >> 31);
            cpu.SetFlag(Flag.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
            cpu.SetFlag(Flag.C, Not(BorrowFrom(result)));
            cpu.SetFlag(Flag.V, OverflowFrom_Subtraction(leftOperand, rightOperand, cpu.Reg[rd]));

            return 1;
        }

        private static UInt64 SUB4(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)(instruction & 0x7f);

            cpu.Reg[SP] -= (UInt32)imm << 2;

            return 1;
        }

        private static UInt64 SWI(CPU_Core cpu, UInt16 instruction)
        {
            return cpu._callbackInterface.HandleSWI();
        }

        private static UInt64 TST(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rn = (UInt16)(instruction & 0b111);

            UInt32 aluOut = cpu.Reg[rn] & cpu.Reg[rm];

            cpu.SetFlag(Flag.N, aluOut >> 31);
            cpu.SetFlag(Flag.Z, (aluOut == 0) ? 1u : 0u);

            return 1;
        }
    }
}
