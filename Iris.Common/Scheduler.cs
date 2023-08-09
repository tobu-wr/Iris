using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Iris.Common
{
    public sealed class Scheduler
    {
        public delegate void Task_Delegate();

        private record struct TaskListEntry(UInt32 CycleCount, Task_Delegate Task);

        private UInt32 _cycleCounter;
        private readonly TaskListEntry[] _taskList; // sorted by CycleCount from smallest to largest
        private int _taskCount;

        public Scheduler(int taskListSize)
        {
            _taskList = new TaskListEntry[taskListSize];
        }

        public void Reset()
        {
            _cycleCounter = 0;
            _taskCount = 0;
        }

        // cycleCount should be greater than 0
        public void AddTask(UInt32 cycleCount, Task_Delegate task)
        {
            // adjust the cycle count of the new task
            cycleCount += _cycleCounter;

            // get the position and reference of the last task
            int i = _taskCount - 1;
            ref TaskListEntry entry = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_taskList), i);

            // find the last task whose the cycle count is smaller or equal to the new one
            // (searching is done backward because the new task is more likely to be added towards the end)
            while ((i >= 0) && (entry.CycleCount > cycleCount))
            {
                --i;
                entry = ref Unsafe.Subtract(ref entry, 1);
            }

            // the position and reference of the new task is the next one
            ++i;
            entry = ref Unsafe.Add(ref entry, 1);

            // move the following tasks to make space for the new one (if it's not added at the end ofc)
            if (i < _taskCount)
                Array.Copy(_taskList, i, _taskList, i + 1, _taskCount - i);

            // add the new task
            entry.CycleCount = cycleCount;
            entry.Task = task;

            // increment the task count
            ++_taskCount;
        }

        public bool HasTaskReady()
        {
            return (_taskCount > 0) && (MemoryMarshal.GetArrayDataReference(_taskList).CycleCount <= _cycleCounter);
        }

        public void AdvanceCycleCounter(UInt32 cycleCount)
        {
            _cycleCounter += cycleCount;
        }

        public void ProcessTasks()
        {
            ref TaskListEntry firstEntryRef = ref MemoryMarshal.GetArrayDataReference(_taskList);

            int i = 0;

            while ((i < _taskCount) && (_taskList[i].CycleCount <= _cycleCounter))
            {
                _taskList[i].CycleCount = 0; // ensure that task won't move in the list if another task get added while executing that one
                _taskList[i].Task();

                ++i;
            }

            if (i < _taskCount)
                Array.Copy(_taskList, i, _taskList, 0, _taskCount - i);

            _taskCount -= i;

            for (i = 0; i < _taskCount; ++i)
                Unsafe.Add(ref firstEntryRef, i).CycleCount -= _cycleCounter;

            _cycleCounter = 0;
        }
    }
}
