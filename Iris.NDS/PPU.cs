using Iris.Common;

namespace Iris.NDS
{
    public sealed class PPU
    {
        private const int KB = 1024;

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
