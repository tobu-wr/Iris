namespace Iris.Common
{
    public sealed class Scheduler
    {
        public delegate void Task_Delegate();

        private record struct TaskListEntry(UInt32 CycleCount, Task_Delegate Task);

        private UInt32 _cycleCount;
        private readonly List<TaskListEntry> _taskList = new();

        public void Reset()
        {
            _cycleCount = 0;
            _taskList.Clear();
        }

        public void AddTask(UInt32 cycleCount, Task_Delegate task)
        {
            int i = 0;

            while ((i < _taskList.Count) && (_taskList[i].CycleCount < cycleCount))
                ++i;

            _taskList.Insert(i, new(cycleCount, task));
        }

        public bool HasTaskReady()
        {
            return (_taskList.Count > 0) && (_taskList.First().CycleCount <= _cycleCount);
        }

        public void IncrementCycleCount(UInt32 cycleCount)
        {
            _cycleCount += cycleCount;
        }

        public void ProcessTasks()
        {
            while (HasTaskReady())
            {
                _taskList.First().Task();
                _taskList.RemoveAt(0);
            }

            for (int i = 0; i < _taskList.Count; ++i)
                _taskList[i] = new(_taskList[i].CycleCount - _cycleCount, _taskList[i].Task);

            _cycleCount = 0;
        }
    }
}
