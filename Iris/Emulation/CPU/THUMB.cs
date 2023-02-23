using System.Numerics;

namespace Iris.Emulation.CPU
{
    internal sealed partial class Core
    {
        // This doesn't compile and I don't know why :(
        //private unsafe readonly record struct THUMB_InstructionListEntry(UInt16 Mask, UInt16 Expected, delegate*<Core, UInt16, void> Handler);

        private readonly struct THUMB_InstructionListEntry
        {
            internal readonly UInt16 Mask;
            internal readonly UInt16 Expected;
            internal unsafe readonly delegate*<Core, UInt16, void> Handler;

            internal unsafe THUMB_InstructionListEntry(UInt16 mask, UInt16 expected, delegate*<Core, UInt16, void> handler)
            {
                Mask = mask;
                Expected = expected;
                Handler = handler;
            }
        }

        private static unsafe readonly THUMB_InstructionListEntry[] THUMB_InstructionList = new THUMB_InstructionListEntry[]
        {
            // ADC
            new(0xffc0, 0x4140, &THUMB_ADC),

            // ADD
            new(0xfe00, 0x1c00, &THUMB_ADD1),
            new(0xf800, 0x3000, &THUMB_ADD2),
            new(0xfe00, 0x1800, &THUMB_ADD3),
            new(0xff00, 0x4400, &THUMB_ADD4),
            new(0xf800, 0xa000, &THUMB_ADD5),
            new(0xf800, 0xa800, &THUMB_ADD6),
            new(0xff80, 0xb000, &THUMB_ADD7),

            // AND
            new(0xffc0, 0x4000, &THUMB_AND),

            // ASR
            new(0xf800, 0x1000, &THUMB_ASR1),
            new(0xffc0, 0x4100, &THUMB_ASR2),

            // B
            new(0xff00, 0xd000, &THUMB_B1), // condition field 0b0000
            new(0xff00, 0xd100, &THUMB_B1), // condition field 0b0001
            new(0xff00, 0xd200, &THUMB_B1), // condition field 0b0010
            new(0xff00, 0xd300, &THUMB_B1), // condition field 0b0011
            new(0xff00, 0xd400, &THUMB_B1), // condition field 0b0100
            new(0xff00, 0xd500, &THUMB_B1), // condition field 0b0101
            new(0xff00, 0xd600, &THUMB_B1), // condition field 0b0110
            new(0xff00, 0xd700, &THUMB_B1), // condition field 0b0111
            new(0xff00, 0xd800, &THUMB_B1), // condition field 0b1000
            new(0xff00, 0xd900, &THUMB_B1), // condition field 0b1001
            new(0xff00, 0xda00, &THUMB_B1), // condition field 0b1010
            new(0xff00, 0xdb00, &THUMB_B1), // condition field 0b1011
            new(0xff00, 0xdc00, &THUMB_B1), // condition field 0b1100
            new(0xff00, 0xdd00, &THUMB_B1), // condition field 0b1101
            new(0xf800, 0xe000, &THUMB_B2),

            // BIC
            new(0xffc0, 0x4380, &THUMB_BIC),

            // BL
            new(0xf000, 0xf000, &THUMB_BL),

            // BX
            new(0xff80, 0x4700, &THUMB_BX),

            // CMN
            new(0xffc0, 0x42c0, &THUMB_CMN),

            // CMP
            new(0xf800, 0x2800, &THUMB_CMP1),
            new(0xffc0, 0x4280, &THUMB_CMP2),
            new(0xff00, 0x4500, &THUMB_CMP3),

            // EOR
            new(0xffc0, 0x4040, &THUMB_EOR),

            // LDMIA
            new(0xf800, 0xc800, &THUMB_LDMIA),

            // LDR
            new(0xf800, 0x6800, &THUMB_LDR1),
            new(0xfe00, 0x5800, &THUMB_LDR2),
            new(0xf800, 0x4800, &THUMB_LDR3),
            new(0xf800, 0x9800, &THUMB_LDR4),

            // LDRB
            new(0xf800, 0x7800, &THUMB_LDRB1),
            new(0xfe00, 0x5c00, &THUMB_LDRB2),

            // LDRH
            new(0xf800, 0x8800, &THUMB_LDRH1),
            new(0xfe00, 0x5a00, &THUMB_LDRH2),

            // LDRSB
            new(0xfe00, 0x5600, &THUMB_LDRSB),

            // LDRSH
            new(0xfe00, 0x5e00, &THUMB_LDRSH),

            // LSL
            new(0xf800, 0x0000, &THUMB_LSL1),
            new(0xffc0, 0x4080, &THUMB_LSL2),

            // LSR
            new(0xf800, 0x0800, &THUMB_LSR1),
            new(0xffc0, 0x40c0, &THUMB_LSR2),

            // MOV
            new(0xf800, 0x2000, &THUMB_MOV1),
            //new(0xffc0, 0x1c00, &THUMB_MOV2),
            new(0xff00, 0x4600, &THUMB_MOV3),

            // MUL
            new(0xffc0, 0x4340, &THUMB_MUL),

            // MVN
            new(0xffc0, 0x43c0, &THUMB_MVN),

            // NEG
            new(0xffc0, 0x4240, &THUMB_NEG),

            // ORR
            new(0xffc0, 0x4300, &THUMB_ORR),

            // POP
            new(0xfe00, 0xbc00, &THUMB_POP),

            // PUSH
            new(0xfe00, 0xb400, &THUMB_PUSH),

            // ROR
            new(0xffc0, 0x41c0, &THUMB_ROR),

            // SBC
            new(0xffc0, 0x4180, &THUMB_SBC),

            // STMIA
            new(0xf800, 0xc000, &THUMB_STMIA),

            // STR
            new(0xf800, 0x6000, &THUMB_STR1),
            new(0xfe00, 0x5000, &THUMB_STR2),
            new(0xf800, 0x9000, &THUMB_STR3),

            // STRB
            new(0xf800, 0x7000, &THUMB_STRB1),
            new(0xfe00, 0x5400, &THUMB_STRB2),

            // STRH
            new(0xf800, 0x8000, &THUMB_STRH1),
            new(0xfe00, 0x5200, &THUMB_STRH2),

            // SUB
            new(0xfe00, 0x1e00, &THUMB_SUB1),
            new(0xf800, 0x3800, &THUMB_SUB2),
            new(0xfe00, 0x1a00, &THUMB_SUB3),
            new(0xff80, 0xb080, &THUMB_SUB4),

            // SWI
            new(0xff00, 0xdf00, &THUMB_SWI),

            // TST
            new(0xffc0, 0x4200, &THUMB_TST),
        };

