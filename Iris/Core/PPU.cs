namespace Iris.Core
{
    public class PPU
    {
        private const int KB = 1024;

        public byte[] PaletteRAM = new byte[1 * KB];
        public byte[] VRAM = new byte[96 * KB];

        public ushort DISPSTAT = 0;
        public ushort DISPCNT = 0;
        public ushort VCOUNT = 0;

        private const uint ScreenWidth = 240;
        private const uint ScreenHeight = 160;
        private const uint HorizontalLineWidth = 308;
        private const uint HorizontalLineCount = 228;

        private readonly IRenderer _renderer;
        private uint _cycleCounter = 0;

        public PPU(IRenderer renderer)
        {
            _renderer = renderer;
        }

        public void Step()
        {
            VCOUNT = (ushort)(_cycleCounter / HorizontalLineWidth);

            if (_cycleCounter == HorizontalLineWidth * ScreenHeight)
            {
                // start of vertical blank
                ushort bgMode = (ushort)(DISPCNT & 0b111);
                switch (bgMode)
                {
                    case 0b100:
                        {
                            ushort bg2 = (ushort)(DISPCNT >> 10 & 1);
                            if (bg2 == 1)
                            {
                                ushort frameBuffer = (ushort)(DISPCNT >> 4 & 1);
                                uint frameBufferAddress = frameBuffer == 0 ? 0x0_0000u : 0x0_a000u;

                                ushort[] rendererFrameBuffer = new ushort[ScreenWidth * ScreenHeight];
                                for (uint i = 0; i < ScreenWidth * ScreenHeight; ++i)
                                {
                                    byte colorNo = VRAM[frameBufferAddress + i];
                                    ushort color = (ushort)(PaletteRAM[colorNo * 2 + 1] << 8
                                                          | PaletteRAM[colorNo * 2 + 0] << 0);
                                    rendererFrameBuffer[i] = color;
                                }

                                _renderer.DrawFrame(rendererFrameBuffer);
                            }
                            break;
                        }

                    default:
                        Console.WriteLine("PPU: BG mode {0} unimplemented", bgMode);
                        break;
                }

                DISPSTAT |= 1;
                ++_cycleCounter;
            }
            else if (_cycleCounter == HorizontalLineWidth * HorizontalLineCount)
            {
                // end of vertical blank
                DISPSTAT &= ~1 & 0xffff;
                _cycleCounter = 0;
            }
            else
            {
                ++_cycleCounter;
            }
        }
    }
}
