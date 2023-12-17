using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Iris.CPU.CPU_Core;

namespace Iris.CPU
{
    internal sealed class THUMB_Interpreter
    {
        private readonly InstructionLUTEntry<UInt16>[] _instructionLUT = new InstructionLUTEntry<UInt16>[1 << 10];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static UInt16 InstructionLUTHash(UInt16 value)
        {
            return (UInt16)(value >> 6);
        }

        private unsafe void InitInstructionLUT()
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

        private readonly CPU_Core _cpu;

        internal THUMB_Interpreter(CPU_Core cpu)
        {
            _cpu = cpu;
            InitInstructionLUT();
        }

        internal UInt32 Step()
        {
            UInt16 instruction = _cpu._callbackInterface.Read16(_cpu.NextInstructionAddress);
            _cpu.NextInstructionAddress += 2;

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(_cpu.Reg);
            ref UInt32 regPC = ref Unsafe.Add(ref regDataRef, PC);

            regPC = _cpu.NextInstructionAddress + 2;

            ref InstructionLUTEntry<UInt16> instructionLUTDataRef = ref MemoryMarshal.GetArrayDataReference(_instructionLUT);
            ref InstructionLUTEntry<UInt16> instructionLUTEntry = ref Unsafe.Add(ref instructionLUTDataRef, InstructionLUTHash(instruction));

            unsafe
            {
                return instructionLUTEntry._handler(_cpu, instruction);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SetPC(CPU_Core cpu, UInt32 value)
        {
            cpu.NextInstructionAddress = value & 0xffff_fffe;
        }

        private static void SetReg(CPU_Core cpu, UInt32 i, UInt32 value)
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

        private static UInt32 UNKNOWN(CPU_Core cpu, UInt16 instruction)
        {
            throw new Exception(string.Format("Iris.CPU.THUMB_Interpreter: Unknown THUMB instruction 0x{0:x4} at address 0x{1:x8}", instruction, cpu.NextInstructionAddress - 2));
        }

        private static UInt32 ADC(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regRm = ref Unsafe.Add(ref regDataRef, rm);
            ref UInt32 regRd = ref Unsafe.Add(ref regDataRef, rd);

            UInt32 leftOperand = regRd;
            UInt32 rightOperand = regRm;

            UInt64 result = (UInt64)leftOperand + (UInt64)rightOperand + (UInt64)cpu.GetFlag(Flag.C);
            regRd = (UInt32)result;

            cpu.SetFlag(Flag.N, regRd >> 31);
            cpu.SetFlag(Flag.Z, (regRd == 0) ? 1u : 0u);
            cpu.SetFlag(Flag.C, CarryFrom(result));
            cpu.SetFlag(Flag.V, OverflowFrom_Addition(leftOperand, rightOperand, regRd));

            return 1;
        }

        private static UInt32 ADD1(CPU_Core cpu, UInt16 instruction)
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

        private static UInt32 ADD2(CPU_Core cpu, UInt16 instruction)
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

        private static UInt32 ADD3(CPU_Core cpu, UInt16 instruction)
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

        private static UInt32 ADD4(CPU_Core cpu, UInt16 instruction)
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

            return (rd == PC) ? 3u : 1u;
        }

        private static UInt32 ADD5(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 rd = (UInt16)((instruction >> 8) & 0b111);
            UInt16 imm = (UInt16)(instruction & 0xff);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regRd = ref Unsafe.Add(ref regDataRef, rd);
            ref UInt32 regPC = ref Unsafe.Add(ref regDataRef, PC);

            regRd = (regPC & 0xffff_fffc) + (imm * 4u);

            return 1;
        }

        private static UInt32 ADD6(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 rd = (UInt16)((instruction >> 8) & 0b111);
            UInt16 imm = (UInt16)(instruction & 0xff);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regRd = ref Unsafe.Add(ref regDataRef, rd);
            ref UInt32 regSP = ref Unsafe.Add(ref regDataRef, SP);

            regRd = regSP + (imm * 4u);

            return 1;
        }

        private static UInt32 ADD7(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)(instruction & 0x7f);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regSP = ref Unsafe.Add(ref regDataRef, SP);

            regSP += imm * 4u;

            return 1;
        }

        private static UInt32 AND(CPU_Core cpu, UInt16 instruction)
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

        private static UInt32 ASR1(CPU_Core cpu, UInt16 instruction)
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

        private static UInt32 ASR2(CPU_Core cpu, UInt16 instruction)
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