        private unsafe readonly delegate*<Core, UInt16, void>[] THUMB_InstructionLUT = new delegate*<Core, UInt16, void>[1 << 16];

        private void THUMB_InitInstructionLUT()
        {
            for (UInt32 instruction = 0; instruction < THUMB_InstructionLUT.Length; ++instruction)
            {
                bool unknownInstruction = true;

                foreach (THUMB_InstructionListEntry entry in THUMB_InstructionList)
                {
                    if ((instruction & entry.Mask) == entry.Expected)
                    {
                        unsafe
                        {
                            THUMB_InstructionLUT[instruction] = entry.Handler;
                        }

                        unknownInstruction = false;
                        break;
                    }
                }

                if (unknownInstruction)
                {
                    unsafe
                    {
                        THUMB_InstructionLUT[instruction] = &THUMB_UNKNOWN;
                    }
                }
            }
        }

        private void THUMB_Step()
        {
            UInt16 instruction = _callbackInterface.ReadMemory16(NextInstructionAddress);
            NextInstructionAddress += 2;
            Reg[PC] = NextInstructionAddress + 2;

            unsafe
            {
                THUMB_InstructionLUT[instruction](this, instruction);
            }
        }

        private void THUMB_SetPC(UInt32 value)
        {
            NextInstructionAddress = value & 0xffff_fffe;
        }

        private void THUMB_SetReg(UInt32 i, UInt32 value)
        {
            if (i == PC)
                THUMB_SetPC(value);
            else
                Reg[i] = value;
        }

        private static void THUMB_UNKNOWN(Core cpu, UInt16 instruction)
        {
            throw new Exception(string.Format("Emulation.CPU.Core: Unknown THUMB instruction 0x{0:x4} at address 0x{1:x8}", instruction, cpu.NextInstructionAddress - 2));
        }

