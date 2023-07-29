using Iris.Common;

namespace Iris.EmulationCore.GBA
{
    public sealed partial class Core
    {
        private UInt16 _KEYINPUT;
        private UInt16 _KEYCNT;

        public void SetKeyStatus(ISystemCore.Key key, ISystemCore.KeyStatus status)
        {
            int pos;

            switch (key)
            {
                case ISystemCore.Key.A:
                    pos = 0;
                    break;
                case ISystemCore.Key.B:
                    pos = 1;
                    break;
                case ISystemCore.Key.Select:
                    pos = 2;
                    break;
                case ISystemCore.Key.Start:
                    pos = 3;
                    break;
                case ISystemCore.Key.Right:
                    pos = 4;
                    break;
                case ISystemCore.Key.Left:
                    pos = 5;
                    break;
                case ISystemCore.Key.Up:
                    pos = 6;
                    break;
                case ISystemCore.Key.Down:
                    pos = 7;
                    break;
                case ISystemCore.Key.R:
                    pos = 8;
                    break;
                case ISystemCore.Key.L:
                    pos = 9;
                    break;
                default:
                    return;
            }

            _KEYINPUT = (UInt16)((_KEYINPUT & ~(1 << pos)) | ((int)status << pos));
        }
    }
}