        private static UInt32 B1(CPU_Core cpu, UInt16 instruction)
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

        private static UInt32 B2(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)(instruction & 0x7ff);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regPC = ref Unsafe.Add(ref regDataRef, PC);

            SetPC(cpu, regPC + (SignExtend(imm, 11) << 1));

            return 3;
        }

        private static UInt32 BIC(CPU_Core cpu, UInt16 instruction)
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

        private static UInt32 BL(CPU_Core cpu, UInt16 instruction)
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

        private static UInt32 BX(CPU_Core cpu, UInt16 instruction)
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

        private static UInt32 CMN(CPU_Core cpu, UInt16 instruction)
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

        private static UInt32 CMP1(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 rn = (UInt16)((instruction >> 8) & 0b111);
            UInt16 imm = (UInt16)(instruction & 0xff);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regRn = ref Unsafe.Add(ref regDataRef, rn);

            UInt32 leftOperand = regRn;
            UInt32 rightOperand = imm;

            UInt64 result = (UInt64)leftOperand - (UInt64)rightOperand;
            UInt32 aluOut = (UInt32)result;

            cpu.SetFlag(Flag.N, aluOut >> 31);
            cpu.SetFlag(Flag.Z, (aluOut == 0) ? 1u : 0u);
            cpu.SetFlag(Flag.C, Not(BorrowFrom(result)));
            cpu.SetFlag(Flag.V, OverflowFrom_Subtraction(leftOperand, rightOperand, aluOut));

            return 1;
        }

        private static UInt32 CMP2(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rn = (UInt16)(instruction & 0b111);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regRm = ref Unsafe.Add(ref regDataRef, rm);
            ref UInt32 regRn = ref Unsafe.Add(ref regDataRef, rn);

            UInt32 leftOperand = regRn;
            UInt32 rightOperand = regRm;

            UInt64 result = (UInt64)leftOperand - (UInt64)rightOperand;
            UInt32 aluOut = (UInt32)result;

            cpu.SetFlag(Flag.N, aluOut >> 31);
            cpu.SetFlag(Flag.Z, (aluOut == 0) ? 1u : 0u);
            cpu.SetFlag(Flag.C, Not(BorrowFrom(result)));
            cpu.SetFlag(Flag.V, OverflowFrom_Subtraction(leftOperand, rightOperand, aluOut));

            return 1;
        }

        private static UInt32 CMP3(CPU_Core cpu, UInt16 instruction)
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

            UInt64 result = (UInt64)leftOperand - (UInt64)rightOperand;
            UInt32 aluOut = (UInt32)result;

            cpu.SetFlag(Flag.N, aluOut >> 31);
            cpu.SetFlag(Flag.Z, (aluOut == 0) ? 1u : 0u);
            cpu.SetFlag(Flag.C, Not(BorrowFrom(result)));
            cpu.SetFlag(Flag.V, OverflowFrom_Subtraction(leftOperand, rightOperand, aluOut));

