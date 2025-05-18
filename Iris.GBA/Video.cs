﻿using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Iris.GBA
{
    internal sealed class Video : IDisposable
    {
        internal enum Register
        {
            DISPCNT,
            DISPSTAT,
            VCOUNT,

            BG0CNT,
            BG1CNT,
            BG2CNT,
            BG3CNT,

            BG0HOFS,
            BG0VOFS,

            BG1HOFS,
            BG1VOFS,

            BG2HOFS,
            BG2VOFS,

            BG3HOFS,
            BG3VOFS,

            BG2PA,
            BG2PB,
            BG2PC,
            BG2PD,
            BG2X_L,
            BG2X_H,
            BG2Y_L,
            BG2Y_H,

            BG3PA,
            BG3PB,
            BG3PC,
            BG3PD,
            BG3X_L,
            BG3X_H,
            BG3Y_L,
            BG3Y_H,

            WIN0H,
            WIN1H,

            WIN0V,
            WIN1V,

            WININ,
            WINOUT,

            MOSAIC,

            BLDCNT,
            BLDALPHA,
            BLDY
        }

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

        private UInt16 _DISPCNT;
        private UInt16 _DISPSTAT;
        private UInt16 _VCOUNT;

        private UInt16 _BG0CNT;
        private UInt16 _BG1CNT;
        private UInt16 _BG2CNT;
        private UInt16 _BG3CNT;

        private UInt16 _BG0HOFS;
        private UInt16 _BG0VOFS;

        private UInt16 _BG1HOFS;
        private UInt16 _BG1VOFS;

        private UInt16 _BG2HOFS;
        private UInt16 _BG2VOFS;

        private UInt16 _BG3HOFS;
        private UInt16 _BG3VOFS;

        private UInt16 _BG2PA;
        private UInt16 _BG2PB;
        private UInt16 _BG2PC;
        private UInt16 _BG2PD;
        private UInt16 _BG2X_L;
        private UInt16 _BG2X_H;
        private UInt16 _BG2Y_L;
        private UInt16 _BG2Y_H;

        private UInt16 _BG3PA;
        private UInt16 _BG3PB;
        private UInt16 _BG3PC;
        private UInt16 _BG3PD;
        private UInt16 _BG3X_L;
        private UInt16 _BG3X_H;
        private UInt16 _BG3Y_L;
        private UInt16 _BG3Y_H;

        private UInt16 _WIN0H;
        private UInt16 _WIN1H;

        private UInt16 _WIN0V;
        private UInt16 _WIN1V;

        private UInt16 _WININ;
        private UInt16 _WINOUT;

        private UInt16 _MOSAIC;

        private UInt16 _BLDCNT;
        private UInt16 _BLDALPHA;
        private UInt16 _BLDY;

        private const int DisplayScreenWidth = 240;
        private const int DisplayScreenHeight = 160;
        private const int DisplayScreenSize = DisplayScreenWidth * DisplayScreenHeight;

        private const int CharacterWidth = 8;
        private const int CharacterHeight = 8;

        private const UInt64 HDrawCycleCount = 1006;
        private const UInt64 HBlankCycleCount = 226;

        private readonly Common.Scheduler _scheduler;
        private readonly Common.System.PresentFrame_Delegate _presentFrameCallback;

        private DMA _dma;
        private InterruptControl _interruptControl;

        private bool _disposed;

        private readonly UInt16[] _displayFrameBuffer = new UInt16[DisplayScreenSize];

        private Int32 _currentBG2X;
        private Int32 _currentBG2Y;

        private Int32 _currentBG3X;
        private Int32 _currentBG3Y;

        internal Video(Common.Scheduler scheduler, Common.System.PresentFrame_Delegate presentFrameCallback)
        {
            _scheduler = scheduler;
            _presentFrameCallback = presentFrameCallback;

            _scheduler.RegisterTask((int)GBA_System.TaskId.StartHBlank, StartHBlank);
            _scheduler.RegisterTask((int)GBA_System.TaskId.StartScanline, StartScanline);
        }

        ~Video()
        {
            Dispose();
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

        internal void Initialize(DMA dma, InterruptControl interruptControl, Memory memory)
        {
            _dma = dma;
            _interruptControl = interruptControl;

            const Memory.Flag flags = Memory.Flag.All & ~Memory.Flag.Write8;
            memory.Map(_paletteRAM, PaletteRAM_Size, PaletteRAM_StartAddress, PaletteRAM_EndAddress, flags);
            memory.Map(_vram, VRAM_Size, VRAM_StartAddress, VRAM_EndAddress, flags);
            memory.Map(_oam, OAM_Size, OAM_StartAddress, OAM_EndAddress, flags);
        }

        internal void ResetState()
        {
            unsafe
            {
                NativeMemory.Clear(_paletteRAM.ToPointer(), PaletteRAM_Size);
                NativeMemory.Clear(_vram.ToPointer(), VRAM_Size);
                NativeMemory.Clear(_oam.ToPointer(), OAM_Size);
            }

            _DISPCNT = 0;
            _DISPSTAT = 0;
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
            _BG2X_L = 0;
            _BG2X_H = 0;
            _BG2Y_L = 0;
            _BG2Y_H = 0;

            _BG3PA = 0;
            _BG3PB = 0;
            _BG3PC = 0;
            _BG3PD = 0;
            _BG3X_L = 0;
            _BG3X_H = 0;
            _BG3Y_L = 0;
            _BG3Y_H = 0;

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

            _scheduler.ScheduleTaskLate((int)GBA_System.TaskId.StartHBlank, HDrawCycleCount);

            Array.Clear(_displayFrameBuffer);

            _currentBG2X = 0;
            _currentBG2Y = 0;

            _currentBG3X = 0;
            _currentBG3Y = 0;
        }

        internal void LoadState(BinaryReader reader)
        {
            byte[] paletteRamData = reader.ReadBytes(PaletteRAM_Size);
            byte[] vramData = reader.ReadBytes(VRAM_Size);
            byte[] oamData = reader.ReadBytes(OAM_Size);

            Marshal.Copy(paletteRamData, 0, _paletteRAM, PaletteRAM_Size);
            Marshal.Copy(vramData, 0, _vram, VRAM_Size);
            Marshal.Copy(oamData, 0, _oam, OAM_Size);

            _DISPCNT = reader.ReadUInt16();
            _DISPSTAT = reader.ReadUInt16();
            _VCOUNT = reader.ReadUInt16();

            _BG0CNT = reader.ReadUInt16();
            _BG1CNT = reader.ReadUInt16();
            _BG2CNT = reader.ReadUInt16();
            _BG3CNT = reader.ReadUInt16();

            _BG0HOFS = reader.ReadUInt16();
            _BG0VOFS = reader.ReadUInt16();

            _BG1HOFS = reader.ReadUInt16();
            _BG1VOFS = reader.ReadUInt16();

            _BG2HOFS = reader.ReadUInt16();
            _BG2VOFS = reader.ReadUInt16();

            _BG3HOFS = reader.ReadUInt16();
            _BG3VOFS = reader.ReadUInt16();

            _BG2PA = reader.ReadUInt16();
            _BG2PB = reader.ReadUInt16();
            _BG2PC = reader.ReadUInt16();
            _BG2PD = reader.ReadUInt16();
            _BG2X_L = reader.ReadUInt16();
            _BG2X_H = reader.ReadUInt16();
            _BG2Y_L = reader.ReadUInt16();
            _BG2Y_H = reader.ReadUInt16();

            _BG3PA = reader.ReadUInt16();
            _BG3PB = reader.ReadUInt16();
            _BG3PC = reader.ReadUInt16();
            _BG3PD = reader.ReadUInt16();
            _BG3X_L = reader.ReadUInt16();
            _BG3X_H = reader.ReadUInt16();
            _BG3Y_L = reader.ReadUInt16();
            _BG3Y_H = reader.ReadUInt16();

            _WIN0H = reader.ReadUInt16();
            _WIN1H = reader.ReadUInt16();

            _WIN0V = reader.ReadUInt16();
            _WIN1V = reader.ReadUInt16();

            _WININ = reader.ReadUInt16();
            _WINOUT = reader.ReadUInt16();

            _MOSAIC = reader.ReadUInt16();

            _BLDCNT = reader.ReadUInt16();
            _BLDALPHA = reader.ReadUInt16();
            _BLDY = reader.ReadUInt16();

            foreach (ref UInt16 color in _displayFrameBuffer.AsSpan())
                color = reader.ReadUInt16();

            _currentBG2X = reader.ReadInt32();
            _currentBG2Y = reader.ReadInt32();

            _currentBG3X = reader.ReadInt32();
            _currentBG3Y = reader.ReadInt32();
        }

        internal void SaveState(BinaryWriter writer)
        {
            byte[] paletteRamData = new byte[PaletteRAM_Size];
            byte[] vramData = new byte[VRAM_Size];
            byte[] oamData = new byte[OAM_Size];

            Marshal.Copy(_paletteRAM, paletteRamData, 0, PaletteRAM_Size);
            Marshal.Copy(_vram, vramData, 0, VRAM_Size);
            Marshal.Copy(_oam, oamData, 0, OAM_Size);

            writer.Write(paletteRamData);
            writer.Write(vramData);
            writer.Write(oamData);

            writer.Write(_DISPCNT);
            writer.Write(_DISPSTAT);
            writer.Write(_VCOUNT);

            writer.Write(_BG0CNT);
            writer.Write(_BG1CNT);
            writer.Write(_BG2CNT);
            writer.Write(_BG3CNT);

            writer.Write(_BG0HOFS);
            writer.Write(_BG0VOFS);

            writer.Write(_BG1HOFS);
            writer.Write(_BG1VOFS);

            writer.Write(_BG2HOFS);
            writer.Write(_BG2VOFS);

            writer.Write(_BG3HOFS);
            writer.Write(_BG3VOFS);

            writer.Write(_BG2PA);
            writer.Write(_BG2PB);
            writer.Write(_BG2PC);
            writer.Write(_BG2PD);
            writer.Write(_BG2X_L);
            writer.Write(_BG2X_H);
            writer.Write(_BG2Y_L);
            writer.Write(_BG2Y_H);

            writer.Write(_BG3PA);
            writer.Write(_BG3PB);
            writer.Write(_BG3PC);
            writer.Write(_BG3PD);
            writer.Write(_BG3X_L);
            writer.Write(_BG3X_H);
            writer.Write(_BG3Y_L);
            writer.Write(_BG3Y_H);

            writer.Write(_WIN0H);
            writer.Write(_WIN1H);

            writer.Write(_WIN0V);
            writer.Write(_WIN1V);

            writer.Write(_WININ);
            writer.Write(_WINOUT);

            writer.Write(_MOSAIC);

            writer.Write(_BLDCNT);
            writer.Write(_BLDALPHA);
            writer.Write(_BLDY);

            foreach (UInt16 color in _displayFrameBuffer)
                writer.Write(color);

            writer.Write(_currentBG2X);
            writer.Write(_currentBG2Y);

            writer.Write(_currentBG3X);
            writer.Write(_currentBG3Y);
        }

        internal UInt16 ReadRegister(Register register)
        {
            return register switch
            {
                Register.DISPCNT => _DISPCNT,
                Register.DISPSTAT => _DISPSTAT,
                Register.VCOUNT => _VCOUNT,

                Register.BG0CNT => _BG0CNT,
                Register.BG1CNT => _BG1CNT,
                Register.BG2CNT => _BG2CNT,
                Register.BG3CNT => _BG3CNT,

                Register.WININ => _WININ,
                Register.WINOUT => _WINOUT,

                Register.BLDCNT => _BLDCNT,
                Register.BLDALPHA => _BLDALPHA,

                // should never happen
                _ => throw new Exception("Iris.GBA.Video: Register read error"),
            };
        }

        internal void WriteRegister(Register register, UInt16 value, Memory.RegisterWriteMode mode)
        {
            switch (register)
            {
                case Register.DISPCNT:
                    Memory.WriteRegisterHelper(ref _DISPCNT, value, mode);
                    break;
                case Register.DISPSTAT:
                    Memory.WriteRegisterHelper(ref _DISPSTAT, value, mode);
                    break;

                case Register.BG0CNT:
                    Memory.WriteRegisterHelper(ref _BG0CNT, value, mode);
                    break;
                case Register.BG1CNT:
                    Memory.WriteRegisterHelper(ref _BG1CNT, value, mode);
                    break;
                case Register.BG2CNT:
                    Memory.WriteRegisterHelper(ref _BG2CNT, value, mode);
                    break;
                case Register.BG3CNT:
                    Memory.WriteRegisterHelper(ref _BG3CNT, value, mode);
                    break;

                case Register.BG0HOFS:
                    Memory.WriteRegisterHelper(ref _BG0HOFS, value, mode);
                    break;
                case Register.BG0VOFS:
                    Memory.WriteRegisterHelper(ref _BG0VOFS, value, mode);
                    break;

                case Register.BG1HOFS:
                    Memory.WriteRegisterHelper(ref _BG1HOFS, value, mode);
                    break;
                case Register.BG1VOFS:
                    Memory.WriteRegisterHelper(ref _BG1VOFS, value, mode);
                    break;

                case Register.BG2HOFS:
                    Memory.WriteRegisterHelper(ref _BG2HOFS, value, mode);
                    break;
                case Register.BG2VOFS:
                    Memory.WriteRegisterHelper(ref _BG2VOFS, value, mode);
                    break;

                case Register.BG3HOFS:
                    Memory.WriteRegisterHelper(ref _BG3HOFS, value, mode);
                    break;
                case Register.BG3VOFS:
                    Memory.WriteRegisterHelper(ref _BG3VOFS, value, mode);
                    break;

                case Register.BG2PA:
                    Memory.WriteRegisterHelper(ref _BG2PA, value, mode);
                    break;
                case Register.BG2PB:
                    Memory.WriteRegisterHelper(ref _BG2PB, value, mode);
                    break;
                case Register.BG2PC:
                    Memory.WriteRegisterHelper(ref _BG2PC, value, mode);
                    break;
                case Register.BG2PD:
                    Memory.WriteRegisterHelper(ref _BG2PD, value, mode);
                    break;
                case Register.BG2X_L:
                    Memory.WriteRegisterHelper(ref _BG2X_L, value, mode);
                    _currentBG2X = ((_BG2X_H << 20) | (_BG2X_L << 4)) >> 4;
                    break;
                case Register.BG2X_H:
                    Memory.WriteRegisterHelper(ref _BG2X_H, value, mode);
                    _currentBG2X = ((_BG2X_H << 20) | (_BG2X_L << 4)) >> 4;
                    break;
                case Register.BG2Y_L:
                    Memory.WriteRegisterHelper(ref _BG2Y_L, value, mode);
                    _currentBG2Y = ((_BG2Y_H << 20) | (_BG2Y_L << 4)) >> 4;
                    break;
                case Register.BG2Y_H:
                    Memory.WriteRegisterHelper(ref _BG2Y_H, value, mode);
                    _currentBG2Y = ((_BG2Y_H << 20) | (_BG2Y_L << 4)) >> 4;
                    break;

                case Register.BG3PA:
                    Memory.WriteRegisterHelper(ref _BG3PA, value, mode);
                    break;
                case Register.BG3PB:
                    Memory.WriteRegisterHelper(ref _BG3PB, value, mode);
                    break;
                case Register.BG3PC:
                    Memory.WriteRegisterHelper(ref _BG3PC, value, mode);
                    break;
                case Register.BG3PD:
                    Memory.WriteRegisterHelper(ref _BG3PD, value, mode);
                    break;
                case Register.BG3X_L:
                    Memory.WriteRegisterHelper(ref _BG3X_L, value, mode);
                    _currentBG3X = ((_BG3X_H << 20) | (_BG3X_L << 4)) >> 4;
                    break;
                case Register.BG3X_H:
                    Memory.WriteRegisterHelper(ref _BG3X_H, value, mode);
                    _currentBG3X = ((_BG3X_H << 20) | (_BG3X_L << 4)) >> 4;
                    break;
                case Register.BG3Y_L:
                    Memory.WriteRegisterHelper(ref _BG3Y_L, value, mode);
                    _currentBG3Y = ((_BG3Y_H << 20) | (_BG3Y_L << 4)) >> 4;
                    break;
                case Register.BG3Y_H:
                    Memory.WriteRegisterHelper(ref _BG3Y_H, value, mode);
                    _currentBG3Y = ((_BG3Y_H << 20) | (_BG3Y_L << 4)) >> 4;
                    break;

                case Register.WIN0H:
                    Memory.WriteRegisterHelper(ref _WIN0H, value, mode);
                    break;
                case Register.WIN1H:
                    Memory.WriteRegisterHelper(ref _WIN1H, value, mode);
                    break;

                case Register.WIN0V:
                    Memory.WriteRegisterHelper(ref _WIN0V, value, mode);
                    break;
                case Register.WIN1V:
                    Memory.WriteRegisterHelper(ref _WIN1V, value, mode);
                    break;

                case Register.WININ:
                    Memory.WriteRegisterHelper(ref _WININ, value, mode);
                    break;
                case Register.WINOUT:
                    Memory.WriteRegisterHelper(ref _WINOUT, value, mode);
                    break;

                case Register.MOSAIC:
                    Memory.WriteRegisterHelper(ref _MOSAIC, value, mode);
                    break;

                case Register.BLDCNT:
                    Memory.WriteRegisterHelper(ref _BLDCNT, value, mode);
                    break;
                case Register.BLDALPHA:
                    Memory.WriteRegisterHelper(ref _BLDALPHA, value, mode);
                    break;
                case Register.BLDY:
                    Memory.WriteRegisterHelper(ref _BLDY, value, mode);
                    break;

                // should never happen
                default:
                    throw new Exception("Iris.GBA.Video: Register write error");
            }
        }

        internal UInt16[] GetFrameBuffer()
        {
            return _displayFrameBuffer;
        }

        internal void Write8_PaletteRAM(UInt32 address, Byte value)
        {
            UInt32 offset = (UInt32)((address - PaletteRAM_StartAddress) & ~1) % PaletteRAM_Size;

            unsafe
            {
                Unsafe.Write((Byte*)_paletteRAM + offset, value);
                Unsafe.Write((Byte*)_paletteRAM + offset + 1, value);
            }
        }

        internal void Write8_VRAM(UInt32 address, Byte value)
        {
            UInt32 offset = (UInt32)((address - VRAM_StartAddress) & ~1) % VRAM_Size;

            unsafe
            {
                Unsafe.Write((Byte*)_vram + offset, value);
                Unsafe.Write((Byte*)_vram + offset + 1, value);
            }
        }

        private void StartHBlank(UInt64 cycleCountDelay)
        {
            _DISPSTAT |= 0x0002; // set HBlank status

            if ((_DISPSTAT & 0x0010) == 0x0010)
                _interruptControl.RequestInterrupt(InterruptControl.Interrupt.HBlank);

            if (_VCOUNT < DisplayScreenHeight)
                _scheduler.ScheduleTaskSoon((int)GBA_System.TaskId.PerformHBlankTransfers, 0);

            _scheduler.ScheduleTaskLate((int)GBA_System.TaskId.StartScanline, HBlankCycleCount - cycleCountDelay);
        }

        private void StartScanline(UInt64 cycleCountDelay)
        {
            _DISPSTAT = (UInt16)(_DISPSTAT & ~0x0002); // clear HBlank status

            switch (_VCOUNT)
            {
                // rendering
                case 0:
                    _VCOUNT = 1;

                    Render();
                    break;

                // rendering
                // video transfer DMA
                case > 0 and < 159:
                    ++_VCOUNT;

                    _scheduler.ScheduleTaskSoon((int)GBA_System.TaskId.PerformVideoTransfer, 0);

                    Render();
                    break;

                // end rendering
                // start vblank
                // video transfer DMA
                case 159:
                    _VCOUNT = 160;

                    _DISPSTAT |= 0x0001; // set VBlank status

                    if ((_DISPSTAT & 0x0008) == 0x0008)
                        _interruptControl.RequestInterrupt(InterruptControl.Interrupt.VBlank);

                    _scheduler.ScheduleTaskSoon((int)GBA_System.TaskId.PerformVBlankTransfers, 0);
                    _scheduler.ScheduleTaskSoon((int)GBA_System.TaskId.PerformVideoTransfer, 0);

                    _presentFrameCallback();
                    Array.Clear(_displayFrameBuffer);

                    _currentBG2X = ((_BG2X_H << 20) | (_BG2X_L << 4)) >> 4;
                    _currentBG2Y = ((_BG2Y_H << 20) | (_BG2Y_L << 4)) >> 4;

                    _currentBG3X = ((_BG3X_H << 20) | (_BG3X_L << 4)) >> 4;
                    _currentBG3Y = ((_BG3Y_H << 20) | (_BG3Y_L << 4)) >> 4;
                    break;

                // vblank
                // end video transfer DMA
                case 160:
                    _VCOUNT = 161;

                    _scheduler.ScheduleTaskSoon((int)GBA_System.TaskId.PerformVideoTransferEnd, 0);
                    break;

                // vblank
                case > 160 and < 226:
                    ++_VCOUNT;
                    break;

                // end vblank
                case 226:
                    _VCOUNT = 227;

                    _DISPSTAT = (UInt16)(_DISPSTAT & ~0x0001); // clear VBlank status
                    break;

                // start rendering
                case 227:
                    _VCOUNT = 0;

                    Render();
                    break;
            }

            if (_VCOUNT == ((_DISPSTAT >> 8) & 0xff))
            {
                _DISPSTAT |= 0x0004; // set VCountMatch status

                if ((_DISPSTAT & 0x0020) == 0x0020)
                    _interruptControl.RequestInterrupt(InterruptControl.Interrupt.VCountMatch);
            }
            else
            {
                _DISPSTAT = (UInt16)(_DISPSTAT & ~0x0004); // clear VCountMatch status
            }

            _scheduler.ScheduleTaskLate((int)GBA_System.TaskId.StartHBlank, HDrawCycleCount - cycleCountDelay);
        }

        private void Render()
        {
            switch (_DISPCNT & 0b111)
            {
                case 0b000:
                    RenderBackgroundMode0();
                    break;
                case 0b001:
                    RenderBackgroundMode1();
                    break;
                case 0b010:
                    RenderBackgroundMode2();
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

                // TODO: verify
                case 0b110:
                case 0b111:
                    throw new Exception("Iris.GBA.Video: Unknown background mode");
            }

            _currentBG2X += (Int16)_BG2PB;
            _currentBG2Y += (Int16)_BG2PD;

            _currentBG3X += (Int16)_BG3PB;
            _currentBG3Y += (Int16)_BG3PD;
        }

        private void RenderBackgroundMode0()
        {
            bool isFirst = true;

            for (int bgPriority = 3; bgPriority >= 0; --bgPriority)
            {
                if (((_DISPCNT & 0x0800) == 0x0800) && ((_BG3CNT & 0b11) == bgPriority))
                {
                    RenderTextBackground(_BG3CNT, _BG3HOFS, _BG3VOFS, isFirst);
                    isFirst = false;
                }

                if (((_DISPCNT & 0x0400) == 0x0400) && ((_BG2CNT & 0b11) == bgPriority))
                {
                    RenderTextBackground(_BG2CNT, _BG2HOFS, _BG2VOFS, isFirst);
                    isFirst = false;
                }

                if (((_DISPCNT & 0x0200) == 0x0200) && ((_BG1CNT & 0b11) == bgPriority))
                {
                    RenderTextBackground(_BG1CNT, _BG1HOFS, _BG1VOFS, isFirst);
                    isFirst = false;
                }

                if (((_DISPCNT & 0x0100) == 0x0100) && ((_BG0CNT & 0b11) == bgPriority))
                {
                    RenderTextBackground(_BG0CNT, _BG0HOFS, _BG0VOFS, isFirst);
                    isFirst = false;
                }

                if ((_DISPCNT & 0x1000) == 0x1000)
                    RenderObjects((UInt16)bgPriority);
            }
        }

        private void RenderBackgroundMode1()
        {
            bool isFirst = true;

            for (int bgPriority = 3; bgPriority >= 0; --bgPriority)
            {
                if (((_DISPCNT & 0x0400) == 0x0400) && ((_BG2CNT & 0b11) == bgPriority))
                {
                    RenderRotationScalingBackground(_BG2CNT, _currentBG2X, _currentBG2Y, _BG2PA, _BG2PC, isFirst);
                    isFirst = false;
                }

                if (((_DISPCNT & 0x0200) == 0x0200) && ((_BG1CNT & 0b11) == bgPriority))
                {
                    RenderTextBackground(_BG1CNT, _BG1HOFS, _BG1VOFS, isFirst);
                    isFirst = false;
                }

                if (((_DISPCNT & 0x0100) == 0x0100) && ((_BG0CNT & 0b11) == bgPriority))
                {
                    RenderTextBackground(_BG0CNT, _BG0HOFS, _BG0VOFS, isFirst);
                    isFirst = false;
                }

                if ((_DISPCNT & 0x1000) == 0x1000)
                    RenderObjects((UInt16)bgPriority);
            }
        }

        private void RenderBackgroundMode2()
        {
            bool isFirst = true;

            for (int bgPriority = 3; bgPriority >= 0; --bgPriority)
            {
                if (((_DISPCNT & 0x0800) == 0x0800) && ((_BG3CNT & 0b11) == bgPriority))
                {
                    RenderRotationScalingBackground(_BG3CNT, _currentBG3X, _currentBG3Y, _BG3PA, _BG3PC, isFirst);
                    isFirst = false;
                }

                if (((_DISPCNT & 0x0400) == 0x0400) && ((_BG2CNT & 0b11) == bgPriority))
                {
                    RenderRotationScalingBackground(_BG2CNT, _currentBG2X, _currentBG2Y, _BG2PA, _BG2PC, isFirst);
                    isFirst = false;
                }

                if ((_DISPCNT & 0x1000) == 0x1000)
                    RenderObjects((UInt16)bgPriority);
            }
        }

        private void RenderBackgroundMode3()
        {
            if ((_DISPCNT & 0x0400) == 0x0400)
            {
                int pixelNumberBegin = _VCOUNT * DisplayScreenWidth;
                int pixelNumberEnd = pixelNumberBegin + DisplayScreenWidth;

                for (int pixelNumber = pixelNumberBegin; pixelNumber < pixelNumberEnd; ++pixelNumber)
                {
                    unsafe
                    {
                        UInt16 color = Unsafe.Read<UInt16>((UInt16*)_vram + pixelNumber);
                        _displayFrameBuffer[pixelNumber] = color;
                    }
                }
            }

            if ((_DISPCNT & 0x1000) == 0x1000)
            {
                for (int bgPriority = 3; bgPriority >= 0; --bgPriority)
                    RenderObjects((UInt16)bgPriority);
            }
        }

        private void RenderBackgroundMode4()
        {
            if ((_DISPCNT & 0x0400) == 0x0400)
            {
                int pixelNumberBegin = _VCOUNT * DisplayScreenWidth;
                int pixelNumberEnd = pixelNumberBegin + DisplayScreenWidth;

                UInt32 vramFrameBufferOffset = ((_DISPCNT & 0x0010) == 0) ? 0x0_0000u : 0x0_a000u;

                for (int pixelNumber = pixelNumberBegin; pixelNumber < pixelNumberEnd; ++pixelNumber)
                {
                    unsafe
                    {
                        Byte colorNumber = Unsafe.Read<Byte>((Byte*)_vram + vramFrameBufferOffset + pixelNumber);
                        UInt16 color = Unsafe.Read<UInt16>((UInt16*)_paletteRAM + colorNumber);
                        _displayFrameBuffer[pixelNumber] = color;
                    }
                }
            }

            if ((_DISPCNT & 0x1000) == 0x1000)
            {
                for (int bgPriority = 3; bgPriority >= 0; --bgPriority)
                    RenderObjects((UInt16)bgPriority);
            }
        }

        private void RenderBackgroundMode5()
        {
            if ((_DISPCNT & 0x0400) == 0x0400)
            {
                const int VRAM_FrameBufferWidth = 160;
                const int VRAM_FrameBufferHeight = 128;

                if (_VCOUNT < VRAM_FrameBufferHeight)
                {
                    int vramPixelNumberBegin = _VCOUNT * VRAM_FrameBufferWidth;
                    int vramPixelNumberEnd = vramPixelNumberBegin + VRAM_FrameBufferWidth;

                    int displayPixelNumberBegin = _VCOUNT * DisplayScreenWidth;

                    UInt32 vramFrameBufferOffset = ((_DISPCNT & 0x0010) == 0) ? 0x0_0000u : 0x0_a000u;

                    for (int vramPixelNumber = vramPixelNumberBegin, displayPixelNumber = displayPixelNumberBegin; vramPixelNumber < vramPixelNumberEnd; ++vramPixelNumber, ++displayPixelNumber)
                    {
                        unsafe
                        {
                            UInt16 color = Unsafe.Read<UInt16>((Byte*)_vram + vramFrameBufferOffset + (vramPixelNumber * 2));
                            _displayFrameBuffer[displayPixelNumber] = color;
                        }
                    }
                }
            }

            if ((_DISPCNT & 0x1000) == 0x1000)
            {
                for (int bgPriority = 3; bgPriority >= 0; --bgPriority)
                    RenderObjects((UInt16)bgPriority);
            }
        }

        private void RenderTextBackground(UInt16 cnt, UInt16 hofs, UInt16 vofs, bool isFirst)
        {
            int displayPixelNumberBegin = _VCOUNT * DisplayScreenWidth;

            UInt16 virtualScreenSize = (UInt16)((cnt >> 14) & 0b11);
            UInt16 screenBaseBlock = (UInt16)((cnt >> 8) & 0b1_1111);
            UInt16 colorMode = (UInt16)((cnt >> 7) & 1);
            UInt16 characterBaseBlock = (UInt16)((cnt >> 2) & 0b11);

            int virtualScreenWidth = ((virtualScreenSize & 0b01) == 0) ? 256 : 512;
            int virtualScreenHeight = ((virtualScreenSize & 0b10) == 0) ? 256 : 512;

            UInt32 screenBaseBlockOffset = (UInt32)(screenBaseBlock * 2 * KB);
            UInt32 characterBaseBlockOffset = (UInt32)(characterBaseBlock * 16 * KB);

            int v = (_VCOUNT + vofs) % virtualScreenHeight;

            const int SC_Width = 256;
            const int SC_Height = 256;
            const int SC_CharacterCount = (SC_Width / CharacterWidth) * (SC_Height / CharacterHeight);

            int scV = v / SC_Height;
            int scPixelV = v % SC_Height;

            int scCharacterV = scPixelV / CharacterHeight;
            int characterPixelV = scPixelV % CharacterHeight;

            int scNumberBegin = scV * (virtualScreenWidth / SC_Width);
            int characterNumberBegin = scCharacterV * (SC_Width / CharacterWidth);

            int characterPixelNumberBegin = characterPixelV * CharacterWidth;
            int characterPixelNumberBegin_VerticalFlip = (CharacterHeight - 1 - characterPixelV) * CharacterWidth;

            for (int hcount = 0; hcount < DisplayScreenWidth; ++hcount)
            {
                int h = (hcount + hofs) % virtualScreenWidth;

                int scH = h / SC_Width;
                int scPixelH = h % SC_Width;

                int scCharacterH = scPixelH / CharacterWidth;
                int characterPixelH = scPixelH % CharacterWidth;

                int scNumber = scNumberBegin + scH;
                int characterNumber = characterNumberBegin + scCharacterH;

                unsafe
                {
                    UInt16 screenData = Unsafe.Read<UInt16>((Byte*)_vram + screenBaseBlockOffset + (scNumber * SC_CharacterCount * 2) + (characterNumber * 2));

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
                        characterPixelNumber += characterPixelH;
                    else
                        characterPixelNumber += CharacterWidth - 1 - characterPixelH;

                    const UInt32 ObjectCharacterDataOffset = 0x1_0000;

                    UInt16 color;

                    // 16 colors x 16 palettes
                    if (colorMode == 0)
                    {
                        const int CharacterSize = 32;
                        UInt32 characterOffset = (UInt32)(characterBaseBlockOffset + (characterName * CharacterSize));

                        if (characterOffset >= ObjectCharacterDataOffset)
                            continue;

                        Byte colorNumber = Unsafe.Read<Byte>((Byte*)_vram + characterOffset + (characterPixelNumber / 2));

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
                        UInt32 characterOffset = (UInt32)(characterBaseBlockOffset + (characterName * CharacterSize));

                        if (characterOffset >= ObjectCharacterDataOffset)
                            continue;

                        Byte colorNumber = Unsafe.Read<Byte>((Byte*)_vram + characterOffset + characterPixelNumber);

                        if (!isFirst && (colorNumber == 0))
                            continue;

                        color = Unsafe.Read<UInt16>((UInt16*)_paletteRAM + colorNumber);
                    }

                    int displayPixelNumber = displayPixelNumberBegin + hcount;
                    _displayFrameBuffer[displayPixelNumber] = color;
                }
            }
        }

        private void RenderRotationScalingBackground(UInt16 cnt, Int32 x, Int32 y, UInt16 pa, UInt16 pc, bool isFirst)
        {
            int displayPixelNumberBegin = _VCOUNT * DisplayScreenWidth;

            UInt16 virtualScreenSize = (UInt16)((cnt >> 14) & 0b11);
            UInt16 areaOverflow = (UInt16)((cnt >> 13) & 1);
            UInt16 screenBaseBlock = (UInt16)((cnt >> 8) & 0b1_1111);
            UInt16 characterBaseBlock = (UInt16)((cnt >> 2) & 0b11);

            int virtualScreenWidth = 128 << virtualScreenSize;
            int virtualScreenHeight = 128 << virtualScreenSize;

            UInt32 screenBaseBlockOffset = (UInt32)(screenBaseBlock * 2 * KB);
            UInt32 characterBaseBlockOffset = (UInt32)(characterBaseBlock * 16 * KB);

            for (int hcount = 0; hcount < DisplayScreenWidth; ++hcount, x += (Int16)pa, y += (Int16)pc)
            {
                int h = x >> 8;
                int v = y >> 8;

                if (areaOverflow == 0)
                {
                    if ((h < 0) || (h >= virtualScreenWidth))
                        continue;

                    if ((v < 0) || (v >= virtualScreenHeight))
                        continue;
                }
                else
                {
                    h %= virtualScreenWidth;
                    v %= virtualScreenHeight;

                    if (h < 0)
                        h += virtualScreenWidth;

                    if (v < 0)
                        v += virtualScreenHeight;
                }

                int characterH = h / CharacterWidth;
                int characterPixelH = h % CharacterWidth;

                int characterV = v / CharacterHeight;
                int characterPixelV = v % CharacterHeight;

                int characterNumber = (characterV * (virtualScreenWidth / CharacterWidth)) + characterH;
                int characterPixelNumber = (characterPixelV * CharacterWidth) + characterPixelH;

                unsafe
                {
                    Byte characterName = Unsafe.Read<Byte>((Byte*)_vram + screenBaseBlockOffset + characterNumber);

                    const int CharacterSize = 64;
                    UInt32 characterOffset = (UInt32)(characterBaseBlockOffset + (characterName * CharacterSize));

                    const UInt32 ObjectCharacterDataOffset = 0x1_0000;

                    if (characterOffset >= ObjectCharacterDataOffset)
                        continue;

                    Byte colorNumber = Unsafe.Read<Byte>((Byte*)_vram + characterOffset + characterPixelNumber);

                    if (!isFirst && (colorNumber == 0))
                        continue;

                    UInt16 color = Unsafe.Read<UInt16>((UInt16*)_paletteRAM + colorNumber);

                    int displayPixelNumber = displayPixelNumberBegin + hcount;
                    _displayFrameBuffer[displayPixelNumber] = color;
                }
            }
        }

        private void RenderObjects(UInt16 bgPriority)
        {
            int displayPixelNumberBegin = _VCOUNT * DisplayScreenWidth;

            UInt16 mappingFormat = (UInt16)((_DISPCNT >> 6) & 1);

            for (int objNumber = 127; objNumber >= 0; --objNumber)
            {
                unsafe
                {
                    UInt16 attribute0 = Unsafe.Read<UInt16>((UInt16*)_oam + (objNumber * 4));
                    UInt16 attribute1 = Unsafe.Read<UInt16>((UInt16*)_oam + (objNumber * 4) + 1);
                    UInt16 attribute2 = Unsafe.Read<UInt16>((UInt16*)_oam + (objNumber * 4) + 2);

                    UInt16 shape = (UInt16)((attribute0 >> 14) & 0b11);
                    UInt16 colorMode = (UInt16)((attribute0 >> 13) & 1);
                    UInt16 rotationScalingFlag = (UInt16)((attribute0 >> 8) & 1);
                    UInt16 yCoordinate = (UInt16)(attribute0 & 0xff);

                    UInt16 objSize = (UInt16)((attribute1 >> 14) & 0b11);
                    UInt16 verticalFlipFlag = (UInt16)((attribute1 >> 13) & 1);
                    UInt16 horizontalFlipFlag = (UInt16)((attribute1 >> 12) & 1);
                    UInt16 xCoordinate = (UInt16)(attribute1 & 0x1ff);

                    UInt16 colorPalette = (UInt16)((attribute2 >> 12) & 0b1111);
                    UInt16 objPriority = (UInt16)((attribute2 >> 10) & 0b11);
                    UInt16 characterName = (UInt16)(attribute2 & 0x3ff);

                    if (objPriority != bgPriority)
                        continue;

                    (int objWidth, int objHeight) = (shape, objSize) switch
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
                        // TODO: verify
                        _ => (0, 0)
                    };

                    const int VirtualScreenWidth = 512;
                    const int VirtualScreenHeight = 256;

                    int left = xCoordinate;
                    int right = (xCoordinate + objWidth) % VirtualScreenWidth;

                    int top = yCoordinate;
                    int bottom = (yCoordinate + objHeight) % VirtualScreenHeight;

                    bool leftHidden = left >= DisplayScreenWidth;
                    bool rightHidden = right >= DisplayScreenWidth;

                    bool topHidden = top >= DisplayScreenHeight;
                    bool bottomHidden = bottom >= DisplayScreenHeight;

                    if ((leftHidden && rightHidden) || (topHidden && bottomHidden))
                        continue;

                    int hBegin;
                    int vBegin;

                    if (leftHidden)
                    {
                        hBegin = VirtualScreenWidth - left;
                        left = 0;
                    }
                    else if (rightHidden)
                    {
                        hBegin = 0;
                        right = DisplayScreenWidth;
                    }
                    else
                    {
                        hBegin = 0;
                    }

                    if (topHidden)
                    {
                        vBegin = VirtualScreenHeight - top;
                        top = 0;
                    }
                    else if (bottomHidden)
                    {
                        vBegin = 0;
                        bottom = DisplayScreenHeight;
                    }
                    else
                    {
                        vBegin = 0;
                    }

                    if ((top > _VCOUNT) || (bottom <= _VCOUNT))
                        continue;

                    int v = _VCOUNT - top + vBegin;

                    int characterV = v / CharacterHeight;
                    int characterPixelV = v % CharacterHeight;

                    int characterNumberBegin;
                    int characterPixelNumberBegin = CharacterWidth;

                    if (verticalFlipFlag == 0)
                    {
                        characterNumberBegin = characterV;
                        characterPixelNumberBegin *= characterPixelV;
                    }
                    else
                    {
                        characterNumberBegin = (objHeight / CharacterHeight) - 1 - characterV;
                        characterPixelNumberBegin *= CharacterHeight - 1 - characterPixelV;
                    }

                    // 2D mapping
                    if (mappingFormat == 0)
                        characterNumberBegin *= (colorMode == 0) ? 32 : 16;

                    // 1D mapping
                    else
                        characterNumberBegin *= objWidth / CharacterWidth;

                    for (int hcount = left; hcount < right; ++hcount)
                    {
                        int h = hcount - left + hBegin;

                        int characterH = h / CharacterWidth;
                        int characterPixelH = h % CharacterWidth;

                        int characterNumber = characterNumberBegin;
                        int characterPixelNumber = characterPixelNumberBegin;

                        if (horizontalFlipFlag == 0)
                        {
                            characterNumber += characterH;
                            characterPixelNumber += characterPixelH;
                        }
                        else
                        {
                            characterNumber += (objWidth / CharacterWidth) - 1 - characterH;
                            characterPixelNumber += CharacterWidth - 1 - characterPixelH;
                        }

                        const UInt32 CharacterDataOffset = 0x1_0000;
                        const UInt32 PaletteOffset = 0x200;

                        UInt16 color;

                        // 16 colors x 16 palettes
                        if (colorMode == 0)
                        {
                            const int CharacterSize = 32;
                            Byte colorNumber = Unsafe.Read<Byte>((Byte*)_vram + CharacterDataOffset + (characterName * 32) + (characterNumber * CharacterSize) + (characterPixelNumber / 2));

                            if ((characterPixelNumber % 2) == 0)
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
                            const int CharacterSize = 64;
                            Byte colorNumber = Unsafe.Read<Byte>((Byte*)_vram + CharacterDataOffset + (characterName * 32) + (characterNumber * CharacterSize) + characterPixelNumber);

                            if (colorNumber == 0)
                                continue;

                            color = Unsafe.Read<UInt16>((Byte*)_paletteRAM + PaletteOffset + (colorNumber * 2));
                        }

                        int displayPixelNumber = displayPixelNumberBegin + hcount;
                        _displayFrameBuffer[displayPixelNumber] = color;
                    }
                }
            }
        }
    }
}
