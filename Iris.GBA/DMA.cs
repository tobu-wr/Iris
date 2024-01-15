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

        private InterruptControl _interruptControl;
        private Memory _memory;

        private record struct Channel
        (
            UInt32 Source,
            UInt32 SourceReload,
            UInt32 Destination,
            UInt32 DestinationReload,
            UInt32 Length,
            UInt32 LengthReload,
            UInt16 Control
        );

        private Channel _channel0;
        private Channel _channel1;
        private Channel _channel2;
        private Channel _channel3;

        private const UInt32 MaxLengthChannel0 = 0x4000;
        private const UInt32 MaxLengthChannel1 = 0x4000;
        private const UInt32 MaxLengthChannel2 = 0x4000;
        private const UInt32 MaxLengthChannel3 = 0x1_0000;

        internal void Initialize(InterruptControl interruptControl, Memory memory)
        {
            _interruptControl = interruptControl;
            _memory = memory;
        }

        internal void ResetState()
        {
            _channel0 = default;
            _channel1 = default;
            _channel2 = default;
            _channel3 = default;
        }

        internal void LoadState(BinaryReader reader)
        {
            void LoadChannel(ref Channel channel)
            {
                channel.Source = reader.ReadUInt32();
                channel.SourceReload = reader.ReadUInt32();
                channel.Destination = reader.ReadUInt32();
                channel.DestinationReload = reader.ReadUInt32();
                channel.Length = reader.ReadUInt32();
                channel.LengthReload = reader.ReadUInt32();
                channel.Control = reader.ReadUInt16();
            }

            LoadChannel(ref _channel0);
            LoadChannel(ref _channel1);
            LoadChannel(ref _channel2);
            LoadChannel(ref _channel3);
        }

        internal void SaveState(BinaryWriter writer)
        {
            void SaveChannel(Channel channel)
            {
                writer.Write(channel.Source);
                writer.Write(channel.SourceReload);
                writer.Write(channel.Destination);
                writer.Write(channel.DestinationReload);
                writer.Write(channel.Length);
                writer.Write(channel.LengthReload);
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

            void WriteLengthReload(ref Channel channel)
            {
                UInt16 reload = 0;
                Memory.WriteRegisterHelper(ref reload, value, mode);
                channel.LengthReload = reload;
            }

            void WriteControl(ref Channel channel, InterruptControl.Interrupt interrupt, UInt32 maxLength)
            {
                UInt16 previousControl = channel.Control;

                UInt16 newControl = channel.Control;
                Memory.WriteRegisterHelper(ref newControl, value, mode);
                channel.Control = newControl;

                if (((previousControl & 0x8000) == 0) && ((newControl & 0x8000) == 0x8000))
                {
                    channel.Source = channel.SourceReload;
                    channel.Destination = channel.DestinationReload;
                    channel.Length = (channel.LengthReload == 0) ? maxLength : channel.LengthReload;

                    PerformTransfer(ref channel, StartTiming.Immediate, interrupt, maxLength);
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
                    WriteLengthReload(ref _channel0);
                    break;
                case Register.DMA0CNT_H:
                    WriteControl(ref _channel0, InterruptControl.Interrupt.DMA0, MaxLengthChannel0);
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
                    WriteLengthReload(ref _channel1);
                    break;
                case Register.DMA1CNT_H:
                    WriteControl(ref _channel1, InterruptControl.Interrupt.DMA1, MaxLengthChannel1);
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
                    WriteLengthReload(ref _channel2);
                    break;
                case Register.DMA2CNT_H:
                    WriteControl(ref _channel2, InterruptControl.Interrupt.DMA2, MaxLengthChannel2);
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
                    WriteLengthReload(ref _channel3);
                    break;
                case Register.DMA3CNT_H:
                    WriteControl(ref _channel3, InterruptControl.Interrupt.DMA3, MaxLengthChannel3);
                    break;

                // should never happen
                default:
                    throw new Exception("Iris.GBA.DMA: Register write error");
            }
        }

        internal void PerformAllTransfers(StartTiming startTiming)
        {
            if ((_channel0.Control & 0x8000) == 0x8000)
                PerformTransfer(ref _channel0, startTiming, InterruptControl.Interrupt.DMA0, MaxLengthChannel0);

            if ((_channel1.Control & 0x8000) == 0x8000)
                PerformTransfer(ref _channel1, startTiming, InterruptControl.Interrupt.DMA1, MaxLengthChannel1);

            if ((_channel2.Control & 0x8000) == 0x8000)
                PerformTransfer(ref _channel2, startTiming, InterruptControl.Interrupt.DMA2, MaxLengthChannel2);

            if ((_channel3.Control & 0x8000) == 0x8000)
                PerformTransfer(ref _channel3, startTiming, InterruptControl.Interrupt.DMA3, MaxLengthChannel3);
        }

        private void PerformTransfer(ref Channel channel, StartTiming startTiming, InterruptControl.Interrupt interrupt, UInt32 maxLength)
        {
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

                    channel.Source = (UInt32)(channel.Source + sourceIncrement);
                    channel.Destination = (UInt32)(channel.Destination + destinationIncrement);
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

                    channel.Source = (UInt32)(channel.Source + sourceIncrement);
                    channel.Destination = (UInt32)(channel.Destination + destinationIncrement);
                }
            }

            if ((channel.Control & 0x4000) == 0x4000)
                _interruptControl.RequestInterrupt(interrupt);

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

                channel.Length = (channel.LengthReload == 0) ? maxLength : channel.LengthReload;
            }
        }
    }
}
