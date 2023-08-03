namespace Iris.Common
{
    public sealed class Scheduler
    {
        public delegate void Event();

        public bool HasEventToProcess()
        {
            // TODO
            return false;
        }

        public void AddCycles(UInt32 cycles)
        {
            // TODO
        }

        public void ProcessEvents()
        {
            // TODO
        }

        public void AddEvent(UInt32 cycles, Event evt)
        {

        }
    }
}
