namespace Iris.NDS
{
    public sealed class PPU
    {
        private const int KB = 1024;

        private readonly Common.System.PresentFrame_Delegate _presentFrameCallback;

        internal PPU(Common.System.PresentFrame_Delegate presentFrameCallback)
        {
            _presentFrameCallback = presentFrameCallback;
        }

        internal void Step()
        {
            // TODO
        }
    }
}
