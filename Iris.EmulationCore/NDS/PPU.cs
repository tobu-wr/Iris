namespace Iris.EmulationCore.NDS
{
    public sealed class PPU
    {
        private const int KB = 1024;

        public delegate void DrawFrame_Delegate(UInt16[] frameBuffer);
        private readonly DrawFrame_Delegate _drawFrameCallback;

        internal PPU(DrawFrame_Delegate drawFrameCallback)
        {
            _drawFrameCallback = drawFrameCallback;
        }

        internal void Step()
        {
            // TODO
        }
    }
}
