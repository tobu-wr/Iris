namespace Iris.Emulation.GBA
{
    public sealed partial class Core
    {
        internal enum Keys
        {
            A = 0,
            B = 1,
            Select = 2,
            Start = 3,
            Right = 4,
            Left = 5,
            Up = 6,
            Down = 7,
            R = 8,
            L = 9,
        }

        internal enum KeyStatus
        {
            Input = 0,
            NoInput = 1
        }

        private UInt16 _KEYINPUT;
        private UInt16 _KEYCNT;

        internal void SetKeyStatus(Keys key, KeyStatus status)
        {
            _KEYINPUT = (UInt16)((_KEYINPUT & ~(1 << (int)key)) | ((int)status << (int)key));
        }
    }
}
