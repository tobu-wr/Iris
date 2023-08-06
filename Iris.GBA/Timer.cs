namespace Iris.GBA
{
    internal sealed class Timer
    {
        internal UInt16 _TM0CNT_L;
        internal UInt16 _TM0CNT_H;

        internal UInt16 _TM1CNT_L;
        internal UInt16 _TM1CNT_H;

        internal UInt16 _TM2CNT_L;
        internal UInt16 _TM2CNT_H;

        internal UInt16 _TM3CNT_L;
        internal UInt16 _TM3CNT_H;

        internal void Reset()
        {
            _TM0CNT_L = 0;
            _TM0CNT_H = 0;

            _TM1CNT_L = 0;
            _TM1CNT_H = 0;

            _TM2CNT_L = 0;
            _TM2CNT_H = 0;

            _TM3CNT_L = 0;
            _TM3CNT_H = 0;
        }
    }
}
