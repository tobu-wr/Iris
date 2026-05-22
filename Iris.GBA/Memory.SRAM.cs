namespace Iris.GBA
{
    internal sealed partial class Memory
    {
        private sealed class SRAM : BackupMemory
        {
            private const int KB = 1024;
            private readonly Common.MemoryBlock _memoryBlock = new(64 * KB);

            private const UInt32 StartAddress = 0x0e00_0000;
            private const UInt32 EndAddress = 0x1000_0000;

            internal SRAM(Memory memory)
            {
                const Flag flags = Flag.Read8 | Flag.Write8 | Flag.Mirrored;
                memory.Map(_memoryBlock.Data, _memoryBlock.Size, StartAddress, EndAddress, flags);
            }

            public override void Dispose()
            {
                _memoryBlock.Dispose();
            }

            internal override void ResetState()
            {
                _memoryBlock.Fill(0xff);
            }

            internal override void LoadState(BinaryReader reader)
            {
                _memoryBlock.LoadState(reader);
            }

            internal override void SaveState(BinaryWriter writer)
            {
                _memoryBlock.SaveState(writer);
            }

            internal override Byte Read8(UInt32 address)
            {
                UInt32 offset = (address - StartAddress) % (UInt32)_memoryBlock.Size;
                return _memoryBlock.Read<Byte>(offset);
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
                UInt32 offset = (address - StartAddress) % (UInt32)_memoryBlock.Size;
                _memoryBlock.Write(offset, value);
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
}
