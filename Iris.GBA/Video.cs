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
            Common.System.DrawFrame_Delegate DrawFrame
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

        private InterruptControl _interruptControl;
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

        internal void Initialize(InterruptControl interruptControl, Memory memory)
        {
            _interruptControl = interruptControl;

            const Memory.Flag flags = Memory.Flag.All & ~Memory.Flag.Write8;
            memory.Map(_paletteRAM, PaletteRAM_Size, PaletteRAM_StartAddress, PaletteRAM_EndAddress, flags);
            memory.Map(_vram, VRAM_Size, VRAM_StartAddress, VRAM_EndAddress, flags);
            memory.Map(_oam, OAM_Size, OAM_StartAddress, OAM_EndAddress, flags);
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
                        _interruptControl.RequestInterrupt(InterruptControl.Interrupt.VBlank);

                    _callbackInterface.DrawFrame(_displayFrameBuffer);
                    break;

                // VBlank end
                case ScanlineCount - 1:
                    _VCOUNT = 0;
                    _DISPSTAT = (UInt16)(_DISPSTAT & ~0x0001);

                    Array.Clear(_displayFrameBuffer);
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
            UInt16 bgMode = (UInt16)(_DISPCNT & 0b111);

            switch (bgMode)
            {
                case 0b000:
                    RenderBackgroundMode0();
                    break;
                case 0b011:
                    RenderBackgroundMode3();
                    break;
                case 0b100:
                    RenderBackgroundMode4();
                    break;
                case 0b101:
                    RenderBackgroundMode5();
                    break;
                default:
                    throw new Exception(string.Format("Iris.GBA.Video: Wrong background mode {0}", bgMode));
            }

            if ((_DISPCNT & 0x1000) == 0x1000)
                RenderObjects();
        }

        private void RenderBackgroundMode0()
        {
            bool isFirst = true;

            if ((_DISPCNT & 0x0800) == 0x0800)
            {
                RenderTextBackground(_BG3CNT, _BG3HOFS, _BG3VOFS, isFirst);
                isFirst = false;
            }

            if ((_DISPCNT & 0x0400) == 0x0400)
            {
                RenderTextBackground(_BG2CNT, _BG2HOFS, _BG2VOFS, isFirst);
                isFirst = false;
            }

            if ((_DISPCNT & 0x0200) == 0x0200)
            {
                RenderTextBackground(_BG1CNT, _BG1HOFS, _BG1VOFS, isFirst);
                isFirst = false;
            }

            if ((_DISPCNT & 0x0100) == 0x0100)
                RenderTextBackground(_BG0CNT, _BG0HOFS, _BG0VOFS, isFirst);
        }

        private void RenderBackgroundMode3()
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

        private void RenderBackgroundMode4()
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

        private void RenderBackgroundMode5()
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

        private void RenderTextBackground(UInt16 cnt, UInt16 hofs, UInt16 vofs, bool isFirst)
        {
            ref UInt16 displayFrameBufferDataRef = ref MemoryMarshal.GetArrayDataReference(_displayFrameBuffer);

            int displayPixelNumberBegin = _VCOUNT * DisplayScreenWidth;

            UInt16 screenSize = (UInt16)((cnt >> 14) & 0b11);
            UInt16 screenBaseBlock = (UInt16)((cnt >> 8) & 0b1_1111);
            UInt16 colorMode = (UInt16)((cnt >> 7) & 1);
            UInt16 characterBaseBlock = (UInt16)((cnt >> 2) & 0b11);

            int virtualScreenWidth = ((screenSize & 0b01) == 0) ? 256 : 512;
            int virtualScreenHeight = ((screenSize & 0b10) == 0) ? 256 : 512;

            UInt32 screenBaseBlockOffset = (UInt32)(screenBaseBlock * 2 * KB);
            UInt32 characterBaseBlockOffset = (UInt32)(characterBaseBlock * 16 * KB);

            int v = (_VCOUNT + vofs) % virtualScreenHeight;

            const int CharacterWidth = 8;
            const int CharacterHeight = 8;

            const int SC_Width = 256;
            const int SC_Height = 256;
            const int SC_Size = (SC_Width / CharacterWidth) * (SC_Height / CharacterHeight);

            int scNumberBegin = (v / SC_Height) * (virtualScreenWidth / SC_Width);
            int characterNumberBegin = ((v % SC_Height) / CharacterHeight) * (SC_Width / CharacterWidth);

            int characterPixelNumberBegin = (v % CharacterHeight) * CharacterWidth;
            int characterPixelNumberBegin_VerticalFlip = (CharacterHeight - 1 - (v % CharacterHeight)) * CharacterWidth;

            for (int hcount = 0; hcount < DisplayScreenWidth; ++hcount)
            {
                int displayPixelNumber = displayPixelNumberBegin + hcount;

                int h = (hcount + hofs) % virtualScreenWidth;

                int scNumber = scNumberBegin + (h / SC_Width);
                int characterNumber = characterNumberBegin + ((h % SC_Width) / CharacterWidth);

                unsafe
                {
                    UInt16 screenData = Unsafe.Read<UInt16>((Byte*)_vram + screenBaseBlockOffset + (scNumber * SC_Size * 2) + (characterNumber * 2));

                    UInt16 colorPalette = (UInt16)((screenData >> 12) & 0b1111);
                    UInt16 verticalFlipFlag = (UInt16)((screenData >> 11) & 1);
                    UInt16 horizontalFlipFlag = (UInt16)((screenData >> 10) & 1);
                    UInt16 characterName = (UInt16)(screenData & 0x3ff);

                    int characterPixelNumber;

                    if (verticalFlipFlag == 0)
                        characterPixelNumber = characterPixelNumberBegin;
                    else
                        characterPixelNumber = characterPixelNumberBegin_VerticalFlip;

                    if (horizontalFlipFlag == 0)
                        characterPixelNumber += h % CharacterWidth;
                    else
                        characterPixelNumber += CharacterWidth - 1 - (h % CharacterWidth);

                    UInt16 color;

                    // 16 colors x 16 palettes
                    if (colorMode == 0)
                    {
                        const int CharacterSize = 32;
                        Byte colorNumber = Unsafe.Read<Byte>((Byte*)_vram + characterBaseBlockOffset + (characterName * CharacterSize) + (characterPixelNumber / 2));

                        if ((characterPixelNumber % 2) == 0)
                            colorNumber &= 0b1111;
                        else
                            colorNumber >>= 4;

                        if (!isFirst && (colorNumber == 0))
                            continue;

                        color = Unsafe.Read<UInt16>((UInt16*)_paletteRAM + (colorPalette * 16) + colorNumber);
                    }

                    // 256 colors x 1 palette
                    else
                    {
                        const int CharacterSize = 64;
                        Byte colorNumber = Unsafe.Read<Byte>((Byte*)_vram + characterBaseBlockOffset + (characterName * CharacterSize) + characterPixelNumber);

                        if (!isFirst && (colorNumber == 0))
                            continue;

                        color = Unsafe.Read<UInt16>((UInt16*)_paletteRAM + colorNumber);
                    }

                    Unsafe.Add(ref displayFrameBufferDataRef, displayPixelNumber) = color;
                }
            }
        }

        private void RenderObjects()
        {
            ref UInt16 displayFrameBufferDataRef = ref MemoryMarshal.GetArrayDataReference(_displayFrameBuffer);

            int displayPixelNumberBegin = _VCOUNT * DisplayScreenWidth;

            UInt16 mappingFormat = (UInt16)((_DISPCNT >> 6) & 1);

            for (int objNumber = 127; objNumber >= 0; --objNumber)
            {
                unsafe
                {
                    UInt16 attribute0 = Unsafe.Read<UInt16>((UInt16*)_oam + (objNumber * 4));
                    UInt16 attribute1 = Unsafe.Read<UInt16>((UInt16*)_oam + (objNumber * 4) + 1);
                    UInt16 attribute2 = Unsafe.Read<UInt16>((UInt16*)_oam + (objNumber * 4) + 2);

                    UInt16 objShape = (UInt16)((attribute0 >> 14) & 0b11);
                    UInt16 colorMode = (UInt16)((attribute0 >> 13) & 1);
                    UInt16 yCoordinate = (UInt16)(attribute0 & 0xff);

                    UInt16 objSize = (UInt16)((attribute1 >> 14) & 0b11);
                    UInt16 verticalFlipFlag = (UInt16)((attribute1 >> 13) & 1);
                    UInt16 horizontalFlipFlag = (UInt16)((attribute1 >> 12) & 1);
                    UInt16 xCoordinate = (UInt16)(attribute1 & 0x1ff);

                    UInt16 colorPalette = (UInt16)((attribute2 >> 12) & 0b1111);
                    UInt16 characterName = (UInt16)(attribute2 & 0x3ff);

                    (int characterWidth, int characterHeight) = (objShape, objSize) switch
                    {
                        // square
                        (0b00, 0b00) => (8, 8),
                        (0b00, 0b01) => (16, 16),
                        (0b00, 0b10) => (32, 32),
                        (0b00, 0b11) => (64, 64),

                        // horizontal rectangle
                        (0b01, 0b00) => (16, 8),
                        (0b01, 0b01) => (32, 8),
                        (0b01, 0b10) => (32, 16),
                        (0b01, 0b11) => (64, 32),

                        // vertical rectangle
                        (0b10, 0b00) => (8, 16),
                        (0b10, 0b01) => (8, 32),
                        (0b10, 0b10) => (16, 32),
                        (0b10, 0b11) => (32, 64),

                        // prohibited
                        _ => throw new Exception(string.Format("Iris.GBA.Video: Prohibited object shape {0}", objShape))
                    };

                    if (xCoordinate >= DisplayScreenWidth)
                        continue;

                    if ((yCoordinate > _VCOUNT) || ((yCoordinate + characterHeight) <= _VCOUNT))
                        continue;

                    const int BasicCharacterWidth = 8;
                    const int BasicCharacterHeight = 8;

                    int v = _VCOUNT - yCoordinate;

                    int basicCharacterNumberBegin;
                    int basicCharacterPixelNumberBegin;

                    if (verticalFlipFlag == 0)
                    {
                        basicCharacterNumberBegin = (v / BasicCharacterHeight) * (characterWidth / BasicCharacterWidth);
                        basicCharacterPixelNumberBegin = (v % BasicCharacterHeight) * BasicCharacterWidth;
                    }
                    else
                    {
                        basicCharacterNumberBegin = ((characterHeight / BasicCharacterHeight) - 1 - (v / BasicCharacterHeight)) * (characterWidth / BasicCharacterWidth);
                        basicCharacterPixelNumberBegin = (BasicCharacterHeight - 1 - (v % BasicCharacterHeight)) * BasicCharacterWidth;
                    }

                    for (int hcount = xCoordinate; (hcount < (xCoordinate + characterWidth)) && (hcount < DisplayScreenWidth); ++hcount)
                    {
                        int displayPixelNumber = displayPixelNumberBegin + hcount;

                        int h = hcount - xCoordinate;

                        int basicCharacterNumber = basicCharacterNumberBegin;
                        int basicCharacterPixelNumber = basicCharacterPixelNumberBegin;

                        if (horizontalFlipFlag == 0)
                        {
                            basicCharacterNumber += h / BasicCharacterWidth;
                            basicCharacterPixelNumber += h % BasicCharacterWidth;
                        }
                        else
                        {
                            basicCharacterNumber += (characterWidth / BasicCharacterWidth) - 1 - (h / BasicCharacterWidth);
                            basicCharacterPixelNumber += BasicCharacterWidth - 1 - (h % BasicCharacterWidth);
                        }

                        const UInt32 CharacterDataOffset = 0x1_0000;
                        const UInt32 PaletteOffset = 0x200;

                        UInt16 color;

                        // 16 colors x 16 palettes
                        if (colorMode == 0)
                        {
                            const int BasicCharacterSize = 32;
                            Byte colorNumber = Unsafe.Read<Byte>((Byte*)_vram + CharacterDataOffset + ((characterName + basicCharacterNumber) * BasicCharacterSize) + (basicCharacterPixelNumber / 2));

                            if ((basicCharacterPixelNumber % 2) == 0)
                                colorNumber &= 0b1111;
                            else
                                colorNumber >>= 4;

                            if (colorNumber == 0)
                                continue;

                            color = Unsafe.Read<UInt16>((Byte*)_paletteRAM + PaletteOffset + (colorPalette * 16 * 2) + (colorNumber * 2));
                        }

                        // 256 colors x 1 palette
                        else
                        {
                            const int BasicCharacterSize = 64;
                            Byte colorNumber = Unsafe.Read<Byte>((Byte*)_vram + CharacterDataOffset + ((characterName + basicCharacterNumber) * BasicCharacterSize) + basicCharacterPixelNumber);

                            if (colorNumber == 0)
                                continue;

                            color = Unsafe.Read<UInt16>((Byte*)_paletteRAM + PaletteOffset + (colorNumber * 2));
                        }

                        Unsafe.Add(ref displayFrameBufferDataRef, displayPixelNumber) = color;
                    }
                }
            }
        }
    }
}
