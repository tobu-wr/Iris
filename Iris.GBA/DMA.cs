namespace Iris.GBA
{
    internal sealed class DMA
    {
        internal enum StartTiming
        {
            Immediate = 0b00,
            //VBlank = 0b01,
            HBlank = 0b10,
            //Special = 0b11
        }

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

        internal void ResetState()
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

        internal void LoadState(BinaryReader reader)
        {
            _DMA0SAD_L = reader.ReadUInt16();
            _DMA0SAD_H = reader.ReadUInt16();

            _DMA0DAD_L = reader.ReadUInt16();
            _DMA0DAD_H = reader.ReadUInt16();

            _DMA0CNT_L = reader.ReadUInt16();
            _DMA0CNT_H = reader.ReadUInt16();

            _DMA1SAD_L = reader.ReadUInt16();
            _DMA1SAD_H = reader.ReadUInt16();

            _DMA1DAD_L = reader.ReadUInt16();
            _DMA1DAD_H = reader.ReadUInt16();

            _DMA1CNT_L = reader.ReadUInt16();
            _DMA1CNT_H = reader.ReadUInt16();

            _DMA2SAD_L = reader.ReadUInt16();
            _DMA2SAD_H = reader.ReadUInt16();

            _DMA2DAD_L = reader.ReadUInt16();
            _DMA2DAD_H = reader.ReadUInt16();

            _DMA2CNT_L = reader.ReadUInt16();
            _DMA2CNT_H = reader.ReadUInt16();

            _DMA3SAD_L = reader.ReadUInt16();
            _DMA3SAD_H = reader.ReadUInt16();

            _DMA3DAD_L = reader.ReadUInt16();
            _DMA3DAD_H = reader.ReadUInt16();

            _DMA3CNT_L = reader.ReadUInt16();
            _DMA3CNT_H = reader.ReadUInt16();
        }

        internal void SaveState(BinaryWriter writer)
        {
            writer.Write(_DMA0SAD_L);
            writer.Write(_DMA0SAD_H);

            writer.Write(_DMA0DAD_L);
            writer.Write(_DMA0DAD_H);

            writer.Write(_DMA0CNT_L);
            writer.Write(_DMA0CNT_H);

            writer.Write(_DMA1SAD_L);
            writer.Write(_DMA1SAD_H);

            writer.Write(_DMA1DAD_L);
            writer.Write(_DMA1DAD_H);

            writer.Write(_DMA1CNT_L);
            writer.Write(_DMA1CNT_H);

            writer.Write(_DMA2SAD_L);
            writer.Write(_DMA2SAD_H);

            writer.Write(_DMA2DAD_L);
            writer.Write(_DMA2DAD_H);

            writer.Write(_DMA2CNT_L);
            writer.Write(_DMA2CNT_H);

            writer.Write(_DMA3SAD_L);
            writer.Write(_DMA3SAD_H);

            writer.Write(_DMA3DAD_L);
            writer.Write(_DMA3DAD_H);

            writer.Write(_DMA3CNT_L);
            writer.Write(_DMA3CNT_H);
        }

        internal void PerformAllDMA(StartTiming timing)
        {
            PerformDMA0(timing);
            PerformDMA1(timing);
            PerformDMA2(timing);
            PerformDMA3(timing);
        }

        internal void PerformDMA0(StartTiming timing)
        {
            UInt32 source = (UInt32)(((_DMA0SAD_H & 0x07ff) << 16) | _DMA0SAD_L);
            UInt32 destination = (UInt32)(((_DMA0DAD_H & 0x07ff) << 16) | _DMA0DAD_L);
            UInt32 length = ((_DMA0CNT_L & 0x3fff) == 0) ? 0x4000u : (UInt32)(_DMA0CNT_L & 0x3fff);

            PerformDMA(ref _DMA0CNT_H, source, destination, length, timing);
        }

        internal void PerformDMA1(StartTiming timing)
        {
            UInt32 source = (UInt32)(((_DMA1SAD_H & 0x0fff) << 16) | _DMA1SAD_L);
            UInt32 destination = (UInt32)(((_DMA1DAD_H & 0x07ff) << 16) | _DMA1DAD_L);
            UInt32 length = ((_DMA1CNT_L & 0x3fff) == 0) ? 0x4000u : (UInt32)(_DMA1CNT_L & 0x3fff);

            PerformDMA(ref _DMA1CNT_H, source, destination, length, timing);
        }

        internal void PerformDMA2(StartTiming timing)
        {
            UInt32 source = (UInt32)(((_DMA2SAD_H & 0x0fff) << 16) | _DMA2SAD_L);
            UInt32 destination = (UInt32)(((_DMA2DAD_H & 0x07ff) << 16) | _DMA2DAD_L);
            UInt32 length = ((_DMA2CNT_L & 0x3fff) == 0) ? 0x4000u : (UInt32)(_DMA2CNT_L & 0x3fff);

            PerformDMA(ref _DMA2CNT_H, source, destination, length, timing);
        }

        internal void PerformDMA3(StartTiming timing)
        {
            UInt32 source = (UInt32)(((_DMA3SAD_H & 0x0fff) << 16) | _DMA3SAD_L);
            UInt32 destination = (UInt32)(((_DMA3DAD_H & 0x0fff) << 16) | _DMA3DAD_L);
            UInt32 length = (_DMA3CNT_L == 0) ? 0x1_0000u : _DMA3CNT_L;

            PerformDMA(ref _DMA3CNT_H, source, destination, length, timing);
        }

        private void PerformDMA(ref UInt16 cnt_h, UInt32 source, UInt32 destination, UInt32 length, StartTiming timing)
        {
            if ((cnt_h & 0x8000) == 0)
                return;

            if (((cnt_h >> 12) & 0b11) != (int)timing)
                return;

            static int GetIncrement(UInt16 addressControlFlag, int dataSize)
            {
                return addressControlFlag switch
                {
                    // increment
                    0b00 => dataSize,
                    // decrement
                    0b01 => -dataSize,
                    // fixed
                    0b10 => 0,
                    // increment/reload
                    0b11 => dataSize,
                    // should never happen
                    _ => throw new Exception("Iris.GBA.DMA: Wrong address control flag"),
                };
            }

            UInt16 sourceAddressControlFlag = (UInt16)((cnt_h >> 7) & 0b11);
            UInt16 destinationAddressControlFlag = (UInt16)((cnt_h >> 5) & 0b11);

            // 16 bits
            if ((cnt_h & 0x0400) == 0)
            {
                const int DataSize = 2;

                int sourceIncrement = GetIncrement(sourceAddressControlFlag, DataSize);
                int destinationIncrement = GetIncrement(destinationAddressControlFlag, DataSize);

                for (; length > 0; --length)
                {
                    _memory.Write16(destination, _memory.Read16(source));
                    destination = (UInt32)(destination + destinationIncrement);
                    source = (UInt32)(source + sourceIncrement);
                }
            }

            // 32 bits
            else
            {
                const int DataSize = 4;

                int sourceIncrement = GetIncrement(sourceAddressControlFlag, DataSize);
                int destinationIncrement = GetIncrement(destinationAddressControlFlag, DataSize);

                for (; length > 0; --length)
                {
                    _memory.Write32(destination, _memory.Read32(source));
                    destination = (UInt32)(destination + destinationIncrement);
                    source = (UInt32)(source + sourceIncrement);
                }
            }

            cnt_h = (UInt16)(cnt_h & ~0x8000);
        }
    }
}
