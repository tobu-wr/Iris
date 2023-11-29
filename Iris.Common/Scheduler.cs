using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Iris.Common
{
    public sealed class Scheduler(int taskListSize)
    {
        public delegate void Task_Delegate(UInt32 cycleCountDelay);

        private record struct TaskListEntry(UInt32 CycleCount, Task_Delegate Task);

        private UInt32 _cycleCounter;
        private readonly TaskListEntry[] _taskList = new TaskListEntry[taskListSize]; // sorted by CycleCount from smallest to largest
        private int _taskCount;

        public void Reset()
        {
            _cycleCounter = 0;
            _taskCount = 0;
        }

        // cycleCount must be greater than 0
        public void AddTask(UInt32 cycleCount, Task_Delegate task)
        {
            cycleCount += _cycleCounter;

            // get the position and reference of the new task by finding the last task whose cycle count
            // is smaller or equal to the new one, the new task is next to it
            // (searching is done backward because the new task is more likely to be inserted towards the end)
            int i = _taskCount;
            ref TaskListEntry entry = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_taskList), i - 1);

            while ((i > 0) && (entry.CycleCount > cycleCount))
            {
                --i;
                entry = ref Unsafe.Subtract(ref entry, 1);
            }

            entry = ref Unsafe.Add(ref entry, 1);

            // insert the new task
            if (i < _taskCount)
                Array.Copy(_taskList, i, _taskList, i + 1, _taskCount - i);

            entry.CycleCount = cycleCount;
            entry.Task = task;

            ++_taskCount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasTaskReady()
        {
            return (_taskCount > 0) && (MemoryMarshal.GetArrayDataReference(_taskList).CycleCount <= _cycleCounter);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AdvanceCycleCounter(UInt32 cycleCount)
        {
            _cycleCounter += cycleCount;
        }

        public void ProcessTasks()
        {
            ref TaskListEntry taskListDataRef = ref MemoryMarshal.GetArrayDataReference(_taskList);

            // execute the tasks whose cycle count is lower or equal to the cycle counter of the scheduler
            int i = 0;
            ref TaskListEntry entry = ref taskListDataRef;

            while ((i < _taskCount) && (entry.CycleCount <= _cycleCounter))
            {
                entry.Task(_cycleCounter - entry.CycleCount);

                ++i;
                entry = ref Unsafe.Add(ref entry, 1);
            }

            // move the remaining tasks at the begin and update their cycle count
            int remainingTaskCount = _taskCount - i;

            if (remainingTaskCount > 0)
            {
                Array.Copy(_taskList, i, _taskList, 0, remainingTaskCount);

                for (i = 0; i < remainingTaskCount; ++i)
                    Unsafe.Add(ref taskListDataRef, i).CycleCount -= _cycleCounter;
            }

            // reset the cycle counter and update the task count
            _cycleCounter = 0;
            _taskCount = remainingTaskCount;
        }
    }
}
