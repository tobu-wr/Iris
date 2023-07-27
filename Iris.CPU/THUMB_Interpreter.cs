using System.Numerics;

namespace Iris.CPU
{
    internal sealed class THUMB_Interpreter
    {
        private readonly struct InstructionListEntry
        {
            internal readonly UInt16 Mask;
            internal readonly UInt16 Expected;
            internal unsafe readonly delegate*<CPU, UInt16, void> Handler;

            internal unsafe InstructionListEntry(UInt16 mask, UInt16 expected, delegate*<CPU, UInt16, void> handler)
            {
                Mask = mask;
                Expected = expected;
                Handler = handler;
            }
        }

        private unsafe readonly delegate*<CPU, UInt16, void>[] InstructionLUT = new delegate*<CPU, UInt16, void>[1 << 10];

        private static UInt16 InstructionLUTHash(UInt16 value)
        {
            return (UInt16)(value >> 6);
        }

        private unsafe void InitInstructionLUT()
        {
            InstructionListEntry[] InstructionList = new InstructionListEntry[]
            {
                // ADC
                new(0xffc0, 0x4140, &ADC),

                // ADD
                new(0xfe00, 0x1c00, &ADD1),
                new(0xf800, 0x3000, &ADD2),
                new(0xfe00, 0x1800, &ADD3),
                new(0xff00, 0x4400, &ADD4),
                new(0xf800, 0xa000, &ADD5),
                new(0xf800, 0xa800, &ADD6),
                new(0xff80, 0xb000, &ADD7),

                // AND
                new(0xffc0, 0x4000, &AND),

                // ASR
                new(0xf800, 0x1000, &ASR1),
                new(0xffc0, 0x4100, &ASR2),

                // B
                new(0xff00, 0xd000, &B1), // condition field 0b0000
                new(0xff00, 0xd100, &B1), // condition field 0b0001
                new(0xff00, 0xd200, &B1), // condition field 0b0010
                new(0xff00, 0xd300, &B1), // condition field 0b0011
                new(0xff00, 0xd400, &B1), // condition field 0b0100
                new(0xff00, 0xd500, &B1), // condition field 0b0101
                new(0xff00, 0xd600, &B1), // condition field 0b0110
                new(0xff00, 0xd700, &B1), // condition field 0b0111
                new(0xff00, 0xd800, &B1), // condition field 0b1000
                new(0xff00, 0xd900, &B1), // condition field 0b1001
                new(0xff00, 0xda00, &B1), // condition field 0b1010
                new(0xff00, 0xdb00, &B1), // condition field 0b1011
                new(0xff00, 0xdc00, &B1), // condition field 0b1100
                new(0xff00, 0xdd00, &B1), // condition field 0b1101
                new(0xf800, 0xe000, &B2),

                // BIC
                new(0xffc0, 0x4380, &BIC),

                // BL
                new(0xf000, 0xf000, &BL),

                // BX
                new(0xff80, 0x4700, &BX),

                // CMN
                new(0xffc0, 0x42c0, &CMN),

                // CMP
                new(0xf800, 0x2800, &CMP1),
                new(0xffc0, 0x4280, &CMP2),
                new(0xff00, 0x4500, &CMP3),

                // EOR
                new(0xffc0, 0x4040, &EOR),

                // LDMIA
                new(0xf800, 0xc800, &LDMIA),

                // LDR
                new(0xf800, 0x6800, &LDR1),
                new(0xfe00, 0x5800, &LDR2),
                new(0xf800, 0x4800, &LDR3),
                new(0xf800, 0x9800, &LDR4),

                // LDRB
                new(0xf800, 0x7800, &LDRB1),
                new(0xfe00, 0x5c00, &LDRB2),

                // LDRH
                new(0xf800, 0x8800, &LDRH1),
                new(0xfe00, 0x5a00, &LDRH2),

                // LDRSB
                new(0xfe00, 0x5600, &LDRSB),

                // LDRSH
                new(0xfe00, 0x5e00, &LDRSH),

                // LSL
                new(0xf800, 0x0000, &LSL1),
                new(0xffc0, 0x4080, &LSL2),

                // LSR
                new(0xf800, 0x0800, &LSR1),
                new(0xffc0, 0x40c0, &LSR2),

                // MOV
                new(0xf800, 0x2000, &MOV1),
                //new(0xffc0, 0x1c00, &MOV2),
                new(0xff00, 0x4600, &MOV3),

                // MUL
                new(0xffc0, 0x4340, &MUL),

                // MVN
                new(0xffc0, 0x43c0, &MVN),

                // NEG
                new(0xffc0, 0x4240, &NEG),

                // ORR
                new(0xffc0, 0x4300, &ORR),

                // POP
                new(0xfe00, 0xbc00, &POP),

                // PUSH
                new(0xfe00, 0xb400, &PUSH),

                // ROR
                new(0xffc0, 0x41c0, &ROR),

                // SBC
                new(0xffc0, 0x4180, &SBC),

                // STMIA
                new(0xf800, 0xc000, &STMIA),

                // STR
                new(0xf800, 0x6000, &STR1),
                new(0xfe00, 0x5000, &STR2),
                new(0xf800, 0x9000, &STR3),

                // STRB
                new(0xf800, 0x7000, &STRB1),
                new(0xfe00, 0x5400, &STRB2),

                // STRH
                new(0xf800, 0x8000, &STRH1),
                new(0xfe00, 0x5200, &STRH2),

                // SUB
                new(0xfe00, 0x1e00, &SUB1),
                new(0xf800, 0x3800, &SUB2),
                new(0xfe00, 0x1a00, &SUB3),
                new(0xff80, 0xb080, &SUB4),

                // SWI
                new(0xff00, 0xdf00, &SWI),

                // TST
                new(0xffc0, 0x4200, &TST),
            };

            for (UInt32 instruction = 0; instruction < InstructionLUT.Length; ++instruction)
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

        internal THUMB_Interpreter(CPU cpu)
        {
            _cpu = cpu;
            InitInstructionLUT();
        }

        internal void Step()
        {
            UInt16 instruction = _cpu._callbackInterface.ReadMemory16(_cpu.NextInstructionAddress);
            _cpu.NextInstructionAddress += 2;
            _cpu.Reg[CPU.PC] = _cpu.NextInstructionAddress + 2;

            unsafe
            {
                InstructionLUT[InstructionLUTHash(instruction)](_cpu, instruction);
            }
        }

        private static void SetPC(CPU cpu, UInt32 value)
        {
            cpu.NextInstructionAddress = value & 0xffff_fffe;
        }

        private static void SetReg(CPU cpu, UInt32 i, UInt32 value)
        {
            if (i == CPU.PC)
                SetPC(cpu, value);
            else
                cpu.Reg[i] = value;
        }

        private static void UNKNOWN(CPU cpu, UInt16 instruction)
        {
            throw new Exception(string.Format("Iris.CPU.THUMB_Interpreter: Unknown THUMB instruction 0x{0:x4} at address 0x{1:x8}", instruction, cpu.NextInstructionAddress - 2));
        }

        private static void ADC(CPU cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 leftOperand = cpu.Reg[rd];
            UInt64 rightOperand = (UInt64)cpu.Reg[rm] + (UInt64)cpu.GetFlag(CPU.Flag.C);

            UInt64 result = (UInt64)leftOperand + rightOperand;
            cpu.Reg[rd] = (UInt32)result;

            cpu.SetFlag(CPU.Flag.N, cpu.Reg[rd] >> 31);
            cpu.SetFlag(CPU.Flag.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
            cpu.SetFlag(CPU.Flag.C, CPU.CarryFrom(result));
            cpu.SetFlag(CPU.Flag.V, CPU.OverflowFrom_Addition(leftOperand, (UInt32)rightOperand, cpu.Reg[rd]));
        }

        private static void ADD1(CPU cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)((instruction >> 6) & 0b111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 leftOperand = cpu.Reg[rn];
            UInt32 rightOperand = imm;

            UInt64 result = (UInt64)leftOperand + (UInt64)rightOperand;
            cpu.Reg[rd] = (UInt32)result;

            cpu.SetFlag(CPU.Flag.N, cpu.Reg[rd] >> 31);
            cpu.SetFlag(CPU.Flag.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
            cpu.SetFlag(CPU.Flag.C, CPU.CarryFrom(result));
            cpu.SetFlag(CPU.Flag.V, CPU.OverflowFrom_Addition(leftOperand, rightOperand, cpu.Reg[rd]));
        }

        private static void ADD2(CPU cpu, UInt16 instruction)
        {
            UInt16 rd = (UInt16)((instruction >> 8) & 0b111);
            UInt16 imm = (UInt16)(instruction & 0xff);

            UInt32 leftOperand = cpu.Reg[rd];
            UInt32 rightOperand = imm;

            UInt64 result = (UInt64)leftOperand + (UInt64)rightOperand;
            cpu.Reg[rd] = (UInt32)result;

            cpu.SetFlag(CPU.Flag.N, cpu.Reg[rd] >> 31);
            cpu.SetFlag(CPU.Flag.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
            cpu.SetFlag(CPU.Flag.C, CPU.CarryFrom(result));
            cpu.SetFlag(CPU.Flag.V, CPU.OverflowFrom_Addition(leftOperand, rightOperand, cpu.Reg[rd]));
        }

        private static void ADD3(CPU cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 6) & 0b111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 leftOperand = cpu.Reg[rn];
            UInt32 rightOperand = cpu.Reg[rm];

            UInt64 result = (UInt64)leftOperand + (UInt64)rightOperand;
            cpu.Reg[rd] = (UInt32)result;

            cpu.SetFlag(CPU.Flag.N, cpu.Reg[rd] >> 31);
            cpu.SetFlag(CPU.Flag.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
            cpu.SetFlag(CPU.Flag.C, CPU.CarryFrom(result));
            cpu.SetFlag(CPU.Flag.V, CPU.OverflowFrom_Addition(leftOperand, rightOperand, cpu.Reg[rd]));
        }

        private static void ADD4(CPU cpu, UInt16 instruction)
        {
            UInt16 h1 = (UInt16)((instruction >> 7) & 1);
            UInt16 h2 = (UInt16)((instruction >> 6) & 1);
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            rd |= (UInt16)(h1 << 3);
            rm |= (UInt16)(h2 << 3);

            SetReg(cpu, rd, cpu.Reg[rd] + cpu.Reg[rm]);
        }

        private static void ADD5(CPU cpu, UInt16 instruction)
        {
            UInt16 rd = (UInt16)((instruction >> 8) & 0b111);
            UInt16 imm = (UInt16)(instruction & 0xff);

            cpu.Reg[rd] = (cpu.Reg[CPU.PC] & 0xffff_fffc) + (imm * 4u);
        }

        private static void ADD6(CPU cpu, UInt16 instruction)
        {
            UInt16 rd = (UInt16)((instruction >> 8) & 0b111);
            UInt16 imm = (UInt16)(instruction & 0xff);

            cpu.Reg[rd] = cpu.Reg[CPU.SP] + (imm * 4u);
        }

        private static void ADD7(CPU cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)(instruction & 0x7f);

            cpu.Reg[CPU.SP] += imm * 4u;
        }

        private static void AND(CPU cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            cpu.Reg[rd] &= cpu.Reg[rm];

            cpu.SetFlag(CPU.Flag.N, cpu.Reg[rd] >> 31);
            cpu.SetFlag(CPU.Flag.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
        }

        private static void ASR1(CPU cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)((instruction >> 6) & 0b1_1111);
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            int shiftAmount = imm;

            if (shiftAmount == 0)
            {
                cpu.SetFlag(CPU.Flag.C, cpu.Reg[rm] >> 31);
                cpu.Reg[rd] = ((cpu.Reg[rm] >> 31) == 0) ? 0 : 0xffff_ffff;
            }
            else
            {
                cpu.SetFlag(CPU.Flag.C, (cpu.Reg[rm] >> (shiftAmount - 1)) & 1);
                cpu.Reg[rd] = CPU.ArithmeticShiftRight(cpu.Reg[rm], shiftAmount);
            }

            cpu.SetFlag(CPU.Flag.N, cpu.Reg[rd] >> 31);
            cpu.SetFlag(CPU.Flag.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
        }

        private static void ASR2(CPU cpu, UInt16 instruction)
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
                cpu.SetFlag(CPU.Flag.C, (cpu.Reg[rd] >> (shiftAmount - 1)) & 1);
                cpu.Reg[rd] = CPU.ArithmeticShiftRight(cpu.Reg[rd], shiftAmount);
            }
            else
            {
                cpu.SetFlag(CPU.Flag.C, cpu.Reg[rd] >> 31);
                cpu.Reg[rd] = ((cpu.Reg[rd] >> 31) == 0) ? 0 : 0xffff_ffff;
            }

            cpu.SetFlag(CPU.Flag.N, cpu.Reg[rd] >> 31);
            cpu.SetFlag(CPU.Flag.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
        }

        private static void B1(CPU cpu, UInt16 instruction)
        {
            UInt16 cond = (UInt16)((instruction >> 8) & 0b1111);
            UInt16 imm = (UInt16)(instruction & 0xff);

            if (cpu.ConditionPassed(cond))
                SetPC(cpu, cpu.Reg[CPU.PC] + (CPU.SignExtend(imm, 8) << 1));
        }

        private static void B2(CPU cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)(instruction & 0x7ff);

            SetPC(cpu, cpu.Reg[CPU.PC] + (CPU.SignExtend(imm, 11) << 1));
        }

        private static void BIC(CPU cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            cpu.Reg[rd] &= ~cpu.Reg[rm];

            cpu.SetFlag(CPU.Flag.N, cpu.Reg[rd] >> 31);
            cpu.SetFlag(CPU.Flag.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
        }

        private static void BL(CPU cpu, UInt16 instruction)
        {
            UInt16 h = (UInt16)((instruction >> 11) & 0b11);
            UInt16 offset = (UInt16)(instruction & 0x7ff);

            if (h == 0b10)
            {
                cpu.Reg[CPU.LR] = cpu.Reg[CPU.PC] + (CPU.SignExtend(offset, 11) << 12);
            }
            else if (h == 0b11)
            {
                // save NextInstructionAddress because it's invalidated by SetPC
                UInt32 nextInstructionAddress = cpu.NextInstructionAddress;

                SetPC(cpu, cpu.Reg[CPU.LR] + (UInt32)(offset << 1));
                cpu.Reg[CPU.LR] = nextInstructionAddress | 1;
            }
        }

        private static void BX(CPU cpu, UInt16 instruction)
        {
            UInt16 h2 = (UInt16)((instruction >> 6) & 1);
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);

            rm |= (UInt16)(h2 << 3);

            cpu.CPSR = (cpu.CPSR & ~(1u << 5)) | ((cpu.Reg[rm] & 1) << 5);
            SetPC(cpu, cpu.Reg[rm]);
        }

        private static void CMN(CPU cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rn = (UInt16)(instruction & 0b111);

            UInt32 leftOperand = cpu.Reg[rn];
            UInt32 rightOperand = cpu.Reg[rm];

            UInt64 result = (UInt64)leftOperand + (UInt64)rightOperand;
            UInt32 aluOut = (UInt32)result;

            cpu.SetFlag(CPU.Flag.N, aluOut >> 31);
            cpu.SetFlag(CPU.Flag.Z, (aluOut == 0) ? 1u : 0u);
            cpu.SetFlag(CPU.Flag.C, CPU.CarryFrom(result));
            cpu.SetFlag(CPU.Flag.V, CPU.OverflowFrom_Addition(leftOperand, rightOperand, aluOut));
        }

        private static void CMP1(CPU cpu, UInt16 instruction)
        {
            UInt16 rn = (UInt16)((instruction >> 8) & 0b111);
            UInt16 imm = (UInt16)(instruction & 0xff);

            UInt32 leftOperand = cpu.Reg[rn];
            UInt32 rightOperand = imm;

            UInt32 aluOut = leftOperand - rightOperand;

            cpu.SetFlag(CPU.Flag.N, aluOut >> 31);
            cpu.SetFlag(CPU.Flag.Z, (aluOut == 0) ? 1u : 0u);
            cpu.SetFlag(CPU.Flag.C, CPU.Not(CPU.BorrowFrom(leftOperand, rightOperand)));
            cpu.SetFlag(CPU.Flag.V, CPU.OverflowFrom_Subtraction(leftOperand, rightOperand, aluOut));
        }

        private static void CMP2(CPU cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rn = (UInt16)(instruction & 0b111);

            UInt32 leftOperand = cpu.Reg[rn];
            UInt32 rightOperand = cpu.Reg[rm];

            UInt32 aluOut = leftOperand - rightOperand;

            cpu.SetFlag(CPU.Flag.N, aluOut >> 31);
            cpu.SetFlag(CPU.Flag.Z, (aluOut == 0) ? 1u : 0u);
            cpu.SetFlag(CPU.Flag.C, CPU.Not(CPU.BorrowFrom(leftOperand, rightOperand)));
            cpu.SetFlag(CPU.Flag.V, CPU.OverflowFrom_Subtraction(leftOperand, rightOperand, aluOut));
        }

        private static void CMP3(CPU cpu, UInt16 instruction)
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

            cpu.SetFlag(CPU.Flag.N, aluOut >> 31);
            cpu.SetFlag(CPU.Flag.Z, (aluOut == 0) ? 1u : 0u);
            cpu.SetFlag(CPU.Flag.C, CPU.Not(CPU.BorrowFrom(leftOperand, rightOperand)));
            cpu.SetFlag(CPU.Flag.V, CPU.OverflowFrom_Subtraction(leftOperand, rightOperand, aluOut));
        }

        private static void EOR(CPU cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            cpu.Reg[rd] ^= cpu.Reg[rm];

            cpu.SetFlag(CPU.Flag.N, cpu.Reg[rd] >> 31);
            cpu.SetFlag(CPU.Flag.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
        }

        private static void LDMIA(CPU cpu, UInt16 instruction)
        {
            UInt16 rn = (UInt16)((instruction >> 8) & 0b111);
            UInt16 registerList = (UInt16)(instruction & 0xff);

            UInt32 address = cpu.Reg[rn];

            if (registerList == 0)
            {
                cpu.Reg[rn] += 0x40;
                SetPC(cpu, cpu._callbackInterface.ReadMemory32(address));
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

        private static void LDR1(CPU cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)((instruction >> 6) & 0b1_1111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 address = cpu.Reg[rn] + (imm * 4u);
            UInt32 data = BitOperations.RotateRight(cpu._callbackInterface.ReadMemory32(address), (int)(8 * (address & 0b11)));
            cpu.Reg[rd] = data;
        }

        private static void LDR2(CPU cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 6) & 0b111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 address = cpu.Reg[rn] + cpu.Reg[rm];
            UInt32 data = BitOperations.RotateRight(cpu._callbackInterface.ReadMemory32(address), (int)(8 * (address & 0b11)));
            cpu.Reg[rd] = data;
        }

        private static void LDR3(CPU cpu, UInt16 instruction)
        {
            UInt16 rd = (UInt16)((instruction >> 8) & 0b111);
            UInt16 imm = (UInt16)(instruction & 0xff);

            UInt32 address = cpu.Reg[CPU.PC] + (imm * 4u);
            UInt32 data = cpu._callbackInterface.ReadMemory32(address);
            cpu.Reg[rd] = data;
        }

        private static void LDR4(CPU cpu, UInt16 instruction)
        {
            UInt16 rd = (UInt16)((instruction >> 8) & 0b111);
            UInt16 imm = (UInt16)(instruction & 0xff);

            UInt32 address = cpu.Reg[CPU.SP] + (imm * 4u);
            UInt32 data = BitOperations.RotateRight(cpu._callbackInterface.ReadMemory32(address), (int)(8 * (address & 0b11)));
            cpu.Reg[rd] = data;
        }

        private static void LDRB1(CPU cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)((instruction >> 6) & 0b1_1111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 address = cpu.Reg[rn] + imm;
            Byte data = cpu._callbackInterface.ReadMemory8(address);
            cpu.Reg[rd] = data;
        }

        private static void LDRB2(CPU cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 6) & 0b111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 address = cpu.Reg[rn] + cpu.Reg[rm];
            Byte data = cpu._callbackInterface.ReadMemory8(address);
            cpu.Reg[rd] = data;
        }

        private static void LDRH1(CPU cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)((instruction >> 6) & 0b1_1111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 address = cpu.Reg[rn] + (imm * 2u);
            UInt32 data = BitOperations.RotateRight(cpu._callbackInterface.ReadMemory16(address), (int)(8 * (address & 1)));
            cpu.Reg[rd] = data;
        }

        private static void LDRH2(CPU cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 6) & 0b111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 address = cpu.Reg[rn] + cpu.Reg[rm];
            UInt32 data = BitOperations.RotateRight(cpu._callbackInterface.ReadMemory16(address), (int)(8 * (address & 1)));
            cpu.Reg[rd] = data;
        }

        private static void LDRSB(CPU cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 6) & 0b111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 address = cpu.Reg[rn] + cpu.Reg[rm];
            Byte data = cpu._callbackInterface.ReadMemory8(address);
            cpu.Reg[rd] = CPU.SignExtend(data, 8);
        }

