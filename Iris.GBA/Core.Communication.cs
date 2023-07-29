namespace Iris.EmulationCore.GBA
{
    public sealed partial class Core
    {
        private UInt16 _SIODATA0; // SIOMULTI0 / SIODATA32_L
        private UInt16 _SIODATA1; // SIOMULTI1 / SIODATA32_H
        private UInt16 _SIODATA2; // SIOMULTI2
        private UInt16 _SIODATA3; // SIOMULTI3

        private UInt16 _SIOCNT;
        private UInt16 _SIODATA_SEND; // SIOMLT_SEND / SIODATA_8

        private UInt16 _RCNT;

        //private UInt16 _JOYCNT;

        //private UInt16 _JOY_RECV_L;
        //private UInt16 _JOY_RECV_H;

        //private UInt16 _JOY_TRANS_L;
        //private UInt16 _JOY_TRANS_H;

        //private UInt16 _JOYSTAT;
    }
}
