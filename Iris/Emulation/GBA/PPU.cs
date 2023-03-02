using System.Drawing;
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
                            // bg0, cbb=0, sbb=30, 4bpp, 512x256px, x=0,y=0
                            UInt32 characterDataBaseAddress = 0 * 16u * KB;
                            UInt32 screenDataBaseAddress = 30 * 2u * KB;

                            UInt32 xOffset = BG0HOFS & 0x1ffu;
                            UInt32 yOffset = BG0VOFS & 0x1ffu;

                            UInt16[] rendererFrameBuffer = new UInt16[ScreenWidth * ScreenHeight];
                            for (UInt32 i = 0; i < ScreenWidth * ScreenHeight; ++i)
                            {
                                UInt32 x = ((i % ScreenWidth) + xOffset) % 256;
                                UInt32 y = ((i / ScreenWidth) + yOffset) % 256;
                                UInt32 c = (((((i % ScreenWidth) + xOffset) % 512) >= 256) ? 0x400u : 0u) + (((y / 8) * 32) + (x / 8));
                                UInt32 screenDataAddress = screenDataBaseAddress + (c * 2);

                                unsafe
                                {
                                    UInt16 screenData = Unsafe.Read<UInt16>((Byte*)VRAM + screenDataAddress);
                                    UInt16 palette = (UInt16)(screenData >> 12);
                                    UInt16 character = (UInt16)(screenData & 0x3ff);
                                    UInt32 characterDataAddress = (UInt32)(characterDataBaseAddress + (character * 32) + ((y % 8) * 4) + ((x % 8) / 2));
                                    Byte characterData = Unsafe.Read<Byte>((Byte*)VRAM + characterDataAddress);

                                    Byte colorNo = characterData;
                                    if (((x % 8) % 2) == 0)
                                        colorNo &= 0xf;
                                    else
                                        colorNo >>= 4;

                                    UInt32 paletteAddress = palette * 16u * 2u;
                                    UInt16 color = Unsafe.Read<UInt16>((Byte*)PaletteRAM + paletteAddress + (colorNo * 2));
                                    rendererFrameBuffer[i] = color;
                                }
                            }

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
