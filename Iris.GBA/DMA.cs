using System.Diagnostics;

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

        private enum StartTiming
        {
            Immediate = 0b00,
            VBlank = 0b01,
            HBlank = 0b10,
            Special = 0b11
        }

        private readonly Common.Scheduler _scheduler;

        private InterruptControl _interruptControl;
        private Memory _memory;

        private struct Channel(GBA_System.TaskId startTaskId, InterruptControl.Interrupt interrupt, UInt32 maxLength)
        {
            internal UInt32 _source;
            internal UInt32 _sourceReload;
            internal UInt32 _destination;
            internal UInt32 _destinationReload;
            internal UInt32 _length;
            internal UInt16 _lengthReload;
            internal UInt16 _control;
            internal bool _running;

            internal readonly GBA_System.TaskId _startTaskId = startTaskId;
            internal readonly InterruptControl.Interrupt _interrupt = interrupt;
            internal readonly UInt32 _maxLength = maxLength;
        }

        private readonly Channel[] _channels =
        [
            new (GBA_System.TaskId.StartDMA_Channel0, InterruptControl.Interrupt.DMA0, 0x4000),
            new (GBA_System.TaskId.StartDMA_Channel1, InterruptControl.Interrupt.DMA1, 0x4000),
            new (GBA_System.TaskId.StartDMA_Channel2, InterruptControl.Interrupt.DMA2, 0x4000),
            new (GBA_System.TaskId.StartDMA_Channel3, InterruptControl.Interrupt.DMA3, 0x1_0000)
        ];

        internal DMA(Common.Scheduler scheduler)
        {
            _scheduler = scheduler;

            for (int channelIndex = 0; channelIndex < 4; ++channelIndex)
            {
                int channelIndexCopy = channelIndex;
                _scheduler.RegisterTask((int)_channels[channelIndex]._startTaskId, _ => Start(channelIndexCopy));
            }

            _scheduler.RegisterTask((int)GBA_System.TaskId.PerformVBlankTransfers, _ => PerformVBlankTransfers());
            _scheduler.RegisterTask((int)GBA_System.TaskId.PerformHBlankTransfers, _ => PerformHBlankTransfers());
            _scheduler.RegisterTask((int)GBA_System.TaskId.PerformVideoTransfer, _ => PerformVideoTransfer(false));
            _scheduler.RegisterTask((int)GBA_System.TaskId.PerformVideoTransferEnd, _ => PerformVideoTransfer(true));
        }

        internal void Initialize(InterruptControl interruptControl, Memory memory)
        {
            _interruptControl = interruptControl;
            _memory = memory;
        }

        internal void ResetState()
        {
            foreach (ref Channel channel in _channels.AsSpan())
            {
                channel._source = 0;
                channel._sourceReload = 0;
                channel._destination = 0;
                channel._destinationReload = 0;
                channel._length = 0;
                channel._lengthReload = 0;
                channel._control = 0;
                channel._running = false;
            }
        }

        internal void LoadState(BinaryReader reader)
        {
            foreach (ref Channel channel in _channels.AsSpan())
            {
                channel._source = reader.ReadUInt32();
                channel._sourceReload = reader.ReadUInt32();
                channel._destination = reader.ReadUInt32();
                channel._destinationReload = reader.ReadUInt32();
                channel._length = reader.ReadUInt32();
                channel._lengthReload = reader.ReadUInt16();
                channel._control = reader.ReadUInt16();
                channel._running = reader.ReadBoolean();
            }
        }

        internal void SaveState(BinaryWriter writer)
        {
            foreach (Channel channel in _channels)
            {
                writer.Write(channel._source);
                writer.Write(channel._sourceReload);
                writer.Write(channel._destination);
                writer.Write(channel._destinationReload);
                writer.Write(channel._length);
                writer.Write(channel._lengthReload);
                writer.Write(channel._control);
                writer.Write(channel._running);
            }
        }

        internal UInt16 ReadRegister(Register register)
        {
            return register switch
            {
                Register.DMA0CNT_H => _channels[0]._control,
                Register.DMA1CNT_H => _channels[1]._control,
                Register.DMA2CNT_H => _channels[2]._control,
                Register.DMA3CNT_H => _channels[3]._control,

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

            void WriteControl(ref Channel channel)
            {
                UInt16 previousControl = channel._control;

                UInt16 newControl = channel._control;
                Memory.WriteRegisterHelper(ref newControl, value, mode);
                channel._control = newControl;

                if ((previousControl & 0x8000) == 0)
                {
                    if ((newControl & 0x8000) == 0x8000)
                        _scheduler.ScheduleTaskSoon((int)channel._startTaskId, 2);
                }
                else
                {
                    if ((newControl & 0x8000) == 0)
                    {
                        if (channel._running)
                            channel._running = false;
                        else
                            _scheduler.CancelTask((int)channel._startTaskId);
                    }
                }
            }

            switch (register)
            {
                case Register.DMA0SAD_L:
                    WriteSourceReload_Low(ref _channels[0]);
                    break;
                case Register.DMA0SAD_H:
                    WriteSourceReload_High(ref _channels[0], 0x07ff);
                    break;

                case Register.DMA0DAD_L:
                    WriteDestinationReload_Low(ref _channels[0]);
                    break;
                case Register.DMA0DAD_H:
                    WriteDestinationReload_High(ref _channels[0], 0x07ff);
                    break;

                case Register.DMA0CNT_L:
                    WriteLengthReload(ref _channels[0]);
                    break;
                case Register.DMA0CNT_H:
                    WriteControl(ref _channels[0]);
                    break;

                case Register.DMA1SAD_L:
                    WriteSourceReload_Low(ref _channels[1]);
                    break;
                case Register.DMA1SAD_H:
                    WriteSourceReload_High(ref _channels[1], 0x0fff);
                    break;

                case Register.DMA1DAD_L:
                    WriteDestinationReload_Low(ref _channels[1]);
                    break;
                case Register.DMA1DAD_H:
                    WriteDestinationReload_High(ref _channels[1], 0x07ff);
                    break;

                case Register.DMA1CNT_L:
                    WriteLengthReload(ref _channels[1]);
                    break;
                case Register.DMA1CNT_H:
                    WriteControl(ref _channels[1]);
                    break;

                case Register.DMA2SAD_L:
                    WriteSourceReload_Low(ref _channels[2]);
                    break;
                case Register.DMA2SAD_H:
                    WriteSourceReload_High(ref _channels[2], 0x0fff);
                    break;

                case Register.DMA2DAD_L:
                    WriteDestinationReload_Low(ref _channels[2]);
                    break;
                case Register.DMA2DAD_H:
                    WriteDestinationReload_High(ref _channels[2], 0x07ff);
                    break;

                case Register.DMA2CNT_L:
                    WriteLengthReload(ref _channels[2]);
                    break;
                case Register.DMA2CNT_H:
                    WriteControl(ref _channels[2]);
                    break;

                case Register.DMA3SAD_L:
                    WriteSourceReload_Low(ref _channels[3]);
                    break;
                case Register.DMA3SAD_H:
                    WriteSourceReload_High(ref _channels[3], 0x0fff);
                    break;

                case Register.DMA3DAD_L:
                    WriteDestinationReload_Low(ref _channels[3]);
                    break;
                case Register.DMA3DAD_H:
                    WriteDestinationReload_High(ref _channels[3], 0x0fff);
                    break;

                case Register.DMA3CNT_L:
                    WriteLengthReload(ref _channels[3]);
                    break;
                case Register.DMA3CNT_H:
                    WriteControl(ref _channels[3]);
                    break;

                // should never happen
                default:
                    throw new Exception("Iris.GBA.DMA: Register write error");
            }
        }

        private void PerformVBlankTransfers()
        {
            for (int channelIndex = 0; channelIndex < 4; ++channelIndex)
            {
                ref Channel channel = ref _channels[channelIndex];

                if (channel._running && (((channel._control >> 12) & 0b11) == (int)StartTiming.VBlank))
                    PerformTransfer(ref channel, channelIndex);
            }
        }

        private void PerformHBlankTransfers()
        {
            for (int channelIndex = 0; channelIndex < 4; ++channelIndex)
            {
                ref Channel channel = ref _channels[channelIndex];

                if (channel._running && (((channel._control >> 12) & 0b11) == (int)StartTiming.HBlank))
                    PerformTransfer(ref channel, channelIndex);
            }
        }

        private void PerformVideoTransfer(bool disable)
        {
            const int channelIndex = 3;

            ref Channel channel = ref _channels[channelIndex];

            if (channel._running && (((channel._control >> 12) & 0b11) == (int)StartTiming.Special))
            {
                PerformTransfer(ref channel, channelIndex);

                if (disable)
                {
                    channel._control = (UInt16)(channel._control & ~0x8000);
                    channel._running = false;
                }
            }
        }

        private void Start(int channelIndex)
        {
            ref Channel channel = ref _channels[channelIndex];

            channel._source = channel._sourceReload;
            channel._destination = channel._destinationReload;
            channel._length = (channel._lengthReload == 0) ? channel._maxLength : channel._lengthReload;
            channel._running = true;

            if (((channel._control >> 12) & 0b11) == (int)StartTiming.Immediate)
                PerformTransfer(ref channel, channelIndex);
        }

        private void PerformTransfer(ref Channel channel, int channelIndex)
        {
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
                    _ => throw new UnreachableException(),
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
                    _ => throw new UnreachableException(),
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

                    _scheduler.AdvanceCycleCounter(2);
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

                    _scheduler.AdvanceCycleCounter(2);
                }
            }

            if ((channel._control & 0x4000) == 0x4000)
                _interruptControl.RequestInterrupt(channel._interrupt);

            // Repeat off
            if ((channel._control & 0x0200) == 0)
            {
                channel._control = (UInt16)(channel._control & ~0x8000);
                channel._running = false;
            }

            // Repeat on
            else
            {
                if (reloadDestination)
                    channel._destination = channel._destinationReload;

                channel._length = (channel._lengthReload == 0) ? channel._maxLength : channel._lengthReload;
            }
        }
    }
}
