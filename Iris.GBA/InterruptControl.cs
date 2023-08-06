using Iris.CPU;

namespace Iris.GBA
{
    internal sealed class InterruptControl
    {
        internal enum Interrupt
        {
            VBlank = 1 << 0,
            //HBlank = 1 << 1,
            //VCounterMatch = 1 << 2,
            //Timer0 = 1 << 3,
            //Timer1 = 1 << 4,
            //Timer2 = 1 << 5,
            //Timer3 = 1 << 6,
            //SIO = 1 << 7,
            //DMA0 = 1 << 8,
            //DMA1 = 1 << 9,
            //DMA2 = 1 << 10,
            //DMA3 = 1 << 11,
            //Key = 1 << 12,
            //GamePak = 1 << 13,
        }

        internal UInt16 _IE;
        internal UInt16 _IF;
        internal UInt16 _IME;

        private readonly CPU_Core _cpu;

        internal InterruptControl(CPU_Core cpu)
        {
            _cpu = cpu;
        }

        internal void Reset()
        {
            _IE = 0;
            _IF = 0;
            _IME = 0;

            _cpu.NIRQ = CPU_Core.Signal.High;
        }

        internal void RequestInterrupt(Interrupt interrupt)
        {
            _IF |= (UInt16)interrupt;
            UpdateInterrupts();
        }

        internal void UpdateInterrupts()
        {
            _cpu.NIRQ = ((_IME == 0) || ((_IE & _IF) == 0)) ? CPU_Core.Signal.High : CPU_Core.Signal.Low;
        }
    }
}
