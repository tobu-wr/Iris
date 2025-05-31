using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Iris.GBA
{
    internal sealed class SRAM : Memory.BackupMemory
    {
        private const int KB = 1024;
        private const int Size = 64 * KB;
        private readonly IntPtr _data = Marshal.AllocHGlobal(Size);

        private const UInt32 StartAddress = 0x0e00_0000;
        private const UInt32 EndAddress = 0x1000_0000;

        private bool _disposed;

        internal SRAM(Memory memory)
        {
            const Memory.Flag flags = Memory.Flag.Read8 | Memory.Flag.Write8 | Memory.Flag.Mirrored;
            memory.Map(_data, Size, StartAddress, EndAddress, flags);
        }

        ~SRAM()
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
                NativeMemory.Fill((Byte*)_data, Size, 0xff);
            }
        }

        internal override void LoadState(BinaryReader reader)
        {
            byte[] data = reader.ReadBytes(Size);
            Marshal.Copy(data, 0, _data, Size);
        }

        internal override void SaveState(BinaryWriter writer)
        {
            byte[] data = new byte[Size];
            Marshal.Copy(_data, data, 0, Size);
            writer.Write(data);
        }

        internal override Byte Read8(UInt32 address)
        {
            UInt32 offset = (address - StartAddress) % Size;

            unsafe
            {
                return Unsafe.Read<Byte>((Byte*)_data + offset);
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
            UInt32 offset = (address - StartAddress) % Size;

            unsafe
            {
                Unsafe.Write((Byte*)_data + offset, value);
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
