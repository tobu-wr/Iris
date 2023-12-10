namespace Iris.GBA
{
    internal sealed class Communication
    {
        internal UInt16 _SIODATA0; // SIOMULTI0 / SIODATA32_L
        internal UInt16 _SIODATA1; // SIOMULTI1 / SIODATA32_H
        internal UInt16 _SIODATA2; // SIOMULTI2
        internal UInt16 _SIODATA3; // SIOMULTI3

        internal UInt16 _SIOCNT;
        internal UInt16 _SIODATA_SEND; // SIOMLT_SEND / SIODATA_8

        internal UInt16 _RCNT;

        internal UInt16 _JOYCNT;

        internal UInt16 _JOY_RECV_L;
        internal UInt16 _JOY_RECV_H;

        internal UInt16 _JOY_TRANS_L;
        internal UInt16 _JOY_TRANS_H;

        internal UInt16 _JOYSTAT;

        internal void ResetState()
        {
            _SIODATA0 = 0;
            _SIODATA1 = 0;
            _SIODATA2 = 0;
            _SIODATA3 = 0;

            _SIOCNT = 0;
            _SIODATA_SEND = 0;

            _RCNT = 0;

            _JOYCNT = 0;

            _JOY_RECV_L = 0;
            _JOY_RECV_H = 0;

            _JOY_TRANS_L = 0;
            _JOY_TRANS_H = 0;

            _JOYSTAT = 0;
        }

        internal void LoadState(BinaryReader reader)
        {
            _SIODATA0 = reader.ReadUInt16();
            _SIODATA1 = reader.ReadUInt16();
            _SIODATA2 = reader.ReadUInt16();
            _SIODATA3 = reader.ReadUInt16();

            _SIOCNT = reader.ReadUInt16();
            _SIODATA_SEND = reader.ReadUInt16();

            _RCNT = reader.ReadUInt16();

            _JOYCNT = reader.ReadUInt16();

            _JOY_RECV_L = reader.ReadUInt16();
            _JOY_RECV_H = reader.ReadUInt16();

            _JOY_TRANS_L = reader.ReadUInt16();
            _JOY_TRANS_H = reader.ReadUInt16();

            _JOYSTAT = reader.ReadUInt16();
        }

        internal void SaveState(BinaryWriter writer)
        {
            writer.Write(_SIODATA0);
            writer.Write(_SIODATA1);
            writer.Write(_SIODATA2);
            writer.Write(_SIODATA3);

            writer.Write(_SIOCNT);
            writer.Write(_SIODATA_SEND);

            writer.Write(_RCNT);

            writer.Write(_JOYCNT);

            writer.Write(_JOY_RECV_L);
            writer.Write(_JOY_RECV_H);

            writer.Write(_JOY_TRANS_L);
            writer.Write(_JOY_TRANS_H);

            writer.Write(_JOYSTAT);
        }
    }
}
