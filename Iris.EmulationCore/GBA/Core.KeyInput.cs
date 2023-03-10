using Iris.EmulationCore.Common;

namespace Iris.EmulationCore.GBA
{
    public sealed partial class Core
    {
        private UInt16 _KEYINPUT;
        private UInt16 _KEYCNT;

        public void SetKeyStatus(Key key, KeyStatus status)
        {
            int pos;

            switch (key)
            {
                case Key.A:
                    pos = 0;
                    break;
                case Key.B:
                    pos = 1;
                    break;
                case Key.Select:
                    pos = 2;
                    break;
                case Key.Start:
                    pos = 3;
                    break;
                case Key.Right:
                    pos = 4;
                    break;
                case Key.Left:
                    pos = 5;
                    break;
                case Key.Up:
                    pos = 6;
                    break;
                case Key.Down:
                    pos = 7;
                    break;
                case Key.R:
                    pos = 8;
                    break;
                case Key.L:
                    pos = 9;
                    break;
                default:
                    return;
            }

            _KEYINPUT = (UInt16)((_KEYINPUT & ~(1 << pos)) | ((int)status << pos));
        }
    }
}
