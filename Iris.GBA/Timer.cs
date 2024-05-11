namespace Iris.GBA
{
    // TODO: optimize
    // - lazy counter update (on read and overflow)
    // - overflow on scheduler
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

        private struct Channel
        {
            internal UInt16 _counter;
            internal UInt16 _reload;
            internal UInt16 _control;
            internal UInt32 _cycleCounter;
            internal bool _running;
        }

        private Channel _channel0;
        private Channel _channel1;
        private Channel _channel2;
        private Channel _channel3;

        private const int StateSaveVersion = 1;

        internal Timer(Common.Scheduler scheduler)
        {
            _scheduler = scheduler;

            _scheduler.RegisterTask((int)GBA_System.TaskId.StartCountingChannel0, (UInt32 cycleCountDelay) => StartCounting(ref _channel0, cycleCountDelay));
            _scheduler.RegisterTask((int)GBA_System.TaskId.StartCountingChannel1, (UInt32 cycleCountDelay) => StartCounting(ref _channel1, cycleCountDelay));
            _scheduler.RegisterTask((int)GBA_System.TaskId.StartCountingChannel2, (UInt32 cycleCountDelay) => StartCounting(ref _channel2, cycleCountDelay));
            _scheduler.RegisterTask((int)GBA_System.TaskId.StartCountingChannel3, (UInt32 cycleCountDelay) => StartCounting(ref _channel3, cycleCountDelay));
        }

        internal void Initialize(InterruptControl interruptControl)
        {
            _interruptControl = interruptControl;
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
            if (reader.ReadInt32() != StateSaveVersion)
                throw new Exception();

            void LoadChannel(ref Channel channel)
            {
                channel._counter = reader.ReadUInt16();
                channel._reload = reader.ReadUInt16();
                channel._control = reader.ReadUInt16();
                channel._cycleCounter = reader.ReadUInt32();
                channel._running = reader.ReadBoolean();
            }

            LoadChannel(ref _channel0);
            LoadChannel(ref _channel1);
            LoadChannel(ref _channel2);
            LoadChannel(ref _channel3);
        }

        internal void SaveState(BinaryWriter writer)
        {
            writer.Write(StateSaveVersion);

            void SaveChannel(Channel channel)
            {
                writer.Write(channel._counter);
                writer.Write(channel._reload);
                writer.Write(channel._control);
                writer.Write(channel._cycleCounter);
                writer.Write(channel._running);
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
                Register.TM0CNT_L => _channel0._counter,
                Register.TM0CNT_H => _channel0._control,

                Register.TM1CNT_L => _channel1._counter,
                Register.TM1CNT_H => _channel1._control,

                Register.TM2CNT_L => _channel2._counter,
                Register.TM2CNT_H => _channel2._control,

                Register.TM3CNT_L => _channel3._counter,
                Register.TM3CNT_H => _channel3._control,

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

            void WriteControl(ref Channel channel, GBA_System.TaskId startCountingTaskId)
            {
                UInt16 previousControl = channel._control;

                UInt16 newControl = channel._control;
                Memory.WriteRegisterHelper(ref newControl, value, mode);
                channel._control = newControl;

                if (((previousControl & 0x0080) == 0) && ((newControl & 0x0080) == 0x0080))
                    _scheduler.ScheduleTask((int)startCountingTaskId, 2);
                else if (((previousControl & 0x0080) == 0x0080) && ((newControl & 0x0080) == 0))
                    channel._running = false;
            }

            switch (register)
            {
                case Register.TM0CNT_L:
                    WriteReload(ref _channel0);
                    break;
                case Register.TM0CNT_H:
                    WriteControl(ref _channel0, GBA_System.TaskId.StartCountingChannel0);
                    break;

                case Register.TM1CNT_L:
                    WriteReload(ref _channel1);
                    break;
                case Register.TM1CNT_H:
                    WriteControl(ref _channel1, GBA_System.TaskId.StartCountingChannel1);
                    break;

                case Register.TM2CNT_L:
                    WriteReload(ref _channel2);
                    break;
                case Register.TM2CNT_H:
                    WriteControl(ref _channel2, GBA_System.TaskId.StartCountingChannel2);
                    break;

                case Register.TM3CNT_L:
                    WriteReload(ref _channel3);
                    break;
                case Register.TM3CNT_H:
                    WriteControl(ref _channel3, GBA_System.TaskId.StartCountingChannel3);
                    break;

                // should never happen
                default:
                    throw new Exception("Iris.GBA.Timer: Register write error");
            }
        }

        internal void UpdateAllCounters(UInt32 cycleCount)
        {
            UInt32 overflowCount = 0;

            void UpdateCounter(ref Channel channel, bool isFirstChannel, InterruptControl.Interrupt interrupt)
            {
                if (!channel._running)
                {
                    overflowCount = 0;
                    return;
                }

                UInt32 counterIncrement;

                if (((channel._control & 0x0004) == 0) || isFirstChannel)
                {
                    channel._cycleCounter += cycleCount;

                    UInt32 prescaler = (channel._control & 0b11) switch
                    {
                        0b00 => 1,
                        0b01 => 64,
                        0b10 => 256,
                        0b11 => 1024,

                        // cannot happen
                        _ => 0,
                    };

                    counterIncrement = channel._cycleCounter / prescaler;
                    channel._cycleCounter %= prescaler;
                }
                else
                {
                    counterIncrement = overflowCount;
                }

                UInt32 counter = channel._counter + counterIncrement;

                if (counter >= 0x1_0000)
                {
                    (overflowCount, counterIncrement) = Math.DivRem(counter - 0x1_0000u, 0x1_0000u - channel._reload);

                    channel._counter = (UInt16)(channel._reload + counterIncrement);
                    ++overflowCount;

                    if ((channel._control & 0x0040) == 0x0040)
                        _interruptControl.RequestInterrupt(interrupt);
                }
                else
                {
                    channel._counter = (UInt16)counter;
                    overflowCount = 0;
                }
            }

            UpdateCounter(ref _channel0, true, InterruptControl.Interrupt.Timer0);
            UpdateCounter(ref _channel1, false, InterruptControl.Interrupt.Timer1);
            UpdateCounter(ref _channel2, false, InterruptControl.Interrupt.Timer2);
            UpdateCounter(ref _channel3, false, InterruptControl.Interrupt.Timer3);
        }

        private static void StartCounting(ref Channel channel, UInt32 cycleCountDelay)
        {
            if ((channel._control & 0x0080) == 0)
                return;

            channel._counter = channel._reload;
            channel._cycleCounter = cycleCountDelay;
            channel._running = true;
        }
    }
}
