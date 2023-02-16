namespace Iris.Core
{
    internal sealed partial class CPU
    {
        internal enum Architecture
        {
            ARMv4T,
            ARMv5TE
        };

        internal struct CallbackInterface
        {
            internal delegate byte ReadMemory8_Delegate(uint address);
            internal delegate ushort ReadMemory16_Delegate(uint address);
            internal delegate uint ReadMemory32_Delegate(uint address);
            internal delegate void WriteMemory8_Delegate(uint address, byte value);
            internal delegate void WriteMemory16_Delegate(uint address, ushort value);
            internal delegate void WriteMemory32_Delegate(uint address, uint value);
            internal delegate void HandleSWI_Delegate(uint value);
            internal delegate void HandleIRQ_Delegate();

            internal ReadMemory8_Delegate ReadMemory8;
            internal ReadMemory16_Delegate ReadMemory16;
            internal ReadMemory32_Delegate ReadMemory32;
            internal WriteMemory8_Delegate WriteMemory8;
            internal WriteMemory16_Delegate WriteMemory16;
            internal WriteMemory32_Delegate WriteMemory32;
            internal HandleSWI_Delegate HandleSWI;
            internal HandleIRQ_Delegate HandleIRQ;
        }

        private enum Flags
        {
            V = 28,
            C = 29,
            Z = 30,
            N = 31
        };

        private const uint ModeMask = 0b1_1111;
        private const uint UserMode = 0b1_0000;
        private const uint SystemMode = 0b1_1111;
        private const uint SupervisorMode = 0b1_0011;
        private const uint AbortMode = 0b1_0111;
        private const uint UndefinedMode = 0b1_1011;
        private const uint InterruptMode = 0b1_0010;
        private const uint FastInterruptMode = 0b1_0001;

        internal const uint SP = 13;
        internal const uint LR = 14;
        internal const uint PC = 15;

        internal readonly uint[] Reg = new uint[16];
        internal uint CPSR = 0b1_0000;
        internal uint SPSR;

        internal uint Reg8_usr, Reg9_usr, Reg10_usr, Reg11_usr, Reg12_usr, Reg13_usr, Reg14_usr;
        internal uint Reg13_svc, Reg14_svc;
        internal uint Reg13_abt, Reg14_abt;
        internal uint Reg13_und, Reg14_und;
        internal uint Reg13_irq, Reg14_irq;
        internal uint Reg8_fiq, Reg9_fiq, Reg10_fiq, Reg11_fiq, Reg12_fiq, Reg13_fiq, Reg14_fiq;
        internal uint SPSR_svc, SPSR_abt, SPSR_und, SPSR_irq, SPSR_fiq;

        private readonly Architecture _architecture;
        private readonly CallbackInterface _callbackInterface;

        internal uint NextInstructionAddress;
        internal bool IRQPending;

        internal CPU(Architecture architecture, CallbackInterface callbacks)
        {
            _architecture = architecture;
            _callbackInterface = callbacks;
        }

        internal void Step()
        {
            uint i = CPSR >> 7 & 1;

            if (IRQPending && i == 0)
            {
                _callbackInterface.HandleIRQ();
            }

            uint t = CPSR >> 5 & 1;

            if (t == 0)
                ARM_Step();
            else
                THUMB_Step();
        }

        private void SetCPSR(uint value)
        {
            uint previousMode = CPSR & ModeMask;
            uint newMode = value & ModeMask;

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

        private uint GetFlag(Flags flag)
        {
            return CPSR >> (int)flag & 1;
        }

        private void SetFlag(Flags flag, uint value)
        {
            CPSR = CPSR & ~(1u << (int)flag) | value << (int)flag;
        }

        private bool ConditionPassed(uint cond)
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
                0b1000 => GetFlag(Flags.C) == 1 && GetFlag(Flags.Z) == 0,
                // LS
                0b1001 => GetFlag(Flags.C) == 0 || GetFlag(Flags.Z) == 1,
                // GE
                0b1010 => GetFlag(Flags.N) == GetFlag(Flags.V),
                // LT
                0b1011 => GetFlag(Flags.N) != GetFlag(Flags.V),
                // GT
                0b1100 => GetFlag(Flags.Z) == 0 && GetFlag(Flags.N) == GetFlag(Flags.V),
                // LE
                0b1101 => GetFlag(Flags.Z) == 1 || GetFlag(Flags.N) != GetFlag(Flags.V),
                // AL
                0b1110 => true,
                // NV
                0b1111 => false,
                // should never happen
                _ => throw new Exception(string.Format("CPU: Wrong condition code {0}", cond)),
            };
        }

        private static uint Not(uint flag)
        {
            return ~flag & 1;
        }

        private static uint CarryFrom(ulong result)
        {
            return result > 0xffff_ffff ? 1u : 0u;
        }

        private static uint BorrowFrom(uint leftOperand, ulong rightOperand)
        {
            return leftOperand < rightOperand ? 1u : 0u;
        }

        private static uint OverflowFrom_Addition(uint leftOperand, uint rightOperand, uint result)
        {
            return leftOperand >> 31 == rightOperand >> 31
                 && leftOperand >> 31 != result >> 31 ? 1u : 0u;
        }

        private static uint OverflowFrom_Subtraction(uint leftOperand, uint rightOperand, uint result)
        {
            return leftOperand >> 31 != rightOperand >> 31
                 && leftOperand >> 31 != result >> 31 ? 1u : 0u;
        }

        private static uint ArithmeticShiftRight(uint value, int shiftAmount)
        {
            return (uint)((int)value >> shiftAmount);
        }

        private static uint SignExtend(uint value, int size)
        {
            return value >> size - 1 == 1 ? value | 0xffff_ffff << size : value;
        }
    }
}
