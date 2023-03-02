using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Iris.Emulation.GBA
{
    internal sealed class PPU
    {
        private const int KB = 1024;

        internal readonly IntPtr PaletteRAM = Marshal.AllocHGlobal(1 * KB);
        internal readonly IntPtr VRAM = Marshal.AllocHGlobal(96 * KB);
        internal readonly IntPtr OAM = Marshal.AllocHGlobal(1 * KB);

        internal UInt16 DISPSTAT;
        internal UInt16 DISPCNT;
        internal UInt16 VCOUNT;

        internal UInt16 BG0CNT;
        internal UInt16 BG1CNT;
        internal UInt16 BG2CNT;
        internal UInt16 BG3CNT;

        internal UInt16 BG0HOFS;
        internal UInt16 BG0VOFS;

        internal UInt16 BG1HOFS;
        internal UInt16 BG1VOFS;

        internal UInt16 BG2HOFS;
        internal UInt16 BG2VOFS;

        internal UInt16 BG3HOFS;
        internal UInt16 BG3VOFS;

        internal UInt16 WIN0H;
        internal UInt16 WIN1H;

        internal UInt16 WIN0V;
        internal UInt16 WIN1V;

        internal UInt16 WININ;
        internal UInt16 WINOUT;

        internal UInt16 MOSAIC;
        internal UInt16 BLDCNT;
        internal UInt16 BLDALPHA;
        internal UInt16 BLDY;

        private const UInt32 ScreenWidth = 240;
        private const UInt32 ScreenHeight = 160;
        private const UInt32 HorizontalLineWidth = 308;
        private const UInt32 HorizontalLineCount = 228;

        internal struct CallbackInterface
        {
            internal delegate void DrawFrame_Delegate(UInt16[] frameBuffer);
            internal delegate void RequestInterrupt_Delegate(Core.Interrupt interrupt);

            internal DrawFrame_Delegate DrawFrame;
            internal RequestInterrupt_Delegate RequestInterrupt;
        }

        private readonly CallbackInterface _callbackInterface;
        private UInt32 _cycleCounter = 0;

        internal PPU(CallbackInterface callbackInterface)
        {
            _callbackInterface = callbackInterface;
        }

        internal void Step()
        {
            VCOUNT = (UInt16)(_cycleCounter / HorizontalLineWidth);

            if (_cycleCounter == (HorizontalLineWidth * ScreenHeight))
            {
                // start of vertical blank
                UInt16 bgMode = (UInt16)(DISPCNT & 0b111);
                switch (bgMode)
                {
                    case 0b000:
                        {
                            UInt16 bg0 = (UInt16)((DISPCNT >> 8) & 1);
                            UInt16 bg1 = (UInt16)((DISPCNT >> 9) & 1);
                            UInt16 bg2 = (UInt16)((DISPCNT >> 10) & 1);
                            UInt16 bg3 = (UInt16)((DISPCNT >> 11) & 1);
                            UInt16 obj = (UInt16)((DISPCNT >> 12) & 1);

                            if (bg0 == 1)
                            {
                                //UInt16 virtualScreenSize = (UInt16)((BG0CNT >> 14) & 0b11);
                                //UInt32 virtualScreenWidth = ((virtualScreenSize & 0b01) == 0) ? 256u : 512u;
                                //UInt32 virtualScreenHeight = ((virtualScreenSize & 0b10) == 0) ? 256u : 512u;

                                //UInt32 hOffset = BG0HOFS & 0x1ffu;
                                //UInt32 vOffset = BG0VOFS & 0x1ffu;

                                //UInt16 charBaseBlock = (UInt16)((BG0CNT >> 2) & 0b11);
                                //UInt16 screenBaseBlock = (UInt16)((BG0CNT >> 8) & 0x1f);

                                //UInt32 screenDataBaseAddress = screenBaseBlock * 2u * KB;

                                //UInt16[] rendererFrameBuffer = new UInt16[ScreenWidth * ScreenHeight];
                                //for (UInt32 i = 0; i < ScreenWidth * ScreenHeight; ++i)
                                //{
                                //    UInt32 hPixel = (i % ScreenWidth) + hOffset;
                                //    UInt32 vPixel = (i / ScreenWidth) + vOffset;
                                //    UInt32 hBlock = hPixel / 8;
                                //    UInt32 vBlock = vPixel / 8;
                                //    UInt32 blockNumber = vBlock * (virtualScreenWidth / 8) + hBlock;
                                //    UInt32 screenDataAddress = screenDataBaseAddress + blockNumber * 2;
                                //    UInt16 screenData = (UInt16)((VRAM[screenDataAddress + 1] << 8) | VRAM[screenDataAddress]);
                                //    UInt16 characterNumber = (UInt16)(screenData & 0x3ff);



                                //    UInt32 hPixelInBlock = hPixel % 8;
                                //    UInt32 vPixelInBlock = vPixel % 8;
                                //}

                                //_callbackInterface.DrawFrame(rendererFrameBuffer);
                            }

                            UInt16[] rendererFrameBuffer = new UInt16[ScreenWidth * ScreenHeight];
                            _callbackInterface.DrawFrame(rendererFrameBuffer);
                            break;
                        }

                    case 0b100:
                        {
                            UInt16 bg2 = (UInt16)((DISPCNT >> 10) & 1);
                            if (bg2 == 1)
                            {
                                UInt16 frameBuffer = (UInt16)((DISPCNT >> 4) & 1);
                                UInt32 frameBufferAddress = (frameBuffer == 0) ? 0x0_0000u : 0x0_a000u;

                                UInt16[] rendererFrameBuffer = new UInt16[ScreenWidth * ScreenHeight];
                                for (UInt32 i = 0; i < ScreenWidth * ScreenHeight; ++i)
                                {
                                    unsafe
                                    {
                                        Byte colorNo = Unsafe.Read<Byte>((Byte*)VRAM + (frameBufferAddress + i));
                                        UInt16 color = Unsafe.Read<UInt16>((Byte*)PaletteRAM + (colorNo * 2));
                                        rendererFrameBuffer[i] = color;
                                    }
                                }

                                _callbackInterface.DrawFrame(rendererFrameBuffer);
                            }
                            break;
                        }

                    default:
                        Console.WriteLine("PPU: BG mode {0} unimplemented", bgMode);
                        break;
                }

                if ((DISPSTAT & 0x0008) != 0)
                    _callbackInterface.RequestInterrupt(Core.Interrupt.VBlank);

                DISPSTAT |= 1;
                ++_cycleCounter;
            }
            else if (_cycleCounter == (HorizontalLineWidth * HorizontalLineCount))
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
