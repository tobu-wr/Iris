using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Iris.GBA
{
    internal sealed class FLASH : Memory.BackupMemory
    {
        private const int KB = 1024;
        private const int FLASH_BankSize = 64 * KB;
        private int _flashBank;
        private readonly int _flashSize;
        private readonly IntPtr _flash;

        private enum FLASH_State
        {
            WaitingForCommandArgument1,
            WaitingForCommandArgument2,
            WaitingForCommandArgument3,
            WriteCommand,
            SelectBankCommand
        }

        private FLASH_State _flashState;
        private bool _flashIdMode;

        private const UInt32 FLASH_StartAddress = 0x0e00_0000;

        private bool _disposed;

        internal enum Size
        {
            FLASH_64KB = 64 * KB,
            FLASH_128KB = 128 * KB
        }

        internal FLASH(Size size)
        {
            _flashSize = (int)size;
            _flash = Marshal.AllocHGlobal(_flashSize);
        }

        ~FLASH()
        {
            Dispose();
        }

        public override void Dispose()
        {
            if (_disposed)
                return;

            Marshal.FreeHGlobal(_flash);

            GC.SuppressFinalize(this);
            _disposed = true;
        }

        internal override void ResetState()
        {
            unsafe
            {
                NativeMemory.Fill((Byte*)_flash, (nuint)_flashSize, 0xff);
            }

            _flashBank = 0;
            _flashState = FLASH_State.WaitingForCommandArgument1;
        }

        internal override void LoadState(BinaryReader reader)
        {
            byte[] data = reader.ReadBytes(_flashSize);
            Marshal.Copy(data, 0, _flash, _flashSize);

            _flashBank = reader.ReadInt32();
            _flashState = (FLASH_State)reader.ReadInt32();
        }

        internal override void SaveState(BinaryWriter writer)
        {
            byte[] data = new byte[_flashSize];
            Marshal.Copy(_flash, data, 0, _flashSize);
            writer.Write(data);

            writer.Write(_flashBank);
            writer.Write((int)_flashState);
        }

        internal override Byte Read8(UInt32 address)
        {
            UInt32 offset = (address - FLASH_StartAddress) % FLASH_BankSize;

            if (_flashIdMode)
            {
                switch (offset)
                {
                    case 0:
                        return 0x62;
                    case 1:
                        return 0x13;
                }
            }

            unsafe
            {
                return Unsafe.Read<Byte>((Byte*)_flash + offset + (_flashBank * FLASH_BankSize));
            }
        }

        internal override UInt16 Read16(UInt32 address)
        {
            UInt32 offset = (address - FLASH_StartAddress) % FLASH_BankSize;

            unsafe
            {
                Byte value = Unsafe.Read<Byte>((Byte*)_flash + offset + (_flashBank * FLASH_BankSize));
                return (UInt16)((value << 8) | value);
            }
        }

        internal override UInt32 Read32(UInt32 address)
        {
            UInt32 offset = (address - FLASH_StartAddress) % FLASH_BankSize;

            unsafe
            {
                Byte value = Unsafe.Read<Byte>((Byte*)_flash + offset + (_flashBank * FLASH_BankSize));
                return (UInt32)((value << 24) | (value << 16) | (value << 8) | value);
            }
        }

        internal override void Write8(UInt32 address, Byte value)
        {
            UInt32 offset = (address - FLASH_StartAddress) % FLASH_BankSize;

            switch (_flashState)
            {
                case FLASH_State.WaitingForCommandArgument1:
                    if ((offset == 0x5555) && (value == 0xaa))
                        _flashState = FLASH_State.WaitingForCommandArgument2;
                    break;

                case FLASH_State.WaitingForCommandArgument2:
                    if ((offset == 0x2aaa) && (value == 0x55))
                        _flashState = FLASH_State.WaitingForCommandArgument3;
                    break;

                case FLASH_State.WaitingForCommandArgument3:
                    if (offset == 0x5555)
                    {
                        switch (value)
                        {
                            case 0xa0:
                                _flashState = FLASH_State.WriteCommand;
                                break;

                            case 0x80: // erase command
                                _flashState = FLASH_State.WaitingForCommandArgument1;
                                break;

                            case 0x10: // erase entire chip
                                unsafe
                                {
                                    NativeMemory.Fill((Byte*)_flash, (nuint)_flashSize, 0xff);
                                }
                                _flashState = FLASH_State.WaitingForCommandArgument1;
                                break;

                            case 0xb0:
                                _flashState = FLASH_State.SelectBankCommand;
                                break;

                            case 0x90:
                                _flashIdMode = true;
                                _flashState = FLASH_State.WaitingForCommandArgument1;
                                break;

                            case 0xf0:
                                _flashIdMode = false;
                                _flashState = FLASH_State.WaitingForCommandArgument1;
                                break;
                        }
                    }
                    else if (value == 0x30) // erase sector
                    {
                        unsafe
                        {
                            NativeMemory.Fill((Byte*)_flash + offset + (_flashBank * FLASH_BankSize), 0x1000, 0xff);
                        }
                    }
                    break;

                case FLASH_State.WriteCommand:
                    unsafe
                    {
                        Unsafe.Write((Byte*)_flash + offset + (_flashBank * FLASH_BankSize), value);
                    }
                    _flashState = FLASH_State.WaitingForCommandArgument1;
                    break;

                case FLASH_State.SelectBankCommand:
                    if (offset == 0)
                    {
                        _flashBank = value;
                        _flashState = FLASH_State.WaitingForCommandArgument1;
                    }
                    break;
            }
        }

        internal override void Write16(UInt32 address, UInt16 value)
        {
            if (_flashState == FLASH_State.WriteCommand)
            {
                UInt32 offset = (address - FLASH_StartAddress) % FLASH_BankSize;
                value >>= 8 * (int)(address & 1);

                unsafe
                {
                    Unsafe.Write((Byte*)_flash + offset + (_flashBank * FLASH_BankSize), (Byte)value);
                }

                _flashState = FLASH_State.WaitingForCommandArgument1;
            }
        }

        internal override void Write32(UInt32 address, UInt32 value)
        {
            if (_flashState == FLASH_State.WriteCommand)
            {
                UInt32 offset = (address - FLASH_StartAddress) % FLASH_BankSize;
                value >>= 8 * (int)(address & 0b11);

                unsafe
                {
                    Unsafe.Write((Byte*)_flash + offset + (_flashBank * FLASH_BankSize), (Byte)value);
                }

                _flashState = FLASH_State.WaitingForCommandArgument1;
            }
        }
    }
}
