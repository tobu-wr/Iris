using System.Runtime.CompilerServices;

namespace Iris.Common
{
    public static class Pointer
    {
        public static T Read<T>(IntPtr pointer, UInt32 offset)
        {
            unsafe
            {
                return Unsafe.Read<T>((Byte*)pointer + offset);
            }
        }

        public static void Write<T>(IntPtr pointer, UInt32 offset, T value)
        {
            unsafe
            {
                Unsafe.Write((Byte*)pointer + offset, value);
            }
        }
    }
}
