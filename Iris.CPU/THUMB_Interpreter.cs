using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Iris.CPU.CPU;

namespace Iris.CPU
{
    internal sealed class THUMB_Interpreter
    {
        private unsafe readonly InstructionLUTEntry<UInt16>[] _instructionLUT = new InstructionLUTEntry<UInt16>[1 << 10];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static UInt16 InstructionLUTHash(UInt16 value)
        {
            return (UInt16)(value >> 6);
        }

        private unsafe void InitInstructionLUT()
        {
            InstructionListEntry<UInt16>[] InstructionList = new InstructionListEntry<UInt16>[]
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

            for (UInt32 instruction = 0; instruction < _instructionLUT.Length; ++instruction)
            {
                bool unknownInstruction = true;

                foreach (InstructionListEntry<UInt16> entry in InstructionList)
                {
                    if ((instruction & InstructionLUTHash(entry.Mask)) == InstructionLUTHash(entry.Expected))
                    {
                        _instructionLUT[instruction] = new(entry.Handler);
                        unknownInstruction = false;
                        break;
                    }
                }

                if (unknownInstruction)
                    _instructionLUT[instruction] = new(&UNKNOWN);
            }
        }

        private readonly CPU _cpu;

        internal THUMB_Interpreter(CPU cpu)
        {
            _cpu = cpu;
            InitInstructionLUT();
        }

