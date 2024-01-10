namespace Iris.GBA
{
    internal sealed class Communication
    {
        internal enum Register
        {
            SIODATA0,
            SIODATA1,
            SIODATA2,
            SIODATA3,

            SIOCNT,
            SIODATA_SEND,

            RCNT,

            JOYCNT,

            JOY_RECV_L,
            JOY_RECV_H,

            JOY_TRANS_L,
            JOY_TRANS_H,

            JOYSTAT
        }

        private UInt16 _SIODATA0; // SIOMULTI0 / SIODATA32_L
        private UInt16 _SIODATA1; // SIOMULTI1 / SIODATA32_H
        private UInt16 _SIODATA2; // SIOMULTI2
        private UInt16 _SIODATA3; // SIOMULTI3

        private UInt16 _SIOCNT;
        private UInt16 _SIODATA_SEND; // SIOMLT_SEND / SIODATA_8

        private UInt16 _RCNT;

        private UInt16 _JOYCNT;

        private UInt16 _JOY_RECV_L;
        private UInt16 _JOY_RECV_H;

        private UInt16 _JOY_TRANS_L;
        private UInt16 _JOY_TRANS_H;

        private UInt16 _JOYSTAT;

        private InterruptControl _interruptControl;

        internal void Initialize(InterruptControl interruptControl)
        {
            _interruptControl = interruptControl;
        }

        internal void ResetState()
        {
            _SIODATA0 = 0;
            _SIODATA1 = 0;
            _SIODATA2 = 0;
            _SIODATA3 = 0;

            _SIOCNT = 0;
            _SIODATA_SEND = 0;

            _RCNT = 0;

            _JOYCNT = 0;

            _JOY_RECV_L = 0;
            _JOY_RECV_H = 0;

            _JOY_TRANS_L = 0;
            _JOY_TRANS_H = 0;

            _JOYSTAT = 0;
        }

        internal void LoadState(BinaryReader reader)
        {
            _SIODATA0 = reader.ReadUInt16();
            _SIODATA1 = reader.ReadUInt16();
            _SIODATA2 = reader.ReadUInt16();
            _SIODATA3 = reader.ReadUInt16();

            _SIOCNT = reader.ReadUInt16();
            _SIODATA_SEND = reader.ReadUInt16();

            _RCNT = reader.ReadUInt16();

            _JOYCNT = reader.ReadUInt16();

            _JOY_RECV_L = reader.ReadUInt16();
            _JOY_RECV_H = reader.ReadUInt16();

            _JOY_TRANS_L = reader.ReadUInt16();
            _JOY_TRANS_H = reader.ReadUInt16();

            _JOYSTAT = reader.ReadUInt16();
        }

        internal void SaveState(BinaryWriter writer)
        {
            writer.Write(_SIODATA0);
            writer.Write(_SIODATA1);
            writer.Write(_SIODATA2);
            writer.Write(_SIODATA3);

            writer.Write(_SIOCNT);
            writer.Write(_SIODATA_SEND);

            writer.Write(_RCNT);

            writer.Write(_JOYCNT);

            writer.Write(_JOY_RECV_L);
            writer.Write(_JOY_RECV_H);

            writer.Write(_JOY_TRANS_L);
            writer.Write(_JOY_TRANS_H);

            writer.Write(_JOYSTAT);
        }

        internal UInt16 ReadRegister(Register register)
        {
            return register switch
            {
                Register.SIODATA0 => _SIODATA0,
                Register.SIODATA1 => _SIODATA1,
                Register.SIODATA2 => _SIODATA2,
                Register.SIODATA3 => _SIODATA3,

                Register.SIOCNT => _SIOCNT,
                Register.SIODATA_SEND => _SIODATA_SEND,

                Register.RCNT => _RCNT,

                Register.JOYCNT => _JOYCNT,

                Register.JOY_RECV_L => _JOY_RECV_L,
                Register.JOY_RECV_H => _JOY_RECV_H,

                Register.JOY_TRANS_L => _JOY_TRANS_L,
                Register.JOY_TRANS_H => _JOY_TRANS_H,

                Register.JOYSTAT => _JOYSTAT,

                // should never happen
                _ => throw new Exception("Iris.GBA.Communication: Register read error"),
            };
        }

        internal void WriteRegister(Register register, UInt16 value, Memory.RegisterWriteMode mode)
        {
            void CheckTransfer()
            {
                switch ((_RCNT >> 14) & 0b11)
                {
                    case 0b00:
                    case 0b01:
                        switch ((_SIOCNT >> 12) & 0b11)
                        {
                            case 0b00: // 8 bits normal serial communication
                            case 0b01: // 32 bits normal serial communication
                            case 0b10: // 16 bits multiplayer serial communication
                                if ((_SIOCNT & 0x0080) == 0x0080)
                                {
                                    _SIOCNT = (UInt16)(_SIOCNT & ~0x0080);

                                    if ((_SIOCNT & 0x4000) == 0x4000)
                                        _interruptControl.RequestInterrupt(InterruptControl.Interrupt.SIO);
                                }
                                break;

                            case 0b11:
                                throw new Exception("Iris.GBA.Communication: UART communication not implemented");
                        }
                        break;

                    case 0b10:
                        throw new Exception("Iris.GBA.Communication: General purpose communication not implemented");

                    case 0b11:
                        throw new Exception("Iris.GBA.Communication: JOY Bus communication not implemented");
                }
            }

            switch (register)
            {
                case Register.SIODATA0:
                    Memory.WriteRegisterHelper(ref _SIODATA0, value, mode);
                    break;
                case Register.SIODATA1:
                    Memory.WriteRegisterHelper(ref _SIODATA1, value, mode);
                    break;
                case Register.SIODATA2:
                    Memory.WriteRegisterHelper(ref _SIODATA2, value, mode);
                    break;
                case Register.SIODATA3:
                    Memory.WriteRegisterHelper(ref _SIODATA3, value, mode);
                    break;

                case Register.SIOCNT:
                    Memory.WriteRegisterHelper(ref _SIOCNT, value, mode);
                    CheckTransfer();
                    break;
                case Register.SIODATA_SEND:
                    Memory.WriteRegisterHelper(ref _SIODATA_SEND, value, mode);
                    break;

                case Register.RCNT:
                    Memory.WriteRegisterHelper(ref _RCNT, value, mode);
                    CheckTransfer();
                    break;

                case Register.JOYCNT:
                    Memory.WriteRegisterHelper(ref _JOYCNT, value, mode);
                    break;

                case Register.JOY_RECV_L:
                    Memory.WriteRegisterHelper(ref _JOY_RECV_L, value, mode);
                    break;
                case Register.JOY_RECV_H:
                    Memory.WriteRegisterHelper(ref _JOY_RECV_H, value, mode);
                    break;

                case Register.JOY_TRANS_L:
                    Memory.WriteRegisterHelper(ref _JOY_TRANS_L, value, mode);
                    break;
                case Register.JOY_TRANS_H:
                    Memory.WriteRegisterHelper(ref _JOY_TRANS_H, value, mode);
                    break;

                case Register.JOYSTAT:
                    Memory.WriteRegisterHelper(ref _JOYSTAT, value, mode);
                    break;

                // should never happen
                default:
                    throw new Exception("Iris.GBA.Communication: Register write error");
            }
        }
    }
}
