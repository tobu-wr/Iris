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

        private struct Channel
        {
            internal UInt32 _source;
            internal UInt32 _sourceReload;
            internal UInt32 _destination;
            internal UInt32 _destinationReload;
            internal UInt32 _length;
            internal UInt16 _lengthReload;
            internal UInt16 _control;
        }

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
                channel._source = reader.ReadUInt32();
                channel._sourceReload = reader.ReadUInt32();
                channel._destination = reader.ReadUInt32();
                channel._destinationReload = reader.ReadUInt32();
                channel._length = reader.ReadUInt32();
                channel._lengthReload = reader.ReadUInt16();
                channel._control = reader.ReadUInt16();
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
                writer.Write(channel._source);
                writer.Write(channel._sourceReload);
                writer.Write(channel._destination);
                writer.Write(channel._destinationReload);
                writer.Write(channel._length);
                writer.Write(channel._lengthReload);
                writer.Write(channel._control);
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
                Register.DMA0CNT_H => _channel0._control,
                Register.DMA1CNT_H => _channel1._control,
                Register.DMA2CNT_H => _channel2._control,
                Register.DMA3CNT_H => _channel3._control,

                // should never happen
                _ => throw new Exception("Iris.GBA.DMA: Register read error"),
            };
        }

        internal void WriteRegister(Register register, UInt16 value, Memory.RegisterWriteMode mode)
        {
            void WriteSourceReload_Low(ref Channel channel)
            {
                UInt16 low = (UInt16)channel._sourceReload;
                Memory.WriteRegisterHelper(ref low, value, mode);
                channel._sourceReload = (channel._sourceReload & 0xffff_0000) | low;
            }

            void WriteSourceReload_High(ref Channel channel, UInt16 mask)
            {
                UInt16 high = (UInt16)(channel._sourceReload >> 16);
                Memory.WriteRegisterHelper(ref high, (UInt16)(value & mask), mode);
                channel._sourceReload = (channel._sourceReload & 0x0000_ffff) | (UInt32)(high << 16);
            }

            void WriteDestinationReload_Low(ref Channel channel)
            {
                UInt16 low = (UInt16)channel._destinationReload;
                Memory.WriteRegisterHelper(ref low, value, mode);
                channel._destinationReload = (channel._destinationReload & 0xffff_0000) | low;
            }

            void WriteDestinationReload_High(ref Channel channel, UInt16 mask)
            {
                UInt16 high = (UInt16)(channel._destinationReload >> 16);
                Memory.WriteRegisterHelper(ref high, (UInt16)(value & mask), mode);
                channel._destinationReload = (channel._destinationReload & 0x0000_ffff) | (UInt32)(high << 16);
            }

            void WriteLengthReload(ref Channel channel)
            {
                UInt16 reload = channel._lengthReload;
                Memory.WriteRegisterHelper(ref reload, value, mode);
                channel._lengthReload = reload;
            }

            void WriteControl(ref Channel channel, InterruptControl.Interrupt interrupt, UInt32 maxLength)
            {
                UInt16 previousControl = channel._control;

                UInt16 newControl = channel._control;
                Memory.WriteRegisterHelper(ref newControl, value, mode);
                channel._control = newControl;

                if (((previousControl & 0x8000) == 0) && ((newControl & 0x8000) == 0x8000))
                {
                    channel._source = channel._sourceReload;
                    channel._destination = channel._destinationReload;
                    channel._length = (channel._lengthReload == 0) ? maxLength : channel._lengthReload;

                    PerformTransfer(ref channel, StartTiming.Immediate, interrupt, maxLength);
                }
            }

            switch (register)
            {
                case Register.DMA0SAD_L:
                    WriteSourceReload_Low(ref _channel0);
                    break;
                case Register.DMA0SAD_H:
                    WriteSourceReload_High(ref _channel0, 0x07ff);
                    break;

                case Register.DMA0DAD_L:
                    WriteDestinationReload_Low(ref _channel0);
                    break;
                case Register.DMA0DAD_H:
                    WriteDestinationReload_High(ref _channel0, 0x07ff);
                    break;

                case Register.DMA0CNT_L:
                    WriteLengthReload(ref _channel0);
                    break;
                case Register.DMA0CNT_H:
                    WriteControl(ref _channel0, InterruptControl.Interrupt.DMA0, MaxLengthChannel0);
                    break;

                case Register.DMA1SAD_L:
                    WriteSourceReload_Low(ref _channel1);
                    break;
                case Register.DMA1SAD_H:
                    WriteSourceReload_High(ref _channel1, 0x0fff);
                    break;

                case Register.DMA1DAD_L:
                    WriteDestinationReload_Low(ref _channel1);
                    break;
                case Register.DMA1DAD_H:
                    WriteDestinationReload_High(ref _channel1, 0x07ff);
                    break;

                case Register.DMA1CNT_L:
                    WriteLengthReload(ref _channel1);
                    break;
                case Register.DMA1CNT_H:
                    WriteControl(ref _channel1, InterruptControl.Interrupt.DMA1, MaxLengthChannel1);
                    break;

                case Register.DMA2SAD_L:
                    WriteSourceReload_Low(ref _channel2);
                    break;
                case Register.DMA2SAD_H:
                    WriteSourceReload_High(ref _channel2, 0x0fff);
                    break;

                case Register.DMA2DAD_L:
                    WriteDestinationReload_Low(ref _channel2);
                    break;
                case Register.DMA2DAD_H:
                    WriteDestinationReload_High(ref _channel2, 0x07ff);
                    break;

                case Register.DMA2CNT_L:
                    WriteLengthReload(ref _channel2);
                    break;
                case Register.DMA2CNT_H:
                    WriteControl(ref _channel2, InterruptControl.Interrupt.DMA2, MaxLengthChannel2);
                    break;

                case Register.DMA3SAD_L:
                    WriteSourceReload_Low(ref _channel3);
                    break;
                case Register.DMA3SAD_H:
                    WriteSourceReload_High(ref _channel3, 0x0fff);
                    break;

                case Register.DMA3DAD_L:
                    WriteDestinationReload_Low(ref _channel3);
                    break;
                case Register.DMA3DAD_H:
                    WriteDestinationReload_High(ref _channel3, 0x0fff);
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
            if ((_channel0._control & 0x8000) == 0x8000)
                PerformTransfer(ref _channel0, startTiming, InterruptControl.Interrupt.DMA0, MaxLengthChannel0);

            if ((_channel1._control & 0x8000) == 0x8000)
                PerformTransfer(ref _channel1, startTiming, InterruptControl.Interrupt.DMA1, MaxLengthChannel1);

            if ((_channel2._control & 0x8000) == 0x8000)
                PerformTransfer(ref _channel2, startTiming, InterruptControl.Interrupt.DMA2, MaxLengthChannel2);

            if ((_channel3._control & 0x8000) == 0x8000)
                PerformTransfer(ref _channel3, startTiming, InterruptControl.Interrupt.DMA3, MaxLengthChannel3);
        }

        private void PerformTransfer(ref Channel channel, StartTiming startTiming, InterruptControl.Interrupt interrupt, UInt32 maxLength)
        {
            if (((channel._control >> 12) & 0b11) != (int)startTiming)
                return;

            UInt16 sourceAddressControlFlag = (UInt16)((channel._control >> 7) & 0b11);
            UInt16 destinationAddressControlFlag = (UInt16)((channel._control >> 5) & 0b11);

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
            if ((channel._control & 0x0400) == 0)
            {
                const int DataUnitSize = 2;

                int sourceIncrement = GetSourceIncrement(DataUnitSize);
                (int destinationIncrement, reloadDestination) = GetDestinationIncrement(DataUnitSize);

                for (; channel._length > 0; --channel._length)
                {
                    _memory.Write16(channel._destination, _memory.Read16(channel._source));

                    channel._source = (UInt32)(channel._source + sourceIncrement);
                    channel._destination = (UInt32)(channel._destination + destinationIncrement);
                }
            }

            // 32 bits
            else
            {
                const int DataUnitSize = 4;

                int sourceIncrement = GetSourceIncrement(DataUnitSize);
                (int destinationIncrement, reloadDestination) = GetDestinationIncrement(DataUnitSize);

                for (; channel._length > 0; --channel._length)
                {
                    _memory.Write32(channel._destination, _memory.Read32(channel._source));

                    channel._source = (UInt32)(channel._source + sourceIncrement);
                    channel._destination = (UInt32)(channel._destination + destinationIncrement);
                }
            }

            if ((channel._control & 0x4000) == 0x4000)
                _interruptControl.RequestInterrupt(interrupt);

            // Repeat off
            if ((channel._control & 0x0200) == 0)
            {
                channel._control = (UInt16)(channel._control & ~0x8000);
            }

            // Repeat on
            else
            {
                if (reloadDestination)
                    channel._destination = channel._destinationReload;

                channel._length = (channel._lengthReload == 0) ? maxLength : channel._lengthReload;
            }
        }
    }
}
