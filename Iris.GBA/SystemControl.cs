namespace Iris.GBA
{
    internal sealed class SystemControl
    {
        internal UInt16 _WAITCNT;

        internal void Reset()
        {
            _WAITCNT = 0;
        }
    }
}
