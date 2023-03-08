namespace Iris.Emulation.GBA
{
    internal sealed partial class Core
    {
        private enum Interrupt
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

        private UInt16 _IE;
        private UInt16 _IF;
        private UInt16 _IME;

        private void RequestInterrupt(Interrupt interrupt)
        {
            _IF |= (UInt16)interrupt;
            UpdateInterrupts();
        }

        private void UpdateInterrupts()
        {
            _CPU.NIRQ = ((_IME == 0) || (_IE & _IF) == 0) ? CPU.Signal.High : CPU.Signal.Low;
        }
    }
}
