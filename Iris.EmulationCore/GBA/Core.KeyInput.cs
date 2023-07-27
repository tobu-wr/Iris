using Iris.Common;

namespace Iris.EmulationCore.GBA
{
    public sealed partial class Core
    {
        private UInt16 _KEYINPUT;
        private UInt16 _KEYCNT;

        public void SetKeyStatus(ICore.Key key, ICore.KeyStatus status)
        {
            int pos;

            switch (key)
            {
                case ICore.Key.A:
                    pos = 0;
                    break;
                case ICore.Key.B:
                    pos = 1;
                    break;
                case ICore.Key.Select:
                    pos = 2;
                    break;
                case ICore.Key.Start:
                    pos = 3;
                    break;
                case ICore.Key.Right:
                    pos = 4;
                    break;
                case ICore.Key.Left:
                    pos = 5;
                    break;
                case ICore.Key.Up:
                    pos = 6;
                    break;
                case ICore.Key.Down:
                    pos = 7;
                    break;
                case ICore.Key.R:
                    pos = 8;
                    break;
                case ICore.Key.L:
                    pos = 9;
                    break;
                default:
                    return;
            }

            _KEYINPUT = (UInt16)((_KEYINPUT & ~(1 << pos)) | ((int)status << pos));
        }
    }
}
