namespace Iris.GBA
{
    internal sealed class DMA
    {
        internal UInt16 _DMA0SAD_L;
        internal UInt16 _DMA0SAD_H;

        internal UInt16 _DMA0DAD_L;
        internal UInt16 _DMA0DAD_H;

        internal UInt16 _DMA0CNT_L;
        internal UInt16 _DMA0CNT_H;

        internal UInt16 _DMA1SAD_L;
        internal UInt16 _DMA1SAD_H;

        internal UInt16 _DMA1DAD_L;
        internal UInt16 _DMA1DAD_H;

        internal UInt16 _DMA1CNT_L;
        internal UInt16 _DMA1CNT_H;

        internal UInt16 _DMA2SAD_L;
        internal UInt16 _DMA2SAD_H;

        internal UInt16 _DMA2DAD_L;
        internal UInt16 _DMA2DAD_H;

        internal UInt16 _DMA2CNT_L;
        internal UInt16 _DMA2CNT_H;

        internal UInt16 _DMA3SAD_L;
        internal UInt16 _DMA3SAD_H;

        internal UInt16 _DMA3DAD_L;
        internal UInt16 _DMA3DAD_H;

        internal UInt16 _DMA3CNT_L;
        internal UInt16 _DMA3CNT_H;

        private Memory _memory;

        internal void Initialize(Memory memory)
        {
            _memory = memory;
        }

        internal void Reset()
        {
            _DMA0SAD_L = 0;
            _DMA0SAD_H = 0;

            _DMA0DAD_L = 0;
            _DMA0DAD_H = 0;

            _DMA0CNT_L = 0;
            _DMA0CNT_H = 0;

            _DMA1SAD_L = 0;
            _DMA1SAD_H = 0;

            _DMA1DAD_L = 0;
            _DMA1DAD_H = 0;

            _DMA1CNT_L = 0;
            _DMA1CNT_H = 0;

            _DMA2SAD_L = 0;
            _DMA2SAD_H = 0;

            _DMA2DAD_L = 0;
            _DMA2DAD_H = 0;

            _DMA2CNT_L = 0;
            _DMA2CNT_H = 0;

            _DMA3SAD_L = 0;
            _DMA3SAD_H = 0;

            _DMA3DAD_L = 0;
            _DMA3DAD_H = 0;

            _DMA3CNT_L = 0;
            _DMA3CNT_H = 0;
        }

        internal void CheckForDMA0()
        {
            CheckForDMA(_DMA0CNT_L, ref _DMA0CNT_H, ref _DMA0SAD_L, ref _DMA0SAD_H, ref _DMA0DAD_L, ref _DMA0DAD_H);
        }

        internal void CheckForDMA1()
        {
            CheckForDMA(_DMA1CNT_L, ref _DMA1CNT_H, ref _DMA1SAD_L, ref _DMA1SAD_H, ref _DMA1DAD_L, ref _DMA1DAD_H);
        }

        internal void CheckForDMA2()
        {
            CheckForDMA(_DMA2CNT_L, ref _DMA2CNT_H, ref _DMA2SAD_L, ref _DMA2SAD_H, ref _DMA2DAD_L, ref _DMA2DAD_H);
        }

        internal void CheckForDMA3()
        {
            CheckForDMA(_DMA3CNT_L, ref _DMA3CNT_H, ref _DMA3SAD_L, ref _DMA3SAD_H, ref _DMA3DAD_L, ref _DMA3DAD_H);
        }

        private void CheckForDMA(UInt16 cnt_l, ref UInt16 cnt_h, ref UInt16 sad_l, ref UInt16 sad_h, ref UInt16 dad_l, ref UInt16 dad_h)
        {
            return;

            if ((cnt_h & 0x8000) == 0x8000)
            {
                UInt32 source = (UInt32)((sad_h << 16) | sad_l);
                UInt32 destination = (UInt32)((dad_h << 16) | dad_l);

                // 16 bits
                if ((cnt_h & 0x0400) == 0)
                {
                    UInt32 lastDestination = (UInt32)(destination + (cnt_l * 2));

                    while (destination < lastDestination)
                    {
                        _memory.Write16(destination, _memory.Read16(source));
                        destination += 2;
                        source += 2;
                    }
                }

                // 32 bits
                else
                {
                    UInt32 lastDestination = (UInt32)(destination + (cnt_l * 4));

                    while (destination < lastDestination)
                    {
                        _memory.Write32(destination, _memory.Read32(source));
                        destination += 4;
                        source += 4;
                    }
                }

                cnt_h = (UInt16)(cnt_h & ~0x8000);
                sad_l = (UInt16)source;
                sad_h = (UInt16)(source >> 16);
                dad_l = (UInt16)destination;
                dad_h = (UInt16)(destination >> 16);
            }
        }
    }
}
