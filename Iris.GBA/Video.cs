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

        internal delegate void RequestInterrupt_Delegate();

        // could have used function pointers (delegate*) for performance instead of delegates but it's less flexible (cannot use non-static function for instance)
        internal readonly record struct CallbackInterface
        (
            Common.System.DrawFrame_Delegate DrawFrame,
            RequestInterrupt_Delegate RequestVBlankInterrupt
        //RequestInterrupt_Delegate RequestHBlankInterrupt,
        //RequestInterrupt_Delegate RequestVCountMatchInterrupt
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

        private readonly UInt16[] _displayFrameBuffer = new UInt16[DisplayScreenSize];

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

            const Memory.Flag flags = Memory.Flag.All & ~Memory.Flag.Write8;
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

            Array.Clear(_displayFrameBuffer);

            _scheduler.AddTask(ScanlineCycleCount, StartScanline);
        }

        internal void Write8_PaletteRAM(UInt32 address, Byte value)
        {
            UInt32 offset = (UInt32)((address - PaletteRAM_StartAddress) & ~1) % PaletteRAM_Size;

            unsafe
            {
                Unsafe.Write<Byte>((Byte*)_paletteRAM + offset, value);
                Unsafe.Write<Byte>((Byte*)_paletteRAM + offset + 1, value);
            }
        }

        internal void Write8_VRAM(UInt32 address, Byte value)
        {
            UInt32 offset = (UInt32)((address - VRAM_StartAddress) & ~1) % VRAM_Size;

            unsafe
            {
                Unsafe.Write<Byte>((Byte*)_vram + offset, value);
                Unsafe.Write<Byte>((Byte*)_vram + offset + 1, value);
            }
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
                    _DISPSTAT |= 0x0001;

                    if ((_DISPSTAT & 0x0008) == 0x0008)
                        _callbackInterface.RequestVBlankInterrupt();

                    _callbackInterface.DrawFrame(_displayFrameBuffer);
                    break;

                // VBlank end
                case ScanlineCount - 1:
                    _VCOUNT = 0;
                    _DISPSTAT = (UInt16)(_DISPSTAT & ~0x0001);
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
            UInt16 mode = (UInt16)(_DISPCNT & 0b111);

            switch (mode)
            {
                case 0b000:
                    RenderMode0();
                    break;
                case 0b011:
                    RenderMode3();
                    break;
                case 0b100:
                    RenderMode4();
                    break;
                case 0b101:
                    RenderMode5();
                    break;
                default:
                    throw new Exception(string.Format("Iris.GBA.Video: Wrong mode {0}", mode));
            }
        }

        private void RenderMode0()
        {
            if ((_DISPCNT & 0x0100) == 0x0100)
                RenderBackground(_BG0CNT, _BG0HOFS, _BG0VOFS);

            if ((_DISPCNT & 0x0200) == 0x0200)
                RenderBackground(_BG1CNT, _BG1HOFS, _BG1VOFS);
        }

        private void RenderMode3()
        {
            if ((_DISPCNT & 0x0400) == 0)
                return;

            ref UInt16 displayFrameBufferDataRef = ref MemoryMarshal.GetArrayDataReference(_displayFrameBuffer);

            int pixelNumberBegin = _VCOUNT * DisplayScreenWidth;
            int pixelNumberEnd = pixelNumberBegin + DisplayScreenWidth;

            for (int pixelNumber = pixelNumberBegin; pixelNumber < pixelNumberEnd; ++pixelNumber)
            {
                unsafe
                {
                    UInt16 color = Unsafe.Read<UInt16>((UInt16*)_vram + pixelNumber);
                    Unsafe.Add(ref displayFrameBufferDataRef, pixelNumber) = color;
                }
            }
        }

        private void RenderMode4()
        {
            if ((_DISPCNT & 0x0400) == 0)
                return;

            ref UInt16 displayFrameBufferDataRef = ref MemoryMarshal.GetArrayDataReference(_displayFrameBuffer);

            int pixelNumberBegin = _VCOUNT * DisplayScreenWidth;
            int pixelNumberEnd = pixelNumberBegin + DisplayScreenWidth;

            UInt32 vramFrameBufferOffset = ((_DISPCNT & 0x0010) == 0) ? 0x0_0000u : 0x0_a000u;

            for (int pixelNumber = pixelNumberBegin; pixelNumber < pixelNumberEnd; ++pixelNumber)
            {
                unsafe
                {
                    Byte colorNumber = Unsafe.Read<Byte>((Byte*)_vram + vramFrameBufferOffset + pixelNumber);
                    UInt16 color = Unsafe.Read<UInt16>((UInt16*)_paletteRAM + colorNumber);
                    Unsafe.Add(ref displayFrameBufferDataRef, pixelNumber) = color;
                }
            }
        }

        private void RenderMode5()
        {
            if ((_DISPCNT & 0x0400) == 0)
                return;

            const int VRAM_FrameBufferWidth = 160;
            const int VRAM_FrameBufferHeight = 128;

            if (_VCOUNT >= VRAM_FrameBufferHeight)
                return;

            ref UInt16 displayFrameBufferDataRef = ref MemoryMarshal.GetArrayDataReference(_displayFrameBuffer);

            int vramPixelNumberBegin = _VCOUNT * VRAM_FrameBufferWidth;
            int vramPixelNumberEnd = vramPixelNumberBegin + VRAM_FrameBufferWidth;

            int displayPixelNumberBegin = _VCOUNT * DisplayScreenWidth;

            UInt32 vramFrameBufferOffset = ((_DISPCNT & 0x0010) == 0) ? 0x0_0000u : 0x0_a000u;

            for (int vramPixelNumber = vramPixelNumberBegin, displayPixelNumber = displayPixelNumberBegin; vramPixelNumber < vramPixelNumberEnd; ++vramPixelNumber, ++displayPixelNumber)
            {
                unsafe
                {
                    UInt16 color = Unsafe.Read<UInt16>((Byte*)_vram + vramFrameBufferOffset + (vramPixelNumber * 2));
                    Unsafe.Add(ref displayFrameBufferDataRef, displayPixelNumber) = color;
                }
            }
        }

        private void RenderBackground(UInt16 cnt, UInt16 hofs, UInt16 vofs)
        {
            ref UInt16 displayFrameBufferDataRef = ref MemoryMarshal.GetArrayDataReference(_displayFrameBuffer);

            int displayPixelNumberBegin = _VCOUNT * DisplayScreenWidth;

            UInt16 screenSize = (UInt16)((cnt >> 14) & 0b11);
            UInt16 screenBaseBlock = (UInt16)((cnt >> 8) & 0b1_1111);
            UInt16 colorMode = (UInt16)((cnt >> 7) & 1);
            //UInt16  mosaic = (UInt16)((cnt >> 6) & 1);
            UInt16 characterBaseBlock = (UInt16)((cnt >> 2) & 0b11);
            //UInt16  priority = (UInt16)(cnt & 0b11);

            int virtualScreenWidth = ((screenSize & 0b01) == 0) ? 256 : 512;
            int virtualScreenHeight = ((screenSize & 0b10) == 0) ? 256 : 512;

            UInt32 screenBaseBlockOffset = (UInt32)(screenBaseBlock * 2 * KB);
            UInt32 characterBaseBlockOffset = (UInt32)(characterBaseBlock * 16 * KB);

            int v = (_VCOUNT + vofs) % virtualScreenHeight;

            for (int hcount = 0; hcount < DisplayScreenWidth; ++hcount)
            {
                int displayPixelNumber = displayPixelNumberBegin + hcount;

                int h = (hcount + hofs) % virtualScreenWidth;

                const int CharacterWidth = 8;
                const int CharacterHeight = 8;

                const int SC_Width = 256;
                const int SC_Height = 256;
                const int SC_Size = (SC_Width / CharacterWidth) * (SC_Height / SC_Height) * 2;

                int scNumber = ((v / SC_Height) * (virtualScreenWidth / SC_Width)) + (h / SC_Width);
                int characterNumber = (((v % SC_Height) / CharacterHeight) * (SC_Width / CharacterWidth)) + ((h % SC_Width) / CharacterWidth);

                unsafe
                {
                    UInt16 screenData = Unsafe.Read<UInt16>((Byte*)_vram + screenBaseBlockOffset + (scNumber * SC_Size) + (characterNumber * 2));

                    UInt16 colorPalette = (UInt16)((screenData >> 12) & 0b1111);
                    UInt16 verticalFlipFlag = (UInt16)((screenData >> 11) & 1);
                    //UInt16 horizontalFlipFlag = (UInt16)((screenData >> 10) & 1);
                    UInt16 characterName = (UInt16)(screenData & 0x3ff);

                    int characterPixelNumber;

                    if (verticalFlipFlag == 0)
                        characterPixelNumber = ((v % CharacterHeight) * CharacterWidth) + (h % CharacterWidth);
                    else
                        characterPixelNumber = ((CharacterHeight - 1 - (v % CharacterHeight)) * CharacterWidth) + (h % CharacterWidth);

                    UInt16 color;

                    // 16 colors x 16 palettes
                    if (colorMode == 0)
                    {
                        Byte colorNumber = Unsafe.Read<Byte>((Byte*)_vram + characterBaseBlockOffset + (characterName * 32) + (characterPixelNumber / 2));

                        if ((characterPixelNumber % 2) == 0)
                            colorNumber &= 0b1111;
                        else
                            colorNumber >>= 4;

                        color = Unsafe.Read<UInt16>((UInt16*)_paletteRAM + (colorPalette * 16) + colorNumber);
                    }

                    // 256 colors x 1 palette
                    else
                    {
                        Byte colorNumber = Unsafe.Read<Byte>((Byte*)_vram + characterBaseBlockOffset + (characterName * 64) + characterPixelNumber);
                        color = Unsafe.Read<UInt16>((UInt16*)_paletteRAM + colorNumber);
                    }

                    Unsafe.Add(ref displayFrameBufferDataRef, displayPixelNumber) = color;
                }
            }
        }

        //private void RenderBackground(int bg, UInt16[] screenFrameBuffer)
        //{
        //    UInt32 vOffset = bgvofs & 0x1ffu;
        //    UInt32 hOffset = bghofs & 0x1ffu;

        //    for (UInt32 i = 0; i < DisplayScreenSize; ++i)
        //    {
        //        UInt32 h = ((i % DisplayScreenWidth) + hOffset) % screenWidth;
        //        UInt32 v = ((i / DisplayScreenWidth) + vOffset) % screenHeight;

        //        UInt32 screenCharacterNumber = (((v % 256) / CharacterHeight) * (256 / CharacterWidth)) + ((h % 256) / CharacterWidth);

        //        if (h >= 256)
        //            screenCharacterNumber += 0x400;

        //        if (v >= 256)
        //            screenCharacterNumber += 0x400 * (screenWidth / 256);

        //        UInt32 screenDataAddress = screenBaseBlockAddress + (screenCharacterNumber * 2);

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

        //            UInt16 color = Unsafe.Read<UInt16>((Byte*)_paletteRAM + paletteAddress + (colorNumber * 2));
        //        }
        //    }
        //}
    }
}
