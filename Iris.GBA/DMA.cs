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

        private record struct ChannelState
        (
            bool Enabled,
            UInt32 Source,
            UInt32 Destination,
            UInt32 Length
        );

        private ChannelState _channel0;
        private ChannelState _channel1;
        private ChannelState _channel2;
        private ChannelState _channel3;

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

            _channel0 = default;
            _channel1 = default;
            _channel2 = default;
            _channel3 = default;
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

            _channel0.Enabled = reader.ReadBoolean();
            _channel0.Source = reader.ReadUInt32();
            _channel0.Destination = reader.ReadUInt32();
            _channel0.Length = reader.ReadUInt32();

            _channel1.Enabled = reader.ReadBoolean();
            _channel1.Source = reader.ReadUInt32();
            _channel1.Destination = reader.ReadUInt32();
            _channel1.Length = reader.ReadUInt32();

            _channel2.Enabled = reader.ReadBoolean();
            _channel2.Source = reader.ReadUInt32();
            _channel2.Destination = reader.ReadUInt32();
            _channel2.Length = reader.ReadUInt32();

            _channel3.Enabled = reader.ReadBoolean();
            _channel3.Source = reader.ReadUInt32();
            _channel3.Destination = reader.ReadUInt32();
            _channel3.Length = reader.ReadUInt32();
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

            writer.Write(_channel0.Enabled);
            writer.Write(_channel0.Source);
            writer.Write(_channel0.Destination);
            writer.Write(_channel0.Length);

            writer.Write(_channel1.Enabled);
            writer.Write(_channel1.Source);
            writer.Write(_channel1.Destination);
            writer.Write(_channel1.Length);

            writer.Write(_channel2.Enabled);
            writer.Write(_channel2.Source);
            writer.Write(_channel2.Destination);
            writer.Write(_channel2.Length);

            writer.Write(_channel3.Enabled);
            writer.Write(_channel3.Source);
            writer.Write(_channel3.Destination);
            writer.Write(_channel3.Length);
        }

        internal void UpdateChannel0()
        {
            if ((_DMA0CNT_H & 0x8000) == 0)
            {
                _channel0.Enabled = false;
            }
            else if (!_channel0.Enabled)
            {
                _channel0.Enabled = true;
                _channel0.Source = (UInt32)(((_DMA0SAD_H & 0x07ff) << 16) | _DMA0SAD_L);
                _channel0.Destination = (UInt32)(((_DMA0DAD_H & 0x07ff) << 16) | _DMA0DAD_L);
                _channel0.Length = ((_DMA0CNT_L & 0x3fff) == 0) ? 0x4000u : (UInt32)(_DMA0CNT_L & 0x3fff);

                PerformTransfer(ref _DMA0CNT_H, ref _channel0, StartTiming.Immediate, _channel0.Destination, _channel0.Length);
            }
        }

        internal void UpdateChannel1()
        {
            if ((_DMA1CNT_H & 0x8000) == 0)
            {
                _channel1.Enabled = false;
            }
            else if (!_channel1.Enabled)
            {
                _channel1.Enabled = true;
                _channel1.Source = (UInt32)(((_DMA1SAD_H & 0x0fff) << 16) | _DMA1SAD_L);
                _channel1.Destination = (UInt32)(((_DMA1DAD_H & 0x07ff) << 16) | _DMA1DAD_L);
                _channel1.Length = ((_DMA1CNT_L & 0x3fff) == 0) ? 0x4000u : (UInt32)(_DMA1CNT_L & 0x3fff);

                PerformTransfer(ref _DMA1CNT_H, ref _channel1, StartTiming.Immediate, _channel1.Destination, _channel1.Length);
            }
        }

        internal void UpdateChannel2()
        {
            if ((_DMA2CNT_H & 0x8000) == 0)
            {
                _channel2.Enabled = false;
            }
            else if (!_channel2.Enabled)
            {
                _channel2.Enabled = true;
                _channel2.Source = (UInt32)(((_DMA2SAD_H & 0x0fff) << 16) | _DMA2SAD_L);
                _channel2.Destination = (UInt32)(((_DMA2DAD_H & 0x07ff) << 16) | _DMA2DAD_L);
                _channel2.Length = ((_DMA2CNT_L & 0x3fff) == 0) ? 0x4000u : (UInt32)(_DMA2CNT_L & 0x3fff);

                PerformTransfer(ref _DMA2CNT_H, ref _channel2, StartTiming.Immediate, _channel2.Destination, _channel2.Length);
            }
        }

        internal void UpdateChannel3()
        {
            if ((_DMA3CNT_H & 0x8000) == 0)
            {
                _channel3.Enabled = false;
            }
            else if (!_channel3.Enabled)
            {
                _channel3.Enabled = true;
                _channel3.Source = (UInt32)(((_DMA3SAD_H & 0x0fff) << 16) | _DMA3SAD_L);
                _channel3.Destination = (UInt32)(((_DMA3DAD_H & 0x0fff) << 16) | _DMA3DAD_L);
                _channel3.Length = (_DMA3CNT_L == 0) ? 0x1_0000u : _DMA3CNT_L;

                PerformTransfer(ref _DMA3CNT_H, ref _channel3, StartTiming.Immediate, _channel3.Destination, _channel3.Length);
            }
        }

        internal void PerformAllTransfers(StartTiming startTiming)
        {
            if (_channel0.Enabled)
            {
                UInt32 destinationReloadValue = (UInt32)(((_DMA0DAD_H & 0x07ff) << 16) | _DMA0DAD_L);
                UInt32 lengthReloadValue = ((_DMA0CNT_L & 0x3fff) == 0) ? 0x4000u : (UInt32)(_DMA0CNT_L & 0x3fff);

                PerformTransfer(ref _DMA0CNT_H, ref _channel0, startTiming, destinationReloadValue, lengthReloadValue);
            }

            if (_channel1.Enabled)
            {
                UInt32 destinationReloadValue = (UInt32)(((_DMA1DAD_H & 0x07ff) << 16) | _DMA1DAD_L);
                UInt32 lengthReloadValue = ((_DMA1CNT_L & 0x3fff) == 0) ? 0x4000u : (UInt32)(_DMA1CNT_L & 0x3fff);

                PerformTransfer(ref _DMA1CNT_H, ref _channel1, startTiming, destinationReloadValue, lengthReloadValue);
            }

            if (_channel2.Enabled)
            {
                UInt32 destinationReloadValue = (UInt32)(((_DMA2DAD_H & 0x07ff) << 16) | _DMA2DAD_L);
                UInt32 lengthReloadValue = ((_DMA2CNT_L & 0x3fff) == 0) ? 0x4000u : (UInt32)(_DMA2CNT_L & 0x3fff);

                PerformTransfer(ref _DMA2CNT_H, ref _channel2, startTiming, destinationReloadValue, lengthReloadValue);
            }

            if (_channel3.Enabled)
            {
                UInt32 destinationReloadValue = (UInt32)(((_DMA3DAD_H & 0x0fff) << 16) | _DMA3DAD_L);
                UInt32 lengthReloadValue = (_DMA3CNT_L == 0) ? 0x1_0000u : _DMA3CNT_L;

                PerformTransfer(ref _DMA3CNT_H, ref _channel3, startTiming, destinationReloadValue, lengthReloadValue);
            }
        }

        private void PerformTransfer(ref UInt16 cnt_h, ref ChannelState channel, StartTiming startTiming, UInt32 destinationReloadValue, UInt32 lengthReloadValue)
        {
            if (((cnt_h >> 12) & 0b11) != (int)startTiming)
                return;

            UInt16 sourceAddressControlFlag = (UInt16)((cnt_h >> 7) & 0b11);
            UInt16 destinationAddressControlFlag = (UInt16)((cnt_h >> 5) & 0b11);

            int GetSourceIncrement(int dataUnitSize)
            {
                return sourceAddressControlFlag switch
                {
                    // increment
                    0b00 => dataUnitSize,
                    // decrement
                    0b01 => -dataUnitSize,
                    // fixed
                    0b10 => 0,
                    // prohibited
                    0b11 => 0,
                    // should never happen
                    _ => throw new Exception("Iris.GBA.DMA: Wrong source address control flag"),
                };
            }

            (int destinationIncrement, bool reloadDestination) GetDestinationIncrement(int dataUnitSize)
            {
                return destinationAddressControlFlag switch
                {
                    // increment
                    0b00 => (dataUnitSize, false),
                    // decrement
                    0b01 => (-dataUnitSize, false),
                    // fixed
                    0b10 => (0, false),
                    // increment+reload
                    0b11 => (dataUnitSize, true),
                    // should never happen
                    _ => throw new Exception("Iris.GBA.DMA: Wrong destination address control flag"),
                };
            }

            bool reloadDestination;

            // 16 bits
            if ((cnt_h & 0x0400) == 0)
            {
                const int DataUnitSize = 2;

                int sourceIncrement = GetSourceIncrement(DataUnitSize);
                (int destinationIncrement, reloadDestination) = GetDestinationIncrement(DataUnitSize);

                for (; channel.Length > 0; --channel.Length)
                {
                    _memory.Write16(channel.Destination, _memory.Read16(channel.Source));
                    channel.Destination = (UInt32)(channel.Destination + destinationIncrement);
                    channel.Source = (UInt32)(channel.Source + sourceIncrement);
                }
            }

            // 32 bits
            else
            {
                const int DataUnitSize = 4;

                int sourceIncrement = GetSourceIncrement(DataUnitSize);
                (int destinationIncrement, reloadDestination) = GetDestinationIncrement(DataUnitSize);

                for (; channel.Length > 0; --channel.Length)
                {
                    _memory.Write32(channel.Destination, _memory.Read32(channel.Source));
                    channel.Destination = (UInt32)(channel.Destination + destinationIncrement);
                    channel.Source = (UInt32)(channel.Source + sourceIncrement);
                }
            }

            // Repeat OFF
            if ((cnt_h & 0x0200) == 0)
            {
                cnt_h = (UInt16)(cnt_h & ~0x8000);
                channel.Enabled = false;
            }

            // Repeat ON
            else
            {
                if (reloadDestination)
                    channel.Destination = destinationReloadValue;

                channel.Length = lengthReloadValue;
            }
        }
    }
}
