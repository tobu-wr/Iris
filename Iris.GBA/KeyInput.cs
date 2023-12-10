using static Iris.Common.System;

namespace Iris.GBA
{
    internal sealed class KeyInput
    {
        internal UInt16 _KEYINPUT;
        internal UInt16 _KEYCNT;

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

        internal void SetKeyStatus(Key key, KeyStatus status)
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
