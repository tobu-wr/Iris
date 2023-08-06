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

        //internal UInt16 _JOYCNT;

        //internal UInt16 _JOY_RECV_L;
        //internal UInt16 _JOY_RECV_H;

        //internal UInt16 _JOY_TRANS_L;
        //internal UInt16 _JOY_TRANS_H;

        //internal UInt16 _JOYSTAT;

        internal void Reset()
        {
            _SIODATA0 = 0;
            _SIODATA1 = 0;
            _SIODATA2 = 0;
            _SIODATA3 = 0;

            _SIOCNT = 0;
            _SIODATA_SEND = 0;

            _RCNT = 0;
        }
    }
}
