using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Iris.Common
{
    public sealed class Scheduler(int taskListSize, int scheduledTaskListSize)
    {
        public delegate void Task_Delegate(UInt32 cycleCountDelay);
        private readonly Task_Delegate[] _taskList = new Task_Delegate[taskListSize];

        private record struct ScheduledTaskListEntry(int Id, UInt32 CycleCount);
        private readonly ScheduledTaskListEntry[] _scheduledTaskList = new ScheduledTaskListEntry[scheduledTaskListSize]; // sorted by CycleCount from smallest to largest
        private int _scheduledTaskCount;

        private UInt32 _cycleCounter;

        private const int StateSaveVersion = 1;

        public void ResetState()
        {
            _scheduledTaskCount = 0;
            _cycleCounter = 0;
        }

        public void LoadState(BinaryReader reader)
        {
            if (reader.ReadInt32() != StateSaveVersion)
                throw new Exception();

            foreach (ref ScheduledTaskListEntry entry in _scheduledTaskList.AsSpan())
            {
                entry.Id = reader.ReadInt32();
                entry.CycleCount = reader.ReadUInt32();
            }

            _scheduledTaskCount = reader.ReadInt32();
            _cycleCounter = reader.ReadUInt32();
        }

        public void SaveState(BinaryWriter writer)
        {
            writer.Write(StateSaveVersion);

            foreach (ScheduledTaskListEntry entry in _scheduledTaskList)
            {
                writer.Write(entry.Id);
                writer.Write(entry.CycleCount);
            }

            writer.Write(_scheduledTaskCount);
            writer.Write(_cycleCounter);
        }

        public void RegisterTask(int id, Task_Delegate task)
        {
            _taskList[id] = task;
        }

        // cycleCount must be greater than 0
        public void ScheduleTask(int id, UInt32 cycleCount)
        {
            cycleCount += _cycleCounter;

            // get the position and reference of the new task by finding the last task whose cycle count
            // is smaller or equal to the new one, the new task is next to it
            // (searching is done backward because the new task is more likely to be inserted towards the end)
            int i = _scheduledTaskCount;
            ref ScheduledTaskListEntry entry = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_scheduledTaskList), i - 1);

            while ((i > 0) && (entry.CycleCount > cycleCount))
            {
                --i;
                entry = ref Unsafe.Subtract(ref entry, 1);
            }

            entry = ref Unsafe.Add(ref entry, 1);

            // insert the new task
            if (i < _scheduledTaskCount)
                Array.Copy(_scheduledTaskList, i, _scheduledTaskList, i + 1, _scheduledTaskCount - i);

            entry.Id = id;
            entry.CycleCount = cycleCount;

            ++_scheduledTaskCount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasTaskReady()
        {
            return (_scheduledTaskCount > 0) && (MemoryMarshal.GetArrayDataReference(_scheduledTaskList).CycleCount <= _cycleCounter);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AdvanceCycleCounter(UInt32 cycleCount)
        {
            _cycleCounter += cycleCount;
        }

        public void ProcessTasks()
        {
            ref Task_Delegate taskListDataRef = ref MemoryMarshal.GetArrayDataReference(_taskList);
            ref ScheduledTaskListEntry scheduledTaskListDataRef = ref MemoryMarshal.GetArrayDataReference(_scheduledTaskList);

            // execute the tasks whose cycle count is lower or equal to the cycle counter of the scheduler
            int i = 0;
            ref ScheduledTaskListEntry entry = ref scheduledTaskListDataRef;

            while ((i < _scheduledTaskCount) && (entry.CycleCount <= _cycleCounter))
            {
                Unsafe.Add(ref taskListDataRef, entry.Id)(_cycleCounter - entry.CycleCount);

                ++i;
                entry = ref Unsafe.Add(ref entry, 1);
            }

            // move the remaining tasks at the begin and update their cycle count
            int remainingScheduledTaskCount = _scheduledTaskCount - i;

            if (remainingScheduledTaskCount > 0)
            {
                Array.Copy(_scheduledTaskList, i, _scheduledTaskList, 0, remainingScheduledTaskCount);

                for (i = 0; i < remainingScheduledTaskCount; ++i)
                    Unsafe.Add(ref scheduledTaskListDataRef, i).CycleCount -= _cycleCounter;
            }

            // update the scheduled task count and reset the cycle counter
            _scheduledTaskCount = remainingScheduledTaskCount;
            _cycleCounter = 0;
        }
    }
}
