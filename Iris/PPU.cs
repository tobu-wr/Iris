using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Iris
{
    public class PPU
    {
        private const int KB = 1024;

        public Byte[] paletteRAM = new Byte[1 * KB];
        public Byte[] VRAM = new Byte[96 * KB];

        public UInt16 dispstat = 0;
        public UInt16 dispcnt = 0;

        private const UInt32 SCREEN_WIDTH = 240;
        private const UInt32 SCREEN_HEIGHT = 160;
        private const UInt32 HORIZONTAL_LINE_WIDTH = 308;
        private const UInt32 HORIZONTAL_LINE_COUNT = 228;

        private readonly IRenderer renderer;
        private UInt32 cycleCounter = 0;

        public PPU(IRenderer renderer)
        {
            this.renderer = renderer;
        }

        public void Step()
        {
            if (cycleCounter == HORIZONTAL_LINE_WIDTH * SCREEN_HEIGHT)
            {
                // start of vertical blank
                UInt16 bgMode = (UInt16)(dispcnt & 0b111);
                switch (bgMode)
                {
                    case 0b100:
                        {
                            UInt16 bg2 = (UInt16)((dispcnt >> 10) & 1);
                            if (bg2 == 1)
                            {
                                UInt16 frameBuffer = (UInt16)((dispcnt >> 4) & 1);
                                UInt32 frameBufferAddress = (frameBuffer == 0) ? 0x0_0000u : 0x0_a000u;

                                UInt16[] rendererFrameBuffer = new UInt16[SCREEN_WIDTH * SCREEN_HEIGHT];
                                for (UInt32 i = 0; i < SCREEN_WIDTH * SCREEN_HEIGHT; ++i)
                                {
                                    Byte colorNo = VRAM[frameBufferAddress + i];
                                    UInt16 color = paletteRAM[colorNo];
                                    rendererFrameBuffer[i] = color;
                                }

                                renderer.DrawFrame(rendererFrameBuffer);
                            }
                            break;
                        }

                    default:
                        Console.WriteLine("PPU: BG mode {0} unimplemented", bgMode);
                        break;
                }

                dispstat |= 1;
                ++cycleCounter;
            }
            else if (cycleCounter == HORIZONTAL_LINE_WIDTH * HORIZONTAL_LINE_COUNT)
            {
                // end of vertical blank
                dispstat &= (UInt16)(~1 & 0xffff);
                cycleCounter = 0;
            }
            else
            {
                ++cycleCounter;
            }
        }
    }
}