        internal Byte Step()
        {
            UInt16 instruction = _cpu._callbackInterface.ReadMemory16(_cpu.NextInstructionAddress);
            _cpu.NextInstructionAddress += 2;

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(_cpu.Reg);
            ref UInt32 regPC = ref Unsafe.Add(ref regDataRef, PC);

            regPC = _cpu.NextInstructionAddress + 2;

            ref InstructionLUTEntry<UInt16> instructionLUTDataRef = ref MemoryMarshal.GetArrayDataReference(_instructionLUT);
            ref InstructionLUTEntry<UInt16> instructionLUTEntry = ref Unsafe.Add(ref instructionLUTDataRef, InstructionLUTHash(instruction));

            unsafe
            {
                return instructionLUTEntry.Handler(_cpu, instruction);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SetPC(CPU cpu, UInt32 value)
        {
            cpu.NextInstructionAddress = value & 0xffff_fffe;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SetReg(CPU cpu, UInt32 i, UInt32 value)
        {
            if (i == PC)
            {
                SetPC(cpu, value);
            }
            else
            {
                ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
                ref UInt32 regRi = ref Unsafe.Add(ref regDataRef, i);

                regRi = value;
            }
        }

        private static Byte UNKNOWN(CPU cpu, UInt16 instruction)
        {
            throw new Exception(string.Format("Iris.CPU.THUMB_Interpreter: Unknown THUMB instruction 0x{0:x4} at address 0x{1:x8}", instruction, cpu.NextInstructionAddress - 2));
        }

        private static Byte ADC(CPU cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regRm = ref Unsafe.Add(ref regDataRef, rm);
            ref UInt32 regRd = ref Unsafe.Add(ref regDataRef, rd);

            UInt32 leftOperand = regRd;
            UInt64 rightOperand = (UInt64)regRm + (UInt64)cpu.GetFlag(Flag.C);

            UInt64 result = (UInt64)leftOperand + rightOperand;
            regRd = (UInt32)result;

            cpu.SetFlag(Flag.N, regRd >> 31);
            cpu.SetFlag(Flag.Z, (regRd == 0) ? 1u : 0u);
            cpu.SetFlag(Flag.C, CarryFrom(result));
            cpu.SetFlag(Flag.V, OverflowFrom_Addition(leftOperand, (UInt32)rightOperand, regRd));

            return 1;
        }

        private static Byte ADD1(CPU cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)((instruction >> 6) & 0b111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regRn = ref Unsafe.Add(ref regDataRef, rn);
            ref UInt32 regRd = ref Unsafe.Add(ref regDataRef, rd);

            UInt32 leftOperand = regRn;
            UInt32 rightOperand = imm;

            UInt64 result = (UInt64)leftOperand + (UInt64)rightOperand;
            regRd = (UInt32)result;

            cpu.SetFlag(Flag.N, regRd >> 31);
            cpu.SetFlag(Flag.Z, (regRd == 0) ? 1u : 0u);
            cpu.SetFlag(Flag.C, CarryFrom(result));
            cpu.SetFlag(Flag.V, OverflowFrom_Addition(leftOperand, rightOperand, regRd));

            return 1;
        }

        private static Byte ADD2(CPU cpu, UInt16 instruction)
        {
            UInt16 rd = (UInt16)((instruction >> 8) & 0b111);
            UInt16 imm = (UInt16)(instruction & 0xff);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regRd = ref Unsafe.Add(ref regDataRef, rd);

            UInt32 leftOperand = regRd;
            UInt32 rightOperand = imm;

            UInt64 result = (UInt64)leftOperand + (UInt64)rightOperand;
            regRd = (UInt32)result;

            cpu.SetFlag(Flag.N, regRd >> 31);
            cpu.SetFlag(Flag.Z, (regRd == 0) ? 1u : 0u);
            cpu.SetFlag(Flag.C, CarryFrom(result));
            cpu.SetFlag(Flag.V, OverflowFrom_Addition(leftOperand, rightOperand, regRd));

            return 1;
        }

        private static Byte ADD3(CPU cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 6) & 0b111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regRm = ref Unsafe.Add(ref regDataRef, rm);
            ref UInt32 regRn = ref Unsafe.Add(ref regDataRef, rn);
            ref UInt32 regRd = ref Unsafe.Add(ref regDataRef, rd);

            UInt32 leftOperand = regRn;
            UInt32 rightOperand = regRm;

            UInt64 result = (UInt64)leftOperand + (UInt64)rightOperand;
            regRd = (UInt32)result;

            cpu.SetFlag(Flag.N, regRd >> 31);
            cpu.SetFlag(Flag.Z, (regRd == 0) ? 1u : 0u);
            cpu.SetFlag(Flag.C, CarryFrom(result));
            cpu.SetFlag(Flag.V, OverflowFrom_Addition(leftOperand, rightOperand, regRd));

            return 1;
        }

        private static Byte ADD4(CPU cpu, UInt16 instruction)
        {
            UInt16 h1 = (UInt16)((instruction >> 7) & 1);
            UInt16 h2 = (UInt16)((instruction >> 6) & 1);
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            rd |= (UInt16)(h1 << 3);
            rm |= (UInt16)(h2 << 3);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regRm = ref Unsafe.Add(ref regDataRef, rm);
            ref UInt32 regRd = ref Unsafe.Add(ref regDataRef, rd);

            SetReg(cpu, rd, regRd + regRm);

            if (rd == PC)
                return 3;
            else
                return 1;
        }

        private static Byte ADD5(CPU cpu, UInt16 instruction)
        {
            UInt16 rd = (UInt16)((instruction >> 8) & 0b111);
            UInt16 imm = (UInt16)(instruction & 0xff);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regRd = ref Unsafe.Add(ref regDataRef, rd);
            ref UInt32 regPC = ref Unsafe.Add(ref regDataRef, PC);

            regRd = (regPC & 0xffff_fffc) + (imm * 4u);

            return 1;
        }

        private static Byte ADD6(CPU cpu, UInt16 instruction)
        {
            UInt16 rd = (UInt16)((instruction >> 8) & 0b111);
            UInt16 imm = (UInt16)(instruction & 0xff);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regRd = ref Unsafe.Add(ref regDataRef, rd);
            ref UInt32 regSP = ref Unsafe.Add(ref regDataRef, SP);

            regRd = regSP + (imm * 4u);

            return 1;
        }

        private static Byte ADD7(CPU cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)(instruction & 0x7f);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regSP = ref Unsafe.Add(ref regDataRef, SP);

            regSP += imm * 4u;

            return 1;
        }

        private static Byte AND(CPU cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regRm = ref Unsafe.Add(ref regDataRef, rm);
            ref UInt32 regRd = ref Unsafe.Add(ref regDataRef, rd);

            regRd &= regRm;

            cpu.SetFlag(Flag.N, regRd >> 31);
            cpu.SetFlag(Flag.Z, (regRd == 0) ? 1u : 0u);

            return 1;
        }

        private static Byte ASR1(CPU cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)((instruction >> 6) & 0b1_1111);
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regRm = ref Unsafe.Add(ref regDataRef, rm);
            ref UInt32 regRd = ref Unsafe.Add(ref regDataRef, rd);

            int shiftAmount = imm;

            if (shiftAmount == 0)
            {
                cpu.SetFlag(Flag.C, regRm >> 31);
                regRd = ((regRm >> 31) == 0) ? 0 : 0xffff_ffff;
            }
            else
            {
                cpu.SetFlag(Flag.C, (regRm >> (shiftAmount - 1)) & 1);
                regRd = ArithmeticShiftRight(regRm, shiftAmount);
            }

            cpu.SetFlag(Flag.N, regRd >> 31);
            cpu.SetFlag(Flag.Z, (regRd == 0) ? 1u : 0u);

            return 1;
        }

        private static Byte ASR2(CPU cpu, UInt16 instruction)
        {
            UInt16 rs = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regRs = ref Unsafe.Add(ref regDataRef, rs);
            ref UInt32 regRd = ref Unsafe.Add(ref regDataRef, rd);

            int shiftAmount = (int)(regRs & 0xff);

            if (shiftAmount == 0)
            {
                // nothing to do
            }
            else if (shiftAmount < 32)
            {
                cpu.SetFlag(Flag.C, (regRd >> (shiftAmount - 1)) & 1);
                regRd = ArithmeticShiftRight(regRd, shiftAmount);
            }
            else
            {
                cpu.SetFlag(Flag.C, regRd >> 31);
                regRd = ((regRd >> 31) == 0) ? 0 : 0xffff_ffff;
            }

            cpu.SetFlag(Flag.N, regRd >> 31);
            cpu.SetFlag(Flag.Z, (regRd == 0) ? 1u : 0u);

            return 2;
        }

