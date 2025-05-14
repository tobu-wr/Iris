using Xunit;
using Xunit.Abstractions;

namespace Iris.Common
{
    public sealed class Scheduler(int taskListSize, int scheduledTaskListSize)
    {
        public delegate void Task_Delegate(UInt64 cycleCountDelay);
        private readonly Task_Delegate[] _taskList = new Task_Delegate[taskListSize];

        private record struct ScheduledTaskListEntry(int Id, UInt64 CycleCount);
        private readonly ScheduledTaskListEntry[] _scheduledTaskList = new ScheduledTaskListEntry[scheduledTaskListSize];
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

            while ((_scheduledTaskCount > 0) && (_scheduledTaskList[_scheduledTaskCount - 1].CycleCount <= _cycleCounter))
            {
                // save the entry and remove it from the list beforehand
                // because AdvanceCycleCounter can be called again while executing the task

                ScheduledTaskListEntry entry = _scheduledTaskList[_scheduledTaskCount - 1];

                --_scheduledTaskCount;

                _taskList[entry.Id](_cycleCounter - entry.CycleCount);
            }
        }

        public void RegisterTask(int id, Task_Delegate task)
        {
            _taskList[id] = task;
        }

        public void ScheduleTaskSoon(int id, UInt64 cycleCount)
        {
            cycleCount += _cycleCounter;

            int index = _scheduledTaskCount;

            while ((index > 0) && (_scheduledTaskList[index - 1].CycleCount <= cycleCount))
                --index;

            InsertTask(index, id, cycleCount);
        }

        public void ScheduleTaskLate(int id, UInt64 cycleCount)
        {
            cycleCount += _cycleCounter;

            int index = 0;

            while ((index < _scheduledTaskCount) && (_scheduledTaskList[index].CycleCount > cycleCount))
                ++index;

            InsertTask(index, id, cycleCount);
        }

        public void CancelTask(int id)
        {
            for (int index = 0; index < _scheduledTaskCount; ++index)
            {
                if (_scheduledTaskList[index].Id == id)
                {
                    RemoveTask(index);
                    return;
                }
            }
        }

        private void InsertTask(int index, int id, UInt64 cycleCount)
        {
            if (index < _scheduledTaskCount)
                Array.Copy(_scheduledTaskList, index, _scheduledTaskList, index + 1, _scheduledTaskCount - index);

            ++_scheduledTaskCount;

            _scheduledTaskList[index].Id = id;
            _scheduledTaskList[index].CycleCount = cycleCount;
        }

        private void RemoveTask(int index)
        {
            --_scheduledTaskCount;

            if (index < _scheduledTaskCount)
                Array.Copy(_scheduledTaskList, index + 1, _scheduledTaskList, index, _scheduledTaskCount - index);
        }

        public sealed class UnitTests(ITestOutputHelper output)
        {
            private enum TaskId
            {
                FirstTask,
                SecondTask
            }

            private readonly ITestOutputHelper _output = output;

            private static readonly int s_taskCount = Enum.GetNames<TaskId>().Length;
            private readonly Scheduler _scheduler = new(s_taskCount, s_taskCount);

            private bool _firstTaskExecuted;
            private bool _secondTaskExecuted;

            private void SetupSimpleTasks()
            {
                _scheduler.RegisterTask((int)TaskId.FirstTask, _ =>
                {
                    _output.WriteLine("Step 1");

                    Assert.False(_firstTaskExecuted);
                    Assert.False(_secondTaskExecuted);

                    _firstTaskExecuted = true;
                });

                _scheduler.RegisterTask((int)TaskId.SecondTask, _ =>
                {
                    _output.WriteLine("Step 2");

                    Assert.True(_firstTaskExecuted);
                    Assert.False(_secondTaskExecuted);

                    _secondTaskExecuted = true;
                });
            }

            private void ExecuteSimpleTasks()
            {
                _scheduler.AdvanceCycleCounter(42);

                _output.WriteLine("Step 3");

                Assert.True(_firstTaskExecuted);
                Assert.True(_secondTaskExecuted);
            }

            [Theory]
            [InlineData(0, 1)]
            [InlineData(0, 0)]
            private void ScheduleTaskSoon_IndependantTasks_ExecutedInCorrectOrder(UInt64 firstTaskCycleCount, UInt64 secondTaskCycleCount)
            {
                SetupSimpleTasks();

                _scheduler.ScheduleTaskSoon((int)TaskId.FirstTask, firstTaskCycleCount);
                _scheduler.ScheduleTaskSoon((int)TaskId.SecondTask, secondTaskCycleCount);

                ExecuteSimpleTasks();
            }

            [Theory]
            [InlineData(0, 1)]
            [InlineData(0, 0)]
            private void ScheduleTaskLate_IndependantTasks_ExecutedInCorrectOrder(UInt64 firstTaskCycleCount, UInt64 secondTaskCycleCount)
            {
                SetupSimpleTasks();

                _scheduler.ScheduleTaskLate((int)TaskId.FirstTask, firstTaskCycleCount);
                _scheduler.ScheduleTaskLate((int)TaskId.SecondTask, secondTaskCycleCount);

                ExecuteSimpleTasks();
            }

            [Fact]
            private void AdvanceCycleCounter_TwoTasks_FirstGetsPreemptedBySecond()
            {
                bool firstTaskExecuted = false;
                bool firstTaskOngoing = false;
                bool secondTaskExecuted = false;

                _scheduler.RegisterTask((int)TaskId.FirstTask, _ =>
                {
                    _output.WriteLine("Step 1");

                    Assert.False(firstTaskExecuted);
                    Assert.False(firstTaskOngoing);
                    Assert.False(secondTaskExecuted);

                    firstTaskOngoing = true;

                    _scheduler.AdvanceCycleCounter(42);

                    _output.WriteLine("Step 3");

                    Assert.False(firstTaskExecuted);
                    Assert.True(secondTaskExecuted);

                    firstTaskExecuted = true;
                });

                _scheduler.RegisterTask((int)TaskId.SecondTask, _ =>
                {
                    _output.WriteLine("Step 2");

                    Assert.False(firstTaskExecuted);
                    Assert.True(firstTaskOngoing);
                    Assert.False(secondTaskExecuted);

                    secondTaskExecuted = true;
                });

                _scheduler.ScheduleTaskSoon((int)TaskId.FirstTask, 0);
                _scheduler.ScheduleTaskLate((int)TaskId.SecondTask, 1);

                _scheduler.AdvanceCycleCounter(42);

                _output.WriteLine("Step 4");

                Assert.True(firstTaskExecuted);
                Assert.True(secondTaskExecuted);
            }
        }
    }
}
