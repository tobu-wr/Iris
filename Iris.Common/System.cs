namespace Iris.Common
{
    public abstract class System : IDisposable
    {
        public delegate void PollInput_Delegate();
        public delegate void PresentFrame_Delegate(UInt16[] frameBuffer, long renderingDuration);

        public enum Key
        {
            A,
            B,
            Select,
            Start,
            Right,
            Left,
            Up,
            Down,
            R,
            L,
            X,
            Y,
        }

        public enum KeyStatus
        {
            Input = 0,
            NoInput = 1
        }

        public abstract void Dispose();

        public abstract void ResetState();
        public abstract void LoadState(BinaryReader reader);
        public abstract void SaveState(BinaryWriter writer);

        public abstract void LoadROM(string filename);
        public abstract void SetKeyStatus(Key key, KeyStatus status);

        public abstract bool IsRunning();
        public abstract void Run();
        public abstract void Pause();
    }
}
