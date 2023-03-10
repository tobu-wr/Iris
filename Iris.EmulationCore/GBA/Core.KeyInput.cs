using Iris.EmulationCore.Common;

namespace Iris.EmulationCore.GBA
{
    public sealed partial class Core
    {
        private UInt16 _KEYINPUT;
        private UInt16 _KEYCNT;

        public void SetKeyStatus(Key key, KeyStatus status)
        {
            int pos = key switch
            {
                Key.A => 0,
                Key.B => 1,
                Key.Select => 2,
                Key.Start => 3,
                Key.Right => 4,
                Key.Left => 5,
                Key.Up => 6,
                Key.Down => 7,
                Key.R => 8,
                Key.L => 9,
                _ => throw new Exception("Iris.EmulationCore.GBA.Core.KeyInput: Wrong key"),
            };

            _KEYINPUT = (UInt16)((_KEYINPUT & ~(1 << pos)) | ((int)status << pos));
        }
    }
}
