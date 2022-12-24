using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Iris
{
    public class PPU
    {
        public delegate void DrawFrame();

        public Byte[] paletteRAM = new Byte[1 * 1024]; // 1 KB
        public Byte[] VRAM = new Byte[96 * 1024]; // 96 KB

        public UInt16 dispstat = 0;

        // private const UInt32 SCREEN_WIDTH = 240;
        private const UInt32 SCREEN_HEIGHT = 160;
        private const UInt32 HORIZONTAL_LINE_WIDTH = 308;
        private const UInt32 HORIZONTAL_LINE_COUNT = 228;

        private UInt32 cycleCounter = 0;
        private readonly DrawFrame drawFrame;

        public PPU(DrawFrame drawFrame)
        {
            this.drawFrame = drawFrame;
        }

        public void Step()
        {
            if (cycleCounter == HORIZONTAL_LINE_WIDTH * SCREEN_HEIGHT)
            {
                // start of vertical blank
                drawFrame();
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
