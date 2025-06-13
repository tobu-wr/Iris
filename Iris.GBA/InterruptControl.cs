using System.Diagnostics;

namespace Iris.GBA
{
    internal sealed class InterruptControl
    {
        internal enum Register
        {
            IE,
            IF,
            IME
        }

        internal enum Interrupt
        {
            VBlank = 1 << 0,
            HBlank = 1 << 1,
            VCountMatch = 1 << 2,
            Timer0 = 1 << 3,
            Timer1 = 1 << 4,
            Timer2 = 1 << 5,
            Timer3 = 1 << 6,
            SIO = 1 << 7,
            DMA0 = 1 << 8,
            DMA1 = 1 << 9,
            DMA2 = 1 << 10,
            DMA3 = 1 << 11,
            Key = 1 << 12,
            //GamePak = 1 << 13
        }

        private UInt16 _IE;
        private UInt16 _IF;
        private UInt16 _IME;

        private CPU.CPU_Core _cpu;

        internal void Initialize(CPU.CPU_Core cpu)
        {
            _cpu = cpu;
        }

        internal void ResetState()
        {
            _IE = 0;
            _IF = 0;
            _IME = 0;
        }

        internal void LoadState(BinaryReader reader)
        {
            _IE = reader.ReadUInt16();
            _IF = reader.ReadUInt16();
            _IME = reader.ReadUInt16();
        }

        internal void SaveState(BinaryWriter writer)
        {
            writer.Write(_IE);
            writer.Write(_IF);
            writer.Write(_IME);
        }

        internal UInt16 ReadRegister(Register register)
        {
            return register switch
            {
                Register.IE => _IE,
                Register.IF => _IF,
                Register.IME => _IME,

                // should never happen
                _ => throw new Exception("Iris.GBA.InterruptControl: Register read error"),
            };
        }

        internal void WriteRegister(Register register, UInt16 value, Memory.RegisterWriteMode mode)
        {
            switch (register)
            {
                case Register.IE:
                    Memory.WriteRegisterHelper(ref _IE, (UInt16)(value & 0x3fff), mode);
                    break;

                case Register.IF:
                    _IF &= (mode) switch
                    {
                        Memory.RegisterWriteMode.LowByte => (UInt16)~value,
                        Memory.RegisterWriteMode.HighByte => (UInt16)~(value << 8),
                        Memory.RegisterWriteMode.HalfWord => (UInt16)~value,
                        _ => throw new UnreachableException(),
                    };
                    break;

                case Register.IME:
                    Memory.WriteRegisterHelper(ref _IME, (UInt16)(value & 0x0001), mode);
                    break;

                // should never happen
                default:
                    throw new Exception("Iris.GBA.InterruptControl: Register write error");
            }

            CheckInterrupts();
        }

        internal void RequestInterrupt(Interrupt interrupt)
        {
            _IF |= (UInt16)interrupt;
            CheckInterrupts();
        }

        private void CheckInterrupts()
        {
            _cpu.NIRQ = ((_IME == 0) || ((_IE & _IF) == 0)) ? CPU.CPU_Core.Signal.High : CPU.CPU_Core.Signal.Low;
        }
    }
}
