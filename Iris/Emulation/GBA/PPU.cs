namespace Iris.Emulation.GBA
{
    internal sealed class PPU
    {
        private const int KB = 1024;

        internal Byte[] PaletteRAM = new Byte[1 * KB];
        internal Byte[] VRAM = new Byte[96 * KB];

        internal UInt16 DISPSTAT = 0;
        internal UInt16 DISPCNT = 0;
        internal UInt16 VCOUNT = 0;

        private const UInt32 ScreenWidth = 240;
        private const UInt32 ScreenHeight = 160;
        private const UInt32 HorizontalLineWidth = 308;
        private const UInt32 HorizontalLineCount = 228;

        private readonly IRenderer _renderer;
        private UInt32 _cycleCounter = 0;

        internal PPU(IRenderer renderer)
        {
            _renderer = renderer;
        }

        internal void Step()
        {
            VCOUNT = (UInt16)(_cycleCounter / HorizontalLineWidth);

            if (_cycleCounter == HorizontalLineWidth * ScreenHeight)
            {
                // start of vertical blank
                UInt16 bgMode = (UInt16)(DISPCNT & 0b111);
                switch (bgMode)
                {
                    case 0b100:
                        {
                            UInt16 bg2 = (UInt16)(DISPCNT >> 10 & 1);
                            if (bg2 == 1)
                            {
                                UInt16 frameBuffer = (UInt16)(DISPCNT >> 4 & 1);
                                UInt32 frameBufferAddress = frameBuffer == 0 ? 0x0_0000u : 0x0_a000u;

                                UInt16[] rendererFrameBuffer = new UInt16[ScreenWidth * ScreenHeight];
                                for (UInt32 i = 0; i < ScreenWidth * ScreenHeight; ++i)
                                {
                                    Byte colorNo = VRAM[frameBufferAddress + i];
                                    UInt16 color = (UInt16)(PaletteRAM[colorNo * 2 + 1] << 8
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
