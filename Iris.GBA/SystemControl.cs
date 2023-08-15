namespace Iris.GBA
{
    internal sealed class SystemControl
    {
        internal UInt16 _WAITCNT;
        internal Byte _POSTFLG;

        internal void Reset()
        {
            _WAITCNT = 0;
            _POSTFLG = 0;
        }
    }
}
