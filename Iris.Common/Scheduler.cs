namespace Iris.Common
{
    public sealed class Scheduler(int taskListSize, int scheduledTaskListSize)
    {
        public delegate void Task_Delegate(UInt64 cycleCountDelay);
        private readonly Task_Delegate[] _taskList = new Task_Delegate[taskListSize];

        private record struct ScheduledTaskListEntry(int Id, UInt64 CycleCount);
        private readonly ScheduledTaskListEntry[] _scheduledTaskList = new ScheduledTaskListEntry[scheduledTaskListSize]; // sorted by CycleCount in ascending order
        private int _scheduledTaskCount;

        private UInt64 _cycleCounter;

        public void ResetState()
        {
            _scheduledTaskCount = 0;
            _cycleCounter = 0;
        }

        public void LoadState(BinaryReader reader)
        {
            foreach (ref ScheduledTaskListEntry entry in _scheduledTaskList.AsSpan())
            {
                entry.Id = reader.ReadInt32();
                entry.CycleCount = reader.ReadUInt64();
            }

            _scheduledTaskCount = reader.ReadInt32();
            _cycleCounter = reader.ReadUInt64();
        }

        public void SaveState(BinaryWriter writer)
        {
            foreach (ScheduledTaskListEntry entry in _scheduledTaskList)
            {
                writer.Write(entry.Id);
                writer.Write(entry.CycleCount);
            }

            writer.Write(_scheduledTaskCount);
            writer.Write(_cycleCounter);
        }

        public UInt64 GetCycleCounter()
        {
            return _cycleCounter;
        }

        public void AdvanceCycleCounter(UInt64 cycleCount)
        {
            _cycleCounter += cycleCount;

            while ((_scheduledTaskCount > 0) && (_scheduledTaskList[0].CycleCount <= _cycleCounter))
            {
                // save the entry and remove it from the scheduling beforehand
                // because AdvanceCycleCounter can be called again while executing the task
                ScheduledTaskListEntry entry = _scheduledTaskList[0];

                --_scheduledTaskCount;

                if (_scheduledTaskCount > 0)
                    Array.Copy(_scheduledTaskList, 1, _scheduledTaskList, 0, _scheduledTaskCount);

                _taskList[entry.Id](_cycleCounter - entry.CycleCount);
            }
        }

        public void RegisterTask(int id, Task_Delegate task)
        {
            _taskList[id] = task;
        }

        public void ScheduleTask(int id, UInt64 cycleCount)
        {
            cycleCount += _cycleCounter;

            // searching is done backward because a new task is more likely to be inserted towards the end
            int index = _scheduledTaskCount;

            while ((index > 0) && (_scheduledTaskList[index - 1].CycleCount > cycleCount))
                --index;

            if (index < _scheduledTaskCount)
                Array.Copy(_scheduledTaskList, index, _scheduledTaskList, index + 1, _scheduledTaskCount - index);

            ++_scheduledTaskCount;

            _scheduledTaskList[index].Id = id;
            _scheduledTaskList[index].CycleCount = cycleCount;
        }

        public void CancelTask(int id)
        {
            for (int index = 0; index < _scheduledTaskCount; ++index)
            {
                if (_scheduledTaskList[index].Id == id)
                {
                    --_scheduledTaskCount;

                    if (index < _scheduledTaskCount)
                        Array.Copy(_scheduledTaskList, index + 1, _scheduledTaskList, index, _scheduledTaskCount - index);

                    return;
                }
            }
        }
    }
}
