using System.Runtime.CompilerServices;

namespace Iris.CPU
{
    public sealed class CPU
    {
        public enum Architecture
        {
            ARMv4T,
            ARMv5TE
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

        internal readonly Architecture _architecture;
        internal readonly CallbackInterface _callbackInterface;

        private readonly ARM_Interpreter _armInterpreter;
        private readonly THUMB_Interpreter _thumbInterpreter;

        public UInt32 NextInstructionAddress;
        public Signal NIRQ;

        public CPU(Architecture architecture, CallbackInterface callbackInterface)
        {
            _architecture = architecture;
            _callbackInterface = callbackInterface;
            _armInterpreter = new(this);
            _thumbInterpreter = new(this);
        }

        public void Step()
        {
            UInt32 i = (CPSR >> 7) & 1;

            if ((i == 0) && (NIRQ == Signal.Low))
                _callbackInterface.HandleIRQ();

            UInt32 t = (CPSR >> 5) & 1;

            if (t == 0)
                _armInterpreter.Step();
            else
                _thumbInterpreter.Step();
        }

        public void SetCPSR(UInt32 value)
        {
            UInt32 previousMode = CPSR & ModeMask;
            UInt32 newMode = value & ModeMask;

            CPSR = value | 0b1_0000;

            if (previousMode != newMode)
            {
                // save previous mode registers
                switch (previousMode)
                {
                    case UserMode:
                    case SystemMode:
                        Reg8_usr = Reg[8];
                        Reg9_usr = Reg[9];
                        Reg10_usr = Reg[10];
                        Reg11_usr = Reg[11];
                        Reg12_usr = Reg[12];
                        Reg13_usr = Reg[13];
                        Reg14_usr = Reg[14];
                        break;
                    case SupervisorMode:
                        Reg8_usr = Reg[8];
                        Reg9_usr = Reg[9];
                        Reg10_usr = Reg[10];
                        Reg11_usr = Reg[11];
                        Reg12_usr = Reg[12];
                        Reg13_svc = Reg[13];
                        Reg14_svc = Reg[14];
                        SPSR_svc = SPSR;
                        break;
                    case AbortMode:
                        Reg8_usr = Reg[8];
                        Reg9_usr = Reg[9];
                        Reg10_usr = Reg[10];
                        Reg11_usr = Reg[11];
                        Reg12_usr = Reg[12];
                        Reg13_abt = Reg[13];
                        Reg14_abt = Reg[14];
                        SPSR_abt = SPSR;
                        break;
                    case UndefinedMode:
                        Reg8_usr = Reg[8];
                        Reg9_usr = Reg[9];
                        Reg10_usr = Reg[10];
                        Reg11_usr = Reg[11];
                        Reg12_usr = Reg[12];
                        Reg13_und = Reg[13];
                        Reg14_und = Reg[14];
                        SPSR_und = SPSR;
                        break;
                    case InterruptMode:
                        Reg8_usr = Reg[8];
                        Reg9_usr = Reg[9];
                        Reg10_usr = Reg[10];
                        Reg11_usr = Reg[11];
                        Reg12_usr = Reg[12];
                        Reg13_irq = Reg[13];
                        Reg14_irq = Reg[14];
                        SPSR_irq = SPSR;
                        break;
                    case FastInterruptMode:
                        Reg8_fiq = Reg[8];
                        Reg9_fiq = Reg[9];
                        Reg10_fiq = Reg[10];
                        Reg11_fiq = Reg[11];
                        Reg12_fiq = Reg[12];
                        Reg13_fiq = Reg[13];
                        Reg14_fiq = Reg[14];
                        SPSR_fiq = SPSR;
                        break;
                }

                // load new mode registers
                switch (newMode)
                {
                    case UserMode:
                    case SystemMode:
                        Reg[8] = Reg8_usr;
                        Reg[9] = Reg9_usr;
                        Reg[10] = Reg10_usr;
                        Reg[11] = Reg11_usr;
                        Reg[12] = Reg12_usr;
                        Reg[13] = Reg13_usr;
                        Reg[14] = Reg14_usr;
                        break;
                    case SupervisorMode:
                        Reg[8] = Reg8_usr;
                        Reg[9] = Reg9_usr;
                        Reg[10] = Reg10_usr;
                        Reg[11] = Reg11_usr;
                        Reg[12] = Reg12_usr;
                        Reg[13] = Reg13_svc;
                        Reg[14] = Reg14_svc;
                        SPSR = SPSR_svc;
                        break;
                    case AbortMode:
                        Reg[8] = Reg8_usr;
                        Reg[9] = Reg9_usr;
                        Reg[10] = Reg10_usr;
                        Reg[11] = Reg11_usr;
                        Reg[12] = Reg12_usr;
                        Reg[13] = Reg13_abt;
                        Reg[14] = Reg14_abt;
                        SPSR = SPSR_abt;
                        break;
                    case UndefinedMode:
                        Reg[8] = Reg8_usr;
                        Reg[9] = Reg9_usr;
                        Reg[10] = Reg10_usr;
                        Reg[11] = Reg11_usr;
                        Reg[12] = Reg12_usr;
                        Reg[13] = Reg13_und;
                        Reg[14] = Reg14_und;
                        SPSR = SPSR_und;
                        break;
                    case InterruptMode:
                        Reg[8] = Reg8_usr;
                        Reg[9] = Reg9_usr;
                        Reg[10] = Reg10_usr;
                        Reg[11] = Reg11_usr;
                        Reg[12] = Reg12_usr;
                        Reg[13] = Reg13_irq;
                        Reg[14] = Reg14_irq;
                        SPSR = SPSR_irq;
                        break;
                    case FastInterruptMode:
                        Reg[8] = Reg8_fiq;
                        Reg[9] = Reg9_fiq;
                        Reg[10] = Reg10_fiq;
                        Reg[11] = Reg11_fiq;
                        Reg[12] = Reg12_fiq;
                        Reg[13] = Reg13_fiq;
                        Reg[14] = Reg14_fiq;
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
            return ~flag & 1;
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
            return ((value >> (size - 1)) == 1) ? (value | (0xffff_ffff << size)) : value;
        }
    }
}
