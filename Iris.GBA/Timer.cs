namespace Iris.GBA
{
    internal sealed class Timer
    {
        internal enum Register
        {
            TM0CNT_L,
            TM0CNT_H,

            TM1CNT_L,
            TM1CNT_H,

            TM2CNT_L,
            TM2CNT_H,

            TM3CNT_L,
            TM3CNT_H
        }

        private readonly Common.Scheduler _scheduler;

        private InterruptControl _interruptControl;

        private struct Channel(GBA_System.TaskId startTaskId, GBA_System.TaskId handleOverflowTaskId, InterruptControl.Interrupt interrupt)
        {
            internal UInt16 _counter;
            internal UInt16 _reload;
            internal UInt16 _control;
            internal UInt64 _cycleCount; // only used in non-cascading mode
            internal bool _running;

            internal readonly GBA_System.TaskId _startTaskId = startTaskId;
            internal readonly GBA_System.TaskId _handleOverflowTaskId = handleOverflowTaskId;
            internal readonly InterruptControl.Interrupt _interrupt = interrupt;
        }

        private readonly Channel[] _channels;

        internal Timer(Common.Scheduler scheduler)
        {
            _scheduler = scheduler;

            _channels =
            [
                new(GBA_System.TaskId.StartTimer_Channel0, GBA_System.TaskId.HandleTimerOverflow_Channel0, InterruptControl.Interrupt.Timer0),
                new(GBA_System.TaskId.StartTimer_Channel1, GBA_System.TaskId.HandleTimerOverflow_Channel1, InterruptControl.Interrupt.Timer1),
                new(GBA_System.TaskId.StartTimer_Channel2, GBA_System.TaskId.HandleTimerOverflow_Channel2, InterruptControl.Interrupt.Timer2),
                new(GBA_System.TaskId.StartTimer_Channel3, GBA_System.TaskId.HandleTimerOverflow_Channel3, InterruptControl.Interrupt.Timer3)
            ];

            for (int channelIndex = 0; channelIndex < 4; ++channelIndex)
            {
                int channelIndexCopy = channelIndex;
                _scheduler.RegisterTask((int)_channels[channelIndex]._startTaskId, cycleCountDelay => Start(channelIndexCopy, cycleCountDelay));
                _scheduler.RegisterTask((int)_channels[channelIndex]._handleOverflowTaskId, cycleCountDelay => HandleOverflow(channelIndexCopy, cycleCountDelay));
            }
        }

        internal void Initialize(InterruptControl interruptControl)
        {
            _interruptControl = interruptControl;
        }

        internal void ResetState()
        {
            foreach (ref Channel channel in _channels.AsSpan())
            {
                channel._counter = 0;
                channel._reload = 0;
                channel._control = 0;
                channel._cycleCount = 0;
                channel._running = false;
            }
        }

        internal void LoadState(BinaryReader reader)
        {
            foreach (ref Channel channel in _channels.AsSpan())
            {
                channel._counter = reader.ReadUInt16();
                channel._reload = reader.ReadUInt16();
                channel._control = reader.ReadUInt16();
                channel._cycleCount = reader.ReadUInt64();
                channel._running = reader.ReadBoolean();
            }
        }

        internal void SaveState(BinaryWriter writer)
        {
            foreach (Channel channel in _channels)
            {
                writer.Write(channel._counter);
                writer.Write(channel._reload);
                writer.Write(channel._control);
                writer.Write(channel._cycleCount);
                writer.Write(channel._running);
            }
        }

        internal UInt16 ReadRegister(Register register)
        {
            UInt16 ReadCounter(int channelIndex)
            {
                ref Channel channel = ref _channels[channelIndex];

                if (channel._running && (((channel._control & 0x0004) == 0) || (channelIndex == 0)))
                    UpdateCounter(ref channel, channel._control);

                return channel._counter;
            }

            return register switch
            {
                Register.TM0CNT_L => ReadCounter(0),
                Register.TM0CNT_H => _channels[0]._control,

                Register.TM1CNT_L => ReadCounter(1),
                Register.TM1CNT_H => _channels[1]._control,

                Register.TM2CNT_L => ReadCounter(2),
                Register.TM2CNT_H => _channels[2]._control,

                Register.TM3CNT_L => ReadCounter(3),
                Register.TM3CNT_H => _channels[3]._control,

                // should never happen
                _ => throw new Exception("Iris.GBA.Timer: Register read error"),
            };
        }

        internal void WriteRegister(Register register, UInt16 value, Memory.RegisterWriteMode mode)
        {
            void WriteReload(ref Channel channel)
            {
                UInt16 reload = channel._reload;
                Memory.WriteRegisterHelper(ref reload, value, mode);
                channel._reload = reload;
            }

            void WriteControl(int channelIndex)
            {
                ref Channel channel = ref _channels[channelIndex];

                UInt16 previousControl = channel._control;

                UInt16 newControl = channel._control;
                Memory.WriteRegisterHelper(ref newControl, value, mode);
                channel._control = newControl;

                CheckControl(ref channel, channelIndex, previousControl, newControl);
            }

            switch (register)
            {
                case Register.TM0CNT_L:
                    WriteReload(ref _channels[0]);
                    break;
                case Register.TM0CNT_H:
                    WriteControl(0);
                    break;

                case Register.TM1CNT_L:
                    WriteReload(ref _channels[1]);
                    break;
                case Register.TM1CNT_H:
                    WriteControl(1);
                    break;

                case Register.TM2CNT_L:
                    WriteReload(ref _channels[2]);
                    break;
                case Register.TM2CNT_H:
                    WriteControl(2);
                    break;

                case Register.TM3CNT_L:
                    WriteReload(ref _channels[3]);
                    break;
                case Register.TM3CNT_H:
                    WriteControl(3);
                    break;

                // should never happen
                default:
                    throw new Exception("Iris.GBA.Timer: Register write error");
            }
        }

        private void CheckControl(ref Channel channel, int channelIndex, UInt16 previousControl, UInt16 newControl)
        {
            // TODO
            if ((previousControl & 0x0080) == 0)
            {
                if ((newControl & 0x0080) == 0x0080)
                    _scheduler.ScheduleTask((int)channel._startTaskId, 2);
            }
            else
            {
                if ((newControl & 0x0080) == 0)
                {
                    if ((previousControl & 0x0004) == 0)
                    {
                        UpdateCounter(ref channel, previousControl);

                        _scheduler.CancelTask((int)channel._handleOverflowTaskId);
                    }
                }
                else
                {
                    if ((previousControl & 0x0004) == 0)
                    {
                        if ((newControl & 0x0004) == 0)
                        {
                            if ((previousControl & 0b11) != (newControl & 0b11))
                            {
                                UpdateCounter(ref channel, previousControl);

                                _scheduler.CancelTask((int)channel._handleOverflowTaskId);
                                _scheduler.ScheduleTask((int)channel._handleOverflowTaskId, ComputeCycleCountUntilOverflow(ref channel));
                            }
                        }
                        else
                        {
                            UpdateCounter(ref channel, previousControl);

                            _scheduler.CancelTask((int)channel._handleOverflowTaskId);
                        }
                    }
                    else
                    {
                        if ((newControl & 0x0004) == 0)
                        {
                            channel._cycleCount = _scheduler.GetCycleCounter();

                            _scheduler.ScheduleTask((int)channel._handleOverflowTaskId, ComputeCycleCountUntilOverflow(ref channel));
                        }
                    }
                }
            }
        }

        private void UpdateCounter(ref Channel channel, UInt16 control)
        {
            UInt64 currentCycleCount = _scheduler.GetCycleCounter();
            UInt64 cycleCountDelta = currentCycleCount - channel._cycleCount;
            UInt64 prescaler = GetPrescaler(control);

            channel._counter += (UInt16)(cycleCountDelta / prescaler);
            channel._cycleCount = currentCycleCount - (UInt16)(cycleCountDelta % prescaler);
        }

        private void Start(int channelIndex, UInt64 cycleCountDelay)
        {
            ref Channel channel = ref _channels[channelIndex];

            channel._counter = channel._reload;
            channel._running = true;

            if (((channel._control & 0x0004) == 0) || (channelIndex == 0))
            {
                channel._cycleCount = _scheduler.GetCycleCounter() - cycleCountDelay;

                _scheduler.ScheduleTask((int)channel._handleOverflowTaskId, ComputeCycleCountUntilOverflow(ref channel) - cycleCountDelay);
            }
        }

        private void HandleOverflow(int channelIndex, UInt64 cycleCountDelay)
        {
            ref Channel channel = ref _channels[channelIndex];

            channel._counter = channel._reload;
            channel._cycleCount = _scheduler.GetCycleCounter() - cycleCountDelay;

            _scheduler.ScheduleTask((int)channel._handleOverflowTaskId, ComputeCycleCountUntilOverflow(ref channel) - cycleCountDelay);

            if ((channel._control & 0x0040) == 0x0040)
                _interruptControl.RequestInterrupt(channel._interrupt);

            CascadeOverflow(channelIndex);
        }

        private void CascadeOverflow(int channelIndex)
        {
            if (channelIndex == 3)
                return;

            ++channelIndex;

            ref Channel channel = ref _channels[channelIndex];

            if (!channel._running || ((channel._control & 0x0004) == 0))
                return;

            if (channel._counter == 0xffff)
            {
                channel._counter = channel._reload;

                if ((channel._control & 0x0040) == 0x0040)
                    _interruptControl.RequestInterrupt(channel._interrupt);

                CascadeOverflow(channelIndex);
            }
            else
            {
                ++channel._counter;
            }
        }

        private static UInt64 ComputeCycleCountUntilOverflow(ref readonly Channel channel)
        {
            return (0x1_0000u - channel._counter) * GetPrescaler(channel._control);
        }

        private static UInt64 GetPrescaler(UInt16 control)
        {
            return (control & 0b11) switch
            {
                0b00 => 1,
                0b01 => 64,
                0b10 => 256,
                0b11 => 1024,
                _ => 0, // cannot happen
            };
        }
    }
}
