namespace Iris.NDS
{
    public sealed class PPU
    {
        private const int KB = 1024;

        private readonly Common.System.DrawFrame_Delegate _drawFrameCallback;

        internal PPU(Common.System.DrawFrame_Delegate drawFrameCallback)
        {
            _drawFrameCallback = drawFrameCallback;
        }

        internal void Step()
        {
            // TODO
        }
    }
}
