using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Iris.GBA
{
    internal sealed class FLASH : Memory.BackupMemory
    {
        private const int KB = 1024;
        private const int BankSize = 64 * KB;

        private readonly Size _size;
        private readonly IntPtr _data;

        private const UInt32 StartAddress = 0x0e00_0000;

        private enum State
        {
            Idle,
            StateAA,
            State55,
            WriteByte,
            SelectBank
        }

        private State _state;
        private bool _idMode;
        private bool _eraseCommand;
        private Byte _bank;

        private bool _disposed;

        internal enum Size
        {
            FLASH_64KB = 64 * KB,
            FLASH_128KB = 128 * KB
        }

        internal FLASH(Size size)
        {
            _size = size;
            _data = Marshal.AllocHGlobal((int)_size);
        }

        ~FLASH()
        {
            Dispose();
        }

        public override void Dispose()
        {
            if (_disposed)
                return;

            Marshal.FreeHGlobal(_data);

            GC.SuppressFinalize(this);
            _disposed = true;
        }

        internal override void ResetState()
        {
            unsafe
            {
                NativeMemory.Fill((Byte*)_data, (nuint)_size, 0xff);
            }

            _state = State.Idle;
            _idMode = false;
            _eraseCommand = false;
            _bank = 0;
        }

        internal override void LoadState(BinaryReader reader)
        {
            byte[] data = reader.ReadBytes((int)_size);
            Marshal.Copy(data, 0, _data, (int)_size);

            _state = (State)reader.ReadInt32();
            _idMode = reader.ReadBoolean();
            _eraseCommand = reader.ReadBoolean();
            _bank = reader.ReadByte();
        }

        internal override void SaveState(BinaryWriter writer)
        {
            byte[] data = new byte[(int)_size];
            Marshal.Copy(_data, data, 0, (int)_size);
            writer.Write(data);

            writer.Write((int)_state);
            writer.Write(_idMode);
            writer.Write(_eraseCommand);
            writer.Write(_bank);
        }

        internal override Byte Read8(UInt32 address)
        {
            UInt32 offset = (address - StartAddress) % BankSize;

            if (_idMode)
            {
                switch (offset)
                {
                    case 0:
                        return _size switch
                        {
                            Size.FLASH_64KB => 0x32, // Panasonic
                            Size.FLASH_128KB => 0x62, // Sanyo
                            _ => throw new UnreachableException()
                        };

                    case 1:
                        return _size switch
                        {
                            Size.FLASH_64KB => 0x1b, // Panasonic
                            Size.FLASH_128KB => 0x13, // Sanyo
                            _ => throw new UnreachableException()
                        };
                }
            }

            unsafe
            {
                return Unsafe.Read<Byte>((Byte*)_data + (_bank * BankSize) + offset);
            }
        }

        internal override UInt16 Read16(UInt32 address)
        {
            Byte value = Read8(address);
            return (UInt16)((value << 8) | value);
        }

        internal override UInt32 Read32(UInt32 address)
        {
            Byte value = Read8(address);
            return (UInt32)((value << 24) | (value << 16) | (value << 8) | value);
        }

        internal override void Write8(UInt32 address, Byte value)
        {
            UInt32 offset = (address - StartAddress) % BankSize;

            switch (_state)
            {
                case State.Idle:
                    if (offset == 0x5555 && value == 0xaa)
                        _state = State.StateAA;
                    break;

                case State.StateAA:
                    if (offset == 0x2aaa && value == 0x55)
                        _state = State.State55;
                    break;

                case State.State55:
                    if (offset == 0x5555)
                    {
                        switch (value)
                        {
                            case 0x90:
                                _idMode = true;
                                _state = State.Idle;
                                break;

                            case 0xf0:
                                _idMode = false;
                                _state = State.Idle;
                                break;

                            case 0x80:
                                _eraseCommand = true;
                                _state = State.Idle;
                                break;

                            case 0x10:
                                if (_eraseCommand)
                                {
                                    unsafe
                                    {
                                        NativeMemory.Fill((Byte*)_data, (nuint)_size, 0xff);
                                    }

                                    _eraseCommand = false;
                                    _state = State.Idle;
                                }
                                break;

                            case 0xa0:
                                _state = State.WriteByte;
                                break;

                            case 0xb0:
                                _state = State.SelectBank;
                                break;
                        }
                    }
                    else if ((offset & 0xfff) == 0 && value == 0x30 && _eraseCommand)
                    {
                        unsafe
                        {
                            NativeMemory.Fill((Byte*)_data + (_bank * BankSize) + offset, 0x1000, 0xff);
                        }

                        _eraseCommand = false;
                        _state = State.Idle;
                    }
                    break;

                case State.WriteByte:
                    unsafe
                    {
                        Unsafe.Write((Byte*)_data + (_bank * BankSize) + offset, value);
                    }

                    _state = State.Idle;
                    break;

                case State.SelectBank:
                    if (offset == 0)
                    {
                        _bank = (Byte)(value & 1);
                        _state = State.Idle;
                    }
                    break;
            }
        }

        internal override void Write16(UInt32 address, UInt16 value)
        {
            value >>= 8 * (int)(address & 1);
            Write8(address, (Byte)value);
        }

        internal override void Write32(UInt32 address, UInt32 value)
        {
            value >>= 8 * (int)(address & 0b11);
            Write8(address, (Byte)value);
        }
    }
}
