namespace Iris.Emulation.GBA
{
    internal sealed partial class Core
    {
        internal enum Interrupt
        {
            VBlank = 1 << 0,
        }

        private UInt16 _IE;
        private UInt16 _IF;
        private UInt16 _IME;

        internal void RequestInterrupt(Interrupt interrupt)
        {
            _IF |= (UInt16)interrupt;
            UpdateInterrupts();
        }

        private void UpdateInterrupts()
        {
            _CPU.NIRQ = ((_IME == 0) || (_IE & _IF) == 0) ? CPU.Core.Signal.High : CPU.Core.Signal.Low;
        }
    }
}
