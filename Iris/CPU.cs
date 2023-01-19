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
    internal sealed partial class CPU
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

        private readonly UInt32[] _reg = new UInt32[16];
        private UInt32 _cpsr;
        private UInt32 _spsr;

        private UInt32 _reg8, _reg9, _reg10, _reg11, _reg12, _reg13, _reg14;
        private UInt32 _reg13_svc, _reg14_svc;
        private UInt32 _reg13_abt, _reg14_abt;
        private UInt32 _reg13_und, _reg14_und;
        private UInt32 _reg13_irq, _reg14_irq;
        private UInt32 _reg8_fiq, _reg9_fiq, _reg10_fiq, _reg11_fiq, _reg12_fiq, _reg13_fiq, _reg14_fiq;
        private UInt32 _spsr_svc, _spsr_abt, _spsr_und, _spsr_irq, _spsr_fiq;

        private readonly ICallbacks _callbacks;
        private UInt32 _nextInstructionAddress;

        internal CPU(ICallbacks callbacks)
        {
            _callbacks = callbacks;
        }

        internal void Step()
        {
            if (((_cpsr >> 5) & 1) == 0)
                ARM_Step();
            else
                THUMB_Step();
        }

        internal UInt32[] Reg
        {
            get => _reg;
        }

        internal UInt32 CPSR
        {
            set => _cpsr = value;
        }

        internal UInt32 NextInstructionAddress
        {
            set => _nextInstructionAddress = value;
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
                // unconditional
                0b1111 => true,
                // should never happen
                _ => throw new Exception(string.Format("CPU: Wrong condition code {0}", cond)),
            };
        }

        private static UInt32 Not(UInt32 flag)
        {
            return ~flag & 1;
        }

        private static UInt32 CarryFrom(UInt64 result)
        {
            return (result > 0xffff_ffff) ? 1u : 0u;
        }

        private static UInt32 BorrowFrom(UInt32 leftOperand, UInt64 rightOperand)
        {
            return (leftOperand < rightOperand) ? 1u : 0u;
        }

        private static UInt32 OverflowFrom_Addition(UInt32 leftOperand, UInt32 rightOperand, UInt32 result)
        {
            return (((leftOperand >> 31) == (rightOperand >> 31))
                 && ((leftOperand >> 31) != (result >> 31))) ? 1u : 0u;
        }

        private static UInt32 OverflowFrom_Subtraction(UInt32 leftOperand, UInt32 rightOperand, UInt32 result)
        {
            return (((leftOperand >> 31) != (rightOperand >> 31))
                 && ((leftOperand >> 31) != (result >> 31))) ? 1u : 0u;
        }

        private static UInt32 RotateRight(UInt32 value, UInt32 rotateAmount)
        {
            return (value >> ((int)rotateAmount & 0x1f)) | (value << (32 - ((int)rotateAmount & 0x1f)));
        }

        private static UInt32 ArithmeticShiftRight(UInt32 value, UInt32 shiftAmount)
        {
            return (UInt32)((Int32)value >> (int)shiftAmount);
        }

        private static UInt32 SignExtend(UInt32 value, int size)
        {
            return ((value >> (size - 1)) == 1) ? value | (0xffff_ffff << size) : value;
        }

        private static UInt32 NumberOfSetBitsIn(UInt32 value, int size)
        {
            UInt32 count = 0;

            for (var i = 0; i < size; ++i)
                count += (value >> i) & 1;

            return count;
        }
    }
}