            return 1;
        }

        private static UInt32 EOR(CPU_Core cpu, UInt16 instruction)
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

        private static UInt32 LDMIA(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 rn = (UInt16)((instruction >> 8) & 0b111);
            UInt16 registerList = (UInt16)(instruction & 0xff);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regRn = ref Unsafe.Add(ref regDataRef, rn);

            UInt32 address = regRn;

            if (registerList == 0)
            {
                regRn += 0x40;
                SetPC(cpu, cpu._callbackInterface.Read32(address));

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

                        regRi = cpu._callbackInterface.Read32(address);
                        address += 4;
                    }
                }

                return n + 2;
            }
        }

        private static UInt32 LDR1(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)((instruction >> 6) & 0b1_1111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regRn = ref Unsafe.Add(ref regDataRef, rn);
            ref UInt32 regRd = ref Unsafe.Add(ref regDataRef, rd);

            UInt32 address = regRn + (imm * 4u);
            UInt32 data = BitOperations.RotateRight(cpu._callbackInterface.Read32(address), (int)(8 * (address & 0b11)));
            regRd = data;

            return 3;
        }

        private static UInt32 LDR2(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 6) & 0b111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regRm = ref Unsafe.Add(ref regDataRef, rm);
            ref UInt32 regRn = ref Unsafe.Add(ref regDataRef, rn);
            ref UInt32 regRd = ref Unsafe.Add(ref regDataRef, rd);

            UInt32 address = regRn + regRm;
            UInt32 data = BitOperations.RotateRight(cpu._callbackInterface.Read32(address), (int)(8 * (address & 0b11)));
            regRd = data;

            return 3;
        }

        private static UInt32 LDR3(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 rd = (UInt16)((instruction >> 8) & 0b111);
            UInt16 imm = (UInt16)(instruction & 0xff);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regRd = ref Unsafe.Add(ref regDataRef, rd);
            ref UInt32 regPC = ref Unsafe.Add(ref regDataRef, PC);

            UInt32 address = regPC + (imm * 4u);
            UInt32 data = cpu._callbackInterface.Read32(address);
            regRd = data;

            return 3;
        }

        private static UInt32 LDR4(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 rd = (UInt16)((instruction >> 8) & 0b111);
            UInt16 imm = (UInt16)(instruction & 0xff);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regRd = ref Unsafe.Add(ref regDataRef, rd);
            ref UInt32 regSP = ref Unsafe.Add(ref regDataRef, SP);

            UInt32 address = regSP + (imm * 4u);
            UInt32 data = BitOperations.RotateRight(cpu._callbackInterface.Read32(address), (int)(8 * (address & 0b11)));
            regRd = data;

            return 3;
        }

        private static UInt32 LDRB1(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)((instruction >> 6) & 0b1_1111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regRn = ref Unsafe.Add(ref regDataRef, rn);
            ref UInt32 regRd = ref Unsafe.Add(ref regDataRef, rd);

            UInt32 address = regRn + imm;
            Byte data = cpu._callbackInterface.Read8(address);
            regRd = data;

            return 3;
        }

        private static UInt32 LDRB2(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 6) & 0b111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regRm = ref Unsafe.Add(ref regDataRef, rm);
            ref UInt32 regRn = ref Unsafe.Add(ref regDataRef, rn);
            ref UInt32 regRd = ref Unsafe.Add(ref regDataRef, rd);

            UInt32 address = regRn + regRm;
            Byte data = cpu._callbackInterface.Read8(address);
            regRd = data;

            return 3;
        }

        private static UInt32 LDRH1(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)((instruction >> 6) & 0b1_1111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regRn = ref Unsafe.Add(ref regDataRef, rn);
            ref UInt32 regRd = ref Unsafe.Add(ref regDataRef, rd);

            UInt32 address = regRn + (imm * 2u);
            UInt32 data = BitOperations.RotateRight(cpu._callbackInterface.Read16(address), (int)(8 * (address & 1)));
            regRd = data;

            return 3;
        }

        private static UInt32 LDRH2(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 6) & 0b111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regRm = ref Unsafe.Add(ref regDataRef, rm);
            ref UInt32 regRn = ref Unsafe.Add(ref regDataRef, rn);
            ref UInt32 regRd = ref Unsafe.Add(ref regDataRef, rd);

            UInt32 address = regRn + regRm;
            UInt32 data = BitOperations.RotateRight(cpu._callbackInterface.Read16(address), (int)(8 * (address & 1)));
            regRd = data;

            return 3;
        }

        private static UInt32 LDRSB(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 6) & 0b111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regRm = ref Unsafe.Add(ref regDataRef, rm);
            ref UInt32 regRn = ref Unsafe.Add(ref regDataRef, rn);
            ref UInt32 regRd = ref Unsafe.Add(ref regDataRef, rd);

            UInt32 address = regRn + regRm;
            Byte data = cpu._callbackInterface.Read8(address);
            regRd = SignExtend(data, 8);

            return 3;
        }

        private static UInt32 LDRSH(CPU_Core cpu, UInt16 instruction)
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
                Byte data = cpu._callbackInterface.Read8(address);
                regRd = SignExtend(data, 8);
            }
            else
            {
                UInt16 data = cpu._callbackInterface.Read16(address);
                regRd = SignExtend(data, 16);
            }

            return 3;
        }

        private static UInt32 LSL1(CPU_Core cpu, UInt16 instruction)
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

        private static UInt32 LSL2(CPU_Core cpu, UInt16 instruction)
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

        private static UInt32 LSR1(CPU_Core cpu, UInt16 instruction)
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

        private static UInt32 LSR2(CPU_Core cpu, UInt16 instruction)
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

        private static UInt32 MOV1(CPU_Core cpu, UInt16 instruction)
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

        private static UInt32 MOV3(CPU_Core cpu, UInt16 instruction)
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

            return (rd == PC) ? 3u : 1u;
        }

        private static UInt32 MUL(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regRm = ref Unsafe.Add(ref regDataRef, rm);
            ref UInt32 regRd = ref Unsafe.Add(ref regDataRef, rd);

            UInt32 m = ComputeMultiplicationCycleCount(regRd, regRm);
            regRd *= regRm;

            cpu.SetFlag(Flag.N, regRd >> 31);
            cpu.SetFlag(Flag.Z, (regRd == 0) ? 1u : 0u);

            return m + 1;
        }

        private static UInt32 MVN(CPU_Core cpu, UInt16 instruction)
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

        private static UInt32 NEG(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regRm = ref Unsafe.Add(ref regDataRef, rm);
            ref UInt32 regRd = ref Unsafe.Add(ref regDataRef, rd);

            UInt32 leftOperand = 0;
            UInt32 rightOperand = regRm;

            UInt64 result = (UInt64)leftOperand - (UInt64)rightOperand;
            regRd = (UInt32)result;

            cpu.SetFlag(Flag.N, regRd >> 31);
            cpu.SetFlag(Flag.Z, (regRd == 0) ? 1u : 0u);
            cpu.SetFlag(Flag.C, Not(BorrowFrom(result)));
            cpu.SetFlag(Flag.V, OverflowFrom_Subtraction(leftOperand, rightOperand, regRd));

            return 1;
        }

        private static UInt32 ORR(CPU_Core cpu, UInt16 instruction)
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

        private static UInt32 POP(CPU_Core cpu, UInt16 instruction)
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

                    regRi = cpu._callbackInterface.Read32(address);
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

        private static UInt32 PUSH(CPU_Core cpu, UInt16 instruction)
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

                    cpu._callbackInterface.Write32(address, regRi);
                    address += 4;
                }
            }

            if (r == 1)
            {
                ref UInt32 regLR = ref Unsafe.Add(ref regDataRef, LR);

                cpu._callbackInterface.Write32(address, regLR);

                return n + 2;
            }
            else
            {
                return n + 1;
            }
        }

        private static UInt32 ROR(CPU_Core cpu, UInt16 instruction)
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

        private static UInt32 SBC(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regRm = ref Unsafe.Add(ref regDataRef, rm);
            ref UInt32 regRd = ref Unsafe.Add(ref regDataRef, rd);

            UInt32 leftOperand = regRd;
            UInt32 rightOperand = regRm;

            UInt64 result = (UInt64)leftOperand - (UInt64)rightOperand - (UInt64)Not(cpu.GetFlag(Flag.C));
            regRd = (UInt32)result;

            cpu.SetFlag(Flag.N, regRd >> 31);
            cpu.SetFlag(Flag.Z, (regRd == 0) ? 1u : 0u);
            cpu.SetFlag(Flag.C, Not(BorrowFrom(result)));
            cpu.SetFlag(Flag.V, OverflowFrom_Subtraction(leftOperand, rightOperand, regRd));

            return 1;
        }

        private static UInt32 STMIA(CPU_Core cpu, UInt16 instruction)
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
                cpu._callbackInterface.Write32(address, regPC + 2);

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
                            cpu._callbackInterface.Write32(address, oldRegRn);
                        }
                        else
                        {
                            ref UInt32 regRi = ref Unsafe.Add(ref regDataRef, i);

                            cpu._callbackInterface.Write32(address, regRi);
                        }

                        address += 4;
                    }
                }

                return n + 1;
            }
        }

        private static UInt32 STR1(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)((instruction >> 6) & 0b1_1111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regRn = ref Unsafe.Add(ref regDataRef, rn);
            ref UInt32 regRd = ref Unsafe.Add(ref regDataRef, rd);

            UInt32 address = regRn + (imm * 4u);
            cpu._callbackInterface.Write32(address, regRd);

            return 2;
        }

        private static UInt32 STR2(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 6) & 0b111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regRm = ref Unsafe.Add(ref regDataRef, rm);
            ref UInt32 regRn = ref Unsafe.Add(ref regDataRef, rn);
            ref UInt32 regRd = ref Unsafe.Add(ref regDataRef, rd);

            UInt32 address = regRn + regRm;
            cpu._callbackInterface.Write32(address, regRd);

            return 2;
        }

        private static UInt32 STR3(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 rd = (UInt16)((instruction >> 8) & 0b111);
            UInt16 imm = (UInt16)(instruction & 0xff);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regRd = ref Unsafe.Add(ref regDataRef, rd);
            ref UInt32 regSP = ref Unsafe.Add(ref regDataRef, SP);

            UInt32 address = regSP + (imm * 4u);
            cpu._callbackInterface.Write32(address, regRd);

            return 2;
        }

        private static UInt32 STRB1(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)((instruction >> 6) & 0b1_1111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regRn = ref Unsafe.Add(ref regDataRef, rn);
            ref UInt32 regRd = ref Unsafe.Add(ref regDataRef, rd);

            UInt32 address = regRn + imm;
            cpu._callbackInterface.Write8(address, (Byte)regRd);

            return 2;
        }

        private static UInt32 STRB2(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 6) & 0b111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regRm = ref Unsafe.Add(ref regDataRef, rm);
            ref UInt32 regRn = ref Unsafe.Add(ref regDataRef, rn);
            ref UInt32 regRd = ref Unsafe.Add(ref regDataRef, rd);

            UInt32 address = regRn + regRm;
            cpu._callbackInterface.Write8(address, (Byte)regRd);

            return 2;
        }

        private static UInt32 STRH1(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)((instruction >> 6) & 0b1_1111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regRn = ref Unsafe.Add(ref regDataRef, rn);
            ref UInt32 regRd = ref Unsafe.Add(ref regDataRef, rd);

            UInt32 address = regRn + (imm * 2u);
            cpu._callbackInterface.Write16(address, (UInt16)regRd);

            return 2;
        }

        private static UInt32 STRH2(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 6) & 0b111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regRm = ref Unsafe.Add(ref regDataRef, rm);
            ref UInt32 regRn = ref Unsafe.Add(ref regDataRef, rn);
            ref UInt32 regRd = ref Unsafe.Add(ref regDataRef, rd);

            UInt32 address = regRn + regRm;
            cpu._callbackInterface.Write16(address, (UInt16)regRd);

            return 2;
        }

        private static UInt32 SUB1(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)((instruction >> 6) & 0b111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regRn = ref Unsafe.Add(ref regDataRef, rn);
            ref UInt32 regRd = ref Unsafe.Add(ref regDataRef, rd);

            UInt32 leftOperand = regRn;
            UInt32 rightOperand = imm;

            UInt64 result = (UInt64)leftOperand - (UInt64)rightOperand;
            regRd = (UInt32)result;

            cpu.SetFlag(Flag.N, regRd >> 31);
            cpu.SetFlag(Flag.Z, (regRd == 0) ? 1u : 0u);
            cpu.SetFlag(Flag.C, Not(BorrowFrom(result)));
            cpu.SetFlag(Flag.V, OverflowFrom_Subtraction(leftOperand, rightOperand, regRd));

            return 1;
        }

        private static UInt32 SUB2(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 rd = (UInt16)((instruction >> 8) & 0b111);
            UInt16 imm = (UInt16)(instruction & 0xff);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regRd = ref Unsafe.Add(ref regDataRef, rd);

            UInt32 leftOperand = regRd;
            UInt32 rightOperand = imm;

            UInt64 result = (UInt64)leftOperand - (UInt64)rightOperand;
            regRd = (UInt32)result;

            cpu.SetFlag(Flag.N, regRd >> 31);
            cpu.SetFlag(Flag.Z, (regRd == 0) ? 1u : 0u);
            cpu.SetFlag(Flag.C, Not(BorrowFrom(result)));
            cpu.SetFlag(Flag.V, OverflowFrom_Subtraction(leftOperand, rightOperand, regRd));

            return 1;
        }

        private static UInt32 SUB3(CPU_Core cpu, UInt16 instruction)
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

            UInt64 result = (UInt64)leftOperand - (UInt64)rightOperand;
            regRd = (UInt32)result;

            cpu.SetFlag(Flag.N, regRd >> 31);
            cpu.SetFlag(Flag.Z, (regRd == 0) ? 1u : 0u);
            cpu.SetFlag(Flag.C, Not(BorrowFrom(result)));
            cpu.SetFlag(Flag.V, OverflowFrom_Subtraction(leftOperand, rightOperand, regRd));

            return 1;
        }

        private static UInt32 SUB4(CPU_Core cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)(instruction & 0x7f);

            ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(cpu.Reg);
            ref UInt32 regSP = ref Unsafe.Add(ref regDataRef, SP);

            regSP -= (UInt32)imm << 2;

            return 1;
        }

        private static UInt32 SWI(CPU_Core cpu, UInt16 instruction)
        {
            return cpu._callbackInterface.HandleSWI();
        }

        private static UInt32 TST(CPU_Core cpu, UInt16 instruction)
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