        private static void THUMB_ADC(Core cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 leftOperand = cpu.Reg[rd];
            UInt64 rightOperand = (UInt64)cpu.Reg[rm] + (UInt64)cpu.GetFlag(Flags.C);

            UInt64 result = (UInt64)leftOperand + rightOperand;
            cpu.Reg[rd] = (UInt32)result;

            cpu.SetFlag(Flags.N, cpu.Reg[rd] >> 31);
            cpu.SetFlag(Flags.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
            cpu.SetFlag(Flags.C, CarryFrom(result));
            cpu.SetFlag(Flags.V, OverflowFrom_Addition(leftOperand, (UInt32)rightOperand, cpu.Reg[rd]));
        }

        private static void THUMB_ADD1(Core cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)((instruction >> 6) & 0b111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 leftOperand = cpu.Reg[rn];
            UInt32 rightOperand = imm;

            UInt64 result = (UInt64)leftOperand + (UInt64)rightOperand;
            cpu.Reg[rd] = (UInt32)result;

            cpu.SetFlag(Flags.N, cpu.Reg[rd] >> 31);
            cpu.SetFlag(Flags.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
            cpu.SetFlag(Flags.C, CarryFrom(result));
            cpu.SetFlag(Flags.V, OverflowFrom_Addition(leftOperand, rightOperand, cpu.Reg[rd]));
        }

        private static void THUMB_ADD2(Core cpu, UInt16 instruction)
        {
            UInt16 rd = (UInt16)((instruction >> 8) & 0b111);
            UInt16 imm = (UInt16)(instruction & 0xff);

            UInt32 leftOperand = cpu.Reg[rd];
            UInt32 rightOperand = imm;

            UInt64 result = (UInt64)leftOperand + (UInt64)rightOperand;
            cpu.Reg[rd] = (UInt32)result;

            cpu.SetFlag(Flags.N, cpu.Reg[rd] >> 31);
            cpu.SetFlag(Flags.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
            cpu.SetFlag(Flags.C, CarryFrom(result));
            cpu.SetFlag(Flags.V, OverflowFrom_Addition(leftOperand, rightOperand, cpu.Reg[rd]));
        }

        private static void THUMB_ADD3(Core cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 6) & 0b111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 leftOperand = cpu.Reg[rn];
            UInt32 rightOperand = cpu.Reg[rm];

            UInt64 result = (UInt64)leftOperand + (UInt64)rightOperand;
            cpu.Reg[rd] = (UInt32)result;

            cpu.SetFlag(Flags.N, cpu.Reg[rd] >> 31);
            cpu.SetFlag(Flags.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
            cpu.SetFlag(Flags.C, CarryFrom(result));
            cpu.SetFlag(Flags.V, OverflowFrom_Addition(leftOperand, rightOperand, cpu.Reg[rd]));
        }

        private static void THUMB_ADD4(Core cpu, UInt16 instruction)
        {
            UInt16 h1 = (UInt16)((instruction >> 7) & 1);
            UInt16 h2 = (UInt16)((instruction >> 6) & 1);
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            rd |= (UInt16)(h1 << 3);
            rm |= (UInt16)(h2 << 3);

            cpu.THUMB_SetReg(rd, cpu.Reg[rd] + cpu.Reg[rm]);
        }

        private static void THUMB_ADD5(Core cpu, UInt16 instruction)
        {
            UInt16 rd = (UInt16)((instruction >> 8) & 0b111);
            UInt16 imm = (UInt16)(instruction & 0xff);

            cpu.Reg[rd] = (cpu.Reg[PC] & 0xffff_fffc) + (imm * 4u);
        }

        private static void THUMB_ADD6(Core cpu, UInt16 instruction)
        {
            UInt16 rd = (UInt16)((instruction >> 8) & 0b111);
            UInt16 imm = (UInt16)(instruction & 0xff);

            cpu.Reg[rd] = cpu.Reg[SP] + (imm * 4u);
        }

        private static void THUMB_ADD7(Core cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)(instruction & 0x7f);

            cpu.Reg[SP] += imm * 4u;
        }

        private static void THUMB_AND(Core cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            cpu.Reg[rd] &= cpu.Reg[rm];

            cpu.SetFlag(Flags.N, cpu.Reg[rd] >> 31);
            cpu.SetFlag(Flags.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
        }

        private static void THUMB_ASR1(Core cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)((instruction >> 6) & 0b1_1111);
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            int shiftAmount = imm;

            if (shiftAmount == 0)
            {
                cpu.SetFlag(Flags.C, cpu.Reg[rm] >> 31);
                cpu.Reg[rd] = ((cpu.Reg[rm] >> 31) == 0) ? 0 : 0xffff_ffff;
            }
            else
            {
                cpu.SetFlag(Flags.C, (cpu.Reg[rm] >> (shiftAmount - 1)) & 1);
                cpu.Reg[rd] = ArithmeticShiftRight(cpu.Reg[rm], shiftAmount);
            }

            cpu.SetFlag(Flags.N, cpu.Reg[rd] >> 31);
            cpu.SetFlag(Flags.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
        }

        private static void THUMB_ASR2(Core cpu, UInt16 instruction)
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
                cpu.SetFlag(Flags.C, (cpu.Reg[rd] >> (shiftAmount - 1)) & 1);
                cpu.Reg[rd] = ArithmeticShiftRight(cpu.Reg[rd], shiftAmount);
            }
            else
            {
                cpu.SetFlag(Flags.C, cpu.Reg[rd] >> 31);
                cpu.Reg[rd] = ((cpu.Reg[rd] >> 31) == 0) ? 0 : 0xffff_ffff;
            }

            cpu.SetFlag(Flags.N, cpu.Reg[rd] >> 31);
            cpu.SetFlag(Flags.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
        }

        private static void THUMB_B1(Core cpu, UInt16 instruction)
        {
            UInt16 cond = (UInt16)((instruction >> 8) & 0b1111);
            UInt16 imm = (UInt16)(instruction & 0xff);

            if (cpu.ConditionPassed(cond))
                cpu.THUMB_SetPC(cpu.Reg[PC] + (SignExtend(imm, 8) << 1));
        }

        private static void THUMB_B2(Core cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)(instruction & 0x7ff);

            cpu.THUMB_SetPC(cpu.Reg[PC] + (SignExtend(imm, 11) << 1));
        }

        private static void THUMB_BIC(Core cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            cpu.Reg[rd] &= ~cpu.Reg[rm];

            cpu.SetFlag(Flags.N, cpu.Reg[rd] >> 31);
            cpu.SetFlag(Flags.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
        }

        private static void THUMB_BL(Core cpu, UInt16 instruction)
        {
            UInt16 h = (UInt16)((instruction >> 11) & 0b11);
            UInt16 offset = (UInt16)(instruction & 0x7ff);

            if (h == 0b10)
            {
                cpu.Reg[LR] = cpu.Reg[PC] + (SignExtend(offset, 11) << 12);
            }
            else if (h == 0b11)
            {
                // save NextInstructionAddress because it's invalidated by THUMB_SetPC
                UInt32 nextInstructionAddress = cpu.NextInstructionAddress;

                cpu.THUMB_SetPC(cpu.Reg[LR] + (UInt32)(offset << 1));
                cpu.Reg[LR] = nextInstructionAddress | 1;
            }
        }

        private static void THUMB_BX(Core cpu, UInt16 instruction)
        {
            UInt16 h2 = (UInt16)((instruction >> 6) & 1);
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);

            rm |= (UInt16)(h2 << 3);

            cpu.CPSR = (cpu.CPSR & ~(1u << 5)) | ((cpu.Reg[rm] & 1) << 5);
            cpu.THUMB_SetPC(cpu.Reg[rm]);
        }

        private static void THUMB_CMN(Core cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rn = (UInt16)(instruction & 0b111);

            UInt32 leftOperand = cpu.Reg[rn];
            UInt32 rightOperand = cpu.Reg[rm];

            UInt64 result = (UInt64)leftOperand + (UInt64)rightOperand;
            UInt32 aluOut = (UInt32)result;

            cpu.SetFlag(Flags.N, aluOut >> 31);
            cpu.SetFlag(Flags.Z, (aluOut == 0) ? 1u : 0u);
            cpu.SetFlag(Flags.C, CarryFrom(result));
            cpu.SetFlag(Flags.V, OverflowFrom_Addition(leftOperand, rightOperand, aluOut));
        }

        private static void THUMB_CMP1(Core cpu, UInt16 instruction)
        {
            UInt16 rn = (UInt16)((instruction >> 8) & 0b111);
            UInt16 imm = (UInt16)(instruction & 0xff);

            UInt32 leftOperand = cpu.Reg[rn];
            UInt32 rightOperand = imm;

            UInt32 aluOut = leftOperand - rightOperand;

            cpu.SetFlag(Flags.N, aluOut >> 31);
            cpu.SetFlag(Flags.Z, (aluOut == 0) ? 1u : 0u);
            cpu.SetFlag(Flags.C, Not(BorrowFrom(leftOperand, rightOperand)));
            cpu.SetFlag(Flags.V, OverflowFrom_Subtraction(leftOperand, rightOperand, aluOut));
        }

        private static void THUMB_CMP2(Core cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rn = (UInt16)(instruction & 0b111);

            UInt32 leftOperand = cpu.Reg[rn];
            UInt32 rightOperand = cpu.Reg[rm];

            UInt32 aluOut = leftOperand - rightOperand;

            cpu.SetFlag(Flags.N, aluOut >> 31);
            cpu.SetFlag(Flags.Z, (aluOut == 0) ? 1u : 0u);
            cpu.SetFlag(Flags.C, Not(BorrowFrom(leftOperand, rightOperand)));
            cpu.SetFlag(Flags.V, OverflowFrom_Subtraction(leftOperand, rightOperand, aluOut));
        }

        private static void THUMB_CMP3(Core cpu, UInt16 instruction)
        {
            UInt16 h1 = (UInt16)((instruction >> 7) & 1);
            UInt16 h2 = (UInt16)((instruction >> 6) & 1);
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rn = (UInt16)(instruction & 0b111);

            rn |= (UInt16)(h1 << 3);
            rm |= (UInt16)(h2 << 3);

            UInt32 leftOperand = cpu.Reg[rn];
            UInt32 rightOperand = cpu.Reg[rm];

            UInt32 aluOut = leftOperand - rightOperand;

            cpu.SetFlag(Flags.N, aluOut >> 31);
            cpu.SetFlag(Flags.Z, (aluOut == 0) ? 1u : 0u);
            cpu.SetFlag(Flags.C, Not(BorrowFrom(leftOperand, rightOperand)));
            cpu.SetFlag(Flags.V, OverflowFrom_Subtraction(leftOperand, rightOperand, aluOut));
        }

        private static void THUMB_EOR(Core cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            cpu.Reg[rd] ^= cpu.Reg[rm];

            cpu.SetFlag(Flags.N, cpu.Reg[rd] >> 31);
            cpu.SetFlag(Flags.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
        }

        private static void THUMB_LDMIA(Core cpu, UInt16 instruction)
        {
            UInt16 rn = (UInt16)((instruction >> 8) & 0b111);
            UInt16 registerList = (UInt16)(instruction & 0xff);

            UInt32 address = cpu.Reg[rn];

            if (registerList == 0)
            {
                cpu.Reg[rn] += 0x40;
                cpu.THUMB_SetPC(cpu._callbackInterface.ReadMemory32(address));
            }
            else
            {
                cpu.Reg[rn] += (UInt32)BitOperations.PopCount(registerList) * 4;

                for (int i = 0; i <= 7; ++i)
                {
                    if (((registerList >> i) & 1) == 1)
                    {
                        cpu.Reg[i] = cpu._callbackInterface.ReadMemory32(address);
                        address += 4;
                    }
                }
            }
        }

        private static void THUMB_LDR1(Core cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)((instruction >> 6) & 0b1_1111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 address = cpu.Reg[rn] + (imm * 4u);
            UInt32 data = BitOperations.RotateRight(cpu._callbackInterface.ReadMemory32(address), (int)(8 * (address & 0b11)));
            cpu.Reg[rd] = data;
        }

        private static void THUMB_LDR2(Core cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 6) & 0b111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 address = cpu.Reg[rn] + cpu.Reg[rm];
            UInt32 data = BitOperations.RotateRight(cpu._callbackInterface.ReadMemory32(address), (int)(8 * (address & 0b11)));
            cpu.Reg[rd] = data;
        }

        private static void THUMB_LDR3(Core cpu, UInt16 instruction)
        {
            UInt16 rd = (UInt16)((instruction >> 8) & 0b111);
            UInt16 imm = (UInt16)(instruction & 0xff);

            UInt32 address = cpu.Reg[PC] + (imm * 4u);
            UInt32 data = cpu._callbackInterface.ReadMemory32(address);
            cpu.Reg[rd] = data;
        }

        private static void THUMB_LDR4(Core cpu, UInt16 instruction)
        {
            UInt16 rd = (UInt16)((instruction >> 8) & 0b111);
            UInt16 imm = (UInt16)(instruction & 0xff);

            UInt32 address = cpu.Reg[SP] + (imm * 4u);
            UInt32 data = BitOperations.RotateRight(cpu._callbackInterface.ReadMemory32(address), (int)(8 * (address & 0b11)));
            cpu.Reg[rd] = data;
        }

        private static void THUMB_LDRB1(Core cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)((instruction >> 6) & 0b1_1111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 address = cpu.Reg[rn] + imm;
            Byte data = cpu._callbackInterface.ReadMemory8(address);
            cpu.Reg[rd] = data;
        }

        private static void THUMB_LDRB2(Core cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 6) & 0b111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 address = cpu.Reg[rn] + cpu.Reg[rm];
            Byte data = cpu._callbackInterface.ReadMemory8(address);
            cpu.Reg[rd] = data;
        }

