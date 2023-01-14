﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Iris
{
    public class GBA : CPU.ICallbacks
    {
        public enum Keys
        {
            A = 0,
            B = 1,
            Select = 2,
            Start = 3,
            Right = 4,
            Left = 5,
            Up = 6,
            Down = 7,
            R = 8,
            L = 9,
        };

        private const int KB = 1024;

        private Byte[]? _rom;
        private readonly Byte[] _externalWRAM = new Byte[256 * KB];
        private readonly Byte[] _internalWRAM = new Byte[32 * KB];

        private readonly CPU _cpu;
        private readonly PPU _ppu;

        private UInt16 _KEYINPUT = 0x03ff;

        private bool _running = false;

        public GBA(IRenderer renderer)
        {
            _cpu = new(this);
            _ppu = new(renderer);
        }

        public void Init()
        {
            _cpu.Init(0x0800_0000, 0b1101_1111); // flags cleared + IRQ & FIQ interrupts disabled + ARM state + system mode
            _KEYINPUT = 0x03ff;
        }

        public void LoadROM(string filename)
        {
            _rom = File.ReadAllBytes(filename);
            Init();
        }

        public bool IsRunning()
        {
            return _running;
        }

        public void Run()
        {
            _running = true;
            while (_running)
            {
                _cpu.Step();
                _ppu.Step();
            }
        }

        public void Pause()
        {
            _running = false;
        }

        public void SetKeyStatus(Keys key, bool pressed)
        {
            int mask = 1 << (int)key;
            _KEYINPUT = pressed ? (UInt16)(_KEYINPUT & ~mask) : (UInt16)(_KEYINPUT | mask);
        }

        public Byte ReadMemory8(UInt32 address)
        {
            if (0x0200_0000 <= address && address < 0x0300_0000)
            {
                UInt32 offset = address - 0x0200_0000;
                if (offset < _externalWRAM.Length)
                    return _externalWRAM[offset];
            }
            else if (0x0300_0000 <= address && address < 0x0400_0000)
            {
                UInt32 offset = address - 0x0300_0000;
                if (offset < _internalWRAM.Length)
                    return _internalWRAM[offset];
            }
            else if (0x0400_0000 <= address && address < 0x0500_0000)
            {
                UInt32 offset = address - 0x0400_0000;
                switch (offset)
                {
                    case 0x000:
                        return (Byte)_ppu.DISPCNT;
                    case 0x001:
                        return (Byte)(_ppu.DISPCNT >> 8);

                    case 0x004:
                        return (Byte)_ppu.DISPSTAT;
                    case 0x005:
                        return (Byte)(_ppu.DISPSTAT >> 8);

                    case 0x006:
                        return (Byte)_ppu.VCOUNT;
                    case 0x007:
                        return (Byte)(_ppu.VCOUNT >> 8);

                    case 0x050:
                    case 0x051:
                        Console.WriteLine("GBA: Read from BLDCNT register unimplemented");
                        return 0;

                    case 0x088:
                    case 0x089:
                        Console.WriteLine("GBA: Read from SOUNDBIAS register unimplemented");
                        return 0;

                    case 0x0ba:
                    case 0x0bb:
                        Console.WriteLine("GBA: Read from DMA0CNT_H register unimplemented");
                        return 0;

                    case 0x0c4:
                    case 0x0c5:
                        Console.WriteLine("GBA: Read from DMA1CNT_L register unimplemented");
                        return 0;

                    case 0x0c6:
                    case 0x0c7:
                        Console.WriteLine("GBA: Read from DMA1CNT_H register unimplemented");
                        return 0;

                    case 0x0d0:
                    case 0x0d1:
                        Console.WriteLine("GBA: Read from DMA2CNT_L register unimplemented");
                        return 0;

                    case 0x0d2:
                    case 0x0d3:
                        Console.WriteLine("GBA: Read from DMA2CNT_H register unimplemented");
                        return 0;

                    case 0x0de:
                    case 0x0df:
                        Console.WriteLine("GBA: Read from DMA3CNT_H register unimplemented");
                        return 0;

                    case 0x128:
                    case 0x129:
                        Console.WriteLine("GBA: Read from SIOCNT register unimplemented");
                        return 0;

                    case 0x130:
                        return (Byte)_KEYINPUT;
                    case 0x131:
                        return (Byte)(_KEYINPUT >> 8);

                    case 0x200:
                    case 0x201:
                        Console.WriteLine("GBA: Read from IE register unimplemented");
                        return 0;

                    case 0x204:
                    case 0x205:
                        Console.WriteLine("GBA: Read from WAITCNT register unimplemented");
                        return 0;

                    case 0x208:
                    case 0x209:
                        Console.WriteLine("GBA: Read from IME register unimplemented");
                        return 0;
                }
            }
            else if (0x0800_0000 <= address && address < 0x0A00_0000)
            {
                if (_rom == null)
                    throw new Exception("GBA: No ROM loaded");

                UInt32 offset = address - 0x0800_0000;
                if (offset < _rom.Length)
                    return _rom[offset];
            }

            throw new Exception(string.Format("GBA: Invalid read from address 0x{0:x8}", address));
        }

        public UInt16 ReadMemory16(UInt32 address)
        {
            address &= 0xffff_fffe;
            return (UInt16)((ReadMemory8(address + 1) << 8) | ReadMemory8(address));
        }

        public UInt32 ReadMemory32(UInt32 address)
        {
            address &= 0xffff_fffc;
            return (UInt32)((ReadMemory16(address + 2) << 16) | ReadMemory16(address));
        }

        public void WriteMemory8(UInt32 address, Byte value)
        {
            if (0x0200_0000 <= address && address < 0x0300_0000)
            {
                UInt32 offset = address - 0x0200_0000;
                if (offset < _externalWRAM.Length)
                    _externalWRAM[offset] = value;
                else
                    throw new Exception(string.Format("GBA: Invalid write to address 0x{0:x8}", address));
            }
            else if (0x0300_0000 <= address && address < 0x0400_0000)
            {
                UInt32 offset = address - 0x0300_0000;
                if (offset < _internalWRAM.Length)
                    _internalWRAM[offset] = value;
                else
                    throw new Exception(string.Format("GBA: Invalid write to address 0x{0:x8}", address));
            }
            else if (0x0400_0000 <= address && address < 0x0500_0000)
            {
                UInt32 offset = address - 0x0400_0000;
                switch (offset)
                {
                    case 0x000:
                        _ppu.DISPCNT = (UInt16)((_ppu.DISPCNT & 0xff00) | value);
                        break;
                    case 0x001:
                        _ppu.DISPCNT = (UInt16)((_ppu.DISPCNT & 0x00ff) | (value << 8));
                        break;

                    case 0x002:
                    case 0x003:
                        // undocumented - green swap
                        break; // ignore

                    case 0x004:
                        _ppu.DISPSTAT = (UInt16)((_ppu.DISPSTAT & 0xff00) | value);
                        break;
                    case 0x005:
                        _ppu.DISPSTAT = (UInt16)((_ppu.DISPSTAT & 0x00ff) | (value << 8));
                        break;

                    case 0x008:
                    case 0x009:
                        Console.WriteLine("GBA: Write to BG0CNT register unimplemented");
                        break;

                    case 0x00a:
                    case 0x00b:
                        Console.WriteLine("GBA: Write to BG1CNT register unimplemented");
                        break;

                    case 0x00c:
                    case 0x00d:
                        Console.WriteLine("GBA: Write to BG2CNT register unimplemented");
                        break;

                    case 0x00e:
                    case 0x00f:
                        Console.WriteLine("GBA: Write to BG3CNT register unimplemented");
                        break;

                    case 0x040:
                    case 0x041:
                        Console.WriteLine("GBA: Write to WIN0H register unimplemented");
                        break;

                    case 0x042:
                    case 0x043:
                        Console.WriteLine("GBA: Write to WIN1H register unimplemented");
                        break;

                    case 0x044:
                    case 0x045:
                        Console.WriteLine("GBA: Write to WIN0V register unimplemented");
                        break;

                    case 0x046:
                    case 0x047:
                        Console.WriteLine("GBA: Write to WIN1V register unimplemented");
                        break;

                    case 0x048:
                    case 0x049:
                        Console.WriteLine("GBA: Write to WININ register unimplemented");
                        break;

                    case 0x04a:
                    case 0x04b:
                        Console.WriteLine("GBA: Write to WINOUT register unimplemented");
                        break;

                    case 0x050:
                    case 0x051:
                        Console.WriteLine("GBA: Write to BLDCNT register unimplemented");
                        break;

                    case 0x052:
                    case 0x053:
                        Console.WriteLine("GBA: Write to BLDALPHA register unimplemented");
                        break;

                    case 0x062:
                    case 0x063:
                        Console.WriteLine("GBA: Write to SOUND1CNT_H register unimplemented");
                        break;

                    case 0x064:
                    case 0x065:
                        Console.WriteLine("GBA: Write to SOUND1CNT_X register unimplemented");
                        break;

                    case 0x068:
                    case 0x069:
                        Console.WriteLine("GBA: Write to SOUND2CNT_L register unimplemented");
                        break;

                    case 0x06c:
                    case 0x06d:
                        Console.WriteLine("GBA: Write to SOUND2CNT_H register unimplemented");
                        break;

                    case 0x070:
                    case 0x071:
                        Console.WriteLine("GBA: Write to SOUND3CNT_L register unimplemented");
                        break;

                    case 0x078:
                    case 0x079:
                        Console.WriteLine("GBA: Write to SOUND4CNT_L register unimplemented");
                        break;

                    case 0x07c:
                    case 0x07d:
                        Console.WriteLine("GBA: Write to SOUND4CNT_H register unimplemented");
                        break;

                    case 0x080:
                    case 0x081:
                        Console.WriteLine("GBA: Write to SOUNDCNT_L register unimplemented");
                        break;

                    case 0x082:
                    case 0x083:
                        Console.WriteLine("GBA: Write to SOUNDCNT_H register unimplemented");
                        break;

                    case 0x084:
                    case 0x085:
                        Console.WriteLine("GBA: Write to SOUNDCNT_X register unimplemented");
                        break;

                    case 0x088:
                    case 0x089:
                        Console.WriteLine("GBA: Write to SOUNDBIAS register unimplemented");
                        break;

                    case 0x0ba:
                    case 0x0bb:
                        Console.WriteLine("GBA: Write to DMA0CNT_H register unimplemented");
                        break;

                    case 0x0bc:
                    case 0x0bd:
                        Console.WriteLine("GBA: Write to DMA1SAD_L register unimplemented");
                        break;

                    case 0x0be:
                    case 0x0bf:
                        Console.WriteLine("GBA: Write to DMA1SAD_H register unimplemented");
                        break;

                    case 0x0c0:
                    case 0x0c1:
                        Console.WriteLine("GBA: Write to DMA1DAD_L register unimplemented");
                        break;

                    case 0x0c2:
                    case 0x0c3:
                        Console.WriteLine("GBA: Write to DMA1DAD_H register unimplemented");
                        break;

                    case 0x0c6:
                    case 0x0c7:
                        Console.WriteLine("GBA: Write to DMA1CNT_H register unimplemented");
                        break;

                    case 0x0c8:
                    case 0x0c9:
                        Console.WriteLine("GBA: Write to DMA2SAD_L register unimplemented");
                        break;

                    case 0x0ca:
                    case 0x0cb:
                        Console.WriteLine("GBA: Write to DMA2SAD_H register unimplemented");
                        break;

                    case 0x0cc:
                    case 0x0cd:
                        Console.WriteLine("GBA: Write to DMA2DAD_L register unimplemented");
                        break;

                    case 0x0ce:
                    case 0x0cf:
                        Console.WriteLine("GBA: Write to DMA2DAD_H register unimplemented");
                        break;

                    case 0x0d2:
                    case 0x0d3:
                        Console.WriteLine("GBA: Write to DMA2CNT_H register unimplemented");
                        break;

                    case 0x0de:
                    case 0x0df:
                        Console.WriteLine("GBA: Write to DMA3CNT_H register unimplemented");
                        break;

                    case 0x100:
                    case 0x101:
                        Console.WriteLine("GBA: Write to TM0CNT_L register unimplemented");
                        break;

                    case 0x102:
                    case 0x103:
                        Console.WriteLine("GBA: Write to TM0CNT_H register unimplemented");
                        break;

                    case 0x10c:
                    case 0x10d:
                        Console.WriteLine("GBA: Write to TM3CNT_L register unimplemented");
                        break;

                    case 0x10e:
                    case 0x10f:
                        Console.WriteLine("GBA: Write to TM3CNT_H register unimplemented");
                        break;

                    case 0x128:
                    case 0x129:
                        Console.WriteLine("GBA: Write to SIOCNT register unimplemented");
                        break;

                    case 0x12a:
                    case 0x12b:
                        Console.WriteLine("GBA: Write to SIODATA8 register unimplemented");
                        break;

                    case 0x134:
                    case 0x135:
                        Console.WriteLine("GBA: Write to RCNT register unimplemented");
                        break;

                    case 0x200:
                    case 0x201:
                        Console.WriteLine("GBA: Write to IE register unimplemented");
                        break;

                    case 0x202:
                    case 0x203:
                        Console.WriteLine("GBA: Write to IF register unimplemented");
                        break;

                    case 0x204:
                    case 0x205:
                        Console.WriteLine("GBA: Write to WAITCNT register unimplemented");
                        break;

                    case 0x208:
                    case 0x209:
                        Console.WriteLine("GBA: Write to IME register unimplemented");
                        break;

                    case 0x20a:
                    case 0x20b:
                        // unused
                        break; // ignore

                    default:
                        throw new Exception(string.Format("GBA: Invalid write to address 0x{0:x8}", address));
                }
            }
            else if (0x0500_0000 <= address && address < 0x0600_0000)
            {
                UInt32 offset = address - 0x0500_0000;
                if (offset < _ppu.PaletteRAM.Length)
                    _ppu.PaletteRAM[offset] = value;
                else
                    throw new Exception(string.Format("GBA: Invalid write to address 0x{0:x8}", address));
            }
            else if (0x0600_0000 <= address && address < 0x0700_0000)
            {
                UInt32 offset = address - 0x0600_0000;
                if (offset < _ppu.VRAM.Length)
                    _ppu.VRAM[offset] = value;
                else
                    throw new Exception(string.Format("GBA: Invalid write to address 0x{0:x8}", address));
            }
            else
            {
                //throw new Exception(string.Format("GBA: Invalid write to address 0x{0:x8}", address));
            }
        }

        public void WriteMemory16(UInt32 address, UInt16 value)
        {
            address &= 0xffff_fffe;
            WriteMemory8(address + 1, (Byte)(value >> 8));
            WriteMemory8(address, (Byte)value);
        }

        public void WriteMemory32(UInt32 address, UInt32 value)
        {
            address &= 0xffff_fffc;
            WriteMemory16(address + 2, (UInt16)(value >> 16));
            WriteMemory16(address, (UInt16)value);
        }
    }
}
