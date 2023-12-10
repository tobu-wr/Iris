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

        internal void ResetState()
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

        internal void LoadState(BinaryReader reader)
        {
            _TM0CNT_L = reader.ReadUInt16();
            _TM0CNT_H = reader.ReadUInt16();

            _TM1CNT_L = reader.ReadUInt16();
            _TM1CNT_H = reader.ReadUInt16();

            _TM2CNT_L = reader.ReadUInt16();
            _TM2CNT_H = reader.ReadUInt16();

            _TM3CNT_L = reader.ReadUInt16();
            _TM3CNT_H = reader.ReadUInt16();
        }

        internal void SaveState(BinaryWriter writer)
        {
            writer.Write(_TM0CNT_L);
            writer.Write(_TM0CNT_H);

            writer.Write(_TM1CNT_L);
            writer.Write(_TM1CNT_H);

            writer.Write(_TM2CNT_L);
            writer.Write(_TM2CNT_H);

            writer.Write(_TM3CNT_L);
            writer.Write(_TM3CNT_H);
        }
    }
}
