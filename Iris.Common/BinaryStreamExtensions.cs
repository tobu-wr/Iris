using System.Runtime.InteropServices;

namespace Iris.Common
{
    public static class BinaryStreamExtensions
    {
        extension(BinaryReader reader)
        {
            public void ReadData(IntPtr destination, int size)
            {
                byte[] source = reader.ReadBytes(size);
                Marshal.Copy(source, 0, destination, size);
            }
        }

        extension(BinaryWriter writer)
        {
            public void WriteData(IntPtr source, int size)
            {
                byte[] destination = new byte[size];
                Marshal.Copy(source, destination, 0, size);
                writer.Write(destination);
            }
        }
    }
}
