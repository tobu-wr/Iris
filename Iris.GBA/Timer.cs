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

        private record struct Channel
        (
            UInt16 Counter,
            UInt16 Reload,
            UInt16 Control,
            UInt32 CycleCounter,
            bool Running
        );

        private Channel _channel0;
        private Channel _channel1;
        private Channel _channel2;
        private Channel _channel3;

        internal Timer(Common.Scheduler scheduler)
        {
            _scheduler = scheduler;

            _scheduler.RegisterTask((UInt32 cycleCountDelay) => StartCounting(ref _channel0, cycleCountDelay), (int)GBA_System.TaskId.StartCountingChannel0);
            _scheduler.RegisterTask((UInt32 cycleCountDelay) => StartCounting(ref _channel1, cycleCountDelay), (int)GBA_System.TaskId.StartCountingChannel1);
            _scheduler.RegisterTask((UInt32 cycleCountDelay) => StartCounting(ref _channel2, cycleCountDelay), (int)GBA_System.TaskId.StartCountingChannel2);
            _scheduler.RegisterTask((UInt32 cycleCountDelay) => StartCounting(ref _channel3, cycleCountDelay), (int)GBA_System.TaskId.StartCountingChannel3);
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
            void LoadChannel(ref Channel channel)
            {
                channel.Counter = reader.ReadUInt16();
                channel.Reload = reader.ReadUInt16();
                channel.Control = reader.ReadUInt16();
                channel.CycleCounter = reader.ReadUInt32();
                channel.Running = reader.ReadBoolean();
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
                writer.Write(channel.Counter);
                writer.Write(channel.Reload);
                writer.Write(channel.Control);
                writer.Write(channel.CycleCounter);
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
                Register.TM0CNT_L => _channel0.Counter,
                Register.TM0CNT_H => _channel0.Control,

                Register.TM1CNT_L => _channel1.Counter,
                Register.TM1CNT_H => _channel1.Control,

                Register.TM2CNT_L => _channel2.Counter,
                Register.TM2CNT_H => _channel2.Control,

                Register.TM3CNT_L => _channel3.Counter,
                Register.TM3CNT_H => _channel3.Control,

                // should never happen
                _ => throw new Exception("Iris.GBA.Timer: Register read error"),
            };
        }

        internal void WriteRegister(Register register, UInt16 value, Memory.RegisterWriteMode mode)
        {
            void WriteReload(ref Channel channel)
            {
                UInt16 reload = channel.Reload;
                Memory.WriteRegisterHelper(ref reload, value, mode);
                channel.Reload = reload;
            }

            void WriteControl(ref Channel channel, GBA_System.TaskId startCountingTaskId)
            {
                UInt16 previousControl = channel.Control;

                UInt16 newControl = channel.Control;
                Memory.WriteRegisterHelper(ref newControl, value, mode);
                channel.Control = newControl;

                if (((previousControl & 0x0080) == 0) && ((newControl & 0x0080) == 0x0080))
                    _scheduler.ScheduleTask(2, (int)startCountingTaskId);
                else if (((previousControl & 0x0080) == 0x0080) && ((newControl & 0x0080) == 0))
                    channel.Running = false;
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
                if (!channel.Running)
                {
                    overflowCount = 0;
                    return;
                }

                UInt32 counterIncrement;

                if (((channel.Control & 0x0004) == 0) || isFirstChannel)
                {
                    channel.CycleCounter += cycleCount;

                    UInt32 prescaler = (channel.Control & 0b11) switch
                    {
                        0b00 => 1,
                        0b01 => 64,
                        0b10 => 256,
                        0b11 => 1024,

                        // cannot happen
                        _ => 0,
                    };

                    (counterIncrement, channel.CycleCounter) = Math.DivRem(channel.CycleCounter, prescaler);
                }
                else
                {
                    counterIncrement = overflowCount;
                }

                UInt32 counter = channel.Counter + counterIncrement;

                if (counter >= 0x1_0000)
                {
                    (overflowCount, counterIncrement) = Math.DivRem(counter - 0x1_0000u, 0x1_0000u - channel.Reload);

                    channel.Counter = (UInt16)(channel.Reload + counterIncrement);
                    ++overflowCount;

                    if ((channel.Control & 0x0040) == 0x0040)
                        _interruptControl.RequestInterrupt(interrupt);
                }
                else
                {
                    channel.Counter = (UInt16)counter;
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
            if ((channel.Control & 0x0080) == 0)
                return;

            channel.Counter = channel.Reload;
            channel.CycleCounter = cycleCountDelay;
            channel.Running = true;
        }
    }
}
