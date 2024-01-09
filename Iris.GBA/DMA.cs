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

        private UInt16 _DMA0SAD_L;
        private UInt16 _DMA0SAD_H;

        private UInt16 _DMA0DAD_L;
        private UInt16 _DMA0DAD_H;

        private UInt16 _DMA0CNT_L;
        private UInt16 _DMA0CNT_H;

        private UInt16 _DMA1SAD_L;
        private UInt16 _DMA1SAD_H;

        private UInt16 _DMA1DAD_L;
        private UInt16 _DMA1DAD_H;

        private UInt16 _DMA1CNT_L;
        private UInt16 _DMA1CNT_H;

        private UInt16 _DMA2SAD_L;
        private UInt16 _DMA2SAD_H;

        private UInt16 _DMA2DAD_L;
        private UInt16 _DMA2DAD_H;

        private UInt16 _DMA2CNT_L;
        private UInt16 _DMA2CNT_H;

        private UInt16 _DMA3SAD_L;
        private UInt16 _DMA3SAD_H;

        private UInt16 _DMA3DAD_L;
        private UInt16 _DMA3DAD_H;

        private UInt16 _DMA3CNT_L;
        private UInt16 _DMA3CNT_H;

        private Memory _memory;

        private record struct Channel
        (
            UInt32 Source,
            UInt32 Destination,
            UInt32 Length,
            bool Running
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

            void LoadChannel(ref Channel channel)
            {
                channel.Source = reader.ReadUInt32();
                channel.Destination = reader.ReadUInt32();
                channel.Length = reader.ReadUInt32();
                channel.Running = reader.ReadBoolean();
            }

            LoadChannel(ref _channel0);
            LoadChannel(ref _channel1);
            LoadChannel(ref _channel2);
            LoadChannel(ref _channel3);
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

            void SaveChannel(Channel channel)
            {
                writer.Write(channel.Source);
                writer.Write(channel.Destination);
                writer.Write(channel.Length);
                writer.Write(channel.Running);
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
                Register.DMA0CNT_H => _DMA0CNT_H,
                Register.DMA1CNT_H => _DMA1CNT_H,
                Register.DMA2CNT_H => _DMA2CNT_H,
                Register.DMA3CNT_H => _DMA3CNT_H,

                // should never happen
                _ => throw new Exception("Iris.GBA.DMA: Register read error"),
            };
        }

        internal void WriteRegister(Register register, UInt16 value, Memory.RegisterWriteMode mode)
        {
            void UpdateChannel0()
            {
                if ((_DMA0CNT_H & 0x8000) == 0)
                {
                    _channel0.Running = false;
                }
                else if (!_channel0.Running)
                {
                    _channel0.Running = true;
                    _channel0.Source = (UInt32)(((_DMA0SAD_H & 0x07ff) << 16) | _DMA0SAD_L);
                    _channel0.Destination = (UInt32)(((_DMA0DAD_H & 0x07ff) << 16) | _DMA0DAD_L);
                    _channel0.Length = ((_DMA0CNT_L & 0x3fff) == 0) ? 0x4000u : (UInt32)(_DMA0CNT_L & 0x3fff);

                    PerformTransfer(ref _DMA0CNT_H, ref _channel0, StartTiming.Immediate, _channel0.Destination, _channel0.Length);
                }
            }

            void UpdateChannel1()
            {
                if ((_DMA1CNT_H & 0x8000) == 0)
                {
                    _channel1.Running = false;
                }
                else if (!_channel1.Running)
                {
                    _channel1.Running = true;
                    _channel1.Source = (UInt32)(((_DMA1SAD_H & 0x0fff) << 16) | _DMA1SAD_L);
                    _channel1.Destination = (UInt32)(((_DMA1DAD_H & 0x07ff) << 16) | _DMA1DAD_L);
                    _channel1.Length = ((_DMA1CNT_L & 0x3fff) == 0) ? 0x4000u : (UInt32)(_DMA1CNT_L & 0x3fff);

                    PerformTransfer(ref _DMA1CNT_H, ref _channel1, StartTiming.Immediate, _channel1.Destination, _channel1.Length);
                }
            }

            void UpdateChannel2()
            {
                if ((_DMA2CNT_H & 0x8000) == 0)
                {
                    _channel2.Running = false;
                }
                else if (!_channel2.Running)
                {
                    _channel2.Running = true;
                    _channel2.Source = (UInt32)(((_DMA2SAD_H & 0x0fff) << 16) | _DMA2SAD_L);
                    _channel2.Destination = (UInt32)(((_DMA2DAD_H & 0x07ff) << 16) | _DMA2DAD_L);
                    _channel2.Length = ((_DMA2CNT_L & 0x3fff) == 0) ? 0x4000u : (UInt32)(_DMA2CNT_L & 0x3fff);

                    PerformTransfer(ref _DMA2CNT_H, ref _channel2, StartTiming.Immediate, _channel2.Destination, _channel2.Length);
                }
            }

            void UpdateChannel3()
            {
                if ((_DMA3CNT_H & 0x8000) == 0)
                {
                    _channel3.Running = false;
                }
                else if (!_channel3.Running)
                {
                    _channel3.Running = true;
                    _channel3.Source = (UInt32)(((_DMA3SAD_H & 0x0fff) << 16) | _DMA3SAD_L);
                    _channel3.Destination = (UInt32)(((_DMA3DAD_H & 0x0fff) << 16) | _DMA3DAD_L);
                    _channel3.Length = (_DMA3CNT_L == 0) ? 0x1_0000u : _DMA3CNT_L;

                    PerformTransfer(ref _DMA3CNT_H, ref _channel3, StartTiming.Immediate, _channel3.Destination, _channel3.Length);
                }
            }

            switch (register)
            {
                case Register.DMA0SAD_L:
                    Memory.WriteRegisterHelper(ref _DMA0SAD_L, value, mode);
                    break;
                case Register.DMA0SAD_H:
                    Memory.WriteRegisterHelper(ref _DMA0SAD_H, value, mode);
                    break;

                case Register.DMA0DAD_L:
                    Memory.WriteRegisterHelper(ref _DMA0DAD_L, value, mode);
                    break;
                case Register.DMA0DAD_H:
                    Memory.WriteRegisterHelper(ref _DMA0DAD_H, value, mode);
                    break;

                case Register.DMA0CNT_L:
                    Memory.WriteRegisterHelper(ref _DMA0CNT_L, value, mode);
                    break;
                case Register.DMA0CNT_H:
                    Memory.WriteRegisterHelper(ref _DMA0CNT_H, value, mode);
                    UpdateChannel0();
                    break;

                case Register.DMA1SAD_L:
                    Memory.WriteRegisterHelper(ref _DMA1SAD_L, value, mode);
                    break;
                case Register.DMA1SAD_H:
                    Memory.WriteRegisterHelper(ref _DMA1SAD_H, value, mode);
                    break;

                case Register.DMA1DAD_L:
                    Memory.WriteRegisterHelper(ref _DMA1DAD_L, value, mode);
                    break;
                case Register.DMA1DAD_H:
                    Memory.WriteRegisterHelper(ref _DMA1DAD_H, value, mode);
                    break;

                case Register.DMA1CNT_L:
                    Memory.WriteRegisterHelper(ref _DMA1CNT_L, value, mode);
                    break;
                case Register.DMA1CNT_H:
                    Memory.WriteRegisterHelper(ref _DMA1CNT_H, value, mode);
                    UpdateChannel1();
                    break;

                case Register.DMA2SAD_L:
                    Memory.WriteRegisterHelper(ref _DMA2SAD_L, value, mode);
                    break;
                case Register.DMA2SAD_H:
                    Memory.WriteRegisterHelper(ref _DMA2SAD_H, value, mode);
                    break;

                case Register.DMA2DAD_L:
                    Memory.WriteRegisterHelper(ref _DMA2DAD_L, value, mode);
                    break;
                case Register.DMA2DAD_H:
                    Memory.WriteRegisterHelper(ref _DMA2DAD_H, value, mode);
                    break;

                case Register.DMA2CNT_L:
                    Memory.WriteRegisterHelper(ref _DMA2CNT_L, value, mode);
                    break;
                case Register.DMA2CNT_H:
                    Memory.WriteRegisterHelper(ref _DMA2CNT_H, value, mode);
                    UpdateChannel2();
                    break;

                case Register.DMA3SAD_L:
                    Memory.WriteRegisterHelper(ref _DMA3SAD_L, value, mode);
                    break;
                case Register.DMA3SAD_H:
                    Memory.WriteRegisterHelper(ref _DMA3SAD_H, value, mode);
                    break;

                case Register.DMA3DAD_L:
                    Memory.WriteRegisterHelper(ref _DMA3DAD_L, value, mode);
                    break;
                case Register.DMA3DAD_H:
                    Memory.WriteRegisterHelper(ref _DMA3DAD_H, value, mode);
                    break;

                case Register.DMA3CNT_L:
                    Memory.WriteRegisterHelper(ref _DMA3CNT_L, value, mode);
                    break;
                case Register.DMA3CNT_H:
                    Memory.WriteRegisterHelper(ref _DMA3CNT_H, value, mode);
                    UpdateChannel3();
                    break;

                // should never happen
                default:
                    throw new Exception("Iris.GBA.DMA: Register write error");
            }
        }

        internal void PerformAllTransfers(StartTiming startTiming)
        {
            if (_channel0.Running)
            {
                UInt32 destinationReloadValue = (UInt32)(((_DMA0DAD_H & 0x07ff) << 16) | _DMA0DAD_L);
                UInt32 lengthReloadValue = ((_DMA0CNT_L & 0x3fff) == 0) ? 0x4000u : (UInt32)(_DMA0CNT_L & 0x3fff);

                PerformTransfer(ref _DMA0CNT_H, ref _channel0, startTiming, destinationReloadValue, lengthReloadValue);
            }

            if (_channel1.Running)
            {
                UInt32 destinationReloadValue = (UInt32)(((_DMA1DAD_H & 0x07ff) << 16) | _DMA1DAD_L);
                UInt32 lengthReloadValue = ((_DMA1CNT_L & 0x3fff) == 0) ? 0x4000u : (UInt32)(_DMA1CNT_L & 0x3fff);

                PerformTransfer(ref _DMA1CNT_H, ref _channel1, startTiming, destinationReloadValue, lengthReloadValue);
            }

            if (_channel2.Running)
            {
                UInt32 destinationReloadValue = (UInt32)(((_DMA2DAD_H & 0x07ff) << 16) | _DMA2DAD_L);
                UInt32 lengthReloadValue = ((_DMA2CNT_L & 0x3fff) == 0) ? 0x4000u : (UInt32)(_DMA2CNT_L & 0x3fff);

                PerformTransfer(ref _DMA2CNT_H, ref _channel2, startTiming, destinationReloadValue, lengthReloadValue);
            }

            if (_channel3.Running)
            {
                UInt32 destinationReloadValue = (UInt32)(((_DMA3DAD_H & 0x0fff) << 16) | _DMA3DAD_L);
                UInt32 lengthReloadValue = (_DMA3CNT_L == 0) ? 0x1_0000u : _DMA3CNT_L;

                PerformTransfer(ref _DMA3CNT_H, ref _channel3, startTiming, destinationReloadValue, lengthReloadValue);
            }
        }

        private void PerformTransfer(ref UInt16 cnt_h, ref Channel channel, StartTiming startTiming, UInt32 destinationReloadValue, UInt32 lengthReloadValue)
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

            // Repeat off
            if ((cnt_h & 0x0200) == 0)
            {
                cnt_h = (UInt16)(cnt_h & ~0x8000);
                channel.Running = false;
            }

            // Repeat on
            else
            {
                if (reloadDestination)
                    channel.Destination = destinationReloadValue;

                channel.Length = lengthReloadValue;
            }
        }
    }
}
