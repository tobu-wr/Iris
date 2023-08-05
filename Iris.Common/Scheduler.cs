namespace Iris.Common
{
    public sealed class Scheduler
    {
        public delegate void Task_Delegate();

        private record struct TaskListEntry(UInt32 CycleCount, Task_Delegate Task);

        private UInt32 _cycleCounter;
        private readonly TaskListEntry[] _taskList;
        private int _taskCount;

        public Scheduler(int taskCount)
        {
            _taskList = new TaskListEntry[taskCount];
        }

        public void Reset()
        {
            _cycleCounter = 0;
            _taskCount = 0;
        }

        public void AddTask(UInt32 cycleCount, Task_Delegate task)
        {
            cycleCount += _cycleCounter;

            int i = _taskCount - 1;

            while ((i >= 0) && (_taskList[i].CycleCount > cycleCount))
                --i;

            if ((i + 1) < _taskCount)
                Array.Copy(_taskList, i + 1, _taskList, i + 2, _taskCount - i - 1);

            _taskList[i + 1] = new(cycleCount, task);
            ++_taskCount;
        }

        public bool HasTaskReady()
        {
            return (_taskCount > 0) && (_taskList[0].CycleCount <= _cycleCounter);
        }

        public void AdvanceCycleCounter(UInt32 cycleCount)
        {
            _cycleCounter += cycleCount;
        }

        public void ProcessTasks()
        {
            while (HasTaskReady())
            {
                Task_Delegate task = _taskList[0].Task;

                --_taskCount;

                if (_taskCount > 0)
                    Array.Copy(_taskList, 1, _taskList, 0, _taskCount);

                task();
            }

            for (int i = 0; i < _taskCount; ++i)
                _taskList[i].CycleCount -= _cycleCounter;

            _cycleCounter = 0;
        }
    }
}
