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

        internal readonly UInt32[] Reg = new UInt32[16];
        internal UInt32 CPSR = 0b1_0000;
        internal UInt32 SPSR;

        internal UInt32 Reg8_usr, Reg9_usr, Reg10_usr, Reg11_usr, Reg12_usr, Reg13_usr, Reg14_usr;
        internal UInt32 Reg13_svc, Reg14_svc;
        internal UInt32 Reg13_abt, Reg14_abt;
        internal UInt32 Reg13_und, Reg14_und;
        internal UInt32 Reg13_irq, Reg14_irq;
        internal UInt32 Reg8_fiq, Reg9_fiq, Reg10_fiq, Reg11_fiq, Reg12_fiq, Reg13_fiq, Reg14_fiq;
        internal UInt32 SPSR_svc, SPSR_abt, SPSR_und, SPSR_irq, SPSR_fiq;

        private readonly ICallbacks _callbacks;
        internal UInt32 NextInstructionAddress;

        internal CPU(ICallbacks callbacks)
        {
            _callbacks = callbacks;
        }

        internal void Step()
        {
            UInt32 t = (CPSR >> 5) & 1;
            if (t == 0)
                ARM_Step();
            else
                THUMB_Step();
        }

        private void SetCPSR(UInt32 value)
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

        private UInt32 GetFlag(Flags flag)
        {
            return (CPSR >> (int)flag) & 1;
        }

        private void SetFlag(Flags flag, UInt32 value)
        {
            CPSR = (CPSR & ~(1u << (int)flag)) | (value << (int)flag);
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

        private static UInt32 ArithmeticShiftRight(UInt32 value, int shiftAmount)
        {
            return (UInt32)((Int32)value >> shiftAmount);
        }

        private static UInt32 SignExtend(UInt32 value, int size)
        {
            return ((value >> (size - 1)) == 1) ? value | (0xffff_ffff << size) : value;
        }
    }
}
