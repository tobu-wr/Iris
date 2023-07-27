namespace Iris.Common
{
    public delegate void DrawFrame_Delegate(UInt16[] frameBuffer);

    public interface ICore
    {
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

        public void Reset();
        public void LoadROM(string filename);
        public bool IsRunning();
        public void Run();
        public void Pause();
        public void SetKeyStatus(Key key, KeyStatus status);
    }
}
