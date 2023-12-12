namespace Iris.GBA
{
    internal sealed class KeyInput(Common.System.PollInput_Delegate pollInputCallback)
    {
        internal UInt16 _KEYINPUT;
        internal UInt16 _KEYCNT;

        private readonly Common.System.PollInput_Delegate _pollInputCallback = pollInputCallback;

        internal void ResetState()
        {
            _KEYINPUT = 0x03ff;
            _KEYCNT = 0;
        }

        internal void LoadState(BinaryReader reader)
        {
            _KEYINPUT = reader.ReadUInt16();
            _KEYCNT = reader.ReadUInt16();
        }

        internal void SaveState(BinaryWriter writer)
        {
            writer.Write(_KEYINPUT);
            writer.Write(_KEYCNT);
        }

        internal void PollInput()
        {
            _pollInputCallback();
        }

        internal void SetKeyStatus(Common.System.Key key, Common.System.KeyStatus status)
        {
            int pos;

            switch (key)
            {
                case Common.System.Key.A:
                    pos = 0;
                    break;
                case Common.System.Key.B:
                    pos = 1;
                    break;
                case Common.System.Key.Select:
                    pos = 2;
                    break;
                case Common.System.Key.Start:
                    pos = 3;
                    break;
                case Common.System.Key.Right:
                    pos = 4;
                    break;
                case Common.System.Key.Left:
                    pos = 5;
                    break;
                case Common.System.Key.Up:
                    pos = 6;
                    break;
                case Common.System.Key.Down:
                    pos = 7;
                    break;
                case Common.System.Key.R:
                    pos = 8;
                    break;
                case Common.System.Key.L:
                    pos = 9;
                    break;
                default:
                    return;
            }

            _KEYINPUT = (UInt16)((_KEYINPUT & ~(1 << pos)) | ((int)status << pos));
        }
    }
}
