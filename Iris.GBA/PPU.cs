using Iris.Common;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Iris.GBA
{
    internal sealed class PPU
    {
        private const int KB = 1024;

        internal const int PaletteRAMSize = 1 * KB;
        internal const int VRAMSize = 96 * KB;
        internal const int OAMSize = 1 * KB;

        internal readonly IntPtr PaletteRAM = Marshal.AllocHGlobal(PaletteRAMSize);
        internal readonly IntPtr VRAM = Marshal.AllocHGlobal(VRAMSize);
        internal readonly IntPtr OAM = Marshal.AllocHGlobal(OAMSize);

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

        private const UInt32 PhysicalScreenWidth = 240;
        private const UInt32 PhysicalScreenHeight = 160;
        private const UInt32 HorizontalLineWidth = 308;
        private const UInt32 HorizontalLineCount = 228;
        private const UInt32 PhysicalScreenSize = PhysicalScreenWidth * PhysicalScreenHeight;

        internal struct CallbackInterface
        {
            internal delegate void RequestInterrupt_Delegate();

            internal ISystem.DrawFrame_Delegate DrawFrame;
            internal RequestInterrupt_Delegate RequestVBlankInterrupt;
        }

        private readonly CallbackInterface _callbackInterface;
        private UInt32 _cycleCounter;

        //private bool _disposed;

        internal PPU(CallbackInterface callbackInterface)
        {
            _callbackInterface = callbackInterface;
        }

        //~PPU()
        //{
        //    Marshal.FreeHGlobal(PaletteRAM);
        //    Marshal.FreeHGlobal(VRAM);
        //    Marshal.FreeHGlobal(OAM);
        //}

        //internal void Dispose()
        //{
        //    if (_disposed)
        //        return;

        //    Marshal.FreeHGlobal(PaletteRAM);
        //    Marshal.FreeHGlobal(VRAM);
        //    Marshal.FreeHGlobal(OAM);

        //    GC.SuppressFinalize(this);

        //    _disposed = true;
        //}

        internal void Step()
        {
            VCOUNT = (UInt16)(_cycleCounter / HorizontalLineWidth);

            if (_cycleCounter == (HorizontalLineWidth * PhysicalScreenHeight))
            {
                // start of vertical blank
                UInt16 bgMode = (UInt16)(DISPCNT & 0b111);

                switch (bgMode)
                {
                    case 0b000:
                        {
                            UInt16[] screenFrameBuffer = new UInt16[PhysicalScreenSize];

#if !RELEASE_NOPPU
                            UInt16 bg0 = (UInt16)((DISPCNT >> 8) & 1);
                            UInt16 bg1 = (UInt16)((DISPCNT >> 9) & 1);
                            UInt16 bg2 = (UInt16)((DISPCNT >> 10) & 1);
                            UInt16 bg3 = (UInt16)((DISPCNT >> 11) & 1);

                            if (bg3 == 1)
                                RenderBackground(3, screenFrameBuffer);

                            if (bg2 == 1)
                                RenderBackground(2, screenFrameBuffer);

                            if (bg1 == 1)
                                RenderBackground(1, screenFrameBuffer);

                            if (bg0 == 1)
                                RenderBackground(0, screenFrameBuffer);
#endif

                            _callbackInterface.DrawFrame(screenFrameBuffer);
                            break;
                        }
                    case 0b100:
                        {
                            UInt16 bg2 = (UInt16)((DISPCNT >> 10) & 1);

                            if (bg2 == 1)
                            {
                                UInt16[] screenFrameBuffer = new UInt16[PhysicalScreenSize];

#if !RELEASE_NOPPU
                                UInt16 frameBuffer = (UInt16)((DISPCNT >> 4) & 1);
                                UInt32 frameBufferAddress = (frameBuffer == 0) ? 0x0_0000u : 0x0_a000u;

                                for (UInt32 i = 0; i < PhysicalScreenSize; ++i)
                                {
                                    unsafe
                                    {
                                        Byte colorNo = Unsafe.Read<Byte>((Byte*)VRAM + (frameBufferAddress + i));
                                        UInt16 color = Unsafe.Read<UInt16>((Byte*)PaletteRAM + (colorNo * 2));
                                        screenFrameBuffer[i] = color;
                                    }
                                }
#endif

                                _callbackInterface.DrawFrame(screenFrameBuffer);
                            }
                            break;
                        }
                    default:
                        Console.WriteLine("Iris.GBA.PPU: BG mode {0} unimplemented", bgMode);
                        break;
                }

                if ((DISPSTAT & 0x0008) != 0)
                    _callbackInterface.RequestVBlankInterrupt();

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

        private void RenderBackground(int bg, UInt16[] screenFrameBuffer)
        {
            UInt16 bgcnt = 0;
            UInt16 bgvofs = 0;
            UInt16 bghofs = 0;

            switch (bg)
            {
                case 0:
                    bgcnt = BG0CNT;
                    bgvofs = BG0VOFS;
                    bghofs = BG0HOFS;
                    break;
                case 1:
                    bgcnt = BG1CNT;
                    bgvofs = BG1VOFS;
                    bghofs = BG1HOFS;
                    break;
                case 2:
                    bgcnt = BG2CNT;
                    bgvofs = BG2VOFS;
                    bghofs = BG2HOFS;
                    break;
                case 3:
                    bgcnt = BG3CNT;
                    bgvofs = BG3VOFS;
                    bghofs = BG3HOFS;
                    break;
            }

            UInt16 screenSize = (UInt16)((bgcnt >> 14) & 0b11);
            UInt16 screenBaseBlock = (UInt16)((bgcnt >> 8) & 0b1_1111);
            UInt16 colorMode = (UInt16)((bgcnt >> 7) & 1);
            UInt16 characterBaseBlock = (UInt16)((bgcnt >> 2) & 0b11);

            UInt32 screenWidth = ((screenSize & 0b01) == 0) ? 256u : 512u;
            UInt32 screenHeight = ((screenSize & 0b10) == 0) ? 256u : 512u;

            const UInt32 ScreenBaseBlockSize = 2u * KB;
            const UInt32 CharacterBaseBlockSize = 16u * KB;

            UInt32 screenBaseBlockAddress = screenBaseBlock * ScreenBaseBlockSize;
            UInt32 characterBaseBlockAddress = characterBaseBlock * CharacterBaseBlockSize;

            UInt32 vOffset = bgvofs & 0x1ffu;
            UInt32 hOffset = bghofs & 0x1ffu;

            const UInt32 CharacterWidth = 8;
            const UInt32 CharacterHeight = 8;

            const UInt32 ScreenDataSizePerCharacter = 2;

            UInt32 characterDataSize = (colorMode == 0) ? 32u : 64u;

            const UInt32 ColorSize = 2;

            for (UInt32 i = 0; i < PhysicalScreenSize; ++i)
            {
                UInt32 h = ((i % PhysicalScreenWidth) + hOffset) % screenWidth;
                UInt32 v = ((i / PhysicalScreenWidth) + vOffset) % screenHeight;

                UInt32 screenCharacterNumber = (((v % 256) / CharacterHeight) * (256 / CharacterWidth)) + ((h % 256) / CharacterWidth);

                if (h >= 256)
                    screenCharacterNumber += 0x400;

                if (v >= 256)
                    screenCharacterNumber += 0x400 * (screenWidth / 256);

                UInt32 screenDataAddress = screenBaseBlockAddress + (screenCharacterNumber * ScreenDataSizePerCharacter);

                unsafe
                {
                    UInt16 screenData = Unsafe.Read<UInt16>((Byte*)VRAM + screenDataAddress);

                    UInt16 palette = (UInt16)(screenData >> 12);
                    UInt16 verticalFlip = (UInt16)((screenData >> 11) & 1);
                    UInt16 horizontalFlip = (UInt16)((screenData >> 10) & 1);
                    UInt16 characterNumber = (UInt16)(screenData & 0x3ff);

                    UInt32 characterDataAddress = characterBaseBlockAddress + (characterNumber * characterDataSize);

                    if (verticalFlip == 0)
                        characterDataAddress += (v % CharacterHeight) * (characterDataSize / CharacterHeight);
                    else
                        characterDataAddress += (7 - (v % CharacterHeight)) * (characterDataSize / CharacterHeight);

                    if (horizontalFlip == 0)
                        characterDataAddress += (h % CharacterWidth) / ((CharacterHeight * CharacterWidth) / characterDataSize);
                    else
                        characterDataAddress += (7 - (h % CharacterWidth)) / ((CharacterHeight * CharacterWidth) / characterDataSize);

                    Byte characterData = Unsafe.Read<Byte>((Byte*)VRAM + characterDataAddress);

                    Byte colorNumber = characterData;

                    if (colorMode == 0)
                    {
                        if ((h % 2) == 0)
                            colorNumber &= 0xf;
                        else
                            colorNumber >>= 4;
                    }

                    if ((colorNumber == 0) && (bg != 3))
                        continue;

                    UInt32 paletteAddress = (colorMode == 0) ? (palette * 16u * ColorSize) : 0u;

                    UInt16 color = Unsafe.Read<UInt16>((Byte*)PaletteRAM + paletteAddress + (colorNumber * ColorSize));
                    screenFrameBuffer[i] = color;
                }
            }
        }
    }
}