        private static Byte B1(CPU cpu, UInt16 instruction)
        {
            UInt16 cond = (UInt16)((instruction >> 8) & 0b1111);
            UInt16 imm = (UInt16)(instruction & 0xff);

            if (cpu.ConditionPassed(cond))
            {
                ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
                ref UInt32 regPC = ref Unsafe.Add(ref regDataRef, PC);

                SetPC(cpu, regPC + (SignExtend(imm, 8) << 1));

                return 3;
            }
            else
            {
                return 1;
            }
        }

        private static Byte B2(CPU cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)(instruction & 0x7ff);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regPC = ref Unsafe.Add(ref regDataRef, PC);

            SetPC(cpu, regPC + (SignExtend(imm, 11) << 1));

            return 3;
        }

        private static Byte BIC(CPU cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regRm = ref Unsafe.Add(ref regDataRef, rm);
            ref UInt32 regRd = ref Unsafe.Add(ref regDataRef, rd);

            regRd &= ~regRm;

            cpu.SetFlag(Flag.N, regRd >> 31);
            cpu.SetFlag(Flag.Z, (regRd == 0) ? 1u : 0u);

            return 1;
        }

        private static Byte BL(CPU cpu, UInt16 instruction)
        {
            UInt16 h = (UInt16)((instruction >> 11) & 0b11);
            UInt16 offset = (UInt16)(instruction & 0x7ff);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regLR = ref Unsafe.Add(ref regDataRef, LR);

            if (h == 0b10)
            {
                ref UInt32 regPC = ref Unsafe.Add(ref regDataRef, PC);

                regLR = regPC + (SignExtend(offset, 11) << 12);
            }
            else if (h == 0b11)
            {
                // save NextInstructionAddress because it's invalidated by SetPC
                UInt32 nextInstructionAddress = cpu.NextInstructionAddress;

                SetPC(cpu, regLR + (UInt32)(offset << 1));
                regLR = nextInstructionAddress | 1;
            }

            return 4;
        }

        private static Byte BX(CPU cpu, UInt16 instruction)
        {
            UInt16 h2 = (UInt16)((instruction >> 6) & 1);
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);

            rm |= (UInt16)(h2 << 3);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regRm = ref Unsafe.Add(ref regDataRef, rm);

            cpu.CPSR = (cpu.CPSR & ~(1u << 5)) | ((regRm & 1) << 5);
            SetPC(cpu, regRm);

