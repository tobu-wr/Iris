namespace Iris.GBA
{
    internal sealed class DMA
    {
        internal enum Register
        {
            DMA0SAD_L,
            DMA0SAD_H,

            DMA0DAD_L,
            DMA0DAD_H,

            DMA0CNT_L,
            DMA0CNT_H,

            DMA1SAD_L,
            DMA1SAD_H,

            DMA1DAD_L,
            DMA1DAD_H,

            DMA1CNT_L,
            DMA1CNT_H,

            DMA2SAD_L,
            DMA2SAD_H,

            DMA2DAD_L,
            DMA2DAD_H,

            DMA2CNT_L,
            DMA2CNT_H,

            DMA3SAD_L,
            DMA3SAD_H,

            DMA3DAD_L,
            DMA3DAD_H,

            DMA3CNT_L,
            DMA3CNT_H
        }

        internal enum StartTiming
        {
            Immediate = 0b00,
            VBlank = 0b01,
            HBlank = 0b10,
            //Special = 0b11
        }

        private UInt16 _DMA0CNT_L;
        private UInt16 _DMA1CNT_L;
        private UInt16 _DMA2CNT_L;
        private UInt16 _DMA3CNT_L;

        private Memory _memory;

        private record struct Channel
        (
            UInt32 Source,
            UInt32 SourceReload,
            UInt32 Destination,
            UInt32 DestinationReload,
            UInt32 Length,
            UInt16 Control
        );

        private Channel _channel0;
        private Channel _channel1;
        private Channel _channel2;
        private Channel _channel3;

        internal void Initialize(Memory memory)
        {
            _memory = memory;
        }

        internal void ResetState()
        {
            _DMA0CNT_L = 0;
            _DMA1CNT_L = 0;
            _DMA2CNT_L = 0;
            _DMA3CNT_L = 0;

            _channel0 = default;
            _channel1 = default;
            _channel2 = default;
            _channel3 = default;
        }

        internal void LoadState(BinaryReader reader)
        {
            _DMA0CNT_L = reader.ReadUInt16();
            _DMA1CNT_L = reader.ReadUInt16();
            _DMA2CNT_L = reader.ReadUInt16();
            _DMA3CNT_L = reader.ReadUInt16();

            void LoadChannel(ref Channel channel)
            {
                channel.Source = reader.ReadUInt32();
                channel.SourceReload = reader.ReadUInt32();
                channel.Destination = reader.ReadUInt32();
                channel.DestinationReload = reader.ReadUInt32();
                channel.Length = reader.ReadUInt32();
                channel.Control = reader.ReadUInt16();
            }

            LoadChannel(ref _channel0);
            LoadChannel(ref _channel1);
            LoadChannel(ref _channel2);
            LoadChannel(ref _channel3);
        }

        internal void SaveState(BinaryWriter writer)
        {
            writer.Write(_DMA0CNT_L);
            writer.Write(_DMA1CNT_L);
            writer.Write(_DMA2CNT_L);
            writer.Write(_DMA3CNT_L);

            void SaveChannel(Channel channel)
            {
                writer.Write(channel.Source);
                writer.Write(channel.SourceReload);
                writer.Write(channel.Destination);
                writer.Write(channel.DestinationReload);
                writer.Write(channel.Length);
                writer.Write(channel.Control);
            }

            SaveChannel(_channel0);
            SaveChannel(_channel1);
            SaveChannel(_channel2);
            SaveChannel(_channel3);
        }

        internal UInt16 ReadRegister(Register register)
        {
            return register switch
            {
                Register.DMA0CNT_H => _channel0.Control,
                Register.DMA1CNT_H => _channel1.Control,
                Register.DMA2CNT_H => _channel2.Control,
                Register.DMA3CNT_H => _channel3.Control,

                // should never happen
                _ => throw new Exception("Iris.GBA.DMA: Register read error"),
            };
        }

        internal void WriteRegister(Register register, UInt16 value, Memory.RegisterWriteMode mode)
        {
            void WriteSourceReload_LowHalfWord(ref Channel channel)
            {
                UInt16 low = (UInt16)channel.SourceReload;
                Memory.WriteRegisterHelper(ref low, value, mode);
                channel.SourceReload = (channel.SourceReload & 0xffff_0000) | low;
            }

            void WriteSourceReload_HighHalfWord(ref Channel channel, UInt16 mask)
            {
                UInt16 high = (UInt16)(channel.SourceReload >> 16);
                Memory.WriteRegisterHelper(ref high, (UInt16)(value & mask), mode);
                channel.SourceReload = (channel.SourceReload & 0x0000_ffff) | (UInt32)(high << 16);
            }

            void WriteDestinationReload_LowHalfWord(ref Channel channel)
            {
                UInt16 low = (UInt16)channel.DestinationReload;
                Memory.WriteRegisterHelper(ref low, value, mode);
                channel.DestinationReload = (channel.DestinationReload & 0xffff_0000) | low;
            }

            void WriteDestinationReload_HighHalfWord(ref Channel channel, UInt16 mask)
            {
                UInt16 high = (UInt16)(channel.DestinationReload >> 16);
                Memory.WriteRegisterHelper(ref high, (UInt16)(value & mask), mode);
                channel.DestinationReload = (channel.DestinationReload & 0x0000_ffff) | (UInt32)(high << 16);
            }

            void WriteControlChannel0()
            {
                UInt16 previousControl = _channel0.Control;

                UInt16 newControl = _channel0.Control;
                Memory.WriteRegisterHelper(ref newControl, value, mode);
                _channel0.Control = newControl;

                if (((previousControl & 0x8000) == 0) && ((newControl & 0x8000) == 0x8000))
                {
                    _channel0.Source = _channel0.SourceReload;
                    _channel0.Destination = _channel0.DestinationReload;
                    _channel0.Length = (_DMA0CNT_L == 0) ? 0x4000u : _DMA0CNT_L;

                    PerformTransfer(ref _channel0, StartTiming.Immediate, _channel0.Length);
                }
            }

            void WriteControlChannel1()
            {
                UInt16 previousControl = _channel1.Control;

                UInt16 newControl = _channel1.Control;
                Memory.WriteRegisterHelper(ref newControl, value, mode);
                _channel1.Control = newControl;

                if (((previousControl & 0x8000) == 0) && ((newControl & 0x8000) == 0x8000))
                {
                    _channel1.Source = _channel1.SourceReload;
                    _channel1.Destination = _channel1.DestinationReload;
                    _channel1.Length = (_DMA1CNT_L == 0) ? 0x4000u : _DMA1CNT_L;

                    PerformTransfer(ref _channel1, StartTiming.Immediate, _channel1.Length);
                }
            }

            void WriteControlChannel2()
            {
                UInt16 previousControl = _channel2.Control;

                UInt16 newControl = _channel2.Control;
                Memory.WriteRegisterHelper(ref newControl, value, mode);
                _channel2.Control = newControl;

                if (((previousControl & 0x8000) == 0) && ((newControl & 0x8000) == 0x8000))
                {
                    _channel2.Source = _channel2.SourceReload;
                    _channel2.Destination = _channel2.DestinationReload;
                    _channel2.Length = (_DMA2CNT_L == 0) ? 0x4000u : _DMA2CNT_L;

                    PerformTransfer(ref _channel2, StartTiming.Immediate, _channel2.Length);
                }
            }

            void WriteControlChannel3()
            {
                UInt16 previousControl = _channel3.Control;

                UInt16 newControl = _channel3.Control;
                Memory.WriteRegisterHelper(ref newControl, value, mode);
                _channel3.Control = newControl;

                if (((previousControl & 0x8000) == 0) && ((newControl & 0x8000) == 0x8000))
                {
                    _channel3.Source = _channel3.SourceReload;
                    _channel3.Destination = _channel3.DestinationReload;
                    _channel3.Length = (_DMA3CNT_L == 0) ? 0x1_0000u : _DMA3CNT_L;

                    PerformTransfer(ref _channel3, StartTiming.Immediate, _channel3.Length);
                }
            }

            switch (register)
            {
                case Register.DMA0SAD_L:
                    WriteSourceReload_LowHalfWord(ref _channel0);
                    break;
                case Register.DMA0SAD_H:
                    WriteSourceReload_HighHalfWord(ref _channel0, 0x07ff);
                    break;

                case Register.DMA0DAD_L:
                    WriteDestinationReload_LowHalfWord(ref _channel0);
                    break;
                case Register.DMA0DAD_H:
                    WriteDestinationReload_HighHalfWord(ref _channel0, 0x07ff);
                    break;

                case Register.DMA0CNT_L:
                    Memory.WriteRegisterHelper(ref _DMA0CNT_L, (UInt16)(value & 0x3fff), mode);
                    break;
                case Register.DMA0CNT_H:
                    WriteControlChannel0();
                    break;

                case Register.DMA1SAD_L:
                    WriteSourceReload_LowHalfWord(ref _channel1);
                    break;
                case Register.DMA1SAD_H:
                    WriteSourceReload_HighHalfWord(ref _channel1, 0x0fff);
                    break;

                case Register.DMA1DAD_L:
                    WriteDestinationReload_LowHalfWord(ref _channel1);
                    break;
                case Register.DMA1DAD_H:
                    WriteDestinationReload_HighHalfWord(ref _channel1, 0x07ff);
                    break;

                case Register.DMA1CNT_L:
                    Memory.WriteRegisterHelper(ref _DMA1CNT_L, (UInt16)(value & 0x3fff), mode);
                    break;
                case Register.DMA1CNT_H:
                    WriteControlChannel1();
                    break;

                case Register.DMA2SAD_L:
                    WriteSourceReload_LowHalfWord(ref _channel2);
                    break;
                case Register.DMA2SAD_H:
                    WriteSourceReload_HighHalfWord(ref _channel2, 0x0fff);
                    break;

                case Register.DMA2DAD_L:
                    WriteDestinationReload_LowHalfWord(ref _channel2);
                    break;
                case Register.DMA2DAD_H:
                    WriteDestinationReload_HighHalfWord(ref _channel2, 0x07ff);
                    break;

                case Register.DMA2CNT_L:
                    Memory.WriteRegisterHelper(ref _DMA2CNT_L, (UInt16)(value & 0x3fff), mode);
                    break;
                case Register.DMA2CNT_H:
                    WriteControlChannel2();
                    break;

                case Register.DMA3SAD_L:
                    WriteSourceReload_LowHalfWord(ref _channel3);
                    break;
                case Register.DMA3SAD_H:
                    WriteSourceReload_HighHalfWord(ref _channel3, 0x0fff);
                    break;

                case Register.DMA3DAD_L:
                    WriteDestinationReload_LowHalfWord(ref _channel3);
                    break;
                case Register.DMA3DAD_H:
                    WriteDestinationReload_HighHalfWord(ref _channel3, 0x0fff);
                    break;

                case Register.DMA3CNT_L:
                    Memory.WriteRegisterHelper(ref _DMA3CNT_L, value, mode);
                    break;
                case Register.DMA3CNT_H:
                    WriteControlChannel3();
                    break;

                // should never happen
                default:
                    throw new Exception("Iris.GBA.DMA: Register write error");
            }
        }

        internal void PerformAllTransfers(StartTiming startTiming)
        {
            {
                UInt32 lengthReloadValue = (_DMA0CNT_L == 0) ? 0x4000u : _DMA0CNT_L;

                PerformTransfer(ref _channel0, startTiming, lengthReloadValue);
            }

            {
                UInt32 lengthReloadValue = (_DMA1CNT_L == 0) ? 0x4000u : _DMA1CNT_L;

                PerformTransfer(ref _channel1, startTiming, lengthReloadValue);
            }

            {
                UInt32 lengthReloadValue = (_DMA2CNT_L == 0) ? 0x4000u : _DMA2CNT_L;

                PerformTransfer(ref _channel2, startTiming, lengthReloadValue);
            }

            {
                UInt32 lengthReloadValue = (_DMA3CNT_L == 0) ? 0x1_0000u : _DMA3CNT_L;

                PerformTransfer(ref _channel3, startTiming, lengthReloadValue);
            }
        }

        private void PerformTransfer(ref Channel channel, StartTiming startTiming, UInt32 lengthReloadValue)
        {
            if ((channel.Control & 0x8000) == 0)
                return;

            if (((channel.Control >> 12) & 0b11) != (int)startTiming)
                return;

            UInt16 sourceAddressControlFlag = (UInt16)((channel.Control >> 7) & 0b11);
            UInt16 destinationAddressControlFlag = (UInt16)((channel.Control >> 5) & 0b11);

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
            if ((channel.Control & 0x0400) == 0)
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

            // Repeat off
            if ((channel.Control & 0x0200) == 0)
            {
                channel.Control = (UInt16)(channel.Control & ~0x8000);
            }

            // Repeat on
            else
            {
                if (reloadDestination)
                    channel.Destination = channel.DestinationReload;

                channel.Length = lengthReloadValue;
            }
        }
    }
}
