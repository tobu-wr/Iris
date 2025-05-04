namespace Iris.CPU
{
    public sealed class CPU_Core
    {
        public enum Model
        {
            ARM7TDMI,
            ARM946ES
        }

        public delegate Byte Read8_Delegate(UInt32 address);
        public delegate UInt16 Read16_Delegate(UInt32 address);
        public delegate UInt32 Read32_Delegate(UInt32 address);
        public delegate void Write8_Delegate(UInt32 address, Byte value);
        public delegate void Write16_Delegate(UInt32 address, UInt16 value);
        public delegate void Write32_Delegate(UInt32 address, UInt32 value);
        public delegate UInt64 HandleSWI_Delegate();
        public delegate UInt64 HandleIRQ_Delegate();

        public readonly record struct CallbackInterface
        (
            Read8_Delegate Read8,
            Read16_Delegate Read16,
            Read32_Delegate Read32,
            Write8_Delegate Write8,
            Write16_Delegate Write16,
            Write32_Delegate Write32,
            HandleSWI_Delegate HandleSWI,
            HandleIRQ_Delegate HandleIRQ
        );

        public enum Signal
        {
            High,
            Low
        }

        internal enum Flag
        {
            V = 28,
            C = 29,
            Z = 30,
            N = 31
        }

        internal unsafe readonly struct InstructionListEntry<T>(T mask, T expected, delegate*<CPU_Core, T, UInt64> handler, List<Model> modelList)
        {
            internal readonly T _mask = mask;
            internal readonly T _expected = expected;
            internal unsafe readonly delegate*<CPU_Core, T, UInt64> _handler = handler;
            internal readonly List<Model> _modelList = modelList;
        }

        internal unsafe readonly struct InstructionLUTEntry<T>(delegate*<CPU_Core, T, UInt64> handler)
        {
            internal unsafe readonly delegate*<CPU_Core, T, UInt64> _handler = handler;
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
        public UInt32 CPSR;
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

        public void ResetState()
        {
            Array.Clear(Reg);

            CPSR = 0b1_0000;
            SPSR = 0;

            Reg8_usr = 0;
            Reg9_usr = 0;
            Reg10_usr = 0;
            Reg11_usr = 0;
            Reg12_usr = 0;
            Reg13_usr = 0;
            Reg14_usr = 0;

            Reg13_svc = 0;
            Reg14_svc = 0;

            Reg13_abt = 0;
            Reg14_abt = 0;

            Reg13_und = 0;
            Reg14_und = 0;

            Reg13_irq = 0;
            Reg14_irq = 0;

            Reg8_fiq = 0;
            Reg9_fiq = 0;
            Reg10_fiq = 0;
            Reg11_fiq = 0;
            Reg12_fiq = 0;
            Reg13_fiq = 0;
            Reg14_fiq = 0;

            SPSR_svc = 0;
            SPSR_abt = 0;
            SPSR_und = 0;
            SPSR_irq = 0;
            SPSR_fiq = 0;

            NextInstructionAddress = 0;
            NIRQ = Signal.High;
        }

        public void LoadState(BinaryReader reader)
        {
            foreach (ref UInt32 reg in Reg.AsSpan())
                reg = reader.ReadUInt32();

            CPSR = reader.ReadUInt32();
            SPSR = reader.ReadUInt32();

            Reg8_usr = reader.ReadUInt32();
            Reg9_usr = reader.ReadUInt32();
            Reg10_usr = reader.ReadUInt32();
            Reg11_usr = reader.ReadUInt32();
            Reg12_usr = reader.ReadUInt32();
            Reg13_usr = reader.ReadUInt32();
            Reg14_usr = reader.ReadUInt32();

            Reg13_svc = reader.ReadUInt32();
            Reg14_svc = reader.ReadUInt32();

            Reg13_abt = reader.ReadUInt32();
            Reg14_abt = reader.ReadUInt32();

            Reg13_und = reader.ReadUInt32();
            Reg14_und = reader.ReadUInt32();

            Reg13_irq = reader.ReadUInt32();
            Reg14_irq = reader.ReadUInt32();

            Reg8_fiq = reader.ReadUInt32();
            Reg9_fiq = reader.ReadUInt32();
            Reg10_fiq = reader.ReadUInt32();
            Reg11_fiq = reader.ReadUInt32();
            Reg12_fiq = reader.ReadUInt32();
            Reg13_fiq = reader.ReadUInt32();
            Reg14_fiq = reader.ReadUInt32();

            SPSR_svc = reader.ReadUInt32();
            SPSR_abt = reader.ReadUInt32();
            SPSR_und = reader.ReadUInt32();
            SPSR_irq = reader.ReadUInt32();
            SPSR_fiq = reader.ReadUInt32();

            NextInstructionAddress = reader.ReadUInt32();
            NIRQ = (Signal)reader.ReadInt32();
        }

        public void SaveState(BinaryWriter writer)
        {
            foreach (UInt32 reg in Reg)
                writer.Write(reg);

            writer.Write(CPSR);
            writer.Write(SPSR);

            writer.Write(Reg8_usr);
            writer.Write(Reg9_usr);
            writer.Write(Reg10_usr);
            writer.Write(Reg11_usr);
            writer.Write(Reg12_usr);
            writer.Write(Reg13_usr);
            writer.Write(Reg14_usr);

            writer.Write(Reg13_svc);
            writer.Write(Reg14_svc);

            writer.Write(Reg13_abt);
            writer.Write(Reg14_abt);

            writer.Write(Reg13_und);
            writer.Write(Reg14_und);

            writer.Write(Reg13_irq);
            writer.Write(Reg14_irq);

            writer.Write(Reg8_fiq);
            writer.Write(Reg9_fiq);
            writer.Write(Reg10_fiq);
            writer.Write(Reg11_fiq);
            writer.Write(Reg12_fiq);
            writer.Write(Reg13_fiq);
            writer.Write(Reg14_fiq);

            writer.Write(SPSR_svc);
            writer.Write(SPSR_abt);
            writer.Write(SPSR_und);
            writer.Write(SPSR_irq);
            writer.Write(SPSR_fiq);

            writer.Write(NextInstructionAddress);
            writer.Write((int)NIRQ);
        }

        public UInt64 Step()
        {
            UInt32 i = (CPSR >> 7) & 1;

            if ((i == 0) && (NIRQ == Signal.Low))
                return _callbackInterface.HandleIRQ();

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

        internal UInt32 GetFlag(Flag flag)
        {
            return (CPSR >> (int)flag) & 1;
        }

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
                _ => throw new Exception($"Iris.CPU.CPU_Core: Wrong condition code {cond}"),
            };
        }

        internal static UInt32 Not(UInt32 flag)
        {
            return flag ^ 1;
        }

        internal static UInt32 CarryFrom(UInt64 result)
        {
            return (result > 0xffff_ffff) ? 1u : 0u;
        }

        internal static UInt32 BorrowFrom(UInt64 result)
        {
            return (result >= 0x8000_0000_0000_0000) ? 1u : 0u;
        }

        internal static UInt32 OverflowFrom_Addition(UInt32 leftOperand, UInt32 rightOperand, UInt32 result)
        {
            return (((leftOperand >> 31) == (rightOperand >> 31))
                 && ((leftOperand >> 31) != (result >> 31))) ? 1u : 0u;
        }

        internal static UInt32 OverflowFrom_Subtraction(UInt32 leftOperand, UInt32 rightOperand, UInt32 result)
        {
            return (((leftOperand >> 31) != (rightOperand >> 31))
                 && ((leftOperand >> 31) != (result >> 31))) ? 1u : 0u;
        }

        internal static UInt32 ArithmeticShiftRight(UInt32 value, int shiftAmount)
        {
            return (UInt32)((Int32)value >> shiftAmount);
        }

        internal static UInt32 SignExtend(UInt32 value, int size)
        {
            return value | ~((value & (1u << (size - 1))) - 1);
        }

        internal static UInt64 ComputeMultiplicationCycleCount(UInt32 leftMultiplier, UInt32 rightMultiplier)
        {
            static UInt64 ComputeMultiplierCycleCount(UInt32 multiplier)
            {
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
