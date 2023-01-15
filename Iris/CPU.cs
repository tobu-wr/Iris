using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Iris
{
    internal class CPU
    {
        internal interface ICallbacks
        {
            Byte ReadMemory8(UInt32 address);
            UInt16 ReadMemory16(UInt32 address);
            UInt32 ReadMemory32(UInt32 address);
            void WriteMemory8(UInt32 address, Byte value);
            void WriteMemory16(UInt32 address, UInt16 value);
            void WriteMemory32(UInt32 address, UInt32 value);
            void HandleSWI(UInt32 value);
        }

        private readonly struct ARM_InstructionTableEntry
        {
            internal delegate void InstructionHandler(CPU cpu, UInt32 instruction);

            internal readonly UInt32 Mask;
            internal readonly UInt32 Expected;
            internal readonly InstructionHandler Handler;

            internal ARM_InstructionTableEntry(UInt32 mask, UInt32 expected, InstructionHandler handler)
            {
                Mask = mask;
                Expected = expected;
                Handler = handler;
            }
        }

        private static readonly ARM_InstructionTableEntry[] ARM_InstructionTable = new ARM_InstructionTableEntry[]
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

        private readonly struct THUMB_InstructionTableEntry
        {
            internal delegate void InstructionHandler(CPU cpu, UInt16 instruction);

            internal readonly UInt16 Mask;
            internal readonly UInt16 Expected;
            internal readonly InstructionHandler Handler;

            internal THUMB_InstructionTableEntry(UInt16 mask, UInt16 expected, InstructionHandler handler)
            {
                Mask = mask;
                Expected = expected;
                Handler = handler;
            }
        }

        private static readonly THUMB_InstructionTableEntry[] THUMB_InstructionTable = new THUMB_InstructionTableEntry[]
        {
            // ADC
            // new(0xffc0, 0x4140, THUMB_ADC),

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

            // B
            new(0xf000, 0xd000, THUMB_B1),
            new(0xf800, 0xe000, THUMB_B2),

            // BIC
            new(0xffc0, 0x4380, THUMB_BIC),

            // BL
            new(0xf000, 0xf000, THUMB_BL),

            // BX
            new(0xff87, 0x4700, THUMB_BX),

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

            // STMIA
            new(0xf800, 0xc000, THUMB_STMIA),

            // STR
            new(0xf800, 0x6000, THUMB_STR1),
            new(0xf800, 0x9000, THUMB_STR3),

            // STRB
            new(0xf800, 0x7000, THUMB_STRB1),

            // STRH
            new(0xf800, 0x8000, THUMB_STRH1),

            // SUB
            new(0xfe00, 0x1e00, THUMB_SUB1),
            new(0xf800, 0x3800, THUMB_SUB2),
            new(0xfe00, 0x1a00, THUMB_SUB3),
            new(0xff80, 0xb080, THUMB_SUB4),

            // TST
            new(0xffc0, 0x4200, THUMB_TST),
        };

        private enum Flags
        {
            V = 28,
            C = 29,
            Z = 30,
            N = 31
        };

        private const UInt32 ModeMask = 0b1_1111;
        private const UInt32 UserMode = 0b1_0000;
        private const UInt32 SystemMode = 0b1_1111;
        private const UInt32 SupervisorMode = 0b1_0011;
        private const UInt32 AbortMode = 0b1_0111;
        private const UInt32 UndefinedMode = 0b1_1011;
        private const UInt32 InterruptMode = 0b1_0010;
        private const UInt32 FastInterruptMode = 0b1_0001;

        private const UInt32 SP = 13;
        private const UInt32 LR = 14;
        private const UInt32 PC = 15;

        // exposed registers
        internal readonly UInt32[] _reg = new UInt32[16];
        internal UInt32 _cpsr;
        private UInt32 _spsr;

        // banked registers
        private UInt32 _reg8, _reg9, _reg10, _reg11, _reg12, _reg13, _reg14;
        private UInt32 _reg13_svc, _reg14_svc;
        private UInt32 _reg13_abt, _reg14_abt;
        private UInt32 _reg13_und, _reg14_und;
        private UInt32 _reg13_irq, _reg14_irq;
        private UInt32 _reg8_fiq, _reg9_fiq, _reg10_fiq, _reg11_fiq, _reg12_fiq, _reg13_fiq, _reg14_fiq;
        private UInt32 _spsr_svc, _spsr_abt, _spsr_und, _spsr_irq, _spsr_fiq;

        private readonly ICallbacks _callbacks;
        internal UInt32 _nextInstructionAddress;

        internal CPU(ICallbacks callbacks)
        {
            _callbacks = callbacks;
        }

        internal void Step()
        {
            // ARM state
            if (((_cpsr >> 5) & 1) == 0)
            {
                UInt32 instruction = _callbacks.ReadMemory32(_nextInstructionAddress);
                _nextInstructionAddress += 4;

                UInt32 cond = (instruction >> 28) & 0b1111;
                if (ConditionPassed(cond))
                {
                    _reg[PC] = _nextInstructionAddress + 4;

                    foreach (ARM_InstructionTableEntry entry in ARM_InstructionTable)
                    {
                        if ((instruction & entry.Mask) == entry.Expected)
                        {
                            entry.Handler(this, instruction);
                            return;
                        }
                    }

                    throw new Exception(string.Format("CPU: Unknown ARM instruction 0x{0:x8} at address 0x{1:x8}", instruction, _nextInstructionAddress - 4));
                }
            }

            // THUMB state
            else
            {
                UInt16 instruction = _callbacks.ReadMemory16(_nextInstructionAddress);
                _nextInstructionAddress += 2;
                _reg[PC] = _nextInstructionAddress + 2;

                foreach (THUMB_InstructionTableEntry entry in THUMB_InstructionTable)
                {
                    if ((instruction & entry.Mask) == entry.Expected)
                    {
                        entry.Handler(this, instruction);
                        return;
                    }
                }

                throw new Exception(string.Format("CPU: Unknown THUMB instruction 0x{0:x4} at address 0x{1:x8}", instruction, _nextInstructionAddress - 2));
            }
        }

        private void SetPC(UInt32 value)
        {
            _reg[PC] = value;
            _nextInstructionAddress = value;
        }

        private void SetReg(UInt32 i, UInt32 value)
        {
            _reg[i] = value;

            if (i == PC)
                _nextInstructionAddress = value;
        }

        private void SetCPSR(UInt32 value)
        {
            UInt32 previousMode = _cpsr & ModeMask;
            UInt32 newMode = value & ModeMask;

            _cpsr = value;

            if (previousMode != newMode)
            {
                // save previous mode registers
                switch (previousMode)
                {
                    case UserMode:
                    case SystemMode:
                        _reg8 = _reg[8];
                        _reg9 = _reg[9];
                        _reg10 = _reg[10];
                        _reg11 = _reg[11];
                        _reg12 = _reg[12];
                        _reg13 = _reg[13];
                        _reg14 = _reg[14];
                        break;
                    case SupervisorMode:
                        _reg8 = _reg[8];
                        _reg9 = _reg[9];
                        _reg10 = _reg[10];
                        _reg11 = _reg[11];
                        _reg12 = _reg[12];
                        _reg13_svc = _reg[13];
                        _reg14_svc = _reg[14];
                        _spsr_svc = _spsr;
                        break;
                    case AbortMode:
                        _reg8 = _reg[8];
                        _reg9 = _reg[9];
                        _reg10 = _reg[10];
                        _reg11 = _reg[11];
                        _reg12 = _reg[12];
                        _reg13_abt = _reg[13];
                        _reg14_abt = _reg[14];
                        _spsr_abt = _spsr;
                        break;
                    case UndefinedMode:
                        _reg8 = _reg[8];
                        _reg9 = _reg[9];
                        _reg10 = _reg[10];
                        _reg11 = _reg[11];
                        _reg12 = _reg[12];
                        _reg13_und = _reg[13];
                        _reg14_und = _reg[14];
                        _spsr_und = _spsr;
                        break;
                    case InterruptMode:
                        _reg8 = _reg[8];
                        _reg9 = _reg[9];
                        _reg10 = _reg[10];
                        _reg11 = _reg[11];
                        _reg12 = _reg[12];
                        _reg13_irq = _reg[13];
                        _reg14_irq = _reg[14];
                        _spsr_irq = _spsr;
                        break;
                    case FastInterruptMode:
                        _reg8_fiq = _reg[8];
                        _reg9_fiq = _reg[9];
                        _reg10_fiq = _reg[10];
                        _reg11_fiq = _reg[11];
                        _reg12_fiq = _reg[12];
                        _reg13_fiq = _reg[13];
                        _reg14_fiq = _reg[14];
                        _spsr_fiq = _spsr;
                        break;
                }

                // load new mode registers
                switch (newMode)
                {
                    case UserMode:
                    case SystemMode:
                        _reg[8] = _reg8;
                        _reg[9] = _reg9;
                        _reg[10] = _reg10;
                        _reg[11] = _reg11;
                        _reg[12] = _reg12;
                        _reg[13] = _reg13;
                        _reg[14] = _reg14;
                        break;
                    case SupervisorMode:
                        _reg[8] = _reg8;
                        _reg[9] = _reg9;
                        _reg[10] = _reg10;
                        _reg[11] = _reg11;
                        _reg[12] = _reg12;
                        _reg[13] = _reg13_svc;
                        _reg[14] = _reg14_svc;
                        _spsr = _spsr_svc;
                        break;
                    case AbortMode:
                        _reg[8] = _reg8;
                        _reg[9] = _reg9;
                        _reg[10] = _reg10;
                        _reg[11] = _reg11;
                        _reg[12] = _reg12;
                        _reg[13] = _reg13_abt;
                        _reg[14] = _reg14_abt;
                        _spsr = _spsr_abt;
                        break;
                    case UndefinedMode:
                        _reg[8] = _reg8;
                        _reg[9] = _reg9;
                        _reg[10] = _reg10;
                        _reg[11] = _reg11;
                        _reg[12] = _reg12;
                        _reg[13] = _reg13_und;
                        _reg[14] = _reg14_und;
                        _spsr = _spsr_und;
                        break;
                    case InterruptMode:
                        _reg[8] = _reg8;
                        _reg[9] = _reg9;
                        _reg[10] = _reg10;
                        _reg[11] = _reg11;
                        _reg[12] = _reg12;
                        _reg[13] = _reg13_irq;
                        _reg[14] = _reg14_irq;
                        _spsr = _spsr_irq;
                        break;
                    case FastInterruptMode:
                        _reg[8] = _reg8_fiq;
                        _reg[9] = _reg9_fiq;
                        _reg[10] = _reg10_fiq;
                        _reg[11] = _reg11_fiq;
                        _reg[12] = _reg12_fiq;
                        _reg[13] = _reg13_fiq;
                        _reg[14] = _reg14_fiq;
                        _spsr = _spsr_fiq;
                        break;
                }
            }
        }

        private UInt32 GetFlag(Flags flag)
        {
            return (_cpsr >> (int)flag) & 1;
        }

        private void SetFlag(Flags flag, UInt32 value)
        {
            _cpsr = (_cpsr & ~(1u << (int)flag)) | (value << (int)flag);
        }

        private bool ConditionPassed(UInt32 cond)
        {
            return cond switch
            {
                // EQ
                0b0000 => GetFlag(Flags.Z) == 1,
                // NE
                0b0001 => GetFlag(Flags.Z) == 0,
                // CS/HS
                0b0010 => GetFlag(Flags.C) == 1,
                // CC/LO
                0b0011 => GetFlag(Flags.C) == 0,
                // MI
                0b0100 => GetFlag(Flags.N) == 1,
                // PL
                0b0101 => GetFlag(Flags.N) == 0,
                // VS
                0b0110 => GetFlag(Flags.V) == 1,
                // VC
                0b0111 => GetFlag(Flags.V) == 0,
                // HI
                0b1000 => (GetFlag(Flags.C) == 1) && (GetFlag(Flags.Z) == 0),
                // LS
                0b1001 => (GetFlag(Flags.C) == 0) || (GetFlag(Flags.Z) == 1),
                // GE
                0b1010 => GetFlag(Flags.N) == GetFlag(Flags.V),
                // LT
                0b1011 => GetFlag(Flags.N) != GetFlag(Flags.V),
                // GT
                0b1100 => (GetFlag(Flags.Z) == 0) && (GetFlag(Flags.N) == GetFlag(Flags.V)),
                // LE
                0b1101 => (GetFlag(Flags.Z) == 1) || (GetFlag(Flags.N) != GetFlag(Flags.V)),
                // AL
                0b1110 => true,
                // UNPREDICTABLE
                0b1111 => true,
                // should never happen
                _ => throw new NotImplementedException(),
            };
        }

        private static UInt32 CarryFrom(UInt64 result)
        {
            return (result > 0xffff_ffff) ? 1u : 0u;
        }

        private static UInt32 BorrowFrom(UInt32 leftOperand, UInt32 rightOperand)
        {
            return (leftOperand < rightOperand) ? 1u : 0u;
        }

        private static UInt32 OverflowFrom_Addition(UInt32 leftOperand, UInt32 rightOperand, UInt32 result)
        {
            return (((leftOperand >> 31) == (rightOperand >> 31)) && ((leftOperand >> 31) != (result >> 31))) ? 1u : 0u;
        }

        private static UInt32 OverflowFrom_Subtraction(UInt32 leftOperand, UInt32 rightOperand, UInt32 result)
        {
            return (((leftOperand >> 31) != (rightOperand >> 31)) && ((leftOperand >> 31) != (result >> 31))) ? 1u : 0u;
        }

        private static UInt32 RotateRight(UInt32 value, UInt32 rotateAmount)
        {
            return (value >> ((int)rotateAmount & 0x1f)) | (value << (32 - ((int)rotateAmount & 0x1f)));
        }

        private static UInt32 LogicalShiftLeft(UInt32 value, UInt32 shiftAmount)
        {
            return value << (int)shiftAmount;
        }

        private static UInt32 LogicalShiftRight(UInt32 value, UInt32 shiftAmount)
        {
            return value >> (int)shiftAmount;
        }

        private static UInt32 ArithmeticShiftRight(UInt32 value, UInt32 shiftAmount)
        {
            return (UInt32)((Int32)value >> (int)shiftAmount);
        }

        private static UInt32 ZeroExtend(UInt16 value)
        {
            return value;
        }

        private static UInt32 SignExtend(UInt32 value, int size)
        {
            return ((value >> (size - 1)) == 1) ? value | (0xffff_ffff << size) : value;
        }

        private static UInt32 SignExtend30(UInt32 value)
        {
            return SignExtend(value, 24) & 0x3fff_ffff;
        }

        private static UInt32 NumberOfSetBitsIn(UInt32 value, int size)
        {
            UInt32 count = 0;
            for (var i = 0; i < size; ++i)
                count += (value >> i) & 1;
            return count;
        }

        // ********************************************************************
        //                               ARM
        // ********************************************************************

        private UInt32 GetMode()
        {
            return _cpsr & ModeMask;
        }

        private bool InAPrivilegedMode()
        {
            return GetMode() != UserMode;
        }

        private bool CurrentModeHasSPSR()
        {
            return GetMode() switch
            {
                UserMode or SystemMode => false,
                _ => true,
            };
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
                                shifterOperand = _reg[rm];
                                shifterCarryOut = GetFlag(Flags.C);
                            }
                            else
                            {
                                shifterOperand = LogicalShiftLeft(_reg[rm], shiftImm);
                                shifterCarryOut = (_reg[rm] >> (32 - (int)shiftImm)) & 1;
                            }
                            break;
                        case 0b01: // Logical shift right
                            if (shiftImm == 0)
                            {
                                shifterOperand = 0;
                                shifterCarryOut = _reg[rm] >> 31;
                            }
                            else
                            {
                                shifterOperand = LogicalShiftRight(_reg[rm], shiftImm);
                                shifterCarryOut = (_reg[rm] >> ((int)shiftImm - 1)) & 1;
                            }
                            break;
                        case 0b10: // Arithmetic shift right
                            if (shiftImm == 0)
                            {
                                if ((_reg[rm] >> 31) == 0)
                                    shifterOperand = 0;
                                else
                                    shifterOperand = 0xffff_ffff;

                                shifterCarryOut = _reg[rm] >> 31;
                            }
                            else
                            {
                                shifterOperand = ArithmeticShiftRight(_reg[rm], shiftImm);
                                shifterCarryOut = (_reg[rm] >> ((int)shiftImm - 1)) & 1;
                            }
                            break;
                        case 0b11: // Rotate right
                            if (shiftImm == 0)
                            {
                                shifterOperand = LogicalShiftLeft(GetFlag(Flags.C), 31) | LogicalShiftRight(_reg[rm], 1);
                                shifterCarryOut = _reg[rm] & 1;
                            }
                            else
                            {
                                shifterOperand = RotateRight(_reg[rm], shiftImm);
                                shifterCarryOut = (_reg[rm] >> ((int)shiftImm - 1)) & 1;
                            }
                            break;
                    }
                }
                else if (((instruction >> 7) & 1) == 0) // Register shifts
                {
                    UInt32 rs = (instruction >> 8) & 0b1111;
                    UInt32 regRm = (rm == PC) ? _reg[PC] + 4 : _reg[rm];
                    UInt32 regRs = _reg[rs] & 0xff;
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
                                shifterOperand = LogicalShiftLeft(regRm, regRs);
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
                                shifterOperand = LogicalShiftRight(regRm, regRs);
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
                    address = _reg[rn];

                    if (u == 1)
                        SetReg(rn, _reg[rn] + offset);
                    else
                        SetReg(rn, _reg[rn] - offset);
                }
                else if (w == 0) // Offset
                {
                    if (u == 1)
                        address = _reg[rn] + offset;
                    else
                        address = _reg[rn] - offset;
                }
                else // Pre-indexed
                {
                    if (u == 1)
                        address = _reg[rn] + offset;
                    else
                        address = _reg[rn] - offset;

                    SetReg(rn, address);
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
                        address = _reg[rn];

                        if (u == 1)
                            SetReg(rn, _reg[rn] + _reg[rm]);
                        else
                            SetReg(rn, _reg[rn] - _reg[rm]);
                    }
                    else if (w == 0) // Offset
                    {
                        if (u == 1)
                            address = _reg[rn] + _reg[rm];
                        else
                            address = _reg[rn] - _reg[rm];
                    }
                    else // Pre-indexed
                    {
                        if (u == 1)
                            address = _reg[rn] + _reg[rm];
                        else
                            address = _reg[rn] - _reg[rm];

                        SetReg(rn, address);
                    }
                }
                else // Scaled register
                {
                    if (p == 0) // Post-indexed
                    {
                        address = _reg[rn];

                        UInt32 index = 0;
                        switch (shift)
                        {
                            case 0b00: // LSL
                                index = LogicalShiftLeft(_reg[rm], shiftImm);
                                break;
                            case 0b01: // LSR
                                if (shiftImm == 0)
                                    index = 0;
                                else
                                    index = LogicalShiftRight(_reg[rm], shiftImm);
                                break;
                            case 0b10: // ASR
                                if (shiftImm == 0)
                                {
                                    if ((_reg[rm] >> 31) == 1)
                                        index = 0xffff_ffff;
                                    else
                                        index = 0;
                                }
                                else
                                {
                                    index = ArithmeticShiftRight(_reg[rm], shiftImm);
                                }
                                break;
                            case 0b11:
                                if (shiftImm == 0) // RRX
                                    index = LogicalShiftLeft(GetFlag(Flags.C), 31) | LogicalShiftRight(_reg[rm], 1);
                                else // ROR
                                    index = RotateRight(_reg[rm], shiftImm);
                                break;
                        }

                        if (u == 1)
                            SetReg(rn, _reg[rn] + index);
                        else
                            SetReg(rn, _reg[rn] - index);
                    }
                    else if (w == 0) // Offset
                    {
                        UInt32 index = 0;
                        switch (shift)
                        {
                            case 0b00: // LSL
                                index = LogicalShiftLeft(_reg[rm], shiftImm);
                                break;
                            case 0b01: // LSR
                                if (shiftImm == 0)
                                    index = 0;
                                else
                                    index = LogicalShiftRight(_reg[rm], shiftImm);
                                break;
                            case 0b10: // ASR
                                if (shiftImm == 0)
                                {
                                    if ((_reg[rm] >> 31) == 1)
                                        index = 0xffff_ffff;
                                    else
                                        index = 0;
                                }
                                else
                                {
                                    index = ArithmeticShiftRight(_reg[rm], shiftImm);
                                }
                                break;
                            case 0b11:
                                if (shiftImm == 0) // RRX
                                    index = LogicalShiftLeft(GetFlag(Flags.C), 31) | LogicalShiftRight(_reg[rm], 1);
                                else // ROR
                                    index = RotateRight(_reg[rm], shiftImm);
                                break;
                        }

                        if (u == 1)
                            address = _reg[rn] + index;
                        else
                            address = _reg[rn] - index;
                    }
                    else // Pre-indexed
                    {
                        UInt32 index = 0;
                        switch (shift)
                        {
                            case 0b00: // LSL
                                index = LogicalShiftLeft(_reg[rm], shiftImm);
                                break;
                            case 0b01: // LSR
                                if (shiftImm == 0)
                                    index = 0;
                                else
                                    index = LogicalShiftRight(_reg[rm], shiftImm);
                                break;
                            case 0b10: // ASR
                                if (shiftImm == 0)
                                {
                                    if ((_reg[rm] >> 31) == 1)
                                        index = 0xffff_ffff;
                                    else
                                        index = 0;
                                }
                                else
                                {
                                    index = ArithmeticShiftRight(_reg[rm], shiftImm);
                                }
                                break;
                            case 0b11:
                                if (shiftImm == 0) // RRX
                                    index = LogicalShiftLeft(GetFlag(Flags.C), 31) | LogicalShiftRight(_reg[rm], 1);
                                else // ROR
                                    index = RotateRight(_reg[rm], shiftImm);
                                break;
                        }

                        if (u == 1)
                            address = _reg[rn] + index;
                        else
                            address = _reg[rn] - index;

                        SetReg(rn, address);
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
                    address = _reg[rn];

                    if (u == 1)
                        SetReg(rn, _reg[rn] + offset);
                    else
                        SetReg(rn, _reg[rn] - offset);
                }
                else if (w == 0) // Offset
                {
                    if (u == 1)
                        address = _reg[rn] + offset;
                    else
                        address = _reg[rn] - offset;
                }
                else // Pre-indexed
                {
                    if (u == 1)
                        address = _reg[rn] + offset;
                    else
                        address = _reg[rn] - offset;

                    SetReg(rn, address);
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
                        address = _reg[rn] + _reg[rm];
                    else
                        address = _reg[rn] - _reg[rm];
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
                value = _reg[rn] + (NumberOfSetBitsIn(registerList, 16) * 4);

                if (p == 0) // after
                {
                    startAddress = _reg[rn];
                    endAddress = value - 4;
                }
                else // before
                {
                    startAddress = _reg[rn] + 4;
                    endAddress = value;
                }
            }
            else // decrement
            {
                value = _reg[rn] - (NumberOfSetBitsIn(registerList, 16) * 4);

                if (p == 0) // after
                {
                    startAddress = value + 4;
                    endAddress = _reg[rn];
                }
                else // before
                {
                    startAddress = value;
                    endAddress = _reg[rn] - 4;
                }
            }

            UInt32 w = (instruction >> 21) & 1;
            if (w == 1)
                SetReg(rn, value);

            return (startAddress, endAddress);
        }

        private static void ARM_ADC(CPU cpu, UInt32 instruction)
        {
            var (shifterOperand, _) = cpu.GetShifterOperand(instruction);

            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 rd = (instruction >> 12) & 0b1111;

            UInt32 regRn = cpu._reg[rn];
            UInt64 rightOperand = (UInt64)shifterOperand + (UInt64)cpu.GetFlag(Flags.C);
            UInt64 result = (UInt64)regRn + rightOperand;
            cpu.SetReg(rd, (UInt32)result);

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
                    cpu.SetFlag(Flags.N, cpu._reg[rd] >> 31);
                    cpu.SetFlag(Flags.Z, (cpu._reg[rd] == 0) ? 1u : 0u);
                    cpu.SetFlag(Flags.C, CarryFrom(result));
                    cpu.SetFlag(Flags.V, OverflowFrom_Addition(regRn, (UInt32)rightOperand, cpu._reg[rd]));
                }
            }
        }

        private static void ARM_ADD(CPU cpu, UInt32 instruction)
        {
            var (shifterOperand, _) = cpu.GetShifterOperand(instruction);

            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 rd = (instruction >> 12) & 0b1111;

            UInt32 regRn = cpu._reg[rn];
            UInt64 result = (UInt64)regRn + (UInt64)shifterOperand;
            cpu.SetReg(rd, (UInt32)result);

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
                    cpu.SetFlag(Flags.N, cpu._reg[rd] >> 31);
                    cpu.SetFlag(Flags.Z, (cpu._reg[rd] == 0) ? 1u : 0u);
                    cpu.SetFlag(Flags.C, CarryFrom(result));
                    cpu.SetFlag(Flags.V, OverflowFrom_Addition(regRn, shifterOperand, cpu._reg[rd]));
                }
            }
        }

        private static void ARM_AND(CPU cpu, UInt32 instruction)
        {
            var (shifterOperand, shifterCarryOut) = cpu.GetShifterOperand(instruction);

            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 rd = (instruction >> 12) & 0b1111;
            cpu.SetReg(rd, cpu._reg[rn] & shifterOperand);

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
                    cpu.SetFlag(Flags.N, cpu._reg[rd] >> 31);
                    cpu.SetFlag(Flags.Z, (cpu._reg[rd] == 0) ? 1u : 0u);
                    cpu.SetFlag(Flags.C, shifterCarryOut);
                }
            }
        }

        private static void ARM_B(CPU cpu, UInt32 instruction)
        {
            UInt32 imm = instruction & 0xff_ffff;
            cpu.SetPC(cpu._reg[PC] + (SignExtend30(imm) << 2));
        }

        private static void ARM_BL(CPU cpu, UInt32 instruction)
        {
            cpu._reg[LR] = cpu._nextInstructionAddress;

            UInt32 imm = instruction & 0xff_ffff;
            cpu.SetPC(cpu._reg[PC] + (SignExtend30(imm) << 2));
        }

        private static void ARM_BIC(CPU cpu, UInt32 instruction)
        {
            var (shifterOperand, shifterCarryOut) = cpu.GetShifterOperand(instruction);

            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 rd = (instruction >> 12) & 0b1111;
            cpu.SetReg(rd, cpu._reg[rn] & ~shifterOperand);

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
                    cpu.SetFlag(Flags.N, cpu._reg[rd] >> 31);
                    cpu.SetFlag(Flags.Z, (cpu._reg[rd] == 0) ? 1u : 0u);
                    cpu.SetFlag(Flags.C, shifterCarryOut);
                }
            }
        }

        private static void ARM_BX(CPU cpu, UInt32 instruction)
        {
            UInt32 rm = instruction & 0b1111;
            cpu.SetCPSR((cpu._cpsr & ~(1u << 5)) | ((cpu._reg[rm] & 1) << 5));
            cpu.SetPC(cpu._reg[rm] & 0xffff_fffe);
        }

        private static void ARM_CMN(CPU cpu, UInt32 instruction)
        {
            var (shifterOperand, _) = cpu.GetShifterOperand(instruction);

            UInt32 rn = (instruction >> 16) & 0b1111;

            UInt64 result = (UInt64)cpu._reg[rn] + (UInt64)shifterOperand;
            UInt32 aluOut = (UInt32)result;

            cpu.SetFlag(Flags.N, aluOut >> 31);
            cpu.SetFlag(Flags.Z, (aluOut == 0) ? 1u : 0u);
            cpu.SetFlag(Flags.C, CarryFrom(result));
            cpu.SetFlag(Flags.V, OverflowFrom_Addition(cpu._reg[rn], shifterOperand, aluOut));
        }

        private static void ARM_CMP(CPU cpu, UInt32 instruction)
        {
            var (shifterOperand, _) = cpu.GetShifterOperand(instruction);

            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 aluOut = cpu._reg[rn] - shifterOperand;

            cpu.SetFlag(Flags.N, aluOut >> 31);
            cpu.SetFlag(Flags.Z, (aluOut == 0) ? 1u : 0u);
            cpu.SetFlag(Flags.C, ~BorrowFrom(cpu._reg[rn], shifterOperand) & 1);
            cpu.SetFlag(Flags.V, OverflowFrom_Subtraction(cpu._reg[rn], shifterOperand, aluOut));
        }

        private static void ARM_EOR(CPU cpu, UInt32 instruction)
        {
            var (shifterOperand, shifterCarryOut) = cpu.GetShifterOperand(instruction);

            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 rd = (instruction >> 12) & 0b1111;
            cpu.SetReg(rd, cpu._reg[rn] ^ shifterOperand);

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
                    cpu.SetFlag(Flags.N, cpu._reg[rd] >> 31);
                    cpu.SetFlag(Flags.Z, (cpu._reg[rd] == 0) ? 1u : 0u);
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
                    cpu._reg[i] = cpu._callbacks.ReadMemory32(address);
                    address += 4;
                }
            }

            if (((registerList >> 15) & 1) == 1)
            {
                UInt32 value = cpu._callbacks.ReadMemory32(address);
                cpu.SetPC(value & 0xffff_fffc);
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
                            cpu._reg8 = value;
                            break;
                        case 9:
                            cpu._reg9 = value;
                            break;
                        case 10:
                            cpu._reg10 = value;
                            break;
                        case 11:
                            cpu._reg11 = value;
                            break;
                        case 12:
                            cpu._reg12 = value;
                            break;
                        case 13:
                            cpu._reg13 = value;
                            break;
                        case 14:
                            cpu._reg14 = value;
                            break;
                        default:
                            cpu._reg[i] = value;
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
                cpu.SetPC(data & 0xffff_fffc);
            else
                cpu._reg[rd] = data;
        }

        private static void ARM_LDRB(CPU cpu, UInt32 instruction)
        {
            UInt32 address = cpu.GetAddress(instruction);

            UInt32 rd = (instruction >> 12) & 0b1111;
            cpu.SetReg(rd, cpu._callbacks.ReadMemory8(address));
        }

        private static void ARM_LDRH(CPU cpu, UInt32 instruction)
        {
            UInt32 address = cpu.GetAddress_Misc(instruction);

            UInt16 data = cpu._callbacks.ReadMemory16(address);
            UInt32 rd = (instruction >> 12) & 0b1111;
            cpu.SetReg(rd, ZeroExtend(data));
        }

        private static void ARM_LDRSB(CPU cpu, UInt32 instruction)
        {
            UInt32 address = cpu.GetAddress_Misc(instruction);

            Byte data = cpu._callbacks.ReadMemory8(address);
            UInt32 rd = (instruction >> 12) & 0b1111;
            cpu.SetReg(rd, SignExtend(data, 8));
        }

        private static void ARM_MLA(CPU cpu, UInt32 instruction)
        {
            UInt32 rd = (instruction >> 16) & 0b1111;
            UInt32 rn = (instruction >> 12) & 0b1111;
            UInt32 rs = (instruction >> 8) & 0b1111;
            UInt32 rm = instruction & 0b1111;
            cpu.SetReg(rd, (cpu._reg[rm] * cpu._reg[rs]) + cpu._reg[rn]);

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
                    cpu.SetFlag(Flags.N, cpu._reg[rd] >> 31);
                    cpu.SetFlag(Flags.Z, (cpu._reg[rd] == 0) ? 1u : 0u);
                }
            }
        }

        private static void ARM_MOV(CPU cpu, UInt32 instruction)
        {
            var (shifterOperand, shifterCarryOut) = cpu.GetShifterOperand(instruction);

            UInt32 rd = (instruction >> 12) & 0b1111;
            cpu.SetReg(rd, shifterOperand);

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
                    cpu.SetFlag(Flags.N, cpu._reg[rd] >> 31);
                    cpu.SetFlag(Flags.Z, (cpu._reg[rd] == 0) ? 1u : 0u);
                    cpu.SetFlag(Flags.C, shifterCarryOut);
                }
            }
        }

        private static void ARM_MRS(CPU cpu, UInt32 instruction)
        {
            UInt32 r = (instruction >> 22) & 1;
            UInt32 rd = (instruction >> 12) & 0b1111;

            if (r == 1)
                cpu.SetReg(rd, cpu._spsr);
            else
                cpu.SetReg(rd, cpu._cpsr);
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
                operand = cpu._reg[rm];
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
                if (cpu.InAPrivilegedMode())
                    mask = byteMask & (UserMask | PrivMask);
                else
                    mask = byteMask & UserMask;

                cpu.SetCPSR((cpu._cpsr & ~mask) | (operand & mask));
            }
            else if (cpu.CurrentModeHasSPSR())
            {
                UInt32 mask = byteMask & (UserMask | PrivMask | StateMask);
                cpu._spsr = (cpu._spsr & ~mask) | (operand & mask);
            }
        }

        private static void ARM_MUL(CPU cpu, UInt32 instruction)
        {
            UInt32 rd = (instruction >> 16) & 0b1111;
            UInt32 rs = (instruction >> 8) & 0b1111;
            UInt32 rm = instruction & 0b1111;
            cpu.SetReg(rd, cpu._reg[rm] * cpu._reg[rs]);

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
                    cpu.SetFlag(Flags.N, cpu._reg[rd] >> 31);
                    cpu.SetFlag(Flags.Z, (cpu._reg[rd] == 0) ? 1u : 0u);
                }
            }
        }

        private static void ARM_MVN(CPU cpu, UInt32 instruction)
        {
            var (shifterOperand, shifterCarryOut) = cpu.GetShifterOperand(instruction);

            UInt32 rd = (instruction >> 12) & 0b1111;
            cpu.SetReg(rd, ~shifterOperand);

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
                    cpu.SetFlag(Flags.N, cpu._reg[rd] >> 31);
                    cpu.SetFlag(Flags.Z, (cpu._reg[rd] == 0) ? 1u : 0u);
                    cpu.SetFlag(Flags.C, shifterCarryOut);
                }
            }
        }

        private static void ARM_ORR(CPU cpu, UInt32 instruction)
        {
            var (shifterOperand, shifterCarryOut) = cpu.GetShifterOperand(instruction);

            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 rd = (instruction >> 12) & 0b1111;
            cpu.SetReg(rd, cpu._reg[rn] | shifterOperand);

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
                    cpu.SetFlag(Flags.N, cpu._reg[rd] >> 31);
                    cpu.SetFlag(Flags.Z, (cpu._reg[rd] == 0) ? 1u : 0u);
                    cpu.SetFlag(Flags.C, shifterCarryOut);
                }
            }
        }

        private static void ARM_RSB(CPU cpu, UInt32 instruction)
        {
            var (shifterOperand, _) = cpu.GetShifterOperand(instruction);

            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 rd = (instruction >> 12) & 0b1111;

            UInt32 regRn = cpu._reg[rn];
            cpu.SetReg(rd, shifterOperand - regRn);

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
                    cpu.SetFlag(Flags.N, cpu._reg[rd] >> 31);
                    cpu.SetFlag(Flags.Z, (cpu._reg[rd] == 0) ? 1u : 0u);
                    cpu.SetFlag(Flags.C, ~BorrowFrom(shifterOperand, regRn) & 1);
                    cpu.SetFlag(Flags.V, OverflowFrom_Subtraction(shifterOperand, regRn, cpu._reg[rd]));
                }
            }
        }

        private static void ARM_RSC(CPU cpu, UInt32 instruction)
        {
            var (shifterOperand, _) = cpu.GetShifterOperand(instruction);

            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 rd = (instruction >> 12) & 0b1111;

            UInt32 rightOperand = cpu._reg[rn] + (~cpu.GetFlag(Flags.C) & 1);
            cpu.SetReg(rd, shifterOperand - rightOperand);

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
                    cpu.SetFlag(Flags.N, cpu._reg[rd] >> 31);
                    cpu.SetFlag(Flags.Z, (cpu._reg[rd] == 0) ? 1u : 0u);
                    cpu.SetFlag(Flags.C, ~BorrowFrom(shifterOperand, rightOperand) & 1);
                    cpu.SetFlag(Flags.V, OverflowFrom_Subtraction(shifterOperand, rightOperand, cpu._reg[rd]));
                }
            }
        }

        private static void ARM_SBC(CPU cpu, UInt32 instruction)
        {
            var (shifterOperand, _) = cpu.GetShifterOperand(instruction);

            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 rd = (instruction >> 12) & 0b1111;

            UInt32 regRn = cpu._reg[rn];
            UInt32 rightOperand = shifterOperand + (~cpu.GetFlag(Flags.C) & 1);
            cpu.SetReg(rd, regRn - rightOperand);

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
                    cpu.SetFlag(Flags.N, cpu._reg[rd] >> 31);
                    cpu.SetFlag(Flags.Z, (cpu._reg[rd] == 0) ? 1u : 0u);
                    cpu.SetFlag(Flags.C, ~BorrowFrom(regRn, rightOperand) & 1);
                    cpu.SetFlag(Flags.V, OverflowFrom_Subtraction(regRn, rightOperand, cpu._reg[rd]));
                }
            }
        }

        private static void ARM_SMLAL(CPU cpu, UInt32 instruction)
        {
            UInt32 rdHi = (instruction >> 16) & 0b1111;
            UInt32 rdLo = (instruction >> 12) & 0b1111;
            UInt32 rs = (instruction >> 8) & 0b1111;
            UInt32 rm = instruction & 0b1111;

            Int64 result = (Int64)(Int32)cpu._reg[rm] * (Int64)(Int32)cpu._reg[rs];
            UInt64 resultLo = (UInt64)(UInt32)result + (UInt64)cpu._reg[rdLo];
            cpu.SetReg(rdLo, (UInt32)resultLo);
            cpu.SetReg(rdHi, cpu._reg[rdHi] + (UInt32)(result >> 32) + CarryFrom(resultLo));

            UInt32 s = (instruction >> 20) & 1;
            if (s == 1)
            {
                cpu.SetFlag(Flags.N, cpu._reg[rdHi] >> 31);
                cpu.SetFlag(Flags.Z, (cpu._reg[rdHi] == 0 && cpu._reg[rdLo] == 0) ? 1u : 0u);
            }
        }

        private static void ARM_SMULL(CPU cpu, UInt32 instruction)
        {
            UInt32 rdHi = (instruction >> 16) & 0b1111;
            UInt32 rdLo = (instruction >> 12) & 0b1111;
            UInt32 rs = (instruction >> 8) & 0b1111;
            UInt32 rm = instruction & 0b1111;

            Int64 result = (Int64)(Int32)cpu._reg[rm] * (Int64)(Int32)cpu._reg[rs];
            cpu.SetReg(rdHi, (UInt32)(result >> 32));
            cpu.SetReg(rdLo, (UInt32)result);

            UInt32 s = (instruction >> 20) & 1;
            if (s == 1)
            {
                cpu.SetFlag(Flags.N, cpu._reg[rdHi] >> 31);
                cpu.SetFlag(Flags.Z, (cpu._reg[rdHi] == 0 && cpu._reg[rdLo] == 0) ? 1u : 0u);
            }
        }

        private static void ARM_STM1(CPU cpu, UInt32 instruction)
        {
            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 regRn = cpu._reg[rn];

            var (startAddress, _) = cpu.GetAddress_Multiple(instruction);

            UInt32 address = startAddress;

            UInt32 registerList = instruction & 0xffff;
            for (var i = 0; i <= 15; ++i)
            {
                if (((registerList >> i) & 1) == 1)
                {
                    cpu._callbacks.WriteMemory32(address, (i == rn) ? regRn : cpu._reg[i]);
                    address += 4;
                }
            }
        }

        private static void ARM_STR(CPU cpu, UInt32 instruction)
        {
            UInt32 address = cpu.GetAddress(instruction);

            UInt32 rd = (instruction >> 12) & 0b1111;
            cpu._callbacks.WriteMemory32(address, cpu._reg[rd]);
        }

        private static void ARM_STRB(CPU cpu, UInt32 instruction)
        {
            UInt32 address = cpu.GetAddress(instruction);

            UInt32 rd = (instruction >> 12) & 0b1111;
            cpu._callbacks.WriteMemory8(address, (Byte)cpu._reg[rd]);
        }

        private static void ARM_STRH(CPU cpu, UInt32 instruction)
        {
            UInt32 address = cpu.GetAddress_Misc(instruction);

            UInt32 rd = (instruction >> 12) & 0b1111;
            cpu._callbacks.WriteMemory16(address, (UInt16)cpu._reg[rd]);
        }

        private static void ARM_SUB(CPU cpu, UInt32 instruction)
        {
            var (shifterOperand, _) = cpu.GetShifterOperand(instruction);

            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 rd = (instruction >> 12) & 0b1111;

            UInt32 regRn = cpu._reg[rn];
            cpu.SetReg(rd, regRn - shifterOperand);

            UInt32 s = (instruction >> 20) & 1;
            if (s == 1)
            {
                if (rd == PC)
                {
                    if (cpu.CurrentModeHasSPSR())
                        cpu._cpsr = cpu._spsr;
                }
                else
                {
                    cpu.SetFlag(Flags.N, cpu._reg[rd] >> 31);
                    cpu.SetFlag(Flags.Z, (cpu._reg[rd] == 0) ? 1u : 0u);
                    cpu.SetFlag(Flags.C, ~BorrowFrom(regRn, shifterOperand) & 1);
                    cpu.SetFlag(Flags.V, OverflowFrom_Subtraction(regRn, shifterOperand, cpu._reg[rd]));
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

            UInt32 address = cpu._reg[rn];
            UInt32 temp = RotateRight(cpu._callbacks.ReadMemory32(address), 8 * (address & 0b11));
            cpu._callbacks.WriteMemory32(address, cpu._reg[rm]);
            cpu.SetReg(rd, temp);
        }

        private static void ARM_SWPB(CPU cpu, UInt32 instruction)
        {
            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 rd = (instruction >> 12) & 0b1111;
            UInt32 rm = instruction & 0b1111;

            UInt32 address = cpu._reg[rn];
            Byte temp = cpu._callbacks.ReadMemory8(address);
            cpu._callbacks.WriteMemory8(address, (Byte)cpu._reg[rm]);
            cpu.SetReg(rd, temp);
        }

        private static void ARM_TEQ(CPU cpu, UInt32 instruction)
        {
            var (shifterOperand, shifterCarryOut) = cpu.GetShifterOperand(instruction);

            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 aluOut = cpu._reg[rn] ^ shifterOperand;

            cpu.SetFlag(Flags.N, aluOut >> 31);
            cpu.SetFlag(Flags.Z, (aluOut == 0) ? 1u : 0u);
            cpu.SetFlag(Flags.C, shifterCarryOut);
        }

        private static void ARM_TST(CPU cpu, UInt32 instruction)
        {
            var (shifterOperand, shifterCarryOut) = cpu.GetShifterOperand(instruction);

            UInt32 rn = (instruction >> 16) & 0b1111;
            UInt32 aluOut = cpu._reg[rn] & shifterOperand;

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

            UInt64 result = (UInt64)cpu._reg[rm] * (UInt64)cpu._reg[rs];
            UInt64 resultLo = (UInt64)(UInt32)result + (UInt64)cpu._reg[rdLo];
            cpu.SetReg(rdLo, (UInt32)resultLo);
            cpu.SetReg(rdHi, cpu._reg[rdHi] + (UInt32)(result >> 32) + CarryFrom(resultLo));

            UInt32 s = (instruction >> 20) & 1;
            if (s == 1)
            {
                cpu.SetFlag(Flags.N, cpu._reg[rdHi] >> 31);
                cpu.SetFlag(Flags.Z, (cpu._reg[rdHi] == 0 && cpu._reg[rdLo] == 0) ? 1u : 0u);
            }
        }

        private static void ARM_UMULL(CPU cpu, UInt32 instruction)
        {
            UInt32 rdHi = (instruction >> 16) & 0b1111;
            UInt32 rdLo = (instruction >> 12) & 0b1111;
            UInt32 rs = (instruction >> 8) & 0b1111;
            UInt32 rm = instruction & 0b1111;

            UInt64 result = (UInt64)cpu._reg[rm] * (UInt64)cpu._reg[rs];
            cpu.SetReg(rdHi, (UInt32)(result >> 32));
            cpu.SetReg(rdLo, (UInt32)result);

            UInt32 s = (instruction >> 20) & 1;
            if (s == 1)
            {
                cpu.SetFlag(Flags.N, cpu._reg[rdHi] >> 31);
                cpu.SetFlag(Flags.Z, (cpu._reg[rdHi] == 0 && cpu._reg[rdLo] == 0) ? 1u : 0u);
            }
        }

        // ********************************************************************
        //                              THUMB
        // ********************************************************************

        private static void THUMB_ADC(CPU cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 regRd = cpu._reg[rd];
            UInt64 rightOperand = (UInt64)cpu._reg[rm] + (UInt64)cpu.GetFlag(Flags.C);
            UInt64 result = (UInt64)regRd + rightOperand;
            cpu._reg[rd] = (UInt32)result;

            cpu.SetFlag(Flags.N, cpu._reg[rd] >> 31);
            cpu.SetFlag(Flags.Z, (cpu._reg[rd] == 0) ? 1u : 0u);
            cpu.SetFlag(Flags.C, CarryFrom(result));
            cpu.SetFlag(Flags.V, OverflowFrom_Addition(regRd, (UInt32)rightOperand, cpu._reg[rd]));
        }

        private static void THUMB_ADD1(CPU cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)((instruction >> 6) & 0b111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 regRn = cpu._reg[rn];
            UInt64 result = (UInt64)regRn + (UInt64)imm;
            cpu._reg[rd] = (UInt32)result;

            cpu.SetFlag(Flags.N, cpu._reg[rd] >> 31);
            cpu.SetFlag(Flags.Z, (cpu._reg[rd] == 0) ? 1u : 0u);
            cpu.SetFlag(Flags.C, CarryFrom(result));
            cpu.SetFlag(Flags.V, OverflowFrom_Addition(regRn, imm, cpu._reg[rd]));
        }

        private static void THUMB_ADD2(CPU cpu, UInt16 instruction)
        {
            UInt16 rd = (UInt16)((instruction >> 8) & 0b111);
            UInt16 imm = (UInt16)(instruction & 0xff);

            UInt32 regRd = cpu._reg[rd];
            UInt64 result = (UInt64)regRd + (UInt64)imm;
            cpu._reg[rd] = (UInt32)result;

            cpu.SetFlag(Flags.N, cpu._reg[rd] >> 31);
            cpu.SetFlag(Flags.Z, (cpu._reg[rd] == 0) ? 1u : 0u);
            cpu.SetFlag(Flags.C, CarryFrom(result));
            cpu.SetFlag(Flags.V, OverflowFrom_Addition(regRd, imm, cpu._reg[rd]));
        }

        private static void THUMB_ADD3(CPU cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 6) & 0b111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 regRn = cpu._reg[rn];
            UInt32 regRm = cpu._reg[rm];
            UInt64 result = (UInt64)regRn + (UInt64)regRm;
            cpu._reg[rd] = (UInt32)result;

            cpu.SetFlag(Flags.N, cpu._reg[rd] >> 31);
            cpu.SetFlag(Flags.Z, (cpu._reg[rd] == 0) ? 1u : 0u);
            cpu.SetFlag(Flags.C, CarryFrom(result));
            cpu.SetFlag(Flags.V, OverflowFrom_Addition(regRn, regRm, cpu._reg[rd]));
        }

        private static void THUMB_ADD4(CPU cpu, UInt16 instruction)
        {
            UInt16 h1 = (UInt16)((instruction >> 7) & 1);
            UInt16 h2 = (UInt16)((instruction >> 6) & 1);
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);
            cpu.SetReg((UInt32)((h1 << 3) | rd), cpu._reg[(h1 << 3) | rd] + cpu._reg[(h2 << 3) | rm]);
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

        private static void THUMB_B1(CPU cpu, UInt16 instruction)
        {
            UInt16 cond = (UInt16)((instruction >> 8) & 0b1111);
            if (cpu.ConditionPassed(cond))
            {
                UInt16 imm = (UInt16)(instruction & 0xff);
                cpu.SetPC(cpu._reg[PC] + (SignExtend(imm, 8) << 1));
            }
        }

        private static void THUMB_B2(CPU cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)(instruction & 0x7ff);
            cpu.SetPC(cpu._reg[PC] + (SignExtend(imm, 11) << 1));
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
                UInt32 nextInstructionAddress = cpu._nextInstructionAddress;
                cpu.SetPC(cpu._reg[LR] + (UInt32)(offset << 1));
                cpu._reg[LR] = nextInstructionAddress | 1;
            }
        }

        private static void THUMB_BX(CPU cpu, UInt16 instruction)
        {
            UInt16 h2 = (UInt16)((instruction >> 6) & 1);
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);

            UInt32 regRm = cpu._reg[(h2 << 3) | rm];
            cpu.SetCPSR((cpu._cpsr & ~(1u << 5)) | ((regRm & 1) << 5));
            cpu.SetPC(regRm & 0xffff_fffe);
        }

        private static void THUMB_CMP1(CPU cpu, UInt16 instruction)
        {
            UInt16 rn = (UInt16)((instruction >> 8) & 0b111);
            UInt16 imm = (UInt16)(instruction & 0xff);
            UInt32 aluOut = cpu._reg[rn] - imm;

            cpu.SetFlag(Flags.N, aluOut >> 31);
            cpu.SetFlag(Flags.Z, (aluOut == 0) ? 1u : 0u);
            cpu.SetFlag(Flags.C, ~BorrowFrom(cpu._reg[rn], imm) & 1);
            cpu.SetFlag(Flags.V, OverflowFrom_Subtraction(cpu._reg[rn], imm, aluOut));
        }

        private static void THUMB_CMP2(CPU cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rn = (UInt16)(instruction & 0b111);
            UInt32 aluOut = cpu._reg[rn] - cpu._reg[rm];

            cpu.SetFlag(Flags.N, aluOut >> 31);
            cpu.SetFlag(Flags.Z, (aluOut == 0) ? 1u : 0u);
            cpu.SetFlag(Flags.C, ~BorrowFrom(cpu._reg[rn], cpu._reg[rm]) & 1);
            cpu.SetFlag(Flags.V, OverflowFrom_Subtraction(cpu._reg[rn], cpu._reg[rm], aluOut));
        }

        private static void THUMB_CMP3(CPU cpu, UInt16 instruction)
        {
            UInt16 h1 = (UInt16)((instruction >> 7) & 1);
            UInt16 h2 = (UInt16)((instruction >> 6) & 1);
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rn = (UInt16)(instruction & 0b111);
            UInt32 aluOut = cpu._reg[(h1 << 3) | rn] - cpu._reg[(h2 << 3) | rm];

            cpu.SetFlag(Flags.N, aluOut >> 31);
            cpu.SetFlag(Flags.Z, (aluOut == 0) ? 1u : 0u);
            cpu.SetFlag(Flags.C, ~BorrowFrom(cpu._reg[(h1 << 3) | rn], cpu._reg[(h2 << 3) | rm]) & 1);
            cpu.SetFlag(Flags.V, OverflowFrom_Subtraction(cpu._reg[(h1 << 3) | rn], cpu._reg[(h2 << 3) | rm], aluOut));
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

            for (var i = 0; i <= 7; ++i)
            {
                if (((registerList >> i) & 1) == 1)
                {
                    cpu._reg[i] = cpu._callbacks.ReadMemory32(address);
                    address += 4;
                }
            }

            cpu._reg[rn] += NumberOfSetBitsIn(registerList, 8) * 4;
        }

        private static void THUMB_LDR1(CPU cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)((instruction >> 6) & 0b1_1111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt32 address = cpu._reg[rn] + (imm * 4u);

            UInt32 data = RotateRight(cpu._callbacks.ReadMemory32(address), 8 * (address & 0b11));
            UInt16 rd = (UInt16)(instruction & 0b111);
            cpu._reg[rd] = data;
        }

        private static void THUMB_LDR2(CPU cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 6) & 0b111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt32 address = cpu._reg[rn] + cpu._reg[rm];

            UInt32 data = RotateRight(cpu._callbacks.ReadMemory32(address), 8 * (address & 0b11));
            UInt16 rd = (UInt16)(instruction & 0b111);
            cpu._reg[rd] = data;
        }

        private static void THUMB_LDR3(CPU cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)(instruction & 0xff);
            UInt32 address = (cpu._reg[PC] & 0xffff_fffc) + (imm * 4u);

            UInt32 data = cpu._callbacks.ReadMemory32(address);
            UInt16 rd = (UInt16)((instruction >> 8) & 0b111);
            cpu._reg[rd] = data;
        }

        private static void THUMB_LDR4(CPU cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)(instruction & 0xff);
            UInt32 address = cpu._reg[SP] + (imm * 4u);

            UInt32 data = cpu._callbacks.ReadMemory32(address);
            UInt16 rd = (UInt16)((instruction >> 8) & 0b111);
            cpu._reg[rd] = data;
        }

        private static void THUMB_LDRB1(CPU cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)((instruction >> 6) & 0b1_1111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt32 address = cpu._reg[rn] + imm;

            Byte data = cpu._callbacks.ReadMemory8(address);
            UInt16 rd = (UInt16)(instruction & 0b111);
            cpu._reg[rd] = data;
        }

        private static void THUMB_LDRB2(CPU cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 6) & 0b111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt32 address = cpu._reg[rn] + cpu._reg[rm];

            Byte data = cpu._callbacks.ReadMemory8(address);
            UInt16 rd = (UInt16)(instruction & 0b111);
            cpu._reg[rd] = data;
        }

        private static void THUMB_LDRH1(CPU cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)((instruction >> 6) & 0b1_1111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt32 address = cpu._reg[rn] + (imm * 2u);

            UInt16 data = cpu._callbacks.ReadMemory16(address);
            UInt16 rd = (UInt16)(instruction & 0b111);
            cpu._reg[rd] = ZeroExtend(data);
        }

        private static void THUMB_LDRH2(CPU cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 6) & 0b111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt32 address = cpu._reg[rn] + cpu._reg[rm];

            UInt16 data = cpu._callbacks.ReadMemory16(address);
            UInt16 rd = (UInt16)(instruction & 0b111);
            cpu._reg[rd] = ZeroExtend(data);
        }

        private static void THUMB_LDRSB(CPU cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 6) & 0b111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt32 address = cpu._reg[rn] + cpu._reg[rm];

            Byte data = cpu._callbacks.ReadMemory8(address);
            UInt16 rd = (UInt16)(instruction & 0b111);
            cpu._reg[rd] = SignExtend(data, 8);
        }

        private static void THUMB_LDRSH(CPU cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 6) & 0b111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt32 address = cpu._reg[rn] + cpu._reg[rm];

            UInt16 data = cpu._callbacks.ReadMemory16(address);
            UInt16 rd = (UInt16)(instruction & 0b111);
            cpu._reg[rd] = SignExtend(data, 16);
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
                cpu._reg[rd] = LogicalShiftLeft(cpu._reg[rm], imm);
            }

            cpu.SetFlag(Flags.N, cpu._reg[rd] >> 31);
            cpu.SetFlag(Flags.Z, (cpu._reg[rd] == 0) ? 1u : 0u);
        }

        private static void THUMB_LSL2(CPU cpu, UInt16 instruction)
        {
            UInt16 rs = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            if ((cpu._reg[rs] & 0xff) == 0)
            {
                // nothing to do
            }
            else if ((cpu._reg[rs] & 0xff) < 32)
            {
                cpu.SetFlag(Flags.C, (cpu._reg[rd] >> (32 - ((int)cpu._reg[rs] & 0xff))) & 1);
                cpu._reg[rd] = LogicalShiftLeft(cpu._reg[rd], cpu._reg[rs] & 0xff);
            }
            else if ((cpu._reg[rs] & 0xff) == 32)
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
                cpu._reg[rd] = LogicalShiftRight(cpu._reg[rm], imm);
            }

            cpu.SetFlag(Flags.N, cpu._reg[rd] >> 31);
            cpu.SetFlag(Flags.Z, (cpu._reg[rd] == 0) ? 1u : 0u);
        }

        private static void THUMB_LSR2(CPU cpu, UInt16 instruction)
        {
            UInt16 rs = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            if ((cpu._reg[rs] & 0xff) == 0)
            {
                // nothing to do
            }
            else if ((cpu._reg[rs] & 0xff) < 0)
            {
                cpu.SetFlag(Flags.C, (cpu._reg[rd] >> ((int)(cpu._reg[rs] & 0xff) - 1)) & 1);
                cpu._reg[rd] = LogicalShiftRight(cpu._reg[rd], cpu._reg[rs] & 0xff);
            }
            else if ((cpu._reg[rs] & 0xff) == 32)
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
            cpu.SetReg((UInt32)((h1 << 3) | rd), cpu._reg[(h2 << 3) | rm]);
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

            UInt32 regRm = cpu._reg[rm];
            cpu._reg[rd] = 0 - regRm;

            cpu.SetFlag(Flags.N, cpu._reg[rd] >> 31);
            cpu.SetFlag(Flags.Z, (cpu._reg[rd] == 0) ? 1u : 0u);
            cpu.SetFlag(Flags.C, ~BorrowFrom(0, regRm) & 1);
            cpu.SetFlag(Flags.V, OverflowFrom_Subtraction(0, regRm, cpu._reg[rd]));
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
            UInt32 endAddress = cpu._reg[SP] + 4 * (r + NumberOfSetBitsIn(registerList, 8));
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
                cpu.SetPC(value & 0xffff_fffe);
            }

            cpu._reg[SP] = endAddress;
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
                cpu.SetFlag(Flags.C, (cpu._reg[rd] >> (((int)cpu._reg[rs] & 0b1_1111) - 1)) & 1);
                cpu._reg[rd] = RotateRight(cpu._reg[rd], cpu._reg[rs] & 0b1_1111);
            }

            cpu.SetFlag(Flags.N, cpu._reg[rd] >> 31);
            cpu.SetFlag(Flags.Z, (cpu._reg[rd] == 0) ? 1u : 0u);
        }

        private static void THUMB_STMIA(CPU cpu, UInt16 instruction)
        {
            UInt16 rn = (UInt16)((instruction >> 8) & 0b111);
            UInt16 registerList = (UInt16)(instruction & 0xff);
            UInt32 startAddress = cpu._reg[rn];
            UInt32 address = startAddress;

            for (var i = 0; i <= 7; ++i)
            {
                if (((registerList >> i) & 1) == 1)
                {
                    cpu._callbacks.WriteMemory32(address, cpu._reg[i]);
                    address += 4;
                }
            }

            cpu._reg[rn] += NumberOfSetBitsIn(registerList, 8) * 4;
        }

        private static void THUMB_STR1(CPU cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)((instruction >> 6) & 0b1_1111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt32 address = cpu._reg[rn] + (imm * 4u);

            UInt16 rd = (UInt16)(instruction & 0b111);
            cpu._callbacks.WriteMemory32(address, cpu._reg[rd]);
        }

        private static void THUMB_STR3(CPU cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)(instruction & 0xff);
            UInt32 address = cpu._reg[SP] + (imm * 4u);

            UInt16 rd = (UInt16)((instruction >> 8) & 0b111);
            cpu._callbacks.WriteMemory32(address, cpu._reg[rd]);
        }

        private static void THUMB_STRB1(CPU cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)((instruction >> 6) & 0b1_1111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt32 address = cpu._reg[rn] + imm;

            UInt16 rd = (UInt16)(instruction & 0b111);
            cpu._callbacks.WriteMemory8(address, (Byte)cpu._reg[rd]);
        }

        private static void THUMB_STRH1(CPU cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)((instruction >> 6) & 0b1_1111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt32 address = cpu._reg[rn] + (imm * 2u);

            UInt16 rd = (UInt16)(instruction & 0b111);
            cpu._callbacks.WriteMemory16(address, (UInt16)cpu._reg[rd]);
        }

        private static void THUMB_SUB1(CPU cpu, UInt16 instruction)
        {
            UInt16 imm = (UInt16)((instruction >> 6) & 0b111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 regRn = cpu._reg[rn];
            cpu._reg[rd] = regRn - imm;

            cpu.SetFlag(Flags.N, cpu._reg[rd] >> 31);
            cpu.SetFlag(Flags.Z, (cpu._reg[rd] == 0) ? 1u : 0u);
            cpu.SetFlag(Flags.C, ~BorrowFrom(regRn, imm) & 1);
            cpu.SetFlag(Flags.V, OverflowFrom_Subtraction(regRn, imm, cpu._reg[rd]));
        }

        private static void THUMB_SUB2(CPU cpu, UInt16 instruction)
        {
            UInt16 rd = (UInt16)((instruction >> 8) & 0b111);
            UInt16 imm = (UInt16)(instruction & 0xff);

            UInt32 regRd = cpu._reg[rd];
            cpu._reg[rd] -= imm;

            cpu.SetFlag(Flags.N, cpu._reg[rd] >> 31);
            cpu.SetFlag(Flags.Z, (cpu._reg[rd] == 0) ? 1u : 0u);
            cpu.SetFlag(Flags.C, ~BorrowFrom(regRd, imm) & 1);
            cpu.SetFlag(Flags.V, OverflowFrom_Subtraction(regRd, imm, cpu._reg[rd]));
        }

        private static void THUMB_SUB3(CPU cpu, UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 6) & 0b111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            UInt32 regRn = cpu._reg[rn];
            UInt32 regRm = cpu._reg[rm];
            cpu._reg[rd] = regRn - regRm;

            cpu.SetFlag(Flags.N, cpu._reg[rd] >> 31);
            cpu.SetFlag(Flags.Z, (cpu._reg[rd] == 0) ? 1u : 0u);
            cpu.SetFlag(Flags.C, ~BorrowFrom(regRn, regRm) & 1);
            cpu.SetFlag(Flags.V, OverflowFrom_Subtraction(regRn, regRm, cpu._reg[rd]));
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
