namespace Iris.EmulationCore.Common
{
    public delegate void DrawFrame_Delegate(UInt16[] frameBuffer);

    public interface ICore
    {
        public void Reset();
        public void LoadROM(string filename);
        public bool IsRunning();
        public void Run();
        public void Pause();
    }
}
