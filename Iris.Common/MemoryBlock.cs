using System.Runtime.InteropServices;

namespace Iris.Common
{
    public class MemoryBlock(int size) : IDisposable
    {
        public readonly int Size = size;
        public readonly IntPtr Data = Marshal.AllocHGlobal(size);

        private bool _disposed;

        ~MemoryBlock()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            Marshal.FreeHGlobal(Data);

            GC.SuppressFinalize(this);
            _disposed = true;
        }

        public void Clear()
        {
            unsafe
            {
                NativeMemory.Clear((Byte*)Data, (nuint)Size);
            }
        }

        public void Fill(Byte value)
        {
            unsafe
            {
                NativeMemory.Fill((Byte*)Data, (nuint)Size, value);
            }
        }

        public void Fill(UInt32 offset, int size, Byte value)
        {
            unsafe
            {
                NativeMemory.Fill((Byte*)Data + offset, (nuint)size, value);
            }
        }

        public void CopyFrom(byte[] data)
        {
            Marshal.Copy(data, 0, Data, Size);
        }

        public void LoadState(BinaryReader reader)
        {
            byte[] buffer = reader.ReadBytes(Size);
            Marshal.Copy(buffer, 0, Data, Size);
        }

        public void SaveState(BinaryWriter writer)
        {
            byte[] buffer = new byte[Size];
            Marshal.Copy(Data, buffer, 0, Size);
            writer.Write(buffer);
        }

        public T Read<T>(UInt32 offset)
        {
            return Pointer.Read<T>(Data, offset);
        }

        public void Write<T>(UInt32 offset, T value)
        {
            Pointer.Write(Data, offset, value);
        }
    }
}
