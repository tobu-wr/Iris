using Iris.Common;

namespace Iris.GBA
{
    public sealed partial class GBA_System
    {
        private UInt16 _KEYINPUT;
        private UInt16 _KEYCNT;

        public void SetKeyStatus(ISystem.Key key, ISystem.KeyStatus status)
        {
            int pos;

            switch (key)
            {
                case ISystem.Key.A:
                    pos = 0;
                    break;
                case ISystem.Key.B:
                    pos = 1;
                    break;
                case ISystem.Key.Select:
                    pos = 2;
                    break;
                case ISystem.Key.Start:
                    pos = 3;
                    break;
                case ISystem.Key.Right:
                    pos = 4;
                    break;
                case ISystem.Key.Left:
                    pos = 5;
                    break;
                case ISystem.Key.Up:
                    pos = 6;
                    break;
                case ISystem.Key.Down:
                    pos = 7;
                    break;
                case ISystem.Key.R:
                    pos = 8;
                    break;
                case ISystem.Key.L:
                    pos = 9;
                    break;
                default:
                    return;
            }

            _KEYINPUT = (UInt16)((_KEYINPUT & ~(1 << pos)) | ((int)status << pos));
        }
    }
}
