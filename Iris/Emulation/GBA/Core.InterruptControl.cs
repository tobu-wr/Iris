namespace Iris.Emulation.GBA
{
    internal sealed partial class Core
    {
        private UInt16 _IE;
        private UInt16 _IF;
        private UInt16 _IME;

        private void RequestVBlankInterrupt()
        {
            _IF |= 1;
            UpdateInterrupts();
        }

        private void UpdateInterrupts()
        {
            if (_IME == 1)
            {
                if ((_IE & _IF & 1) != 0) // VBlank
                    _CPU.NIRQ = CPU.Core.Signal.Low;
                else
                    _CPU.NIRQ = CPU.Core.Signal.High;
            }
            else
            {
                _CPU.NIRQ = CPU.Core.Signal.High;
            }
        }
    }
}
