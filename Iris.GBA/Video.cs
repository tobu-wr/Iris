using Iris.Common;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Iris.GBA
{
    internal sealed class Video(Scheduler scheduler, Video.CallbackInterface callbackInterface) : IDisposable
    {
        private const int KB = 1024;

        private const int PaletteRAM_Size = 1 * KB;
        private const int VRAM_Size = 96 * KB;
        private const int OAM_Size = 1 * KB;

        private readonly IntPtr _paletteRAM = Marshal.AllocHGlobal(PaletteRAM_Size);
        private readonly IntPtr _vram = Marshal.AllocHGlobal(VRAM_Size);
        private readonly IntPtr _oam = Marshal.AllocHGlobal(OAM_Size);

        private const UInt32 PaletteRAM_StartAddress = 0x0500_0000;
        private const UInt32 PaletteRAM_EndAddress = 0x0600_0000;

        private const UInt32 VRAM_StartAddress = 0x0600_0000;
        private const UInt32 VRAM_EndAddress = 0x0700_0000;

        private const UInt32 OAM_StartAddress = 0x0700_0000;
        private const UInt32 OAM_EndAddress = 0x0800_0000;

        internal UInt16 _DISPSTAT;
        internal UInt16 _DISPCNT;
        internal UInt16 _VCOUNT;

        internal UInt16 _BG0CNT;
        internal UInt16 _BG1CNT;
        internal UInt16 _BG2CNT;
        internal UInt16 _BG3CNT;

        internal UInt16 _BG0HOFS;
        internal UInt16 _BG0VOFS;

        internal UInt16 _BG1HOFS;
        internal UInt16 _BG1VOFS;

        internal UInt16 _BG2HOFS;
        internal UInt16 _BG2VOFS;

        internal UInt16 _BG3HOFS;
        internal UInt16 _BG3VOFS;

        internal UInt16 _BG2PA;
        internal UInt16 _BG2PB;
        internal UInt16 _BG2PC;
        internal UInt16 _BG2PD;
        internal UInt32 _BG2X;
        internal UInt32 _BG2Y;

        internal UInt16 _BG3PA;
        internal UInt16 _BG3PB;
        internal UInt16 _BG3PC;
        internal UInt16 _BG3PD;
        internal UInt32 _BG3X;
        internal UInt32 _BG3Y;

        internal UInt16 _WIN0H;
        internal UInt16 _WIN1H;

        internal UInt16 _WIN0V;
        internal UInt16 _WIN1V;

        internal UInt16 _WININ;
        internal UInt16 _WINOUT;

        internal UInt16 _MOSAIC;

        internal UInt16 _BLDCNT;
        internal UInt16 _BLDALPHA;
        internal UInt16 _BLDY;

        private const UInt16 DISPSTAT_VBlankStatus = 0x0001;
        //private const UInt16 DISPSTAT_HBlankStatus = 0x0002;
        //private const UInt16 DISPSTAT_VCountMatchStatus = 0x0004;
        private const UInt16 DISPSTAT_VBlankIRQEnable = 0x0008;
        //private const UInt16 DISPSTAT_HBlankIRQEnable = 0x0010;
        //private const UInt16 DISPSTAT_VCountMatchIRQEnable = 0x0020;
        //private const UInt16 DISPSTAT_VCountSettingMask = 0xf000;

        private const UInt16 DISPCNT_BGModeMask = 0x0007;
        //private const UInt16 DISPCNT_BGMode0 = 0x0000;
        //private const UInt16 DISPCNT_BGMode1 = 0x0001;
        //private const UInt16 DISPCNT_BGMode2 = 0x0002;
        private const UInt16 DISPCNT_BGMode3 = 0x0003;
        private const UInt16 DISPCNT_BGMode4 = 0x0004;
        private const UInt16 DISPCNT_BGMode5 = 0x0005;
        private const UInt16 DISPCNT_FrameBuffer = 0x0010;
        //private const UInt16 DISPCNT_BG0Enable = 0x0100;
        //private const UInt16 DISPCNT_BG1Enable = 0x0200;
        private const UInt16 DISPCNT_BG2Enable = 0x0400;
        //private const UInt16 DISPCNT_BG3Enable = 0x0800;
        //private const UInt16 DISPCNT_OBJEnable = 0x1000;
        //private const UInt16 DISPCNT_Window0Enable = 0x2000;
        //private const UInt16 DISPCNT_Window1Enable = 0x4000;
        //private const UInt16 DISPCNT_OBJWindowEnable = 0x8000;

        const UInt32 VRAM_FrameBufferOffset0 = 0x0_0000;
        const UInt32 VRAM_FrameBufferOffset1 = 0x0_a000;

        internal delegate void RequestInterrupt_Delegate();

        // could have used function pointers (delegate*) for performance instead of delegates but it's less flexible (cannot use non-static function for instance)
        internal readonly record struct CallbackInterface
        (
            Common.System.DrawFrame_Delegate DrawFrame,
            RequestInterrupt_Delegate RequestVBlankInterrupt
        );

        private const int DisplayScreenWidth = 240;
        private const int DisplayScreenHeight = 160;
        private const int DisplayScreenSize = DisplayScreenWidth * DisplayScreenHeight;

        private const int ScanlineLength = 308;
        private const int ScanlineCount = 228;

        private const UInt32 PixelCycleCount = 4;
        private const UInt32 ScanlineCycleCount = ScanlineLength * PixelCycleCount;

        private readonly Scheduler _scheduler = scheduler;
        private readonly CallbackInterface _callbackInterface = callbackInterface;

        private Memory _memory;
        private bool _disposed;

        private readonly UInt16[] _frameBuffer = new UInt16[DisplayScreenSize];

        ~Video()
        {
            Marshal.FreeHGlobal(_paletteRAM);
            Marshal.FreeHGlobal(_vram);
            Marshal.FreeHGlobal(_oam);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            Marshal.FreeHGlobal(_paletteRAM);
            Marshal.FreeHGlobal(_vram);
            Marshal.FreeHGlobal(_oam);

            GC.SuppressFinalize(this);
            _disposed = true;
        }

        internal void Initialize(Memory memory)
        {
            _memory = memory;

            const Memory.Flag flags = Memory.Flag.All & ~(Memory.Flag.Read8 | Memory.Flag.Write8);
            _memory.Map(_paletteRAM, PaletteRAM_Size, PaletteRAM_StartAddress, PaletteRAM_EndAddress, flags);
            _memory.Map(_vram, VRAM_Size, VRAM_StartAddress, VRAM_EndAddress, flags);
            _memory.Map(_oam, OAM_Size, OAM_StartAddress, OAM_EndAddress, flags);
        }

        internal void Reset()
        {
            _DISPSTAT = 0;
            _DISPCNT = 0;
            _VCOUNT = 0;

            _BG0CNT = 0;
            _BG1CNT = 0;
            _BG2CNT = 0;
            _BG3CNT = 0;

            _BG0HOFS = 0;
            _BG0VOFS = 0;

            _BG1HOFS = 0;
            _BG1VOFS = 0;

            _BG2HOFS = 0;
            _BG2VOFS = 0;

            _BG3HOFS = 0;
            _BG3VOFS = 0;

            _BG2PA = 0;
            _BG2PB = 0;
            _BG2PC = 0;
            _BG2PD = 0;
            _BG2X = 0;
            _BG2Y = 0;

            _BG3PA = 0;
            _BG3PB = 0;
            _BG3PC = 0;
            _BG3PD = 0;
            _BG3X = 0;
            _BG3Y = 0;

            _WIN0H = 0;
            _WIN1H = 0;

            _WIN0V = 0;
            _WIN1V = 0;

            _WININ = 0;
            _WINOUT = 0;

            _MOSAIC = 0;

            _BLDCNT = 0;
            _BLDALPHA = 0;
            _BLDY = 0;

            Array.Clear(_frameBuffer);

            _scheduler.AddTask(ScanlineCycleCount, StartScanline);
        }

        private void StartScanline(UInt32 cycleCountDelay)
        {
            switch (_VCOUNT)
            {
                case < DisplayScreenHeight - 1:
                    ++_VCOUNT;
                    Render();
                    break;

                // VBlank start
                case DisplayScreenHeight - 1:
                    _VCOUNT = DisplayScreenHeight;
                    _DISPSTAT |= DISPSTAT_VBlankStatus;

                    if ((_DISPSTAT & DISPSTAT_VBlankIRQEnable) == DISPSTAT_VBlankIRQEnable)
                        _callbackInterface.RequestVBlankInterrupt();

                    _callbackInterface.DrawFrame(_frameBuffer);
                    break;

                // VBlank end
                case ScanlineCount - 1:
                    _VCOUNT = 0;
                    _DISPSTAT = (UInt16)(_DISPSTAT & ~DISPSTAT_VBlankStatus);
                    Render();
                    break;

                // VBlank
                default:
                    ++_VCOUNT;
                    break;
            }

            _scheduler.AddTask(ScanlineCycleCount - cycleCountDelay, StartScanline);
        }

        private void Render()
        {
            UInt16 mode = (UInt16)(_DISPCNT & DISPCNT_BGModeMask);

            switch (mode)
            {
                case DISPCNT_BGMode3:
                    RenderMode3();
                    break;

                case DISPCNT_BGMode4:
                    RenderMode4();
                    break;

                case DISPCNT_BGMode5:
                    RenderMode5();
                    break;

                default:
                    throw new Exception(string.Format("Iris.GBA.Video: Wrong mode {0}", mode));
            }
        }

        private void RenderMode3()
        {
            if ((_DISPCNT & DISPCNT_BG2Enable) == 0)
                return;

            ref UInt16 frameBufferDataRef = ref MemoryMarshal.GetArrayDataReference(_frameBuffer);

            int pixelNumberBegin = _VCOUNT * DisplayScreenWidth;
            int pixelNumberEnd = pixelNumberBegin + DisplayScreenWidth;

            for (int pixelNumber = pixelNumberBegin; pixelNumber < pixelNumberEnd; ++pixelNumber)
            {
                unsafe
                {
                    UInt16 color = Unsafe.Read<UInt16>((UInt16*)_vram + pixelNumber);
                    Unsafe.Add(ref frameBufferDataRef, pixelNumber) = color;
                }
            }
        }

        private void RenderMode4()
        {
            if ((_DISPCNT & DISPCNT_BG2Enable) == 0)
                return;

            ref UInt16 frameBufferDataRef = ref MemoryMarshal.GetArrayDataReference(_frameBuffer);

            int pixelNumberBegin = _VCOUNT * DisplayScreenWidth;
            int pixelNumberEnd = pixelNumberBegin + DisplayScreenWidth;

            UInt32 vramFrameBufferOffset = ((_DISPCNT & DISPCNT_FrameBuffer) == 0) ? VRAM_FrameBufferOffset0 : VRAM_FrameBufferOffset1;

            for (int pixelNumber = pixelNumberBegin; pixelNumber < pixelNumberEnd; ++pixelNumber)
            {
                unsafe
                {
                    Byte colorNumber = Unsafe.Read<Byte>((Byte*)_vram + (vramFrameBufferOffset + pixelNumber));
                    UInt16 color = Unsafe.Read<UInt16>((UInt16*)_paletteRAM + colorNumber);
                    Unsafe.Add(ref frameBufferDataRef, pixelNumber) = color;
                }
            }
        }

        private void RenderMode5()
        {
            if ((_DISPCNT & DISPCNT_BG2Enable) == 0)
                return;

            const int VRAM_FrameBufferWidth = 160;
            const int VRAM_FrameBufferHeight = 128;

            if (_VCOUNT >= VRAM_FrameBufferHeight)
                return;

            ref UInt16 frameBufferDataRef = ref MemoryMarshal.GetArrayDataReference(_frameBuffer);

            int vramPixelNumberBegin = _VCOUNT * VRAM_FrameBufferWidth;
            int vramPixelNumberEnd = vramPixelNumberBegin + VRAM_FrameBufferWidth;

            int pixelNumberBegin = _VCOUNT * DisplayScreenWidth;

            UInt32 vramFrameBufferOffset = ((_DISPCNT & DISPCNT_FrameBuffer) == 0) ? VRAM_FrameBufferOffset0 : VRAM_FrameBufferOffset1;

            for (int vramPixelNumber = vramPixelNumberBegin, pixelNumber = pixelNumberBegin; vramPixelNumber < vramPixelNumberEnd; ++vramPixelNumber, ++pixelNumber)
            {
                unsafe
                {
                    UInt16 color = Unsafe.Read<UInt16>((Byte*)_vram + (vramFrameBufferOffset + vramPixelNumber * 2));
                    Unsafe.Add(ref frameBufferDataRef, pixelNumber) = color;
                }
            }
        }

        //private void StartVBlank()
        //{
        //    UInt16 bgMode = (UInt16)(_DISPCNT & 0b111);

        //    switch (bgMode)
        //    {
        //        case 0b000:
        //            {
        //                UInt16[] screenFrameBuffer = new UInt16[DisplayScreenSize];

        //                UInt16 bg0 = (UInt16)((_DISPCNT >> 8) & 1);
        //                UInt16 bg1 = (UInt16)((_DISPCNT >> 9) & 1);
        //                UInt16 bg2 = (UInt16)((_DISPCNT >> 10) & 1);
        //                UInt16 bg3 = (UInt16)((_DISPCNT >> 11) & 1);

        //                if (bg3 == 1)
        //                    RenderBackground(3, screenFrameBuffer);

        //                if (bg2 == 1)
        //                    RenderBackground(2, screenFrameBuffer);

        //                if (bg1 == 1)
        //                    RenderBackground(1, screenFrameBuffer);

        //                if (bg0 == 1)
        //                    RenderBackground(0, screenFrameBuffer);

        //                _callbackInterface.DrawFrame(screenFrameBuffer);
        //                break;
        //            }
        //        default:
        //            Console.WriteLine("Iris.GBA.PPU: BG mode {0} unimplemented", bgMode);
        //            break;
        //    }
        //}

        //private void RenderBackground(int bg, UInt16[] screenFrameBuffer)
        //{
        //    UInt16 bgcnt = 0;
        //    UInt16 bgvofs = 0;
        //    UInt16 bghofs = 0;

        //    switch (bg)
        //    {
        //        case 0:
        //            bgcnt = _BG0CNT;
        //            bgvofs = _BG0VOFS;
        //            bghofs = _BG0HOFS;
        //            break;
        //        case 1:
        //            bgcnt = _BG1CNT;
        //            bgvofs = _BG1VOFS;
        //            bghofs = _BG1HOFS;
        //            break;
        //        case 2:
        //            bgcnt = _BG2CNT;
        //            bgvofs = _BG2VOFS;
        //            bghofs = _BG2HOFS;
        //            break;
        //        case 3:
        //            bgcnt = _BG3CNT;
        //            bgvofs = _BG3VOFS;
        //            bghofs = _BG3HOFS;
        //            break;
        //    }

        //    UInt16 screenSize = (UInt16)((bgcnt >> 14) & 0b11);
        //    UInt16 screenBaseBlock = (UInt16)((bgcnt >> 8) & 0b1_1111);
        //    UInt16 colorMode = (UInt16)((bgcnt >> 7) & 1);
        //    UInt16 characterBaseBlock = (UInt16)((bgcnt >> 2) & 0b11);

        //    UInt32 screenWidth = ((screenSize & 0b01) == 0) ? 256u : 512u;
        //    UInt32 screenHeight = ((screenSize & 0b10) == 0) ? 256u : 512u;

        //    const UInt32 ScreenBaseBlockSize = 2u * KB;
        //    const UInt32 CharacterBaseBlockSize = 16u * KB;

        //    UInt32 screenBaseBlockAddress = screenBaseBlock * ScreenBaseBlockSize;
        //    UInt32 characterBaseBlockAddress = characterBaseBlock * CharacterBaseBlockSize;

        //    UInt32 vOffset = bgvofs & 0x1ffu;
        //    UInt32 hOffset = bghofs & 0x1ffu;

        //    const UInt32 CharacterWidth = 8;
        //    const UInt32 CharacterHeight = 8;

        //    const UInt32 ScreenDataSizePerCharacter = 2;

        //    UInt32 characterDataSize = (colorMode == 0) ? 32u : 64u;

        //    const UInt32 ColorSize = 2;

        //    for (UInt32 i = 0; i < DisplayScreenSize; ++i)
        //    {
        //        UInt32 h = ((i % DisplayScreenWidth) + hOffset) % screenWidth;
        //        UInt32 v = ((i / DisplayScreenWidth) + vOffset) % screenHeight;

        //        UInt32 screenCharacterNumber = (((v % 256) / CharacterHeight) * (256 / CharacterWidth)) + ((h % 256) / CharacterWidth);

        //        if (h >= 256)
        //            screenCharacterNumber += 0x400;

        //        if (v >= 256)
        //            screenCharacterNumber += 0x400 * (screenWidth / 256);

        //        UInt32 screenDataAddress = screenBaseBlockAddress + (screenCharacterNumber * ScreenDataSizePerCharacter);

        //        unsafe
        //        {
        //            UInt16 screenData = Unsafe.Read<UInt16>((Byte*)_vram + screenDataAddress);

        //            UInt16 palette = (UInt16)(screenData >> 12);
        //            UInt16 verticalFlip = (UInt16)((screenData >> 11) & 1);
        //            UInt16 horizontalFlip = (UInt16)((screenData >> 10) & 1);
        //            UInt16 characterNumber = (UInt16)(screenData & 0x3ff);

        //            UInt32 characterDataAddress = characterBaseBlockAddress + (characterNumber * characterDataSize);

        //            if (verticalFlip == 0)
        //                characterDataAddress += (v % CharacterHeight) * (characterDataSize / CharacterHeight);
        //            else
        //                characterDataAddress += (CharacterHeight - 1 - (v % CharacterHeight)) * (characterDataSize / CharacterHeight);

        //            if (horizontalFlip == 0)
        //                characterDataAddress += (h % CharacterWidth) / ((CharacterHeight * CharacterWidth) / characterDataSize);
        //            else
        //                characterDataAddress += (CharacterWidth - 1 - (h % CharacterWidth)) / ((CharacterHeight * CharacterWidth) / characterDataSize);

        //            Byte characterData = Unsafe.Read<Byte>((Byte*)_vram + characterDataAddress);

        //            Byte colorNumber = characterData;

        //            if (colorMode == 0)
        //            {
        //                if (horizontalFlip == 0)
        //                {
        //                    if ((h % 2) == 0)
        //                        colorNumber &= 0xf;
        //                    else
        //                        colorNumber >>= 4;
        //                }
        //                else
        //                {
        //                    if ((h % 2) == 0)
        //                        colorNumber >>= 4;
        //                    else
        //                        colorNumber &= 0xf;
        //                }

        //            }

        //            if ((colorNumber == 0) && (bg != 3))
        //                continue;

        //            UInt32 paletteAddress = (colorMode == 0) ? (palette * 16u * ColorSize) : 0u;

        //            UInt16 color = Unsafe.Read<UInt16>((Byte*)_paletteRAM + paletteAddress + (colorNumber * ColorSize));
        //            screenFrameBuffer[i] = color;
        //        }
        //    }
        //}
    }
}
