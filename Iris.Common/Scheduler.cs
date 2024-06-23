using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Iris.Common
{
    public sealed class Scheduler(int taskListSize, int scheduledTaskListSize)
    {
        public delegate void Task_Delegate(UInt64 cycleCountDelay);
        private readonly Task_Delegate[] _taskList = new Task_Delegate[taskListSize];

        private struct ScheduledTaskListEntry
        {
            internal int _id;
            internal UInt64 _cycleCount;
        }

        private readonly ScheduledTaskListEntry[] _scheduledTaskList = new ScheduledTaskListEntry[scheduledTaskListSize]; // sorted by CycleCount from smallest to largest
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
                entry._id = reader.ReadInt32();
                entry._cycleCount = reader.ReadUInt64();
            }

            _scheduledTaskCount = reader.ReadInt32();
            _cycleCounter = reader.ReadUInt64();
        }

        public void SaveState(BinaryWriter writer)
        {
            foreach (ScheduledTaskListEntry entry in _scheduledTaskList)
            {
                writer.Write(entry._id);
                writer.Write(entry._cycleCount);
            }

            writer.Write(_scheduledTaskCount);
            writer.Write(_cycleCounter);
        }

        public void RegisterTask(int id, Task_Delegate task)
        {
            _taskList[id] = task;
        }

        // cycleCount must be greater than 0
        public void ScheduleTask(int id, UInt64 cycleCount)
        {
            cycleCount += _cycleCounter;

            // get the position and reference of the new task
            // (searching is done backward because the new task is more likely to be inserted towards the end)
            int index = _scheduledTaskCount;
            ref ScheduledTaskListEntry entry = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_scheduledTaskList), index - 1);

            while ((index > 0) && (entry._cycleCount > cycleCount))
            {
                --index;
                entry = ref Unsafe.Subtract(ref entry, 1);
            }

            entry = ref Unsafe.Add(ref entry, 1);

            // insert the new task
            if (index < _scheduledTaskCount)
                Array.Copy(_scheduledTaskList, index, _scheduledTaskList, index + 1, _scheduledTaskCount - index);

            entry._id = id;
            entry._cycleCount = cycleCount;

            ++_scheduledTaskCount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasTaskReady()
        {
            return (_scheduledTaskCount > 0) && (MemoryMarshal.GetArrayDataReference(_scheduledTaskList)._cycleCount <= _cycleCounter);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AdvanceCycleCounter(UInt64 cycleCount)
        {
            _cycleCounter += cycleCount;
        }

        public void ProcessTasks()
        {
            ref Task_Delegate taskListDataRef = ref MemoryMarshal.GetArrayDataReference(_taskList);
            ref ScheduledTaskListEntry scheduledTaskListDataRef = ref MemoryMarshal.GetArrayDataReference(_scheduledTaskList);

            // execute the tasks that are ready
            int index = 0;
            ref ScheduledTaskListEntry entry = ref scheduledTaskListDataRef;

            while ((index < _scheduledTaskCount) && (entry._cycleCount <= _cycleCounter))
            {
                Unsafe.Add(ref taskListDataRef, entry._id)(_cycleCounter - entry._cycleCount);

                ++index;
                entry = ref Unsafe.Add(ref entry, 1);
            }

            // move the remaining tasks at the begin
            int remainingScheduledTaskCount = _scheduledTaskCount - index;

            if (remainingScheduledTaskCount > 0)
                Array.Copy(_scheduledTaskList, index, _scheduledTaskList, 0, remainingScheduledTaskCount);

            _scheduledTaskCount = remainingScheduledTaskCount;
        }
    }
}
