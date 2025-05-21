using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Iris.GBA
{
    internal sealed class SRAM : Memory.BackupMemory
    {
        private const int KB = 1024;
        private const int SRAM_Size = 64 * KB;
        private readonly IntPtr _sram = Marshal.AllocHGlobal(SRAM_Size);

        private const UInt32 SRAM_StartAddress = 0x0e00_0000;
        private const UInt32 SRAM_EndAddress = 0x1000_0000;

        private bool _disposed;

        internal SRAM(Memory memory)
        {
            memory.Map(_sram, SRAM_Size, SRAM_StartAddress, SRAM_EndAddress, Memory.Flag.Read8 | Memory.Flag.Write8 | Memory.Flag.Mirrored);
        }

        ~SRAM()
        {
            Dispose();
        }

        public override void Dispose()
        {
            if (_disposed)
                return;

            Marshal.FreeHGlobal(_sram);

            GC.SuppressFinalize(this);
            _disposed = true;
        }

        internal override void ResetState()
        {
            unsafe
            {
                NativeMemory.Fill((Byte*)_sram, SRAM_Size, 0xff);
            }
        }

        internal override void LoadState(BinaryReader reader)
        {
            byte[] data = reader.ReadBytes(SRAM_Size);
            Marshal.Copy(data, 0, _sram, SRAM_Size);
        }

        internal override void SaveState(BinaryWriter writer)
        {
            byte[] data = new byte[SRAM_Size];
            Marshal.Copy(_sram, data, 0, SRAM_Size);
            writer.Write(data);
        }

        internal override Byte Read8(UInt32 address)
        {
            // should never happen
            throw new Exception("Iris.GBA.SRAM: Read8 have been called");
        }

        internal override UInt16 Read16(UInt32 address)
        {
            Byte value = Read(address);
            return (UInt16)((value << 8) | value);
        }

        internal override UInt32 Read32(UInt32 address)
        {
            Byte value = Read(address);
            return (UInt32)((value << 24) | (value << 16) | (value << 8) | value);
        }

        internal override void Write8(UInt32 address, Byte value)
        {
            // should never happen
            throw new Exception("Iris.GBA.SRAM: Write8 have been called");
        }

        internal override void Write16(UInt32 address, UInt16 value)
        {
            value >>= 8 * (int)(address & 1);
            Write(address, (Byte)value);
        }

        internal override void Write32(UInt32 address, UInt32 value)
        {
            value >>= 8 * (int)(address & 0b11);
            Write(address, (Byte)value);
        }

        private Byte Read(UInt32 address)
        {
            UInt32 offset = (address - SRAM_StartAddress) % SRAM_Size;

            unsafe
            {
                return Unsafe.Read<Byte>((Byte*)_sram + offset);
            }
        }

        private void Write(UInt32 address, Byte value)
        {
            UInt32 offset = (address - SRAM_StartAddress) % SRAM_Size;

            unsafe
            {
                Unsafe.Write((Byte*)_sram + offset, (Byte)value);
            }
        }
    }
}
