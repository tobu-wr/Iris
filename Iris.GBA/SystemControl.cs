namespace Iris.GBA
{
    internal sealed class SystemControl
    {
        internal UInt16 _WAITCNT;
        internal Byte _POSTFLG;
        internal Byte _HALTCNT;

        internal void ResetState()
        {
            _WAITCNT = 0;
            _POSTFLG = 0;
            _HALTCNT = 0;
        }

        internal void LoadState(BinaryReader reader)
        {
            _WAITCNT = reader.ReadUInt16();
            _POSTFLG = reader.ReadByte();
            _HALTCNT = reader.ReadByte();
        }

        internal void SaveState(BinaryWriter writer)
        {
            writer.Write(_WAITCNT);
            writer.Write(_POSTFLG);
            writer.Write(_HALTCNT);
        }
    }
}
