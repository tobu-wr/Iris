﻿using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Iris.CPU
{
    public sealed class CPU_Core
    {
        [Flags]
        public enum Model
        {
            ARM7TDMI,
            ARM946ES
        }

        // could have used function pointers (delegate*) for performance instead of delegates but it's less flexible (cannot use non-static function for instance)
        public struct CallbackInterface
        {
            public delegate Byte ReadMemory8_Delegate(UInt32 address);
            public delegate UInt16 ReadMemory16_Delegate(UInt32 address);
            public delegate UInt32 ReadMemory32_Delegate(UInt32 address);
            public delegate void WriteMemory8_Delegate(UInt32 address, Byte value);
            public delegate void WriteMemory16_Delegate(UInt32 address, UInt16 value);
            public delegate void WriteMemory32_Delegate(UInt32 address, UInt32 value);
            public delegate void HandleSWI_Delegate(UInt32 value);
            public delegate void HandleIRQ_Delegate();

            public ReadMemory8_Delegate ReadMemory8;
            public ReadMemory16_Delegate ReadMemory16;
            public ReadMemory32_Delegate ReadMemory32;
            public WriteMemory8_Delegate WriteMemory8;
            public WriteMemory16_Delegate WriteMemory16;
            public WriteMemory32_Delegate WriteMemory32;
            public HandleSWI_Delegate HandleSWI;
            public HandleIRQ_Delegate HandleIRQ;
        }

        public enum Signal
        {
            Low,
            High
        }

        internal enum Flag
        {
            V = 28,
            C = 29,
            Z = 30,
            N = 31
        }

        internal readonly struct InstructionListEntry<T>
        {
            internal readonly T _mask;
            internal readonly T _expected;
            internal unsafe readonly delegate*<CPU_Core, T, UInt32> _handler;
            internal readonly Model _models;

            internal unsafe InstructionListEntry(T mask, T expected, delegate*<CPU_Core, T, UInt32> handler, Model models)
            {
                _mask = mask;
                _expected = expected;
                _handler = handler;
                _models = models;
            }
        }

        internal readonly struct InstructionLUTEntry<T>
        {
            internal unsafe readonly delegate*<CPU_Core, T, UInt32> _handler;

            internal unsafe InstructionLUTEntry(delegate*<CPU_Core, T, UInt32> handler)
            {
                _handler = handler;
            }
        }

        internal const UInt32 ModeMask = 0b1_1111;
        internal const UInt32 UserMode = 0b1_0000;
        internal const UInt32 SystemMode = 0b1_1111;
        internal const UInt32 SupervisorMode = 0b1_0011;
        internal const UInt32 AbortMode = 0b1_0111;
        internal const UInt32 UndefinedMode = 0b1_1011;
        internal const UInt32 InterruptMode = 0b1_0010;
        internal const UInt32 FastInterruptMode = 0b1_0001;

        public const UInt32 SP = 13;
        public const UInt32 LR = 14;
        public const UInt32 PC = 15;

        public readonly UInt32[] Reg = new UInt32[16];
        public UInt32 CPSR = 0b1_0000;
        public UInt32 SPSR;

        public UInt32 Reg8_usr, Reg9_usr, Reg10_usr, Reg11_usr, Reg12_usr, Reg13_usr, Reg14_usr;
        public UInt32 Reg13_svc, Reg14_svc;
        public UInt32 Reg13_abt, Reg14_abt;
        public UInt32 Reg13_und, Reg14_und;
        public UInt32 Reg13_irq, Reg14_irq;
        public UInt32 Reg8_fiq, Reg9_fiq, Reg10_fiq, Reg11_fiq, Reg12_fiq, Reg13_fiq, Reg14_fiq;
        public UInt32 SPSR_svc, SPSR_abt, SPSR_und, SPSR_irq, SPSR_fiq;

        internal readonly Model _model;
        internal readonly CallbackInterface _callbackInterface;

        private readonly ARM_Interpreter _armInterpreter;
        private readonly THUMB_Interpreter _thumbInterpreter;

        public UInt32 NextInstructionAddress;
        public Signal NIRQ;

        public CPU_Core(Model model, CallbackInterface callbackInterface)
        {
            _model = model;
            _callbackInterface = callbackInterface;
            _armInterpreter = new(this);
            _thumbInterpreter = new(this);
        }

        public UInt32 Step()
        {
            UInt32 i = (CPSR >> 7) & 1;

            if ((i == 0) && (NIRQ == Signal.Low))
                _callbackInterface.HandleIRQ();

            UInt32 t = (CPSR >> 5) & 1;

            if (t == 0)
                return _armInterpreter.Step();
            else
                return _thumbInterpreter.Step();
        }

        public void SetCPSR(UInt32 value)
        {
            UInt32 previousMode = CPSR & ModeMask;
            UInt32 newMode = value & ModeMask;

            CPSR = value | 0b1_0000;

            if (previousMode != newMode)
            {
                ref UInt32 regDataRef = ref MemoryMarshal.GetArrayDataReference(Reg);
                ref UInt32 reg8 = ref Unsafe.Add(ref regDataRef, 8);
                ref UInt32 reg9 = ref Unsafe.Add(ref regDataRef, 9);
                ref UInt32 reg10 = ref Unsafe.Add(ref regDataRef, 10);
                ref UInt32 reg11 = ref Unsafe.Add(ref regDataRef, 11);
                ref UInt32 reg12 = ref Unsafe.Add(ref regDataRef, 12);
                ref UInt32 reg13 = ref Unsafe.Add(ref regDataRef, 13);
                ref UInt32 reg14 = ref Unsafe.Add(ref regDataRef, 14);

                // save previous mode registers
                switch (previousMode)
                {
                    case UserMode:
                    case SystemMode:
                        Reg8_usr = reg8;
                        Reg9_usr = reg9;
                        Reg10_usr = reg10;
                        Reg11_usr = reg11;
                        Reg12_usr = reg12;
                        Reg13_usr = reg13;
                        Reg14_usr = reg14;
                        break;
                    case SupervisorMode:
                        Reg8_usr = reg8;
                        Reg9_usr = reg9;
                        Reg10_usr = reg10;
                        Reg11_usr = reg11;
                        Reg12_usr = reg12;
                        Reg13_svc = reg13;
                        Reg14_svc = reg14;
                        SPSR_svc = SPSR;
                        break;
                    case AbortMode:
                        Reg8_usr = reg8;
                        Reg9_usr = reg9;
                        Reg10_usr = reg10;
                        Reg11_usr = reg11;
                        Reg12_usr = reg12;
                        Reg13_abt = reg13;
                        Reg14_abt = reg14;
                        SPSR_abt = SPSR;
                        break;
                    case UndefinedMode:
                        Reg8_usr = reg8;
                        Reg9_usr = reg9;
                        Reg10_usr = reg10;
                        Reg11_usr = reg11;
                        Reg12_usr = reg12;
                        Reg13_und = reg13;
                        Reg14_und = reg14;
                        SPSR_und = SPSR;
                        break;
                    case InterruptMode:
                        Reg8_usr = reg8;
                        Reg9_usr = reg9;
                        Reg10_usr = reg10;
                        Reg11_usr = reg11;
                        Reg12_usr = reg12;
                        Reg13_irq = reg13;
                        Reg14_irq = reg14;
                        SPSR_irq = SPSR;
                        break;
                    case FastInterruptMode:
                        Reg8_fiq = reg8;
                        Reg9_fiq = reg9;
                        Reg10_fiq = reg10;
                        Reg11_fiq = reg11;
                        Reg12_fiq = reg12;
                        Reg13_fiq = reg13;
                        Reg14_fiq = reg14;
                        SPSR_fiq = SPSR;
                        break;
                }

                // load new mode registers
                switch (newMode)
                {
                    case UserMode:
                    case SystemMode:
                        reg8 = Reg8_usr;
                        reg9 = Reg9_usr;
                        reg10 = Reg10_usr;
                        reg11 = Reg11_usr;
                        reg12 = Reg12_usr;
                        reg13 = Reg13_usr;
                        reg14 = Reg14_usr;
                        break;
                    case SupervisorMode:
                        reg8 = Reg8_usr;
                        reg9 = Reg9_usr;
                        reg10 = Reg10_usr;
                        reg11 = Reg11_usr;
                        reg12 = Reg12_usr;
                        reg13 = Reg13_svc;
                        reg14 = Reg14_svc;
                        SPSR = SPSR_svc;
                        break;
                    case AbortMode:
                        reg8 = Reg8_usr;
                        reg9 = Reg9_usr;
                        reg10 = Reg10_usr;
                        reg11 = Reg11_usr;
                        reg12 = Reg12_usr;
                        reg13 = Reg13_abt;
                        reg14 = Reg14_abt;
                        SPSR = SPSR_abt;
                        break;
                    case UndefinedMode:
                        reg8 = Reg8_usr;
                        reg9 = Reg9_usr;
                        reg10 = Reg10_usr;
                        reg11 = Reg11_usr;
                        reg12 = Reg12_usr;
                        reg13 = Reg13_und;
                        reg14 = Reg14_und;
                        SPSR = SPSR_und;
                        break;
                    case InterruptMode:
                        reg8 = Reg8_usr;
                        reg9 = Reg9_usr;
                        reg10 = Reg10_usr;
                        reg11 = Reg11_usr;
                        reg12 = Reg12_usr;
                        reg13 = Reg13_irq;
                        reg14 = Reg14_irq;
                        SPSR = SPSR_irq;
                        break;
                    case FastInterruptMode:
                        reg8 = Reg8_fiq;
                        reg9 = Reg9_fiq;
                        reg10 = Reg10_fiq;
                        reg11 = Reg11_fiq;
                        reg12 = Reg12_fiq;
                        reg13 = Reg13_fiq;
                        reg14 = Reg14_fiq;
                        SPSR = SPSR_fiq;
                        break;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal UInt32 GetFlag(Flag flag)
        {
            return (CPSR >> (int)flag) & 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SetFlag(Flag flag, UInt32 value)
        {
            CPSR = (CPSR & ~(1u << (int)flag)) | (value << (int)flag);
        }

        internal bool ConditionPassed(UInt32 cond)
        {
            return cond switch
            {
                // EQ
                0b0000 => GetFlag(Flag.Z) == 1,
                // NE
                0b0001 => GetFlag(Flag.Z) == 0,
                // CS/HS
                0b0010 => GetFlag(Flag.C) == 1,
                // CC/LO
                0b0011 => GetFlag(Flag.C) == 0,
                // MI
                0b0100 => GetFlag(Flag.N) == 1,
                // PL
                0b0101 => GetFlag(Flag.N) == 0,
                // VS
                0b0110 => GetFlag(Flag.V) == 1,
                // VC
                0b0111 => GetFlag(Flag.V) == 0,
                // HI
                0b1000 => (GetFlag(Flag.C) == 1) && (GetFlag(Flag.Z) == 0),
                // LS
                0b1001 => (GetFlag(Flag.C) == 0) || (GetFlag(Flag.Z) == 1),
                // GE
                0b1010 => GetFlag(Flag.N) == GetFlag(Flag.V),
                // LT
                0b1011 => GetFlag(Flag.N) != GetFlag(Flag.V),
                // GT
                0b1100 => (GetFlag(Flag.Z) == 0) && (GetFlag(Flag.N) == GetFlag(Flag.V)),
                // LE
                0b1101 => (GetFlag(Flag.Z) == 1) || (GetFlag(Flag.N) != GetFlag(Flag.V)),
                // AL
                0b1110 => true,
                // NV
                0b1111 => false,
                // should never happen
                _ => throw new Exception(string.Format("Iris.CPU.CPU: Wrong condition code {0}", cond)),
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static UInt32 Not(UInt32 flag)
        {
            return flag ^ 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static UInt32 CarryFrom(UInt64 result)
        {
            return (result > 0xffff_ffff) ? 1u : 0u;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static UInt32 BorrowFrom(UInt32 leftOperand, UInt64 rightOperand)
        {
            return (leftOperand < rightOperand) ? 1u : 0u;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static UInt32 OverflowFrom_Addition(UInt32 leftOperand, UInt32 rightOperand, UInt32 result)
        {
            return (((leftOperand >> 31) == (rightOperand >> 31))
                 && ((leftOperand >> 31) != (result >> 31))) ? 1u : 0u;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static UInt32 OverflowFrom_Subtraction(UInt32 leftOperand, UInt32 rightOperand, UInt32 result)
        {
            return (((leftOperand >> 31) != (rightOperand >> 31))
                 && ((leftOperand >> 31) != (result >> 31))) ? 1u : 0u;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static UInt32 ArithmeticShiftRight(UInt32 value, int shiftAmount)
        {
            return (UInt32)((Int32)value >> shiftAmount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static UInt32 SignExtend(UInt32 value, int size)
        {
            return value | ~((value & (1u << (size - 1))) - 1);
        }

        internal static UInt32 ComputeMultiplicationCycleCount(UInt32 leftMultiplier, UInt32 rightMultiplier)
        {
            static UInt32 ComputeMultiplierCycleCount(UInt32 multiplier)
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                static bool CheckMultiplierAgainstMask(UInt32 multiplier, UInt32 mask)
                {
                    UInt32 masked = multiplier & mask;
                    return (masked == 0) || (masked == mask);
                }

                if (CheckMultiplierAgainstMask(multiplier, 0xffff_ff00))
                    return 1;
                else if (CheckMultiplierAgainstMask(multiplier, 0xffff_0000))
                    return 2;
                else if (CheckMultiplierAgainstMask(multiplier, 0xff00_0000))
                    return 3;
                else
                    return 4;
            }

            return Math.Max(ComputeMultiplierCycleCount(leftMultiplier), ComputeMultiplierCycleCount(rightMultiplier));
        }
    }
}