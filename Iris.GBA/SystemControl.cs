namespace Iris.GBA
{
    internal sealed class SystemControl
    {
        internal enum Register
        {
            WAITCNT,
            UNKNOWN_0
        }

        private UInt16 _WAITCNT;
        private UInt16 _UNKNOWN_0;

        internal void ResetState()
        {
            _WAITCNT = 0;
            _UNKNOWN_0 = 0;
        }

        internal void LoadState(BinaryReader reader)
        {
            _WAITCNT = reader.ReadUInt16();
            _UNKNOWN_0 = reader.ReadUInt16();
        }

        internal void SaveState(BinaryWriter writer)
        {
            writer.Write(_WAITCNT);
            writer.Write(_UNKNOWN_0);
        }

        internal UInt16 ReadRegister(Register register)
        {
            return register switch
            {
                Register.WAITCNT => _WAITCNT,
                Register.UNKNOWN_0 => (UInt16)(_UNKNOWN_0 & 0x00ff),

                // should never happen
                _ => throw new Exception("Iris.GBA.SystemControl: Register read error"),
            };
        }

        internal void WriteRegister(Register register, UInt16 value, Memory.RegisterWriteMode mode)
        {
            switch (register)
            {
                case Register.WAITCNT:
                    Memory.WriteRegisterHelper(ref _WAITCNT, value, mode);
                    break;
                case Register.UNKNOWN_0:
                    Memory.WriteRegisterHelper(ref _UNKNOWN_0, value, mode);
                    break;

                // should never happen
                default:
                    throw new Exception("Iris.GBA.SystemControl: Register write error");
            }
        }
    }
}