        private static void THUMB_LDRH1(Core cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)((instruction >> 6) & 0b1_1111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 address = cpu.Reg[rn] + (imm * 2u);
            UInt32 data = BitOperations.RotateRight(cpu._callbackInterface.ReadMemory16(address), (int)(8 * (address & 1)));
            cpu.Reg[rd] = data;
        }

        private static void THUMB_LDRH2(Core cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 6) & 0b111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 address = cpu.Reg[rn] + cpu.Reg[rm];
            UInt32 data = BitOperations.RotateRight(cpu._callbackInterface.ReadMemory16(address), (int)(8 * (address & 1)));
            cpu.Reg[rd] = data;
        }

        private static void THUMB_LDRSB(Core cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 6) & 0b111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 address = cpu.Reg[rn] + cpu.Reg[rm];
            Byte data = cpu._callbackInterface.ReadMemory8(address);
            cpu.Reg[rd] = SignExtend(data, 8);
        }

        private static void THUMB_LDRSH(Core cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 6) & 0b111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 address = cpu.Reg[rn] + cpu.Reg[rm];

            if ((address & 1) == 1)
            {
                Byte data = cpu._callbackInterface.ReadMemory8(address);
                cpu.Reg[rd] = SignExtend(data, 8);
            }
            else
            {
                UInt16 data = cpu._callbackInterface.ReadMemory16(address);
                cpu.Reg[rd] = SignExtend(data, 16);
            }
        }

        private static void THUMB_LSL1(Core cpu, UInt16 instruction)
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
                cpu.SetFlag(Flags.C, (cpu.Reg[rm] >> (32 - shiftAmount)) & 1);
                cpu.Reg[rd] = cpu.Reg[rm] << shiftAmount;
            }

            cpu.SetFlag(Flags.N, cpu.Reg[rd] >> 31);
            cpu.SetFlag(Flags.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
        }

        private static void THUMB_LSL2(Core cpu, UInt16 instruction)
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
                cpu.SetFlag(Flags.C, (cpu.Reg[rd] >> (32 - shiftAmount)) & 1);
                cpu.Reg[rd] = cpu.Reg[rd] << shiftAmount;
            }
            else if (shiftAmount == 32)
            {
                cpu.SetFlag(Flags.C, cpu.Reg[rd] & 1);
                cpu.Reg[rd] = 0;
            }
            else
            {
                cpu.SetFlag(Flags.C, 0);
                cpu.Reg[rd] = 0;
            }

            cpu.SetFlag(Flags.N, cpu.Reg[rd] >> 31);
            cpu.SetFlag(Flags.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
        }

        private static void THUMB_LSR1(Core cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)((instruction >> 6) & 0b1_1111);
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            int shiftAmount = imm;

            if (shiftAmount == 0)
            {
                cpu.SetFlag(Flags.C, cpu.Reg[rm] >> 31);
                cpu.Reg[rd] = 0;
            }
            else
            {
                cpu.SetFlag(Flags.C, (cpu.Reg[rm] >> (shiftAmount - 1)) & 1);
                cpu.Reg[rd] = cpu.Reg[rm] >> shiftAmount;
            }

            cpu.SetFlag(Flags.N, cpu.Reg[rd] >> 31);
            cpu.SetFlag(Flags.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
        }

        private static void THUMB_LSR2(Core cpu, UInt16 instruction)
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
                cpu.SetFlag(Flags.C, (cpu.Reg[rd] >> (shiftAmount - 1)) & 1);
                cpu.Reg[rd] = cpu.Reg[rd] >> shiftAmount;
            }
            else if (shiftAmount == 32)
            {
                cpu.SetFlag(Flags.C, cpu.Reg[rd] >> 31);
                cpu.Reg[rd] = 0;
            }
            else
            {
                cpu.SetFlag(Flags.C, 0);
                cpu.Reg[rd] = 0;
            }

            cpu.SetFlag(Flags.N, cpu.Reg[rd] >> 31);
            cpu.SetFlag(Flags.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
        }

        private static void THUMB_MOV1(Core cpu, UInt16 instruction)
        {
            UInt16 rd = (UInt16)((instruction >> 8) & 0b111);
            UInt16 imm = (UInt16)(instruction & 0xff);

            cpu.Reg[rd] = imm;

            cpu.SetFlag(Flags.N, cpu.Reg[rd] >> 31);
            cpu.SetFlag(Flags.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
        }

        private static void THUMB_MOV3(Core cpu, UInt16 instruction)
        {
            UInt16 h1 = (UInt16)((instruction >> 7) & 1);
            UInt16 h2 = (UInt16)((instruction >> 6) & 1);
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            rd |= (UInt16)(h1 << 3);
            rm |= (UInt16)(h2 << 3);

            cpu.THUMB_SetReg(rd, cpu.Reg[rm]);
        }

        private static void THUMB_MUL(Core cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            cpu.Reg[rd] *= cpu.Reg[rm];

            cpu.SetFlag(Flags.N, cpu.Reg[rd] >> 31);
            cpu.SetFlag(Flags.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
        }

        private static void THUMB_MVN(Core cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            cpu.Reg[rd] = ~cpu.Reg[rm];

            cpu.SetFlag(Flags.N, cpu.Reg[rd] >> 31);
            cpu.SetFlag(Flags.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
        }

        private static void THUMB_NEG(Core cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 leftOperand = 0;
            UInt32 rightOperand = cpu.Reg[rm];

            cpu.Reg[rd] = leftOperand - rightOperand;

            cpu.SetFlag(Flags.N, cpu.Reg[rd] >> 31);
            cpu.SetFlag(Flags.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
            cpu.SetFlag(Flags.C, Not(BorrowFrom(leftOperand, rightOperand)));
            cpu.SetFlag(Flags.V, OverflowFrom_Subtraction(leftOperand, rightOperand, cpu.Reg[rd]));
        }

        private static void THUMB_ORR(Core cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            cpu.Reg[rd] |= cpu.Reg[rm];

            cpu.SetFlag(Flags.N, cpu.Reg[rd] >> 31);
            cpu.SetFlag(Flags.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
        }

        private static void THUMB_POP(Core cpu, UInt16 instruction)
        {
            UInt16 r = (UInt16)((instruction >> 8) & 1);
            UInt16 registerList = (UInt16)(instruction & 0xff);

            UInt32 address = cpu.Reg[SP];
            cpu.Reg[SP] += 4 * (r + (UInt32)BitOperations.PopCount(registerList));

            for (int i = 0; i <= 7; ++i)
            {
                if (((registerList >> i) & 1) == 1)
                {
                    cpu.Reg[i] = cpu._callbackInterface.ReadMemory32(address);
                    address += 4;
                }
            }

            if (r == 1)
                cpu.THUMB_SetPC(cpu._callbackInterface.ReadMemory32(address));
        }

        private static void THUMB_PUSH(Core cpu, UInt16 instruction)
        {
            UInt16 r = (UInt16)((instruction >> 8) & 1);
            UInt16 registerList = (UInt16)(instruction & 0xff);

            cpu.Reg[SP] -= 4 * (r + (UInt32)BitOperations.PopCount(registerList));
            UInt32 address = cpu.Reg[SP];

            for (int i = 0; i <= 7; ++i)
            {
                if (((registerList >> i) & 1) == 1)
                {
                    cpu._callbackInterface.WriteMemory32(address, cpu.Reg[i]);
                    address += 4;
                }
            }

            if (r == 1)
                cpu._callbackInterface.WriteMemory32(address, cpu.Reg[LR]);
        }

        private static void THUMB_ROR(Core cpu, UInt16 instruction)
        {
            UInt16 rs = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            if ((cpu.Reg[rs] & 0xff) == 0)
            {
                // nothing to do
            }
            else if ((cpu.Reg[rs] & 0b1_1111) == 0)
            {
                cpu.SetFlag(Flags.C, cpu.Reg[rd] >> 31);
            }
            else
            {
                cpu.SetFlag(Flags.C, (cpu.Reg[rd] >> (int)((cpu.Reg[rs] & 0b1_1111) - 1)) & 1);
                cpu.Reg[rd] = BitOperations.RotateRight(cpu.Reg[rd], (int)(cpu.Reg[rs] & 0b1_1111));
            }

            cpu.SetFlag(Flags.N, cpu.Reg[rd] >> 31);
            cpu.SetFlag(Flags.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
        }

        private static void THUMB_SBC(Core cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 leftOperand = cpu.Reg[rd];
            UInt64 rightOperand = (UInt64)cpu.Reg[rm] + (UInt64)Not(cpu.GetFlag(Flags.C));

            cpu.Reg[rd] = leftOperand - (UInt32)rightOperand;

            cpu.SetFlag(Flags.N, cpu.Reg[rd] >> 31);
            cpu.SetFlag(Flags.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
            cpu.SetFlag(Flags.C, Not(BorrowFrom(leftOperand, rightOperand)));
            cpu.SetFlag(Flags.V, OverflowFrom_Subtraction(leftOperand, (UInt32)rightOperand, cpu.Reg[rd]));
        }

        private static void THUMB_STMIA(Core cpu, UInt16 instruction)
        {
            UInt16 rn = (UInt16)((instruction >> 8) & 0b111);
            UInt16 registerList = (UInt16)(instruction & 0xff);

            UInt32 address = cpu.Reg[rn];

            if (registerList == 0)
            {
                cpu.Reg[rn] += 0x40;
                cpu._callbackInterface.WriteMemory32(address, cpu.Reg[PC] + 2);
            }
            else
            {
                UInt32 oldRegRn = cpu.Reg[rn];
                cpu.Reg[rn] += (UInt32)BitOperations.PopCount(registerList) * 4;

                for (int i = 0; i <= 7; ++i)
                {
                    if (((registerList >> i) & 1) == 1)
                    {
                        if ((i == rn) && ((registerList & ~(0xff << i)) == 0))
                            cpu._callbackInterface.WriteMemory32(address, oldRegRn);
                        else
                            cpu._callbackInterface.WriteMemory32(address, cpu.Reg[i]);

                        address += 4;
                    }
                }
            }
        }

        private static void THUMB_STR1(Core cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)((instruction >> 6) & 0b1_1111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 address = cpu.Reg[rn] + (imm * 4u);
            cpu._callbackInterface.WriteMemory32(address, cpu.Reg[rd]);
        }

        private static void THUMB_STR2(Core cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 6) & 0b111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 address = cpu.Reg[rn] + cpu.Reg[rm];
            cpu._callbackInterface.WriteMemory32(address, cpu.Reg[rd]);
        }

        private static void THUMB_STR3(Core cpu, UInt16 instruction)
        {
            UInt16 rd = (UInt16)((instruction >> 8) & 0b111);
            UInt16 imm = (UInt16)(instruction & 0xff);

            UInt32 address = cpu.Reg[SP] + (imm * 4u);
            cpu._callbackInterface.WriteMemory32(address, cpu.Reg[rd]);
        }

        private static void THUMB_STRB1(Core cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)((instruction >> 6) & 0b1_1111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 address = cpu.Reg[rn] + imm;
            cpu._callbackInterface.WriteMemory8(address, (Byte)cpu.Reg[rd]);
        }

        private static void THUMB_STRB2(Core cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 6) & 0b111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 address = cpu.Reg[rn] + cpu.Reg[rm];
            cpu._callbackInterface.WriteMemory8(address, (Byte)cpu.Reg[rd]);
        }

        private static void THUMB_STRH1(Core cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)((instruction >> 6) & 0b1_1111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 address = cpu.Reg[rn] + (imm * 2u);
            cpu._callbackInterface.WriteMemory16(address, (UInt16)cpu.Reg[rd]);
        }

        private static void THUMB_STRH2(Core cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 6) & 0b111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 address = cpu.Reg[rn] + cpu.Reg[rm];
            cpu._callbackInterface.WriteMemory16(address, (UInt16)cpu.Reg[rd]);
        }

        private static void THUMB_SUB1(Core cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)((instruction >> 6) & 0b111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 leftOperand = cpu.Reg[rn];
            UInt32 rightOperand = imm;

            cpu.Reg[rd] = leftOperand - rightOperand;

            cpu.SetFlag(Flags.N, cpu.Reg[rd] >> 31);
            cpu.SetFlag(Flags.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
            cpu.SetFlag(Flags.C, Not(BorrowFrom(leftOperand, rightOperand)));
            cpu.SetFlag(Flags.V, OverflowFrom_Subtraction(leftOperand, rightOperand, cpu.Reg[rd]));
        }

        private static void THUMB_SUB2(Core cpu, UInt16 instruction)
        {
            UInt16 rd = (UInt16)((instruction >> 8) & 0b111);
            UInt16 imm = (UInt16)(instruction & 0xff);

            UInt32 leftOperand = cpu.Reg[rd];
            UInt32 rightOperand = imm;

            cpu.Reg[rd] = leftOperand - rightOperand;

            cpu.SetFlag(Flags.N, cpu.Reg[rd] >> 31);
            cpu.SetFlag(Flags.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
            cpu.SetFlag(Flags.C, Not(BorrowFrom(leftOperand, rightOperand)));
            cpu.SetFlag(Flags.V, OverflowFrom_Subtraction(leftOperand, rightOperand, cpu.Reg[rd]));
        }

        private static void THUMB_SUB3(Core cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 6) & 0b111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 leftOperand = cpu.Reg[rn];
            UInt32 rightOperand = cpu.Reg[rm];

            cpu.Reg[rd] = leftOperand - rightOperand;

            cpu.SetFlag(Flags.N, cpu.Reg[rd] >> 31);
            cpu.SetFlag(Flags.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
            cpu.SetFlag(Flags.C, Not(BorrowFrom(leftOperand, rightOperand)));
            cpu.SetFlag(Flags.V, OverflowFrom_Subtraction(leftOperand, rightOperand, cpu.Reg[rd]));
        }

        private static void THUMB_SUB4(Core cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)(instruction & 0x7f);

            cpu.Reg[SP] -= (UInt32)imm << 2;
        }

        private static void THUMB_SWI(Core cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)(instruction & 0xff);

            cpu._callbackInterface.HandleSWI((UInt32)(imm << 16));
        }

        private static void THUMB_TST(Core cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rn = (UInt16)(instruction & 0b111);

            UInt32 aluOut = cpu.Reg[rn] & cpu.Reg[rm];

            cpu.SetFlag(Flags.N, aluOut >> 31);
            cpu.SetFlag(Flags.Z, (aluOut == 0) ? 1u : 0u);
        }
    }
}
