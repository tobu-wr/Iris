using Iris.Common;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Iris.GBA
{
    internal sealed class Video : IDisposable
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

        private const UInt32 PhysicalScreenWidth = 240;
        private const UInt32 PhysicalScreenHeight = 160;
        private const UInt32 HorizontalLineWidth = 308;
        private const UInt32 HorizontalLineCount = 228;
        private const UInt32 PhysicalScreenSize = PhysicalScreenWidth * PhysicalScreenHeight;

        internal delegate void RequestInterrupt_Delegate();

        // could have used function pointers (delegate*) for performance instead of delegates but it's less flexible (cannot use non-static function for instance)
        internal readonly record struct CallbackInterface
        (
            Common.System.DrawFrame_Delegate DrawFrame,
            RequestInterrupt_Delegate RequestVBlankInterrupt
        );

        private readonly Scheduler _scheduler;
        private readonly CallbackInterface _callbackInterface;

        private Memory _memory;

        private bool _initialized;
        private bool _disposed;

        internal Video(Scheduler scheduler, CallbackInterface callbackInterface)
        {
            _scheduler = scheduler;
            _callbackInterface = callbackInterface;
        }

        ~Video()
        {
            if (_initialized)
            {
                _memory.Unmap(PaletteRAM_StartAddress, PaletteRAM_EndAddress);
                _memory.Unmap(VRAM_StartAddress, VRAM_EndAddress);
                _memory.Unmap(OAM_StartAddress, OAM_EndAddress);
            }

            Marshal.FreeHGlobal(_paletteRAM);
            Marshal.FreeHGlobal(_vram);
            Marshal.FreeHGlobal(_oam);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            if (_initialized)
            {
                _memory.Unmap(PaletteRAM_StartAddress, PaletteRAM_EndAddress);
                _memory.Unmap(VRAM_StartAddress, VRAM_EndAddress);
                _memory.Unmap(OAM_StartAddress, OAM_EndAddress);
            }

            Marshal.FreeHGlobal(_paletteRAM);
            Marshal.FreeHGlobal(_vram);
            Marshal.FreeHGlobal(_oam);

            GC.SuppressFinalize(this);
            _disposed = true;
        }

        internal void Initialize(Memory memory)
        {
            if (_initialized)
                return;

            _memory = memory;

            _memory.Map(_paletteRAM, PaletteRAM_Size / Memory.PageSize, PaletteRAM_StartAddress, PaletteRAM_EndAddress, Memory.Flag.All & ~(Memory.Flag.Read8 | Memory.Flag.Write8));
            _memory.Map(_vram, VRAM_Size / Memory.PageSize, VRAM_StartAddress, VRAM_EndAddress, Memory.Flag.All & ~(Memory.Flag.Read8 | Memory.Flag.Write8));
            _memory.Map(_oam, OAM_Size / Memory.PageSize, OAM_StartAddress, OAM_EndAddress, Memory.Flag.All & ~(Memory.Flag.Read8 | Memory.Flag.Write8));

            _initialized = true;
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

            _scheduler.AddTask(4 * HorizontalLineWidth, StartHorizontalLine);
        }

        private void StartHorizontalLine(UInt32 cycleCountDelay)
        {
            ++_VCOUNT;

            switch ((UInt32)_VCOUNT)
            {
                case PhysicalScreenHeight:
                    StartVBlank();
                    break;

                case HorizontalLineCount:
                    EndVBlank();
                    break;
            }

            _scheduler.AddTask(4 * HorizontalLineWidth - cycleCountDelay, StartHorizontalLine);
        }

        private void StartVBlank()
        {
            UInt16 bgMode = (UInt16)(_DISPCNT & 0b111);

            switch (bgMode)
            {
                case 0b000:
                    {
                        UInt16[] screenFrameBuffer = new UInt16[PhysicalScreenSize];

#if !RELEASE_NOPPU
                        UInt16 bg0 = (UInt16)((_DISPCNT >> 8) & 1);
                        UInt16 bg1 = (UInt16)((_DISPCNT >> 9) & 1);
                        UInt16 bg2 = (UInt16)((_DISPCNT >> 10) & 1);
                        UInt16 bg3 = (UInt16)((_DISPCNT >> 11) & 1);

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
                        UInt16 bg2 = (UInt16)((_DISPCNT >> 10) & 1);

                        if (bg2 == 1)
                        {
                            UInt16[] screenFrameBuffer = new UInt16[PhysicalScreenSize];

#if !RELEASE_NOPPU
                            UInt16 frameBuffer = (UInt16)((_DISPCNT >> 4) & 1);
                            UInt32 frameBufferAddress = (frameBuffer == 0) ? 0x0_0000u : 0x0_a000u;

                            for (UInt32 i = 0; i < PhysicalScreenSize; ++i)
                            {
                                unsafe
                                {
                                    Byte colorNo = Unsafe.Read<Byte>((Byte*)_vram + (frameBufferAddress + i));
                                    UInt16 color = Unsafe.Read<UInt16>((Byte*)_paletteRAM + (colorNo * 2));
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

            if ((_DISPSTAT & 0x0008) != 0)
                _callbackInterface.RequestVBlankInterrupt();

            _DISPSTAT |= 1;
        }

        private void EndVBlank()
        {
            _DISPSTAT &= ~1 & 0xffff;
            _VCOUNT = 0;
        }

        private void RenderBackground(int bg, UInt16[] screenFrameBuffer)
        {
            UInt16 bgcnt = 0;
            UInt16 bgvofs = 0;
            UInt16 bghofs = 0;

            switch (bg)
            {
                case 0:
                    bgcnt = _BG0CNT;
                    bgvofs = _BG0VOFS;
                    bghofs = _BG0HOFS;
                    break;
                case 1:
                    bgcnt = _BG1CNT;
                    bgvofs = _BG1VOFS;
                    bghofs = _BG1HOFS;
                    break;
                case 2:
                    bgcnt = _BG2CNT;
                    bgvofs = _BG2VOFS;
                    bghofs = _BG2HOFS;
                    break;
                case 3:
                    bgcnt = _BG3CNT;
                    bgvofs = _BG3VOFS;
                    bghofs = _BG3HOFS;
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
                    UInt16 screenData = Unsafe.Read<UInt16>((Byte*)_vram + screenDataAddress);

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

                    Byte characterData = Unsafe.Read<Byte>((Byte*)_vram + characterDataAddress);

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

                    UInt16 color = Unsafe.Read<UInt16>((Byte*)_paletteRAM + paletteAddress + (colorNumber * ColorSize));
                    screenFrameBuffer[i] = color;
                }
            }
        }
    }
}