            return 3;
        }

        private static Byte CMN(CPU cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rn = (UInt16)(instruction & 0b111);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regRm = ref Unsafe.Add(ref regDataRef, rm);
            ref UInt32 regRn = ref Unsafe.Add(ref regDataRef, rn);

            UInt32 leftOperand = regRn;
            UInt32 rightOperand = regRm;

            UInt64 result = (UInt64)leftOperand + (UInt64)rightOperand;
            UInt32 aluOut = (UInt32)result;

            cpu.SetFlag(Flag.N, aluOut >> 31);
            cpu.SetFlag(Flag.Z, (aluOut == 0) ? 1u : 0u);
            cpu.SetFlag(Flag.C, CarryFrom(result));
            cpu.SetFlag(Flag.V, OverflowFrom_Addition(leftOperand, rightOperand, aluOut));

            return 1;
        }

        private static Byte CMP1(CPU cpu, UInt16 instruction)
        {
            UInt16 rn = (UInt16)((instruction >> 8) & 0b111);
            UInt16 imm = (UInt16)(instruction & 0xff);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regRn = ref Unsafe.Add(ref regDataRef, rn);

            UInt32 leftOperand = regRn;
            UInt32 rightOperand = imm;

            UInt32 aluOut = leftOperand - rightOperand;

            cpu.SetFlag(Flag.N, aluOut >> 31);
            cpu.SetFlag(Flag.Z, (aluOut == 0) ? 1u : 0u);
            cpu.SetFlag(Flag.C, Not(BorrowFrom(leftOperand, rightOperand)));
            cpu.SetFlag(Flag.V, OverflowFrom_Subtraction(leftOperand, rightOperand, aluOut));

            return 1;
        }

        private static Byte CMP2(CPU cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rn = (UInt16)(instruction & 0b111);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regRm = ref Unsafe.Add(ref regDataRef, rm);
            ref UInt32 regRn = ref Unsafe.Add(ref regDataRef, rn);

            UInt32 leftOperand = regRn;
            UInt32 rightOperand = regRm;

            UInt32 aluOut = leftOperand - rightOperand;

            cpu.SetFlag(Flag.N, aluOut >> 31);
            cpu.SetFlag(Flag.Z, (aluOut == 0) ? 1u : 0u);
            cpu.SetFlag(Flag.C, Not(BorrowFrom(leftOperand, rightOperand)));
            cpu.SetFlag(Flag.V, OverflowFrom_Subtraction(leftOperand, rightOperand, aluOut));

            return 1;
        }

        private static Byte CMP3(CPU cpu, UInt16 instruction)
        {
            UInt16 h1 = (UInt16)((instruction >> 7) & 1);
            UInt16 h2 = (UInt16)((instruction >> 6) & 1);
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rn = (UInt16)(instruction & 0b111);

            rn |= (UInt16)(h1 << 3);
            rm |= (UInt16)(h2 << 3);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regRm = ref Unsafe.Add(ref regDataRef, rm);
            ref UInt32 regRn = ref Unsafe.Add(ref regDataRef, rn);

            UInt32 leftOperand = regRn;
            UInt32 rightOperand = regRm;

            UInt32 aluOut = leftOperand - rightOperand;

            cpu.SetFlag(Flag.N, aluOut >> 31);
            cpu.SetFlag(Flag.Z, (aluOut == 0) ? 1u : 0u);
            cpu.SetFlag(Flag.C, Not(BorrowFrom(leftOperand, rightOperand)));
            cpu.SetFlag(Flag.V, OverflowFrom_Subtraction(leftOperand, rightOperand, aluOut));

            return 1;
        }

        private static Byte EOR(CPU cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regRm = ref Unsafe.Add(ref regDataRef, rm);
            ref UInt32 regRd = ref Unsafe.Add(ref regDataRef, rd);

            regRd ^= regRm;

            cpu.SetFlag(Flag.N, regRd >> 31);
            cpu.SetFlag(Flag.Z, (regRd == 0) ? 1u : 0u);

            return 1;
        }

        private static Byte LDMIA(CPU cpu, UInt16 instruction)
        {
            UInt16 rn = (UInt16)((instruction >> 8) & 0b111);
            UInt16 registerList = (UInt16)(instruction & 0xff);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regRn = ref Unsafe.Add(ref regDataRef, rn);

            UInt32 address = regRn;

            if (registerList == 0)
            {
                regRn += 0x40;
                SetPC(cpu, cpu._callbackInterface.ReadMemory32(address));

                return 5;
            }
            else
            {
                UInt32 n = (UInt32)BitOperations.PopCount(registerList);
                regRn += n * 4;

                for (int i = 0; i <= 7; ++i)
                {
                    if (((registerList >> i) & 1) == 1)
                    {
                        ref UInt32 regRi = ref Unsafe.Add(ref regDataRef, i);

                        regRi = cpu._callbackInterface.ReadMemory32(address);
                        address += 4;
                    }
                }

                return (Byte)(n + 2);
            }
        }

        private static Byte LDR1(CPU cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)((instruction >> 6) & 0b1_1111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regRn = ref Unsafe.Add(ref regDataRef, rn);
            ref UInt32 regRd = ref Unsafe.Add(ref regDataRef, rd);

            UInt32 address = regRn + (imm * 4u);
            UInt32 data = BitOperations.RotateRight(cpu._callbackInterface.ReadMemory32(address), (int)(8 * (address & 0b11)));
            regRd = data;

            return 3;
        }

        private static Byte LDR2(CPU cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 6) & 0b111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regRm = ref Unsafe.Add(ref regDataRef, rm);
            ref UInt32 regRn = ref Unsafe.Add(ref regDataRef, rn);
            ref UInt32 regRd = ref Unsafe.Add(ref regDataRef, rd);

            UInt32 address = regRn + regRm;
            UInt32 data = BitOperations.RotateRight(cpu._callbackInterface.ReadMemory32(address), (int)(8 * (address & 0b11)));
            regRd = data;

            return 3;
        }

        private static Byte LDR3(CPU cpu, UInt16 instruction)
        {
            UInt16 rd = (UInt16)((instruction >> 8) & 0b111);
            UInt16 imm = (UInt16)(instruction & 0xff);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regRd = ref Unsafe.Add(ref regDataRef, rd);
            ref UInt32 regPC = ref Unsafe.Add(ref regDataRef, PC);

            UInt32 address = regPC + (imm * 4u);
            UInt32 data = cpu._callbackInterface.ReadMemory32(address);
            regRd = data;

            return 3;
        }

        private static Byte LDR4(CPU cpu, UInt16 instruction)
        {
            UInt16 rd = (UInt16)((instruction >> 8) & 0b111);
            UInt16 imm = (UInt16)(instruction & 0xff);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regRd = ref Unsafe.Add(ref regDataRef, rd);
            ref UInt32 regSP = ref Unsafe.Add(ref regDataRef, SP);

            UInt32 address = regSP + (imm * 4u);
            UInt32 data = BitOperations.RotateRight(cpu._callbackInterface.ReadMemory32(address), (int)(8 * (address & 0b11)));
            regRd = data;

            return 3;
        }

        private static Byte LDRB1(CPU cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)((instruction >> 6) & 0b1_1111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regRn = ref Unsafe.Add(ref regDataRef, rn);
            ref UInt32 regRd = ref Unsafe.Add(ref regDataRef, rd);

            UInt32 address = regRn + imm;
            Byte data = cpu._callbackInterface.ReadMemory8(address);
            regRd = data;

            return 3;
        }

        private static Byte LDRB2(CPU cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 6) & 0b111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regRm = ref Unsafe.Add(ref regDataRef, rm);
            ref UInt32 regRn = ref Unsafe.Add(ref regDataRef, rn);
            ref UInt32 regRd = ref Unsafe.Add(ref regDataRef, rd);

            UInt32 address = regRn + regRm;
            Byte data = cpu._callbackInterface.ReadMemory8(address);
            regRd = data;

            return 3;
        }

        private static Byte LDRH1(CPU cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)((instruction >> 6) & 0b1_1111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regRn = ref Unsafe.Add(ref regDataRef, rn);
            ref UInt32 regRd = ref Unsafe.Add(ref regDataRef, rd);

            UInt32 address = regRn + (imm * 2u);
            UInt32 data = BitOperations.RotateRight(cpu._callbackInterface.ReadMemory16(address), (int)(8 * (address & 1)));
            regRd = data;

            return 3;
        }

        private static Byte LDRH2(CPU cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 6) & 0b111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regRm = ref Unsafe.Add(ref regDataRef, rm);
            ref UInt32 regRn = ref Unsafe.Add(ref regDataRef, rn);
            ref UInt32 regRd = ref Unsafe.Add(ref regDataRef, rd);

            UInt32 address = regRn + regRm;
            UInt32 data = BitOperations.RotateRight(cpu._callbackInterface.ReadMemory16(address), (int)(8 * (address & 1)));
            regRd = data;

            return 3;
        }

        private static Byte LDRSB(CPU cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 6) & 0b111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regRm = ref Unsafe.Add(ref regDataRef, rm);
            ref UInt32 regRn = ref Unsafe.Add(ref regDataRef, rn);
            ref UInt32 regRd = ref Unsafe.Add(ref regDataRef, rd);

            UInt32 address = regRn + regRm;
            Byte data = cpu._callbackInterface.ReadMemory8(address);
            regRd = SignExtend(data, 8);

            return 3;
        }

        private static Byte LDRSH(CPU cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 6) & 0b111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regRm = ref Unsafe.Add(ref regDataRef, rm);
            ref UInt32 regRn = ref Unsafe.Add(ref regDataRef, rn);
            ref UInt32 regRd = ref Unsafe.Add(ref regDataRef, rd);

            UInt32 address = regRn + regRm;

            if ((address & 1) == 1)
            {
                Byte data = cpu._callbackInterface.ReadMemory8(address);
                regRd = SignExtend(data, 8);
            }
            else
            {
                UInt16 data = cpu._callbackInterface.ReadMemory16(address);
                regRd = SignExtend(data, 16);
            }

            return 3;
        }

        private static Byte LSL1(CPU cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)((instruction >> 6) & 0b1_1111);
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regRm = ref Unsafe.Add(ref regDataRef, rm);
            ref UInt32 regRd = ref Unsafe.Add(ref regDataRef, rd);

            int shiftAmount = imm;

            if (shiftAmount == 0)
            {
                regRd = regRm;
            }
            else
            {
                cpu.SetFlag(Flag.C, (regRm >> (32 - shiftAmount)) & 1);
                regRd = regRm << shiftAmount;
            }

            cpu.SetFlag(Flag.N, regRd >> 31);
            cpu.SetFlag(Flag.Z, (regRd == 0) ? 1u : 0u);

            return 1;
        }

        private static Byte LSL2(CPU cpu, UInt16 instruction)
        {
            UInt16 rs = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regRs = ref Unsafe.Add(ref regDataRef, rs);
            ref UInt32 regRd = ref Unsafe.Add(ref regDataRef, rd);

            int shiftAmount = (int)(regRs & 0xff);

            if (shiftAmount == 0)
            {
                // nothing to do
            }
            else if (shiftAmount < 32)
            {
                cpu.SetFlag(Flag.C, (regRd >> (32 - shiftAmount)) & 1);
                regRd <<= shiftAmount;
            }
            else if (shiftAmount == 32)
            {
                cpu.SetFlag(Flag.C, regRd & 1);
                regRd = 0;
            }
            else
            {
                cpu.SetFlag(Flag.C, 0);
                regRd = 0;
            }

            cpu.SetFlag(Flag.N, regRd >> 31);
            cpu.SetFlag(Flag.Z, (regRd == 0) ? 1u : 0u);

            return 2;
        }

        private static Byte LSR1(CPU cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)((instruction >> 6) & 0b1_1111);
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regRm = ref Unsafe.Add(ref regDataRef, rm);
            ref UInt32 regRd = ref Unsafe.Add(ref regDataRef, rd);

            int shiftAmount = imm;

            if (shiftAmount == 0)
            {
                cpu.SetFlag(Flag.C, regRm >> 31);
                regRd = 0;
            }
            else
            {
                cpu.SetFlag(Flag.C, (regRm >> (shiftAmount - 1)) & 1);
                regRd = regRm >> shiftAmount;
            }

            cpu.SetFlag(Flag.N, regRd >> 31);
            cpu.SetFlag(Flag.Z, (regRd == 0) ? 1u : 0u);

            return 1;
        }

        private static Byte LSR2(CPU cpu, UInt16 instruction)
        {
            UInt16 rs = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regRs = ref Unsafe.Add(ref regDataRef, rs);
            ref UInt32 regRd = ref Unsafe.Add(ref regDataRef, rd);

            int shiftAmount = (int)(regRs & 0xff);

            if (shiftAmount == 0)
            {
                // nothing to do
            }
            else if (shiftAmount < 32)
            {
                cpu.SetFlag(Flag.C, (regRd >> (shiftAmount - 1)) & 1);
                regRd >>= shiftAmount;
            }
            else if (shiftAmount == 32)
            {
                cpu.SetFlag(Flag.C, regRd >> 31);
                regRd = 0;
            }
            else
            {
                cpu.SetFlag(Flag.C, 0);
                regRd = 0;
            }

            cpu.SetFlag(Flag.N, regRd >> 31);
            cpu.SetFlag(Flag.Z, (regRd == 0) ? 1u : 0u);

            return 2;
        }

        private static Byte MOV1(CPU cpu, UInt16 instruction)
        {
            UInt16 rd = (UInt16)((instruction >> 8) & 0b111);
            UInt16 imm = (UInt16)(instruction & 0xff);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regRd = ref Unsafe.Add(ref regDataRef, rd);

            regRd = imm;

            cpu.SetFlag(Flag.N, regRd >> 31);
            cpu.SetFlag(Flag.Z, (regRd == 0) ? 1u : 0u);

            return 1;
        }

        private static Byte MOV3(CPU cpu, UInt16 instruction)
        {
            UInt16 h1 = (UInt16)((instruction >> 7) & 1);
            UInt16 h2 = (UInt16)((instruction >> 6) & 1);
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            rd |= (UInt16)(h1 << 3);
            rm |= (UInt16)(h2 << 3);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regRm = ref Unsafe.Add(ref regDataRef, rm);

            SetReg(cpu, rd, regRm);

            if (rd == PC)
                return 3;
            else
                return 1;
        }

        private static Byte MUL(CPU cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regRm = ref Unsafe.Add(ref regDataRef, rm);
            ref UInt32 regRd = ref Unsafe.Add(ref regDataRef, rd);

            UInt32 m;

            static bool CheckMultiplier(UInt32 multiplier, UInt32 mask) => ((multiplier & mask) == 0) || ((multiplier & mask) == mask);

            if (CheckMultiplier(regRm, 0xffff_ff00))
                m = 1;
            else if (CheckMultiplier(regRm, 0xffff_0000))
                m = 2;
            else if (CheckMultiplier(regRm, 0xff00_0000))
                m = 3;
            else
                m = 4;

            regRd *= regRm;

            cpu.SetFlag(Flag.N, regRd >> 31);
            cpu.SetFlag(Flag.Z, (regRd == 0) ? 1u : 0u);

            return (Byte)(m + 1);
        }

        private static Byte MVN(CPU cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regRm = ref Unsafe.Add(ref regDataRef, rm);
            ref UInt32 regRd = ref Unsafe.Add(ref regDataRef, rd);

            regRd = ~regRm;

            cpu.SetFlag(Flag.N, regRd >> 31);
            cpu.SetFlag(Flag.Z, (regRd == 0) ? 1u : 0u);

            return 1;
        }

        private static Byte NEG(CPU cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regRm = ref Unsafe.Add(ref regDataRef, rm);
            ref UInt32 regRd = ref Unsafe.Add(ref regDataRef, rd);

            UInt32 leftOperand = 0;
            UInt32 rightOperand = regRm;

            regRd = leftOperand - rightOperand;

            cpu.SetFlag(Flag.N, regRd >> 31);
            cpu.SetFlag(Flag.Z, (regRd == 0) ? 1u : 0u);
            cpu.SetFlag(Flag.C, Not(BorrowFrom(leftOperand, rightOperand)));
            cpu.SetFlag(Flag.V, OverflowFrom_Subtraction(leftOperand, rightOperand, regRd));

            return 1;
        }

        private static Byte ORR(CPU cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regRm = ref Unsafe.Add(ref regDataRef, rm);
            ref UInt32 regRd = ref Unsafe.Add(ref regDataRef, rd);

            regRd |= regRm;

            cpu.SetFlag(Flag.N, regRd >> 31);
            cpu.SetFlag(Flag.Z, (regRd == 0) ? 1u : 0u);

            return 1;
        }

        private static Byte POP(CPU cpu, UInt16 instruction)
        {
            UInt16 r = (UInt16)((instruction >> 8) & 1);
            UInt16 registerList = (UInt16)(instruction & 0xff);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regSP = ref Unsafe.Add(ref regDataRef, SP);

            UInt32 n = (UInt32)BitOperations.PopCount(registerList);
            UInt32 address = regSP;
            regSP += 4 * (r + n);

            for (int i = 0; i <= 7; ++i)
            {
                if (((registerList >> i) & 1) == 1)
                {
                    ref UInt32 regRi = ref Unsafe.Add(ref regDataRef, i);

                    regRi = cpu._callbackInterface.ReadMemory32(address);
                    address += 4;
                }
            }

            if (r == 1)
            {
                SetPC(cpu, cpu._callbackInterface.ReadMemory32(address));

                return (Byte)(n + 5);
            }
            else
            {
                return (Byte)(n + 4);
            }
        }

        private static Byte PUSH(CPU cpu, UInt16 instruction)
        {
            UInt16 r = (UInt16)((instruction >> 8) & 1);
            UInt16 registerList = (UInt16)(instruction & 0xff);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regSP = ref Unsafe.Add(ref regDataRef, SP);

            UInt32 n = (UInt32)BitOperations.PopCount(registerList);
            regSP -= 4 * (r + n);
            UInt32 address = regSP;

            for (int i = 0; i <= 7; ++i)
            {
                if (((registerList >> i) & 1) == 1)
                {
                    ref UInt32 regRi = ref Unsafe.Add(ref regDataRef, i);

                    cpu._callbackInterface.WriteMemory32(address, regRi);
                    address += 4;
                }
            }

            if (r == 1)
            {
                ref UInt32 regLR = ref Unsafe.Add(ref regDataRef, LR);

                cpu._callbackInterface.WriteMemory32(address, regLR);

                return (Byte)(n + 2);
            }
            else
            {
                return (Byte)(n + 1);
            }
        }

        private static Byte ROR(CPU cpu, UInt16 instruction)
        {
            UInt16 rs = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regRs = ref Unsafe.Add(ref regDataRef, rs);
            ref UInt32 regRd = ref Unsafe.Add(ref regDataRef, rd);

            if ((regRs & 0xff) == 0)
            {
                // nothing to do
            }
            else if ((regRs & 0b1_1111) == 0)
            {
                cpu.SetFlag(Flag.C, regRd >> 31);
            }
            else
            {
                cpu.SetFlag(Flag.C, (regRd >> (int)((regRs & 0b1_1111) - 1)) & 1);
                regRd = BitOperations.RotateRight(regRd, (int)(regRs & 0b1_1111));
            }

            cpu.SetFlag(Flag.N, regRd >> 31);
            cpu.SetFlag(Flag.Z, (regRd == 0) ? 1u : 0u);

            return 2;
        }

        private static Byte SBC(CPU cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regRm = ref Unsafe.Add(ref regDataRef, rm);
            ref UInt32 regRd = ref Unsafe.Add(ref regDataRef, rd);

            UInt32 leftOperand = regRd;
            UInt64 rightOperand = (UInt64)regRm + (UInt64)Not(cpu.GetFlag(Flag.C));

            regRd = leftOperand - (UInt32)rightOperand;

            cpu.SetFlag(Flag.N, regRd >> 31);
            cpu.SetFlag(Flag.Z, (regRd == 0) ? 1u : 0u);
            cpu.SetFlag(Flag.C, Not(BorrowFrom(leftOperand, rightOperand)));
            cpu.SetFlag(Flag.V, OverflowFrom_Subtraction(leftOperand, (UInt32)rightOperand, regRd));

            return 1;
        }

        private static Byte STMIA(CPU cpu, UInt16 instruction)
        {
            UInt16 rn = (UInt16)((instruction >> 8) & 0b111);
            UInt16 registerList = (UInt16)(instruction & 0xff);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regRn = ref Unsafe.Add(ref regDataRef, rn);

            UInt32 address = regRn;

            if (registerList == 0)
            {
                ref UInt32 regPC = ref Unsafe.Add(ref regDataRef, PC);

                regRn += 0x40;
                cpu._callbackInterface.WriteMemory32(address, regPC + 2);

                return 2;
            }
            else
            {
                UInt32 n = (UInt32)BitOperations.PopCount(registerList);
                UInt32 oldRegRn = regRn;
                regRn += n * 4;

                for (int i = 0; i <= 7; ++i)
                {
                    if (((registerList >> i) & 1) == 1)
                    {
                        if ((i == rn) && ((registerList & ~(0xff << i)) == 0))
                        {
                            cpu._callbackInterface.WriteMemory32(address, oldRegRn);
                        }
                        else
                        {
                            ref UInt32 regRi = ref Unsafe.Add(ref regDataRef, i);

                            cpu._callbackInterface.WriteMemory32(address, regRi);
                        }

                        address += 4;
                    }
                }

                return (Byte)(n + 1);
            }
        }

        private static Byte STR1(CPU cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)((instruction >> 6) & 0b1_1111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regRn = ref Unsafe.Add(ref regDataRef, rn);
            ref UInt32 regRd = ref Unsafe.Add(ref regDataRef, rd);

            UInt32 address = regRn + (imm * 4u);
            cpu._callbackInterface.WriteMemory32(address, regRd);

            return 2;
        }

        private static Byte STR2(CPU cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 6) & 0b111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regRm = ref Unsafe.Add(ref regDataRef, rm);
            ref UInt32 regRn = ref Unsafe.Add(ref regDataRef, rn);
            ref UInt32 regRd = ref Unsafe.Add(ref regDataRef, rd);

            UInt32 address = regRn + regRm;
            cpu._callbackInterface.WriteMemory32(address, regRd);

            return 2;
        }

        private static Byte STR3(CPU cpu, UInt16 instruction)
        {
            UInt16 rd = (UInt16)((instruction >> 8) & 0b111);
            UInt16 imm = (UInt16)(instruction & 0xff);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regRd = ref Unsafe.Add(ref regDataRef, rd);
            ref UInt32 regSP = ref Unsafe.Add(ref regDataRef, SP);

            UInt32 address = regSP + (imm * 4u);
            cpu._callbackInterface.WriteMemory32(address, regRd);

            return 2;
        }

        private static Byte STRB1(CPU cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)((instruction >> 6) & 0b1_1111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regRn = ref Unsafe.Add(ref regDataRef, rn);
            ref UInt32 regRd = ref Unsafe.Add(ref regDataRef, rd);

            UInt32 address = regRn + imm;
            cpu._callbackInterface.WriteMemory8(address, (Byte)regRd);

            return 2;
        }

        private static Byte STRB2(CPU cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 6) & 0b111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regRm = ref Unsafe.Add(ref regDataRef, rm);
            ref UInt32 regRn = ref Unsafe.Add(ref regDataRef, rn);
            ref UInt32 regRd = ref Unsafe.Add(ref regDataRef, rd);

            UInt32 address = regRn + regRm;
            cpu._callbackInterface.WriteMemory8(address, (Byte)regRd);

            return 2;
        }

        private static Byte STRH1(CPU cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)((instruction >> 6) & 0b1_1111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regRn = ref Unsafe.Add(ref regDataRef, rn);
            ref UInt32 regRd = ref Unsafe.Add(ref regDataRef, rd);

            UInt32 address = regRn + (imm * 2u);
            cpu._callbackInterface.WriteMemory16(address, (UInt16)regRd);

            return 2;
        }

        private static Byte STRH2(CPU cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 6) & 0b111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regRm = ref Unsafe.Add(ref regDataRef, rm);
            ref UInt32 regRn = ref Unsafe.Add(ref regDataRef, rn);
            ref UInt32 regRd = ref Unsafe.Add(ref regDataRef, rd);

            UInt32 address = regRn + regRm;
            cpu._callbackInterface.WriteMemory16(address, (UInt16)regRd);

            return 2;
        }

        private static Byte SUB1(CPU cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)((instruction >> 6) & 0b111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regRn = ref Unsafe.Add(ref regDataRef, rn);
            ref UInt32 regRd = ref Unsafe.Add(ref regDataRef, rd);

            UInt32 leftOperand = regRn;
            UInt32 rightOperand = imm;

            regRd = leftOperand - rightOperand;

            cpu.SetFlag(Flag.N, regRd >> 31);
            cpu.SetFlag(Flag.Z, (regRd == 0) ? 1u : 0u);
            cpu.SetFlag(Flag.C, Not(BorrowFrom(leftOperand, rightOperand)));
            cpu.SetFlag(Flag.V, OverflowFrom_Subtraction(leftOperand, rightOperand, regRd));

            return 1;
        }

        private static Byte SUB2(CPU cpu, UInt16 instruction)
        {
            UInt16 rd = (UInt16)((instruction >> 8) & 0b111);
            UInt16 imm = (UInt16)(instruction & 0xff);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regRd = ref Unsafe.Add(ref regDataRef, rd);

            UInt32 leftOperand = regRd;
            UInt32 rightOperand = imm;

            regRd = leftOperand - rightOperand;

            cpu.SetFlag(Flag.N, regRd >> 31);
            cpu.SetFlag(Flag.Z, (regRd == 0) ? 1u : 0u);
            cpu.SetFlag(Flag.C, Not(BorrowFrom(leftOperand, rightOperand)));
            cpu.SetFlag(Flag.V, OverflowFrom_Subtraction(leftOperand, rightOperand, regRd));

            return 1;
        }

        private static Byte SUB3(CPU cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 6) & 0b111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regRm = ref Unsafe.Add(ref regDataRef, rm);
            ref UInt32 regRn = ref Unsafe.Add(ref regDataRef, rn);
            ref UInt32 regRd = ref Unsafe.Add(ref regDataRef, rd);

            UInt32 leftOperand = regRn;
            UInt32 rightOperand = regRm;

            regRd = leftOperand - rightOperand;

            cpu.SetFlag(Flag.N, regRd >> 31);
            cpu.SetFlag(Flag.Z, (regRd == 0) ? 1u : 0u);
            cpu.SetFlag(Flag.C, Not(BorrowFrom(leftOperand, rightOperand)));
            cpu.SetFlag(Flag.V, OverflowFrom_Subtraction(leftOperand, rightOperand, regRd));

            return 1;
        }

        private static Byte SUB4(CPU cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)(instruction & 0x7f);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regSP = ref Unsafe.Add(ref regDataRef, SP);

            regSP -= (UInt32)imm << 2;

            return 1;
        }

        private static Byte SWI(CPU cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)(instruction & 0xff);

            cpu._callbackInterface.HandleSWI((UInt32)(imm << 16));

            return 3;
        }

        private static Byte TST(CPU cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rn = (UInt16)(instruction & 0b111);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regRm = ref Unsafe.Add(ref regDataRef, rm);
            ref UInt32 regRn = ref Unsafe.Add(ref regDataRef, rn);

            UInt32 aluOut = regRn & regRm;

            cpu.SetFlag(Flag.N, aluOut >> 31);
            cpu.SetFlag(Flag.Z, (aluOut == 0) ? 1u : 0u);

            return 1;
        }
    }
}
