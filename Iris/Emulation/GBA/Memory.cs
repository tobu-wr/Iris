namespace Iris.Emulation.GBA
{
    internal sealed partial class Core
    {
        private const int KB = 1024;

        private Byte[]? _ROM;
        private readonly Byte[] _SRAM = new Byte[64 * KB];
        private readonly Byte[] _externalWRAM = new Byte[256 * KB];
        private readonly Byte[] _internalWRAM = new Byte[32 * KB];

        internal void LoadROM(string filename)
        {
            _ROM = File.ReadAllBytes(filename);
        }

        private Byte ReadMemory8(UInt32 address)
        {
            address &= 0x0fff_ffff;
            if (address is >= 0x0000_0000 and < 0x0000_4000)
            {
                // BIOS
                return 0;
            }
            else if (address is >= 0x0000_4000 and < 0x0200_0000)
            {
                // unused
                return 0;
            }
            else if (address is >= 0x0200_0000 and < 0x0300_0000)
            {
                UInt32 offset = (address - 0x0200_0000) & 0x0003_ffff;
                return _externalWRAM[offset];
            }
            else if (address is >= 0x0300_0000 and < 0x0400_0000)
            {
                UInt32 offset = (address - 0x0300_0000) & 0x0000_7fff;
                return _internalWRAM[offset];
            }
            else if (address is >= 0x0400_0000 and < 0x0500_0000)
            {
                UInt32 offset = address - 0x0400_0000;
                switch (offset)
                {
                    case 0x000:
                        return (Byte)_ppu.DISPCNT;
                    case 0x001:
                        return (Byte)(_ppu.DISPCNT >> 8);

                    case 0x002:
                    case 0x003:
                        // undocumented - green swap
                        return 0;

                    case 0x004:
                        return (Byte)_ppu.DISPSTAT;
                    case 0x005:
                        return (Byte)(_ppu.DISPSTAT >> 8);

                    case 0x006:
                        return (Byte)_ppu.VCOUNT;
                    case 0x007:
                        return (Byte)(_ppu.VCOUNT >> 8);

                    case 0x008:
                    case 0x009:
                        Console.WriteLine("Emulation.GBA.Core: Read from BG0CNT register unimplemented");
                        return 0;

                    case 0x00a:
                    case 0x00b:
                        Console.WriteLine("Emulation.GBA.Core: Read from BG1CNT register unimplemented");
                        return 0;

                    case 0x00c:
                    case 0x00d:
                        Console.WriteLine("Emulation.GBA.Core: Read from BG2CNT register unimplemented");
                        return 0;

                    case 0x00e:
                    case 0x00f:
                        Console.WriteLine("Emulation.GBA.Core: Read from BG3CNT register unimplemented");
                        return 0;

                    case 0x050:
                    case 0x051:
                        Console.WriteLine("Emulation.GBA.Core: Read from BLDCNT register unimplemented");
                        return 0;

                    case 0x088:
                        return (Byte)_SOUNDBIAS;
                    case 0x089:
                        return (Byte)(_SOUNDBIAS >> 8);

                    case 0x0ba:
                        return (Byte)_DMA0CNT_H;
                    case 0x0bb:
                        return (Byte)(_DMA0CNT_H >> 8);

                    case 0x0c4:
                        return (Byte)_DMA1CNT_L;
                    case 0x0c5:
                        return (Byte)(_DMA1CNT_L >> 8);

                    case 0x0c6:
                        return (Byte)_DMA1CNT_H;
                    case 0x0c7:
                        return (Byte)(_DMA1CNT_H >> 8);

                    case 0x0d0:
                        return (Byte)_DMA2CNT_L;
                    case 0x0d1:
                        return (Byte)(_DMA2CNT_L >> 8);

                    case 0x0d2:
                        return (Byte)_DMA2CNT_H;
                    case 0x0d3:
                        return (Byte)(_DMA2CNT_H >> 8);

                    case 0x0de:
                        return (Byte)_DMA3CNT_H;
                    case 0x0df:
                        return (Byte)(_DMA3CNT_H >> 8);

                    case 0x102:
                        return (Byte)_TM0CNT_H;
                    case 0x103:
                        return (Byte)(_TM0CNT_H >> 8);

                    case 0x106:
                        return (Byte)_TM1CNT_H;
                    case 0x107:
                        return (Byte)(_TM1CNT_H >> 8);

                    case 0x10a:
                        return (Byte)_TM2CNT_H;
                    case 0x10b:
                        return (Byte)(_TM2CNT_H >> 8);

                    case 0x10e:
                        return (Byte)_TM3CNT_H;
                    case 0x10f:
                        return (Byte)(_TM3CNT_H >> 8);

                    case 0x128:
                        return (Byte)_SIOCNT;
                    case 0x129:
                        return (Byte)(_SIOCNT >> 8);

                    case 0x130:
                        return (Byte)_KEYINPUT;
                    case 0x131:
                        return (Byte)(_KEYINPUT >> 8);

                    case 0x132:
                        return (Byte)_KEYCNT;
                    case 0x133:
                        return (Byte)(_KEYCNT >> 8);

                    case 0x200:
                        return (Byte)_IE;
                    case 0x201:
                        return (Byte)(_IE >> 8);

                    case 0x204:
                        return (Byte)_WAITCNT;
                    case 0x205:
                        return (Byte)(_WAITCNT >> 8);

                    case 0x208:
                        return (Byte)_IME;
                    case 0x209:
                        return (Byte)(_IME >> 8);
                }
            }
            else if (address is >= 0x0600_0000 and < 0x0601_8000)
            {
                UInt32 offset = address - 0x0600_0000;
                return _ppu.VRAM[offset];
            }
            else if (address is >= 0x0800_0000 and < 0x0a00_0000)
            {
                if (_ROM is null)
                    throw new Exception("Emulation.GBA.Core: No ROM loaded");

                UInt32 offset = address - 0x0800_0000;
                if (offset < _ROM.Length)
                    return _ROM[offset];
            }
            else if (address is >= 0x0e00_0000 and < 0x0e01_0000)
            {
                UInt32 offset = address - 0x0e00_0000;
                return _SRAM[offset];
            }

            throw new Exception(string.Format("Emulation.GBA.Core: Invalid read from address 0x{0:x8}", address));
        }

        private UInt16 ReadMemory16(UInt32 address)
        {
            address &= 0x0fff_fffe;
            return (UInt16)(ReadMemory8(address + 1) << 8 | ReadMemory8(address));
        }

        private UInt32 ReadMemory32(UInt32 address)
        {
            address &= 0x0fff_fffc;
            return (UInt32)(ReadMemory16(address + 2) << 16 | ReadMemory16(address));
        }

        private void WriteMemory8(UInt32 address, Byte value)
        {
            if (address is >= 0x0000_4000 and < 0x0200_0000)
            {
                // unused
            }
            else if (address is >= 0x0200_0000 and < 0x0300_0000)
            {
                UInt32 offset = (address - 0x0200_0000) & 0x0003_ffff;
                _externalWRAM[offset] = value;
            }
            else if (address is >= 0x0300_0000 and < 0x0400_0000)
            {
                UInt32 offset = (address - 0x0300_0000) & 0x0000_7fff;
                _internalWRAM[offset] = value;
            }
            else if (address is >= 0x0400_0000 and < 0x0500_0000)
            {
                UInt32 offset = address - 0x0400_0000;
                switch (offset)
                {
                    case 0x000:
                        _ppu.DISPCNT = (UInt16)(_ppu.DISPCNT & 0xff00 | value);
                        break;
                    case 0x001:
                        _ppu.DISPCNT = (UInt16)(_ppu.DISPCNT & 0x00ff | value << 8);
                        break;

                    case 0x002:
                    case 0x003:
                        // undocumented - green swap
                        break; // ignore

                    case 0x004:
                        _ppu.DISPSTAT = (UInt16)(_ppu.DISPSTAT & 0xff00 | value);
                        break;
                    case 0x005:
                        _ppu.DISPSTAT = (UInt16)(_ppu.DISPSTAT & 0x00ff | value << 8);
                        break;

                    case 0x008:
                    case 0x009:
                        Console.WriteLine("Emulation.GBA.Core: Write to BG0CNT register unimplemented");
                        break;

                    case 0x00a:
                    case 0x00b:
                        Console.WriteLine("Emulation.GBA.Core: Write to BG1CNT register unimplemented");
                        break;

                    case 0x00c:
                    case 0x00d:
                        Console.WriteLine("Emulation.GBA.Core: Write to BG2CNT register unimplemented");
                        break;

                    case 0x00e:
                    case 0x00f:
                        Console.WriteLine("Emulation.GBA.Core: Write to BG3CNT register unimplemented");
                        break;

                    case 0x010:
                    case 0x011:
                        Console.WriteLine("Emulation.GBA.Core: Write to BG0HOFS register unimplemented");
                        break;

                    case 0x012:
                    case 0x013:
                        Console.WriteLine("Emulation.GBA.Core: Write to BG0VOFS register unimplemented");
                        break;

                    case 0x014:
                    case 0x015:
                        Console.WriteLine("Emulation.GBA.Core: Write to BG1HOFS register unimplemented");
                        break;

                    case 0x016:
                    case 0x017:
                        Console.WriteLine("Emulation.GBA.Core: Write to BG1VOFS register unimplemented");
                        break;

                    case 0x018:
                    case 0x019:
                        Console.WriteLine("Emulation.GBA.Core: Write to BG2HOFS register unimplemented");
                        break;

                    case 0x01a:
                    case 0x01b:
                        Console.WriteLine("Emulation.GBA.Core: Write to BG2VOFS register unimplemented");
                        break;

                    case 0x01c:
                    case 0x01d:
                        Console.WriteLine("Emulation.GBA.Core: Write to BG3HOFS register unimplemented");
                        break;

                    case 0x01e:
                    case 0x01f:
                        Console.WriteLine("Emulation.GBA.Core: Write to BG3VOFS register unimplemented");
                        break;

                    case 0x040:
                    case 0x041:
                        Console.WriteLine("Emulation.GBA.Core: Write to WIN0H register unimplemented");
                        break;

                    case 0x042:
                    case 0x043:
                        Console.WriteLine("Emulation.GBA.Core: Write to WIN1H register unimplemented");
                        break;

                    case 0x044:
                    case 0x045:
                        Console.WriteLine("Emulation.GBA.Core: Write to WIN0V register unimplemented");
                        break;

                    case 0x046:
                    case 0x047:
                        Console.WriteLine("Emulation.GBA.Core: Write to WIN1V register unimplemented");
                        break;

                    case 0x048:
                    case 0x049:
                        Console.WriteLine("Emulation.GBA.Core: Write to WININ register unimplemented");
                        break;

                    case 0x04a:
                    case 0x04b:
                        Console.WriteLine("Emulation.GBA.Core: Write to WINOUT register unimplemented");
                        break;

                    case 0x04c:
                    case 0x04d:
                        Console.WriteLine("Emulation.GBA.Core: Write to MOSAIC register unimplemented");
                        break;

                    case 0x050:
                    case 0x051:
                        Console.WriteLine("Emulation.GBA.Core: Write to BLDCNT register unimplemented");
                        break;

                    case 0x052:
                    case 0x053:
                        Console.WriteLine("Emulation.GBA.Core: Write to BLDALPHA register unimplemented");
                        break;

                    case 0x054:
                    case 0x055:
                        Console.WriteLine("Emulation.GBA.Core: Write to BLDY register unimplemented");
                        break;

                    case 0x062:
                    case 0x063:
                        Console.WriteLine("Emulation.GBA.Core: Write to SOUND1CNT_H register unimplemented");
                        break;

                    case 0x064:
                    case 0x065:
                        Console.WriteLine("Emulation.GBA.Core: Write to SOUND1CNT_X register unimplemented");
                        break;

                    case 0x068:
                    case 0x069:
                        Console.WriteLine("Emulation.GBA.Core: Write to SOUND2CNT_L register unimplemented");
                        break;

                    case 0x06c:
                    case 0x06d:
                        Console.WriteLine("Emulation.GBA.Core: Write to SOUND2CNT_H register unimplemented");
                        break;

                    case 0x070:
                    case 0x071:
                        Console.WriteLine("Emulation.GBA.Core: Write to SOUND3CNT_L register unimplemented");
                        break;

                    case 0x078:
                    case 0x079:
                        Console.WriteLine("Emulation.GBA.Core: Write to SOUND4CNT_L register unimplemented");
                        break;

                    case 0x07c:
                    case 0x07d:
                        Console.WriteLine("Emulation.GBA.Core: Write to SOUND4CNT_H register unimplemented");
                        break;

                    case 0x080:
                    case 0x081:
                        Console.WriteLine("Emulation.GBA.Core: Write to SOUNDCNT_L register unimplemented");
                        break;

                    case 0x082:
                        _SOUNDCNT_H = (UInt16)(_SOUNDCNT_H & 0xff00 | value);
                        break;
                    case 0x083:
                        _SOUNDCNT_H = (UInt16)(_SOUNDCNT_H & 0x00ff | value << 8);
                        break;

                    case 0x084:
                        _SOUNDCNT_X = (UInt16)(_SOUNDCNT_X & 0xff00 | value);
                        break;
                    case 0x085:
                        _SOUNDCNT_X = (UInt16)(_SOUNDCNT_X & 0x00ff | value << 8);
                        break;

                    case 0x088:
                        _SOUNDBIAS = (UInt16)(_SOUNDBIAS & 0xff00 | value);
                        break;
                    case 0x089:
                        _SOUNDBIAS = (UInt16)(_SOUNDBIAS & 0x00ff | value << 8);
                        break;

                    case 0x0ba:
                        _DMA0CNT_H = (UInt16)(_DMA0CNT_H & 0xff00 | value);
                        break;
                    case 0x0bb:
                        _DMA0CNT_H = (UInt16)(_DMA0CNT_H & 0x00ff | value << 8);
                        break;

                    case 0x0bc:
                        _DMA1SAD_L = (UInt16)(_DMA1SAD_L & 0xff00 | value);
                        break;
                    case 0x0bd:
                        _DMA1SAD_L = (UInt16)(_DMA1SAD_L & 0x00ff | value << 8);
                        break;

                    case 0x0be:
                        _DMA1SAD_H = (UInt16)(_DMA1SAD_H & 0xff00 | value);
                        break;
                    case 0x0bf:
                        _DMA1SAD_H = (UInt16)(_DMA1SAD_H & 0x00ff | value << 8);
                        break;

                    case 0x0c0:
                        _DMA1DAD_L = (UInt16)(_DMA1DAD_L & 0xff00 | value);
                        break;
                    case 0x0c1:
                        _DMA1DAD_L = (UInt16)(_DMA1DAD_L & 0x00ff | value << 8);
                        break;

                    case 0x0c2:
                        _DMA1DAD_H = (UInt16)(_DMA1DAD_H & 0xff00 | value);
                        break;
                    case 0x0c3:
                        _DMA1DAD_H = (UInt16)(_DMA1DAD_H & 0x00ff | value << 8);
                        break;

                    case 0x0c4:
                        _DMA1CNT_L = (UInt16)(_DMA1CNT_L & 0xff00 | value);
                        break;
                    case 0x0c5:
                        _DMA1CNT_L = (UInt16)(_DMA1CNT_L & 0x00ff | value << 8);
                        break;

                    case 0x0c6:
                        _DMA1CNT_H = (UInt16)(_DMA1CNT_H & 0xff00 | value);
                        break;
                    case 0x0c7:
                        _DMA1CNT_H = (UInt16)(_DMA1CNT_H & 0x00ff | value << 8);
                        break;

                    case 0x0c8:
                    case 0x0c9:
                        Console.WriteLine("Emulation.GBA.Core: Write to DMA2SAD_L register unimplemented");
                        break;

                    case 0x0ca:
                        _DMA2SAD_H = (UInt16)(_DMA2SAD_H & 0xff00 | value);
                        break;
                    case 0x0cb:
                        _DMA2SAD_H = (UInt16)(_DMA2SAD_H & 0x00ff | value << 8);
                        break;

                    case 0x0cc:
                    case 0x0cd:
                        Console.WriteLine("Emulation.GBA.Core: Write to DMA2DAD_L register unimplemented");
                        break;

                    case 0x0ce:
                    case 0x0cf:
                        Console.WriteLine("Emulation.GBA.Core: Write to DMA2DAD_H register unimplemented");
                        break;

                    case 0x0d0:
                        _DMA2CNT_L = (UInt16)(_DMA2CNT_L & 0xff00 | value);
                        break;
                    case 0x0d1:
                        _DMA2CNT_L = (UInt16)(_DMA2CNT_L & 0x00ff | value << 8);
                        break;

                    case 0x0d2:
                        _DMA2CNT_H = (UInt16)(_DMA2CNT_H & 0xff00 | value);
                        break;
                    case 0x0d3:
                        _DMA2CNT_H = (UInt16)(_DMA2CNT_H & 0x00ff | value << 8);
                        break;

                    case 0x0d4:
                        _DMA3SAD_L = (UInt16)(_DMA3SAD_L & 0xff00 | value);
                        break;
                    case 0x0d5:
                        _DMA3SAD_L = (UInt16)(_DMA3SAD_L & 0x00ff | value << 8);
                        break;

                    case 0x0d6:
                        _DMA3SAD_H = (UInt16)(_DMA3SAD_H & 0xff00 | value);
                        break;
                    case 0x0d7:
                        _DMA3SAD_H = (UInt16)(_DMA3SAD_H & 0x00ff | value << 8);
                        break;

                    case 0x0d8:
                        _DMA3DAD_L = (UInt16)(_DMA3DAD_L & 0xff00 | value);
                        break;
                    case 0x0d9:
                        _DMA3DAD_L = (UInt16)(_DMA3DAD_L & 0x00ff | value << 8);
                        break;

                    case 0x0da:
                        _DMA3DAD_H = (UInt16)(_DMA3DAD_H & 0xff00 | value);
                        break;
                    case 0x0db:
                        _DMA3DAD_H = (UInt16)(_DMA3DAD_H & 0x00ff | value << 8);
                        break;

                    case 0x0dc:
                    case 0x0dd:
                        Console.WriteLine("Emulation.GBA.Core: Write to DMA3CNT_L register unimplemented");
                        break;

                    case 0x0de:
                        _DMA3CNT_H = (UInt16)(_DMA3CNT_H & 0xff00 | value);
                        break;
                    case 0x0df:
                        _DMA3CNT_H = (UInt16)(_DMA3CNT_H & 0x00ff | value << 8);
                        break;

                    case 0x100:
                    case 0x101:
                        Console.WriteLine("Emulation.GBA.Core: Write to TM0CNT_L register unimplemented");
                        break;

                    case 0x102:
                        _TM0CNT_H = (UInt16)(_TM0CNT_H & 0xff00 | value);
                        break;
                    case 0x103:
                        _TM0CNT_H = (UInt16)(_TM0CNT_H & 0x00ff | value << 8);
                        break;

                    case 0x104:
                    case 0x105:
                        Console.WriteLine("Emulation.GBA.Core: Write to TM1CNT_L register unimplemented");
                        break;

                    case 0x106:
                        _TM1CNT_H = (UInt16)(_TM1CNT_H & 0xff00 | value);
                        break;
                    case 0x107:
                        _TM1CNT_H = (UInt16)(_TM1CNT_H & 0x00ff | value << 8);
                        break;

                    case 0x108:
                    case 0x109:
                        Console.WriteLine("Emulation.GBA.Core: Write to TM2CNT_L register unimplemented");
                        break;

                    case 0x10a:
                        _TM2CNT_H = (UInt16)(_TM2CNT_H & 0xff00 | value);
                        break;
                    case 0x10b:
                        _TM2CNT_H = (UInt16)(_TM2CNT_H & 0x00ff | value << 8);
                        break;

                    case 0x10c:
                    case 0x10d:
                        Console.WriteLine("Emulation.GBA.Core: Write to TM3CNT_L register unimplemented");
                        break;

                    case 0x10e:
                        _TM3CNT_H = (UInt16)(_TM3CNT_H & 0xff00 | value);
                        break;
                    case 0x10f:
                        _TM3CNT_H = (UInt16)(_TM3CNT_H & 0x00ff | value << 8);
                        break;

                    case 0x120:
                    case 0x121:
                        Console.WriteLine("Emulation.GBA.Core: Write to SIODATA32_L/SIOMULTI0 register unimplemented");
                        break;

                    case 0x122:
                    case 0x123:
                        Console.WriteLine("Emulation.GBA.Core: Write to SIODATA32_H/SIOMULTI1 register unimplemented");
                        break;

                    case 0x124:
                    case 0x125:
                        Console.WriteLine("Emulation.GBA.Core: Write to SIOMULTI2 register unimplemented");
                        break;

                    case 0x126:
                    case 0x127:
                        Console.WriteLine("Emulation.GBA.Core: Write to SIOMULTI3 register unimplemented");
                        break;

                    case 0x128:
                        _SIOCNT = (UInt16)(_SIOCNT & 0xff00 | value);
                        break;
                    case 0x129:
                        _SIOCNT = (UInt16)(_SIOCNT & 0x00ff | value << 8);
                        break;

                    case 0x12a:
                    case 0x12b:
                        Console.WriteLine("Emulation.GBA.Core: Write to SIODATA8 register unimplemented");
                        break;

                    case 0x130:
                    case 0x131:
                        Console.WriteLine("Emulation.GBA.Core: Write to KEYINPUT register unimplemented");
                        break;

                    case 0x132:
                        _KEYCNT = (UInt16)(_KEYCNT & 0xff00 | value);
                        break;
                    case 0x133:
                        _KEYCNT = (UInt16)(_KEYCNT & 0x00ff | value << 8);
                        break;

                    case 0x134:
                    case 0x135:
                        Console.WriteLine("Emulation.GBA.Core: Write to RCNT register unimplemented");
                        break;

                    case 0x200:
                        _IE = (UInt16)(_IE & 0xff00 | value);
                        break;
                    case 0x201:
                        _IE = (UInt16)(_IE & 0x00ff | value << 8);
                        break;

                    case 0x202:
                    case 0x203:
                        Console.WriteLine("Emulation.GBA.Core: Write to IF register unimplemented");
                        break;

                    case 0x204:
                        _WAITCNT = (UInt16)(_WAITCNT & 0xff00 | value);
                        break;
                    case 0x205:
                        _WAITCNT = (UInt16)(_WAITCNT & 0x00ff | value << 8);
                        break;

                    case 0x206:
                    case 0x207:
                        // unused
                        break; // ignore

                    case 0x208:
                        _IME = (UInt16)(_IME & 0xff00 | value);
                        break;
                    case 0x209:
                        _IME = (UInt16)(_IME & 0x00ff | value << 8);
                        break;

                    case 0x20a:
                    case 0x20b:
                        // unused
                        break; // ignore

                    default:
                        throw new Exception(string.Format("Emulation.GBA.Core: Invalid write to address 0x{0:x8}", address));
                }
            }
            else if (address is >= 0x0500_0000 and < 0x0600_0000)
            {
                UInt32 offset = address - 0x0500_0000;
                if (offset < _ppu.PaletteRAM.Length)
                    _ppu.PaletteRAM[offset] = value;
                else
                    throw new Exception(string.Format("Emulation.GBA.Core: Invalid write to address 0x{0:x8}", address));
            }
            else if (address is >= 0x0600_0000 and < 0x0700_0000)
            {
                UInt32 offset = address - 0x0600_0000;
                if (offset < _ppu.VRAM.Length)
                    _ppu.VRAM[offset] = value;
                else
                    throw new Exception(string.Format("Emulation.GBA.Core: Invalid write to address 0x{0:x8}", address));
            }
            else if (address is >= 0x0700_0000 and < 0x0800_0000)
            {
                Console.WriteLine("Emulation.GBA.Core: Write to OAM");
            }
            else if (address is >= 0x0e00_0000 and < 0x0e01_0000)
            {
                UInt32 offset = address - 0x0e00_0000;
                _SRAM[offset] = value;
            }
            else
            {
                Console.WriteLine("Emulation.GBA.Core: Write to address 0x{0:x8}", address);
            }
        }

        private void WriteMemory16(UInt32 address, UInt16 value)
        {
            address &= 0xffff_fffe;
            WriteMemory8(address + 1, (Byte)(value >> 8));
            WriteMemory8(address, (Byte)value);
        }

        private void WriteMemory32(UInt32 address, UInt32 value)
        {
            address &= 0xffff_fffc;
            WriteMemory16(address + 2, (UInt16)(value >> 16));
            WriteMemory16(address, (UInt16)value);
        }
    }
}
