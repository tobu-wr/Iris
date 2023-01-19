namespace Iris
{
    internal sealed partial class CPU
    {
        private delegate void THUMB_InstructionHandler(CPU cpu, UInt16 instruction);
        private readonly record struct THUMB_InstructionListEntry(UInt16 Mask, UInt16 Expected, THUMB_InstructionHandler Handler);

        private static readonly THUMB_InstructionListEntry[] THUMB_InstructionList = new THUMB_InstructionListEntry[]
        {
            // ADC
            new(0xffc0, 0x4140, THUMB_ADC),

            // ADD
            new(0xfe00, 0x1c00, THUMB_ADD1),
            new(0xf800, 0x3000, THUMB_ADD2),
            new(0xfe00, 0x1800, THUMB_ADD3),
            new(0xff00, 0x4400, THUMB_ADD4),
            new(0xf800, 0xa000, THUMB_ADD5),
            new(0xf800, 0xa800, THUMB_ADD6),
            new(0xff80, 0xb000, THUMB_ADD7),

            // AND
            new(0xffc0, 0x4000, THUMB_AND),

            // ASR
            new(0xf800, 0x1000, THUMB_ASR1),
            new(0xffc0, 0x4100, THUMB_ASR2),

            // B
            new(0xf000, 0xd000, THUMB_B1),
            new(0xf800, 0xe000, THUMB_B2),

            // BIC
            new(0xffc0, 0x4380, THUMB_BIC),

            // BL
            new(0xf000, 0xf000, THUMB_BL),

            // BX
            new(0xff87, 0x4700, THUMB_BX),

            // CMN
            new(0xffc0, 0x42c0, THUMB_CMN),

            // CMP
            new(0xf800, 0x2800, THUMB_CMP1),
            new(0xffc0, 0x4280, THUMB_CMP2),
            new(0xff00, 0x4500, THUMB_CMP3),

            // EOR
            new(0xffc0, 0x4040, THUMB_EOR),

            // LDMIA
            new(0xf800, 0xc800, THUMB_LDMIA),

            // LDR
            new(0xf800, 0x6800, THUMB_LDR1),
            new(0xfe00, 0x5800, THUMB_LDR2),
            new(0xf800, 0x4800, THUMB_LDR3),
            new(0xf800, 0x9800, THUMB_LDR4),

            // LDRB
            new(0xf800, 0x7800, THUMB_LDRB1),
            new(0xfe00, 0x5c00, THUMB_LDRB2),

            // LDRH
            new(0xf800, 0x8800, THUMB_LDRH1),
            new(0xfe00, 0x5a00, THUMB_LDRH2),

            // LDRSB
            new(0xfe00, 0x5600, THUMB_LDRSB),

            // LDRSH
            new(0xfe00, 0x5e00, THUMB_LDRSH),

            // LSL
            new(0xf800, 0x0000, THUMB_LSL1),
            new(0xffc0, 0x4080, THUMB_LSL2),

            // LSR
            new(0xf800, 0x0800, THUMB_LSR1),
            new(0xffc0, 0x40c0, THUMB_LSR2),

            // MOV
            new(0xf800, 0x2000, THUMB_MOV1),
            new(0xff00, 0x4600, THUMB_MOV3),

            // MUL
            new(0xffc0, 0x4340, THUMB_MUL),

            // MVN
            new(0xffc0, 0x43c0, THUMB_MVN),

            // NEG
            new(0xffc0, 0x4240, THUMB_NEG),

            // ORR
            new(0xffc0, 0x4300, THUMB_ORR),

            // POP
            new(0xfe00, 0xbc00, THUMB_POP),

            // PUSH
            new(0xfe00, 0xb400, THUMB_PUSH),

            // ROR
            new(0xffc0, 0x41c0, THUMB_ROR),

            // SBC
            new(0xffc0, 0x4180, THUMB_SBC),

            // STMIA
            new(0xf800, 0xc000, THUMB_STMIA),

            // STR
            new(0xf800, 0x6000, THUMB_STR1),
            new(0xfe00, 0x5000, THUMB_STR2),
            new(0xf800, 0x9000, THUMB_STR3),

            // STRB
            new(0xf800, 0x7000, THUMB_STRB1),
            new(0xfe00, 0x5400, THUMB_STRB2),

            // STRH
            new(0xf800, 0x8000, THUMB_STRH1),
            new(0xfe00, 0x5200, THUMB_STRH2),

            // SUB
            new(0xfe00, 0x1e00, THUMB_SUB1),
            new(0xf800, 0x3800, THUMB_SUB2),
            new(0xfe00, 0x1a00, THUMB_SUB3),
            new(0xff80, 0xb080, THUMB_SUB4),

            // TST
            new(0xffc0, 0x4200, THUMB_TST),
        };

        private void THUMB_Step()
        {
            UInt16 instruction = _callbacks.ReadMemory16(_nextInstructionAddress);
            _nextInstructionAddress += 2;
            _reg[PC] = _nextInstructionAddress + 2;

            foreach (THUMB_InstructionListEntry entry in THUMB_InstructionList)
            {
                if ((instruction & entry.Mask) == entry.Expected)
                {
                    entry.Handler(this, instruction);
                    return;
                }
            }

            throw new Exception(string.Format("CPU: Unknown THUMB instruction 0x{0:x4} at address 0x{1:x8}", instruction, _nextInstructionAddress - 2));
        }

        private void THUMB_SetPC(UInt32 value)
        {
            value &= 0xffff_fffe;
            _reg[PC] = value;
            _nextInstructionAddress = value;
        }

        private void THUMB_SetReg(UInt32 i, UInt32 value)
        {
            if (i == PC)
                THUMB_SetPC(value);
            else
                _reg[i] = value;
        }

        private static void THUMB_ADC(CPU cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 leftOperand = cpu._reg[rd];
            UInt64 rightOperand = (UInt64)cpu._reg[rm] + (UInt64)cpu.GetFlag(Flags.C);

            UInt64 result = (UInt64)leftOperand + rightOperand;
            cpu._reg[rd] = (UInt32)result;

            cpu.SetFlag(Flags.N, cpu._reg[rd] >> 31);
            cpu.SetFlag(Flags.Z, (cpu._reg[rd] == 0) ? 1u : 0u);
            cpu.SetFlag(Flags.C, CarryFrom(result));
            cpu.SetFlag(Flags.V, OverflowFrom_Addition(leftOperand, (UInt32)rightOperand, cpu._reg[rd]));
        }

        private static void THUMB_ADD1(CPU cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)((instruction >> 6) & 0b111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 leftOperand = cpu._reg[rn];
            UInt32 rightOperand = imm;

            UInt64 result = (UInt64)leftOperand + (UInt64)rightOperand;
            cpu._reg[rd] = (UInt32)result;

            cpu.SetFlag(Flags.N, cpu._reg[rd] >> 31);
            cpu.SetFlag(Flags.Z, (cpu._reg[rd] == 0) ? 1u : 0u);
            cpu.SetFlag(Flags.C, CarryFrom(result));
            cpu.SetFlag(Flags.V, OverflowFrom_Addition(leftOperand, rightOperand, cpu._reg[rd]));
        }

        private static void THUMB_ADD2(CPU cpu, UInt16 instruction)
        {
            UInt16 rd = (UInt16)((instruction >> 8) & 0b111);
            UInt16 imm = (UInt16)(instruction & 0xff);

            UInt32 leftOperand = cpu._reg[rd];
            UInt32 rightOperand = imm;

            UInt64 result = (UInt64)leftOperand + (UInt64)rightOperand;
            cpu._reg[rd] = (UInt32)result;

            cpu.SetFlag(Flags.N, cpu._reg[rd] >> 31);
            cpu.SetFlag(Flags.Z, (cpu._reg[rd] == 0) ? 1u : 0u);
            cpu.SetFlag(Flags.C, CarryFrom(result));
            cpu.SetFlag(Flags.V, OverflowFrom_Addition(leftOperand, rightOperand, cpu._reg[rd]));
        }

        private static void THUMB_ADD3(CPU cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 6) & 0b111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 leftOperand = cpu._reg[rn];
            UInt32 rightOperand = cpu._reg[rm];

            UInt64 result = (UInt64)leftOperand + (UInt64)rightOperand;
            cpu._reg[rd] = (UInt32)result;

            cpu.SetFlag(Flags.N, cpu._reg[rd] >> 31);
            cpu.SetFlag(Flags.Z, (cpu._reg[rd] == 0) ? 1u : 0u);
            cpu.SetFlag(Flags.C, CarryFrom(result));
            cpu.SetFlag(Flags.V, OverflowFrom_Addition(leftOperand, rightOperand, cpu._reg[rd]));
        }

        private static void THUMB_ADD4(CPU cpu, UInt16 instruction)
        {
            UInt16 h1 = (UInt16)((instruction >> 7) & 1);
            UInt16 h2 = (UInt16)((instruction >> 6) & 1);
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 hrd = (UInt32)((h1 << 3) | rd);
            UInt32 hrm = (UInt32)((h2 << 3) | rm);

            cpu.THUMB_SetReg(hrd, cpu._reg[hrd] + cpu._reg[hrm]);
        }

        private static void THUMB_ADD5(CPU cpu, UInt16 instruction)
        {
            UInt16 rd = (UInt16)((instruction >> 8) & 0b111);
            UInt16 imm = (UInt16)(instruction & 0xff);

            cpu._reg[rd] = (cpu._reg[PC] & 0xffff_fffc) + (imm * 4u);
        }

        private static void THUMB_ADD6(CPU cpu, UInt16 instruction)
        {
            UInt16 rd = (UInt16)((instruction >> 8) & 0b111);
            UInt16 imm = (UInt16)(instruction & 0xff);

            cpu._reg[rd] = cpu._reg[SP] + (imm * 4u);
        }

        private static void THUMB_ADD7(CPU cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)(instruction & 0x7f);

            cpu._reg[SP] += imm * 4u;
        }

        private static void THUMB_AND(CPU cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            cpu._reg[rd] &= cpu._reg[rm];

            cpu.SetFlag(Flags.N, cpu._reg[rd] >> 31);
            cpu.SetFlag(Flags.Z, (cpu._reg[rd] == 0) ? 1u : 0u);
        }

        private static void THUMB_ASR1(CPU cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)((instruction >> 6) & 0b1_1111);
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            if (imm == 0)
            {
                cpu.SetFlag(Flags.C, cpu._reg[rm] >> 31);

                if ((cpu._reg[rm] >> 31) == 0)
                    cpu._reg[rd] = 0;
                else
                    cpu._reg[rd] = 0xffff_ffff;
            }
            else
            {
                cpu.SetFlag(Flags.C, (cpu._reg[rm] >> (imm - 1)) & 1);
                cpu._reg[rd] = ArithmeticShiftRight(cpu._reg[rm], imm);
            }

            cpu.SetFlag(Flags.N, cpu._reg[rd] >> 31);
            cpu.SetFlag(Flags.Z, (cpu._reg[rd] == 0) ? 1u : 0u);
        }

        private static void THUMB_ASR2(CPU cpu, UInt16 instruction)
        {
            UInt16 rs = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 regRs = cpu._reg[rs] & 0xff;

            if (regRs == 0)
            {
                // nothing to do
            }
            else if (regRs < 32)
            {
                cpu.SetFlag(Flags.C, (cpu._reg[rd] >> ((int)regRs - 1)) & 1);
                cpu._reg[rd] = ArithmeticShiftRight(cpu._reg[rd], regRs);
            }
            else
            {
                cpu.SetFlag(Flags.C, cpu._reg[rd] >> 31);

                if ((cpu._reg[rd] >> 31) == 0)
                    cpu._reg[rd] = 0;
                else
                    cpu._reg[rd] = 0xffff_ffff;
            }

            cpu.SetFlag(Flags.N, cpu._reg[rd] >> 31);
            cpu.SetFlag(Flags.Z, (cpu._reg[rd] == 0) ? 1u : 0u);
        }

        private static void THUMB_B1(CPU cpu, UInt16 instruction)
        {
            UInt16 cond = (UInt16)((instruction >> 8) & 0b1111);
            UInt16 imm = (UInt16)(instruction & 0xff);

            if (cpu.ConditionPassed(cond))
                cpu.THUMB_SetPC(cpu._reg[PC] + (SignExtend(imm, 8) << 1));
        }

        private static void THUMB_B2(CPU cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)(instruction & 0x7ff);

            cpu.THUMB_SetPC(cpu._reg[PC] + (SignExtend(imm, 11) << 1));
        }

        private static void THUMB_BIC(CPU cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            cpu._reg[rd] &= ~cpu._reg[rm];

            cpu.SetFlag(Flags.N, cpu._reg[rd] >> 31);
            cpu.SetFlag(Flags.Z, (cpu._reg[rd] == 0) ? 1u : 0u);
        }

        private static void THUMB_BL(CPU cpu, UInt16 instruction)
        {
            UInt16 h = (UInt16)((instruction >> 11) & 0b11);
            UInt16 offset = (UInt16)(instruction & 0x7ff);

            if (h == 0b10)
            {
                cpu._reg[LR] = cpu._reg[PC] + (SignExtend(offset, 11) << 12);
            }
            else if (h == 0b11)
            {
                // save _nextInstructionAddress because it's "invalidated" by THUMB_SetPC
                UInt32 nextInstructionAddress = cpu._nextInstructionAddress;

                cpu.THUMB_SetPC(cpu._reg[LR] + ((UInt32)offset << 1));
                cpu._reg[LR] = nextInstructionAddress | 1;
            }
        }

        private static void THUMB_BX(CPU cpu, UInt16 instruction)
        {
            UInt16 h2 = (UInt16)((instruction >> 6) & 1);
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);

            UInt32 regRm = cpu._reg[(h2 << 3) | rm];

            cpu.SetCPSR((cpu._cpsr & ~(1u << 5)) | ((regRm & 1) << 5));
            cpu.THUMB_SetPC(regRm & 0xffff_fffe);
        }

        private static void THUMB_CMN(CPU cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rn = (UInt16)(instruction & 0b111);

            UInt32 leftOperand = cpu._reg[rn];
            UInt32 rightOperand = cpu._reg[rm];

            UInt64 result = (UInt64)leftOperand + (UInt64)rightOperand;
            UInt32 aluOut = (UInt32)result;

            cpu.SetFlag(Flags.N, aluOut >> 31);
            cpu.SetFlag(Flags.Z, (aluOut == 0) ? 1u : 0u);
            cpu.SetFlag(Flags.C, CarryFrom(result));
            cpu.SetFlag(Flags.V, OverflowFrom_Addition(leftOperand, rightOperand, aluOut));
        }

        private static void THUMB_CMP1(CPU cpu, UInt16 instruction)
        {
            UInt16 rn = (UInt16)((instruction >> 8) & 0b111);
            UInt16 imm = (UInt16)(instruction & 0xff);

            UInt32 leftOperand = cpu._reg[rn];
            UInt32 rightOperand = imm;

            UInt32 aluOut = leftOperand - rightOperand;

            cpu.SetFlag(Flags.N, aluOut >> 31);
            cpu.SetFlag(Flags.Z, (aluOut == 0) ? 1u : 0u);
            cpu.SetFlag(Flags.C, Not(BorrowFrom(leftOperand, rightOperand)));
            cpu.SetFlag(Flags.V, OverflowFrom_Subtraction(leftOperand, rightOperand, aluOut));
        }

        private static void THUMB_CMP2(CPU cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rn = (UInt16)(instruction & 0b111);

            UInt32 leftOperand = cpu._reg[rn];
            UInt32 rightOperand = cpu._reg[rm];

            UInt32 aluOut = leftOperand - rightOperand;

            cpu.SetFlag(Flags.N, aluOut >> 31);
            cpu.SetFlag(Flags.Z, (aluOut == 0) ? 1u : 0u);
            cpu.SetFlag(Flags.C, Not(BorrowFrom(leftOperand, rightOperand)));
            cpu.SetFlag(Flags.V, OverflowFrom_Subtraction(leftOperand, rightOperand, aluOut));
        }

        private static void THUMB_CMP3(CPU cpu, UInt16 instruction)
        {
            UInt16 h1 = (UInt16)((instruction >> 7) & 1);
            UInt16 h2 = (UInt16)((instruction >> 6) & 1);
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rn = (UInt16)(instruction & 0b111);

            UInt32 leftOperand = cpu._reg[(h1 << 3) | rn];
            UInt32 rightOperand = cpu._reg[(h2 << 3) | rm];

            UInt32 aluOut = leftOperand - rightOperand;

            cpu.SetFlag(Flags.N, aluOut >> 31);
            cpu.SetFlag(Flags.Z, (aluOut == 0) ? 1u : 0u);
            cpu.SetFlag(Flags.C, Not(BorrowFrom(leftOperand, rightOperand)));
            cpu.SetFlag(Flags.V, OverflowFrom_Subtraction(leftOperand, rightOperand, aluOut));
        }

        private static void THUMB_EOR(CPU cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            cpu._reg[rd] ^= cpu._reg[rm];

            cpu.SetFlag(Flags.N, cpu._reg[rd] >> 31);
            cpu.SetFlag(Flags.Z, (cpu._reg[rd] == 0) ? 1u : 0u);
        }

        private static void THUMB_LDMIA(CPU cpu, UInt16 instruction)
        {
            UInt16 rn = (UInt16)((instruction >> 8) & 0b111);
            UInt16 registerList = (UInt16)(instruction & 0xff);

            UInt32 startAddress = cpu._reg[rn];
            UInt32 address = startAddress;

            if (registerList != 0)
            {
                cpu._reg[rn] += NumberOfSetBitsIn(registerList, 8) * 4;

                for (var i = 0; i <= 7; ++i)
                {
                    if (((registerList >> i) & 1) == 1)
                    {
                        cpu._reg[i] = cpu._callbacks.ReadMemory32(address);
                        address += 4;
                    }
                }
            }
            else
            {
                cpu.THUMB_SetPC(cpu._callbacks.ReadMemory32(address));
                cpu._reg[rn] += 0x40;
            }
        }

        private static void THUMB_LDR1(CPU cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)((instruction >> 6) & 0b1_1111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 address = cpu._reg[rn] + (imm * 4u);
            UInt32 data = cpu._callbacks.ReadMemory32(address);
            cpu._reg[rd] = RotateRight(data, 8 * (address & 0b11));
        }

        private static void THUMB_LDR2(CPU cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 6) & 0b111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 address = cpu._reg[rn] + cpu._reg[rm];
            UInt32 data = cpu._callbacks.ReadMemory32(address);
            cpu._reg[rd] = RotateRight(data, 8 * (address & 0b11));
        }

        private static void THUMB_LDR3(CPU cpu, UInt16 instruction)
        {
            UInt16 rd = (UInt16)((instruction >> 8) & 0b111);
            UInt16 imm = (UInt16)(instruction & 0xff);

            UInt32 address = (cpu._reg[PC] & 0xffff_fffc) + (imm * 4u);
            UInt32 data = cpu._callbacks.ReadMemory32(address);
            cpu._reg[rd] = data;
        }

        private static void THUMB_LDR4(CPU cpu, UInt16 instruction)
        {
            UInt16 rd = (UInt16)((instruction >> 8) & 0b111);
            UInt16 imm = (UInt16)(instruction & 0xff);

            UInt32 address = cpu._reg[SP] + (imm * 4u);
            UInt32 data = cpu._callbacks.ReadMemory32(address);
            cpu._reg[rd] = RotateRight(data, 8 * (address & 0b11));
        }

        private static void THUMB_LDRB1(CPU cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)((instruction >> 6) & 0b1_1111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 address = cpu._reg[rn] + imm;
            Byte data = cpu._callbacks.ReadMemory8(address);
            cpu._reg[rd] = data;
        }

        private static void THUMB_LDRB2(CPU cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 6) & 0b111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 address = cpu._reg[rn] + cpu._reg[rm];
            Byte data = cpu._callbacks.ReadMemory8(address);
            cpu._reg[rd] = data;
        }

        private static void THUMB_LDRH1(CPU cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)((instruction >> 6) & 0b1_1111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 address = cpu._reg[rn] + (imm * 2u);
            UInt16 data = cpu._callbacks.ReadMemory16(address);
            cpu._reg[rd] = RotateRight(data, 8 * (address & 1));
        }

        private static void THUMB_LDRH2(CPU cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 6) & 0b111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 address = cpu._reg[rn] + cpu._reg[rm];
            UInt16 data = cpu._callbacks.ReadMemory16(address);
            cpu._reg[rd] = RotateRight(data, 8 * (address & 1));
        }

        private static void THUMB_LDRSB(CPU cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 6) & 0b111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 address = cpu._reg[rn] + cpu._reg[rm];
            Byte data = cpu._callbacks.ReadMemory8(address);
            cpu._reg[rd] = SignExtend(data, 8);
        }

        private static void THUMB_LDRSH(CPU cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 6) & 0b111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 address = cpu._reg[rn] + cpu._reg[rm];

            if ((address & 1) == 1)
            {
                Byte data = cpu._callbacks.ReadMemory8(address);
                cpu._reg[rd] = SignExtend(data, 8);
            }
            else
            {
                UInt16 data = cpu._callbacks.ReadMemory16(address);
                cpu._reg[rd] = SignExtend(data, 16);
            }
        }

        private static void THUMB_LSL1(CPU cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)((instruction >> 6) & 0b1_1111);
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            if (imm == 0)
            {
                cpu._reg[rd] = cpu._reg[rm];
            }
            else
            {
                cpu.SetFlag(Flags.C, (cpu._reg[rm] >> (32 - imm)) & 1);
                cpu._reg[rd] = cpu._reg[rm] << imm;
            }

            cpu.SetFlag(Flags.N, cpu._reg[rd] >> 31);
            cpu.SetFlag(Flags.Z, (cpu._reg[rd] == 0) ? 1u : 0u);
        }

        private static void THUMB_LSL2(CPU cpu, UInt16 instruction)
        {
            UInt16 rs = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 regRs = cpu._reg[rs] & 0xff;

            if (regRs == 0)
            {
                // nothing to do
            }
            else if (regRs < 32)
            {
                cpu.SetFlag(Flags.C, (cpu._reg[rd] >> (32 - (int)regRs)) & 1);
                cpu._reg[rd] = cpu._reg[rd] << (int)regRs;
            }
            else if (regRs == 32)
            {
                cpu.SetFlag(Flags.C, cpu._reg[rd] & 1);
                cpu._reg[rd] = 0;
            }
            else
            {
                cpu.SetFlag(Flags.C, 0);
                cpu._reg[rd] = 0;
            }

            cpu.SetFlag(Flags.N, cpu._reg[rd] >> 31);
            cpu.SetFlag(Flags.Z, (cpu._reg[rd] == 0) ? 1u : 0u);
        }

        private static void THUMB_LSR1(CPU cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)((instruction >> 6) & 0b1_1111);
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            if (imm == 0)
            {
                cpu.SetFlag(Flags.C, cpu._reg[rm] >> 31);
                cpu._reg[rd] = 0;
            }
            else
            {
                cpu.SetFlag(Flags.C, (cpu._reg[rm] >> (imm - 1)) & 1);
                cpu._reg[rd] = cpu._reg[rm] >> imm;
            }

            cpu.SetFlag(Flags.N, cpu._reg[rd] >> 31);
            cpu.SetFlag(Flags.Z, (cpu._reg[rd] == 0) ? 1u : 0u);
        }

        private static void THUMB_LSR2(CPU cpu, UInt16 instruction)
        {
            UInt16 rs = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 regRs = cpu._reg[rs] & 0xff;

            if (regRs == 0)
            {
                // nothing to do
            }
            else if (regRs < 32)
            {
                cpu.SetFlag(Flags.C, (cpu._reg[rd] >> ((int)regRs - 1)) & 1);
                cpu._reg[rd] = cpu._reg[rd] >> (int)regRs;
            }
            else if (regRs == 32)
            {
                cpu.SetFlag(Flags.C, cpu._reg[rd] >> 31);
                cpu._reg[rd] = 0;
            }
            else
            {
                cpu.SetFlag(Flags.C, 0);
                cpu._reg[rd] = 0;
            }

            cpu.SetFlag(Flags.N, cpu._reg[rd] >> 31);
            cpu.SetFlag(Flags.Z, (cpu._reg[rd] == 0) ? 1u : 0u);
        }

        private static void THUMB_MOV1(CPU cpu, UInt16 instruction)
        {
            UInt16 rd = (UInt16)((instruction >> 8) & 0b111);
            UInt16 imm = (UInt16)(instruction & 0xff);

            cpu._reg[rd] = imm;

            cpu.SetFlag(Flags.N, cpu._reg[rd] >> 31);
            cpu.SetFlag(Flags.Z, (cpu._reg[rd] == 0) ? 1u : 0u);
        }

        private static void THUMB_MOV3(CPU cpu, UInt16 instruction)
        {
            UInt16 h1 = (UInt16)((instruction >> 7) & 1);
            UInt16 h2 = (UInt16)((instruction >> 6) & 1);
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt16 hrd = (UInt16)((h1 << 3) | rd);
            UInt16 hrm = (UInt16)((h2 << 3) | rm);

            cpu.THUMB_SetReg(hrd, cpu._reg[hrm]);
        }

        private static void THUMB_MUL(CPU cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            cpu._reg[rd] *= cpu._reg[rm];

            cpu.SetFlag(Flags.N, cpu._reg[rd] >> 31);
            cpu.SetFlag(Flags.Z, (cpu._reg[rd] == 0) ? 1u : 0u);
        }

        private static void THUMB_MVN(CPU cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            cpu._reg[rd] = ~cpu._reg[rm];

            cpu.SetFlag(Flags.N, cpu._reg[rd] >> 31);
            cpu.SetFlag(Flags.Z, (cpu._reg[rd] == 0) ? 1u : 0u);
        }

        private static void THUMB_NEG(CPU cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 leftOperand = 0;
            UInt32 rightOperand = cpu._reg[rm];

            cpu._reg[rd] = leftOperand - rightOperand;

            cpu.SetFlag(Flags.N, cpu._reg[rd] >> 31);
            cpu.SetFlag(Flags.Z, (cpu._reg[rd] == 0) ? 1u : 0u);
            cpu.SetFlag(Flags.C, Not(BorrowFrom(leftOperand, rightOperand)));
            cpu.SetFlag(Flags.V, OverflowFrom_Subtraction(leftOperand, rightOperand, cpu._reg[rd]));
        }

        private static void THUMB_ORR(CPU cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            cpu._reg[rd] |= cpu._reg[rm];

            cpu.SetFlag(Flags.N, cpu._reg[rd] >> 31);
            cpu.SetFlag(Flags.Z, (cpu._reg[rd] == 0) ? 1u : 0u);
        }

        private static void THUMB_POP(CPU cpu, UInt16 instruction)
        {
            UInt16 r = (UInt16)((instruction >> 8) & 1);
            UInt16 registerList = (UInt16)(instruction & 0xff);

            UInt32 startAddress = cpu._reg[SP];
            UInt32 address = startAddress;

            for (var i = 0; i <= 7; ++i)
            {
                if (((registerList >> i) & 1) == 1)
                {
                    cpu._reg[i] = cpu._callbacks.ReadMemory32(address);
                    address += 4;
                }
            }

            if (r == 1)
            {
                UInt32 value = cpu._callbacks.ReadMemory32(address);
                cpu.THUMB_SetPC(value & 0xffff_fffe);
            }

            cpu._reg[SP] += 4 * (r + NumberOfSetBitsIn(registerList, 8));
        }

        private static void THUMB_PUSH(CPU cpu, UInt16 instruction)
        {
            UInt16 r = (UInt16)((instruction >> 8) & 1);
            UInt16 registerList = (UInt16)(instruction & 0xff);

            UInt32 startAddress = cpu._reg[SP] - 4 * (r + NumberOfSetBitsIn(registerList, 8));
            UInt32 address = startAddress;

            for (var i = 0; i <= 7; ++i)
            {
                if (((registerList >> i) & 1) == 1)
                {
                    cpu._callbacks.WriteMemory32(address, cpu._reg[i]);
                    address += 4;
                }
            }

            if (r == 1)
                cpu._callbacks.WriteMemory32(address, cpu._reg[LR]);

            cpu._reg[SP] = startAddress;
        }

        private static void THUMB_ROR(CPU cpu, UInt16 instruction)
        {
            UInt16 rs = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            if ((cpu._reg[rs] & 0xff) == 0)
            {
                // nothing to do
            }
            else if ((cpu._reg[rs] & 0b1_1111) == 0)
            {
                cpu.SetFlag(Flags.C, cpu._reg[rd] >> 31);
            }
            else
            {
                cpu.SetFlag(Flags.C, (cpu._reg[rd] >> ((int)(cpu._reg[rs] & 0b1_1111) - 1)) & 1);
                cpu._reg[rd] = RotateRight(cpu._reg[rd], cpu._reg[rs] & 0b1_1111);
            }

            cpu.SetFlag(Flags.N, cpu._reg[rd] >> 31);
            cpu.SetFlag(Flags.Z, (cpu._reg[rd] == 0) ? 1u : 0u);
        }

        private static void THUMB_SBC(CPU cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 leftOperand = cpu._reg[rd];
            UInt64 rightOperand = (UInt64)cpu._reg[rm] + (UInt64)Not(cpu.GetFlag(Flags.C));

            cpu._reg[rd] = leftOperand - (UInt32)rightOperand;

            cpu.SetFlag(Flags.N, cpu._reg[rd] >> 31);
            cpu.SetFlag(Flags.Z, (cpu._reg[rd] == 0) ? 1u : 0u);
            cpu.SetFlag(Flags.C, Not(BorrowFrom(leftOperand, rightOperand)));
            cpu.SetFlag(Flags.V, OverflowFrom_Subtraction(leftOperand, (UInt32)rightOperand, cpu._reg[rd]));
        }

        private static void THUMB_STMIA(CPU cpu, UInt16 instruction)
        {
            UInt16 rn = (UInt16)((instruction >> 8) & 0b111);
            UInt16 registerList = (UInt16)(instruction & 0xff);

            UInt32 regRn = cpu._reg[rn];

            UInt32 startAddress = regRn;
            UInt32 address = startAddress;

            if (registerList != 0)
            {
                cpu._reg[rn] += NumberOfSetBitsIn(registerList, 8) * 4;

                for (var i = 0; i <= 7; ++i)
                {
                    if (((registerList >> i) & 1) == 1)
                    {
                        if ((i == rn) && (registerList & ~(0xffff_ffff << i)) == 0)
                            cpu._callbacks.WriteMemory32(address, regRn);
                        else
                            cpu._callbacks.WriteMemory32(address, cpu._reg[i]);

                        address += 4;
                    }
                }
            }
            else
            {
                cpu._callbacks.WriteMemory32(address, cpu._reg[PC] + 2);
                cpu._reg[rn] += 0x40;
            }
        }

        private static void THUMB_STR1(CPU cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)((instruction >> 6) & 0b1_1111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 address = cpu._reg[rn] + (imm * 4u);
            cpu._callbacks.WriteMemory32(address, cpu._reg[rd]);
        }

        private static void THUMB_STR2(CPU cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 6) & 0b111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 address = cpu._reg[rn] + cpu._reg[rm];
            cpu._callbacks.WriteMemory32(address, cpu._reg[rd]);
        }

        private static void THUMB_STR3(CPU cpu, UInt16 instruction)
        {
            UInt16 rd = (UInt16)((instruction >> 8) & 0b111);
            UInt16 imm = (UInt16)(instruction & 0xff);

            UInt32 address = cpu._reg[SP] + (imm * 4u);
            cpu._callbacks.WriteMemory32(address, cpu._reg[rd]);
        }

        private static void THUMB_STRB1(CPU cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)((instruction >> 6) & 0b1_1111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 address = cpu._reg[rn] + imm;
            cpu._callbacks.WriteMemory8(address, (Byte)cpu._reg[rd]);
        }

        private static void THUMB_STRB2(CPU cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 6) & 0b111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 address = cpu._reg[rn] + cpu._reg[rm];
            cpu._callbacks.WriteMemory8(address, (Byte)cpu._reg[rd]);
        }

        private static void THUMB_STRH1(CPU cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)((instruction >> 6) & 0b1_1111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 address = cpu._reg[rn] + (imm * 2u);
            cpu._callbacks.WriteMemory16(address, (UInt16)cpu._reg[rd]);
        }

        private static void THUMB_STRH2(CPU cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 6) & 0b111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 address = cpu._reg[rn] + cpu._reg[rm];
            cpu._callbacks.WriteMemory16(address, (UInt16)cpu._reg[rd]);
        }

        private static void THUMB_SUB1(CPU cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)((instruction >> 6) & 0b111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 leftOperand = cpu._reg[rn];
            UInt32 rightOperand = imm;

            cpu._reg[rd] = leftOperand - rightOperand;

            cpu.SetFlag(Flags.N, cpu._reg[rd] >> 31);
            cpu.SetFlag(Flags.Z, (cpu._reg[rd] == 0) ? 1u : 0u);
            cpu.SetFlag(Flags.C, Not(BorrowFrom(leftOperand, rightOperand)));
            cpu.SetFlag(Flags.V, OverflowFrom_Subtraction(leftOperand, rightOperand, cpu._reg[rd]));
        }

        private static void THUMB_SUB2(CPU cpu, UInt16 instruction)
        {
            UInt16 rd = (UInt16)((instruction >> 8) & 0b111);
            UInt16 imm = (UInt16)(instruction & 0xff);

            UInt32 leftOperand = cpu._reg[rd];
            UInt32 rightOperand = imm;

            cpu._reg[rd] = leftOperand - rightOperand;

            cpu.SetFlag(Flags.N, cpu._reg[rd] >> 31);
            cpu.SetFlag(Flags.Z, (cpu._reg[rd] == 0) ? 1u : 0u);
            cpu.SetFlag(Flags.C, Not(BorrowFrom(leftOperand, rightOperand)));
            cpu.SetFlag(Flags.V, OverflowFrom_Subtraction(leftOperand, rightOperand, cpu._reg[rd]));
        }

        private static void THUMB_SUB3(CPU cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 6) & 0b111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 leftOperand = cpu._reg[rn];
            UInt32 rightOperand = cpu._reg[rm];

            cpu._reg[rd] = leftOperand - rightOperand;

            cpu.SetFlag(Flags.N, cpu._reg[rd] >> 31);
            cpu.SetFlag(Flags.Z, (cpu._reg[rd] == 0) ? 1u : 0u);
            cpu.SetFlag(Flags.C, Not(BorrowFrom(leftOperand, rightOperand)));
            cpu.SetFlag(Flags.V, OverflowFrom_Subtraction(leftOperand, rightOperand, cpu._reg[rd]));
        }

        private static void THUMB_SUB4(CPU cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)(instruction & 0x7f);

            cpu._reg[SP] -= (UInt32)imm << 2;
        }

        private static void THUMB_TST(CPU cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rn = (UInt16)(instruction & 0b111);

            UInt32 aluOut = cpu._reg[rn] & cpu._reg[rm];

            cpu.SetFlag(Flags.N, aluOut >> 31);
            cpu.SetFlag(Flags.Z, (aluOut == 0) ? 1u : 0u);
        }
    }
}