        private static void LDRSH(CPU cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 6) & 0b111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 address = cpu.Reg[rn] + cpu.Reg[rm];

            if ((address & 1) == 1)
            {
                Byte data = cpu._callbackInterface.ReadMemory8(address);
                cpu.Reg[rd] = CPU.SignExtend(data, 8);
            }
            else
            {
                UInt16 data = cpu._callbackInterface.ReadMemory16(address);
                cpu.Reg[rd] = CPU.SignExtend(data, 16);
            }
        }

        private static void LSL1(CPU cpu, UInt16 instruction)
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
                cpu.SetFlag(CPU.Flag.C, (cpu.Reg[rm] >> (32 - shiftAmount)) & 1);
                cpu.Reg[rd] = cpu.Reg[rm] << shiftAmount;
            }

            cpu.SetFlag(CPU.Flag.N, cpu.Reg[rd] >> 31);
            cpu.SetFlag(CPU.Flag.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
        }

        private static void LSL2(CPU cpu, UInt16 instruction)
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
                cpu.SetFlag(CPU.Flag.C, (cpu.Reg[rd] >> (32 - shiftAmount)) & 1);
                cpu.Reg[rd] = cpu.Reg[rd] << shiftAmount;
            }
            else if (shiftAmount == 32)
            {
                cpu.SetFlag(CPU.Flag.C, cpu.Reg[rd] & 1);
                cpu.Reg[rd] = 0;
            }
            else
            {
                cpu.SetFlag(CPU.Flag.C, 0);
                cpu.Reg[rd] = 0;
            }

            cpu.SetFlag(CPU.Flag.N, cpu.Reg[rd] >> 31);
            cpu.SetFlag(CPU.Flag.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
        }

        private static void LSR1(CPU cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)((instruction >> 6) & 0b1_1111);
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            int shiftAmount = imm;

            if (shiftAmount == 0)
            {
                cpu.SetFlag(CPU.Flag.C, cpu.Reg[rm] >> 31);
                cpu.Reg[rd] = 0;
            }
            else
            {
                cpu.SetFlag(CPU.Flag.C, (cpu.Reg[rm] >> (shiftAmount - 1)) & 1);
                cpu.Reg[rd] = cpu.Reg[rm] >> shiftAmount;
            }

            cpu.SetFlag(CPU.Flag.N, cpu.Reg[rd] >> 31);
            cpu.SetFlag(CPU.Flag.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
        }

        private static void LSR2(CPU cpu, UInt16 instruction)
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
                cpu.SetFlag(CPU.Flag.C, (cpu.Reg[rd] >> (shiftAmount - 1)) & 1);
                cpu.Reg[rd] = cpu.Reg[rd] >> shiftAmount;
            }
            else if (shiftAmount == 32)
            {
                cpu.SetFlag(CPU.Flag.C, cpu.Reg[rd] >> 31);
                cpu.Reg[rd] = 0;
            }
            else
            {
                cpu.SetFlag(CPU.Flag.C, 0);
                cpu.Reg[rd] = 0;
            }

            cpu.SetFlag(CPU.Flag.N, cpu.Reg[rd] >> 31);
            cpu.SetFlag(CPU.Flag.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
        }

        private static void MOV1(CPU cpu, UInt16 instruction)
        {
            UInt16 rd = (UInt16)((instruction >> 8) & 0b111);
            UInt16 imm = (UInt16)(instruction & 0xff);

            cpu.Reg[rd] = imm;

            cpu.SetFlag(CPU.Flag.N, cpu.Reg[rd] >> 31);
            cpu.SetFlag(CPU.Flag.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
        }

        private static void MOV3(CPU cpu, UInt16 instruction)
        {
            UInt16 h1 = (UInt16)((instruction >> 7) & 1);
            UInt16 h2 = (UInt16)((instruction >> 6) & 1);
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            rd |= (UInt16)(h1 << 3);
            rm |= (UInt16)(h2 << 3);

            SetReg(cpu, rd, cpu.Reg[rm]);
        }

        private static void MUL(CPU cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            cpu.Reg[rd] *= cpu.Reg[rm];

            cpu.SetFlag(CPU.Flag.N, cpu.Reg[rd] >> 31);
            cpu.SetFlag(CPU.Flag.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
        }

        private static void MVN(CPU cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            cpu.Reg[rd] = ~cpu.Reg[rm];

            cpu.SetFlag(CPU.Flag.N, cpu.Reg[rd] >> 31);
            cpu.SetFlag(CPU.Flag.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
        }

        private static void NEG(CPU cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 leftOperand = 0;
            UInt32 rightOperand = cpu.Reg[rm];

            cpu.Reg[rd] = leftOperand - rightOperand;

            cpu.SetFlag(CPU.Flag.N, cpu.Reg[rd] >> 31);
            cpu.SetFlag(CPU.Flag.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
            cpu.SetFlag(CPU.Flag.C, CPU.Not(CPU.BorrowFrom(leftOperand, rightOperand)));
            cpu.SetFlag(CPU.Flag.V, CPU.OverflowFrom_Subtraction(leftOperand, rightOperand, cpu.Reg[rd]));
        }

        private static void ORR(CPU cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            cpu.Reg[rd] |= cpu.Reg[rm];

            cpu.SetFlag(CPU.Flag.N, cpu.Reg[rd] >> 31);
            cpu.SetFlag(CPU.Flag.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
        }

        private static void POP(CPU cpu, UInt16 instruction)
        {
            UInt16 r = (UInt16)((instruction >> 8) & 1);
            UInt16 registerList = (UInt16)(instruction & 0xff);

            UInt32 address = cpu.Reg[CPU.SP];
            cpu.Reg[CPU.SP] += 4 * (r + (UInt32)BitOperations.PopCount(registerList));

            for (int i = 0; i <= 7; ++i)
            {
                if (((registerList >> i) & 1) == 1)
                {
                    cpu.Reg[i] = cpu._callbackInterface.ReadMemory32(address);
                    address += 4;
                }
            }

            if (r == 1)
                SetPC(cpu, cpu._callbackInterface.ReadMemory32(address));
        }

        private static void PUSH(CPU cpu, UInt16 instruction)
        {
            UInt16 r = (UInt16)((instruction >> 8) & 1);
            UInt16 registerList = (UInt16)(instruction & 0xff);

            cpu.Reg[CPU.SP] -= 4 * (r + (UInt32)BitOperations.PopCount(registerList));
            UInt32 address = cpu.Reg[CPU.SP];

            for (int i = 0; i <= 7; ++i)
            {
                if (((registerList >> i) & 1) == 1)
                {
                    cpu._callbackInterface.WriteMemory32(address, cpu.Reg[i]);
                    address += 4;
                }
            }

            if (r == 1)
                cpu._callbackInterface.WriteMemory32(address, cpu.Reg[CPU.LR]);
        }

        private static void ROR(CPU cpu, UInt16 instruction)
        {
            UInt16 rs = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            if ((cpu.Reg[rs] & 0xff) == 0)
            {
                // nothing to do
            }
            else if ((cpu.Reg[rs] & 0b1_1111) == 0)
            {
                cpu.SetFlag(CPU.Flag.C, cpu.Reg[rd] >> 31);
            }
            else
            {
                cpu.SetFlag(CPU.Flag.C, (cpu.Reg[rd] >> (int)((cpu.Reg[rs] & 0b1_1111) - 1)) & 1);
                cpu.Reg[rd] = BitOperations.RotateRight(cpu.Reg[rd], (int)(cpu.Reg[rs] & 0b1_1111));
            }

            cpu.SetFlag(CPU.Flag.N, cpu.Reg[rd] >> 31);
            cpu.SetFlag(CPU.Flag.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
        }

        private static void SBC(CPU cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 leftOperand = cpu.Reg[rd];
            UInt64 rightOperand = (UInt64)cpu.Reg[rm] + (UInt64)CPU.Not(cpu.GetFlag(CPU.Flag.C));

            cpu.Reg[rd] = leftOperand - (UInt32)rightOperand;

            cpu.SetFlag(CPU.Flag.N, cpu.Reg[rd] >> 31);
            cpu.SetFlag(CPU.Flag.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
            cpu.SetFlag(CPU.Flag.C, CPU.Not(CPU.BorrowFrom(leftOperand, rightOperand)));
            cpu.SetFlag(CPU.Flag.V, CPU.OverflowFrom_Subtraction(leftOperand, (UInt32)rightOperand, cpu.Reg[rd]));
        }

        private static void STMIA(CPU cpu, UInt16 instruction)
        {
            UInt16 rn = (UInt16)((instruction >> 8) & 0b111);
            UInt16 registerList = (UInt16)(instruction & 0xff);

            UInt32 address = cpu.Reg[rn];

            if (registerList == 0)
            {
                cpu.Reg[rn] += 0x40;
                cpu._callbackInterface.WriteMemory32(address, cpu.Reg[CPU.PC] + 2);
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

        private static void STR1(CPU cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)((instruction >> 6) & 0b1_1111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 address = cpu.Reg[rn] + (imm * 4u);
            cpu._callbackInterface.WriteMemory32(address, cpu.Reg[rd]);
        }

        private static void STR2(CPU cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 6) & 0b111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 address = cpu.Reg[rn] + cpu.Reg[rm];
            cpu._callbackInterface.WriteMemory32(address, cpu.Reg[rd]);
        }

        private static void STR3(CPU cpu, UInt16 instruction)
        {
            UInt16 rd = (UInt16)((instruction >> 8) & 0b111);
            UInt16 imm = (UInt16)(instruction & 0xff);

            UInt32 address = cpu.Reg[CPU.SP] + (imm * 4u);
            cpu._callbackInterface.WriteMemory32(address, cpu.Reg[rd]);
        }

        private static void STRB1(CPU cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)((instruction >> 6) & 0b1_1111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 address = cpu.Reg[rn] + imm;
            cpu._callbackInterface.WriteMemory8(address, (Byte)cpu.Reg[rd]);
        }

        private static void STRB2(CPU cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 6) & 0b111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 address = cpu.Reg[rn] + cpu.Reg[rm];
            cpu._callbackInterface.WriteMemory8(address, (Byte)cpu.Reg[rd]);
        }

        private static void STRH1(CPU cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)((instruction >> 6) & 0b1_1111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 address = cpu.Reg[rn] + (imm * 2u);
            cpu._callbackInterface.WriteMemory16(address, (UInt16)cpu.Reg[rd]);
        }

        private static void STRH2(CPU cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 6) & 0b111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 address = cpu.Reg[rn] + cpu.Reg[rm];
            cpu._callbackInterface.WriteMemory16(address, (UInt16)cpu.Reg[rd]);
        }

        private static void SUB1(CPU cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)((instruction >> 6) & 0b111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 leftOperand = cpu.Reg[rn];
            UInt32 rightOperand = imm;

            cpu.Reg[rd] = leftOperand - rightOperand;

            cpu.SetFlag(CPU.Flag.N, cpu.Reg[rd] >> 31);
            cpu.SetFlag(CPU.Flag.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
            cpu.SetFlag(CPU.Flag.C, CPU.Not(CPU.BorrowFrom(leftOperand, rightOperand)));
            cpu.SetFlag(CPU.Flag.V, CPU.OverflowFrom_Subtraction(leftOperand, rightOperand, cpu.Reg[rd]));
        }

        private static void SUB2(CPU cpu, UInt16 instruction)
        {
            UInt16 rd = (UInt16)((instruction >> 8) & 0b111);
            UInt16 imm = (UInt16)(instruction & 0xff);

            UInt32 leftOperand = cpu.Reg[rd];
            UInt32 rightOperand = imm;

            cpu.Reg[rd] = leftOperand - rightOperand;

            cpu.SetFlag(CPU.Flag.N, cpu.Reg[rd] >> 31);
            cpu.SetFlag(CPU.Flag.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
            cpu.SetFlag(CPU.Flag.C, CPU.Not(CPU.BorrowFrom(leftOperand, rightOperand)));
            cpu.SetFlag(CPU.Flag.V, CPU.OverflowFrom_Subtraction(leftOperand, rightOperand, cpu.Reg[rd]));
        }

        private static void SUB3(CPU cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 6) & 0b111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 leftOperand = cpu.Reg[rn];
            UInt32 rightOperand = cpu.Reg[rm];

            cpu.Reg[rd] = leftOperand - rightOperand;

            cpu.SetFlag(CPU.Flag.N, cpu.Reg[rd] >> 31);
            cpu.SetFlag(CPU.Flag.Z, (cpu.Reg[rd] == 0) ? 1u : 0u);
            cpu.SetFlag(CPU.Flag.C, CPU.Not(CPU.BorrowFrom(leftOperand, rightOperand)));
            cpu.SetFlag(CPU.Flag.V, CPU.OverflowFrom_Subtraction(leftOperand, rightOperand, cpu.Reg[rd]));
        }

        private static void SUB4(CPU cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)(instruction & 0x7f);

            cpu.Reg[CPU.SP] -= (UInt32)imm << 2;
        }

        private static void SWI(CPU cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)(instruction & 0xff);

            cpu._callbackInterface.HandleSWI((UInt32)(imm << 16));
        }

        private static void TST(CPU cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rn = (UInt16)(instruction & 0b111);

            UInt32 aluOut = cpu.Reg[rn] & cpu.Reg[rm];

            cpu.SetFlag(CPU.Flag.N, aluOut >> 31);
            cpu.SetFlag(CPU.Flag.Z, (aluOut == 0) ? 1u : 0u);
        }
    }
}
