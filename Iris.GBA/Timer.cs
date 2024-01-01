using Iris.Common;

namespace Iris.GBA
{
    internal sealed class Timer
    {
        internal enum Register
        {
            TM0CNT_L
        }

        internal UInt16 _TM0CNT_L;
        internal UInt16 _TM0CNT_H;

        internal UInt16 _TM1CNT_L;
        internal UInt16 _TM1CNT_H;

        internal UInt16 _TM2CNT_L;
        internal UInt16 _TM2CNT_H;

        internal UInt16 _TM3CNT_L;
        internal UInt16 _TM3CNT_H;

        private readonly Scheduler _scheduler;

        private readonly int _performTimer0TaskId;

        private record struct TimerState
        (
            bool Enabled,
            UInt16 Counter,
            UInt16 Reload
        );

        private TimerState _timer0;

        internal Timer(Scheduler scheduler)
        {
            _scheduler = scheduler;

            _performTimer0TaskId = _scheduler.RegisterTask(PerformTimer0);
        }

        internal void ResetState()
        {
            _TM0CNT_L = 0;
            _TM0CNT_H = 0;

            _TM1CNT_L = 0;
            _TM1CNT_H = 0;

            _TM2CNT_L = 0;
            _TM2CNT_H = 0;

            _TM3CNT_L = 0;
            _TM3CNT_H = 0;
        }

        internal void LoadState(BinaryReader reader)
        {
            _TM0CNT_L = reader.ReadUInt16();
            _TM0CNT_H = reader.ReadUInt16();

            _TM1CNT_L = reader.ReadUInt16();
            _TM1CNT_H = reader.ReadUInt16();

            _TM2CNT_L = reader.ReadUInt16();
            _TM2CNT_H = reader.ReadUInt16();

            _TM3CNT_L = reader.ReadUInt16();
            _TM3CNT_H = reader.ReadUInt16();
        }

        internal void SaveState(BinaryWriter writer)
        {
            writer.Write(_TM0CNT_L);
            writer.Write(_TM0CNT_H);

            writer.Write(_TM1CNT_L);
            writer.Write(_TM1CNT_H);

            writer.Write(_TM2CNT_L);
            writer.Write(_TM2CNT_H);

            writer.Write(_TM3CNT_L);
            writer.Write(_TM3CNT_H);
        }

        internal UInt16 ReadRegister(Register register)
        {
            return register switch
            {
                Register.TM0CNT_L => _timer0.Counter,
                _ => 0,
            };
        }

        internal void WriteRegister(Register register, UInt16 value)
        {
            switch (register)
            {
                case Register.TM0CNT_L:
                    _timer0.Reload = value;
                    break;
            }
        }

        internal void UpdateTimer0()
        {
            if ((_TM0CNT_H & 0x0080) == 0)
            {
                _timer0.Enabled = false;
            }
            else if (!_timer0.Enabled)
            {
                _timer0.Enabled = true;
                _timer0.Counter = _timer0.Reload;

                _scheduler.ScheduleTask(GetPrescalar(_TM0CNT_H), _performTimer0TaskId);
            }
        }

        private void PerformTimer0(UInt32 cycleCountDelay)
        {
            if (!_timer0.Enabled)
                return;

            UInt32 prescalar = GetPrescalar(_TM0CNT_H);

            void IncrementCounter()
            {
                if (_timer0.Counter == 0xffff)
                {
                    _timer0.Counter = _timer0.Reload;

                    if ((_TM0CNT_H & 0x0040) == 0x0040)
                    {
                        // TODO: IRQ
                    }
                }
                else
                {
                    ++_timer0.Counter;
                }
            }

            IncrementCounter();

            for (; cycleCountDelay >= prescalar; cycleCountDelay -= prescalar)
                IncrementCounter();

            _scheduler.ScheduleTask(prescalar - cycleCountDelay, _performTimer0TaskId);
        }

        private static UInt32 GetPrescalar(UInt16 cnt_h)
        {
            return (cnt_h & 0b11) switch
            {
                0b00 => 1,
                0b01 => 64,
                0b10 => 256,
                0b11 => 1024,
                // should never happen
                _ => throw new Exception("Iris.GBA.Timer: Wrong prescalar selection"),
            };
        }
    }
}
