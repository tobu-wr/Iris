namespace Iris.GBA
{
    internal sealed class Sound
    {
        internal enum Register
        {
            SOUND1CNT_L,
            SOUND1CNT_H,
            SOUND1CNT_X,

            SOUND2CNT_L,
            SOUND2CNT_H,

            SOUND3CNT_L,
            SOUND3CNT_H,
            SOUND3CNT_X,

            SOUND4CNT_L,
            SOUND4CNT_H,

            SOUNDCNT_L,
            SOUNDCNT_H,
            SOUNDCNT_X,

            SOUNDBIAS,

            WAVE_RAM0_L,
            WAVE_RAM0_H,

            WAVE_RAM1_L,
            WAVE_RAM1_H,

            WAVE_RAM2_L,
            WAVE_RAM2_H,

            WAVE_RAM3_L,
            WAVE_RAM3_H,

            FIFO_A_L,
            FIFO_A_H,

            FIFO_B_L,
            FIFO_B_H
        }

        private UInt16 _SOUND1CNT_L;
        private UInt16 _SOUND1CNT_H;
        private UInt16 _SOUND1CNT_X;

        private UInt16 _SOUND2CNT_L;
        private UInt16 _SOUND2CNT_H;

        private UInt16 _SOUND3CNT_L;
        private UInt16 _SOUND3CNT_H;
        private UInt16 _SOUND3CNT_X;

        private UInt16 _SOUND4CNT_L;
        private UInt16 _SOUND4CNT_H;

        private UInt16 _SOUNDCNT_L;
        private UInt16 _SOUNDCNT_H;
        private UInt16 _SOUNDCNT_X;

        private UInt16 _SOUNDBIAS;

        private UInt16 _WAVE_RAM0_L;
        private UInt16 _WAVE_RAM0_H;

        private UInt16 _WAVE_RAM1_L;
        private UInt16 _WAVE_RAM1_H;

        private UInt16 _WAVE_RAM2_L;
        private UInt16 _WAVE_RAM2_H;

        private UInt16 _WAVE_RAM3_L;
        private UInt16 _WAVE_RAM3_H;

        private UInt16 _FIFO_A_L;
        private UInt16 _FIFO_A_H;

        private UInt16 _FIFO_B_L;
        private UInt16 _FIFO_B_H;

        internal void ResetState()
        {
            _SOUND1CNT_L = 0;
            _SOUND1CNT_H = 0;
            _SOUND1CNT_X = 0;

            _SOUND2CNT_L = 0;
            _SOUND2CNT_H = 0;

            _SOUND3CNT_L = 0;
            _SOUND3CNT_H = 0;
            _SOUND3CNT_X = 0;

            _SOUND4CNT_L = 0;
            _SOUND4CNT_H = 0;

            _SOUNDCNT_L = 0;
            _SOUNDCNT_H = 0;
            _SOUNDCNT_X = 0;

            _SOUNDBIAS = 0;

            _WAVE_RAM0_L = 0;
            _WAVE_RAM0_H = 0;

            _WAVE_RAM1_L = 0;
            _WAVE_RAM1_H = 0;

            _WAVE_RAM2_L = 0;
            _WAVE_RAM2_H = 0;

            _WAVE_RAM3_L = 0;
            _WAVE_RAM3_H = 0;

            _FIFO_A_L = 0;
            _FIFO_A_H = 0;

            _FIFO_B_L = 0;
            _FIFO_B_H = 0;
        }

        internal void LoadState(BinaryReader reader)
        {
            _SOUND1CNT_L = reader.ReadUInt16();
            _SOUND1CNT_H = reader.ReadUInt16();
            _SOUND1CNT_X = reader.ReadUInt16();

            _SOUND2CNT_L = reader.ReadUInt16();
            _SOUND2CNT_H = reader.ReadUInt16();

            _SOUND3CNT_L = reader.ReadUInt16();
            _SOUND3CNT_H = reader.ReadUInt16();
            _SOUND3CNT_X = reader.ReadUInt16();

            _SOUND4CNT_L = reader.ReadUInt16();
            _SOUND4CNT_H = reader.ReadUInt16();

            _SOUNDCNT_L = reader.ReadUInt16();
            _SOUNDCNT_H = reader.ReadUInt16();
            _SOUNDCNT_X = reader.ReadUInt16();

            _SOUNDBIAS = reader.ReadUInt16();

            _WAVE_RAM0_L = reader.ReadUInt16();
            _WAVE_RAM0_H = reader.ReadUInt16();

            _WAVE_RAM1_L = reader.ReadUInt16();
            _WAVE_RAM1_H = reader.ReadUInt16();

            _WAVE_RAM2_L = reader.ReadUInt16();
            _WAVE_RAM2_H = reader.ReadUInt16();

            _WAVE_RAM3_L = reader.ReadUInt16();
            _WAVE_RAM3_H = reader.ReadUInt16();

            _FIFO_A_L = reader.ReadUInt16();
            _FIFO_A_H = reader.ReadUInt16();

            _FIFO_B_L = reader.ReadUInt16();
            _FIFO_B_H = reader.ReadUInt16();
        }

        internal void SaveState(BinaryWriter writer)
        {
            writer.Write(_SOUND1CNT_L);
            writer.Write(_SOUND1CNT_H);
            writer.Write(_SOUND1CNT_X);

            writer.Write(_SOUND2CNT_L);
            writer.Write(_SOUND2CNT_H);

            writer.Write(_SOUND3CNT_L);
            writer.Write(_SOUND3CNT_H);
            writer.Write(_SOUND3CNT_X);

            writer.Write(_SOUND4CNT_L);
            writer.Write(_SOUND4CNT_H);

            writer.Write(_SOUNDCNT_L);
            writer.Write(_SOUNDCNT_H);
            writer.Write(_SOUNDCNT_X);

            writer.Write(_SOUNDBIAS);

            writer.Write(_WAVE_RAM0_L);
            writer.Write(_WAVE_RAM0_H);

            writer.Write(_WAVE_RAM1_L);
            writer.Write(_WAVE_RAM1_H);

            writer.Write(_WAVE_RAM2_L);
            writer.Write(_WAVE_RAM2_H);

            writer.Write(_WAVE_RAM3_L);
            writer.Write(_WAVE_RAM3_H);

            writer.Write(_FIFO_A_L);
            writer.Write(_FIFO_A_H);

            writer.Write(_FIFO_B_L);
            writer.Write(_FIFO_B_H);
        }

        internal UInt16 ReadRegister(Register register)
        {
            return register switch
            {
                Register.SOUND1CNT_L => _SOUND1CNT_L,
                Register.SOUND1CNT_H => (UInt16)(_SOUND1CNT_H & 0xffc0),
                Register.SOUND1CNT_X => (UInt16)(_SOUND1CNT_X & 0x4000),

                Register.SOUND2CNT_L => (UInt16)(_SOUND2CNT_L & 0xffc0),
                Register.SOUND2CNT_H => (UInt16)(_SOUND2CNT_H & 0x4000),

                Register.SOUND3CNT_L => _SOUND3CNT_L,
                Register.SOUND3CNT_H => (UInt16)(_SOUND3CNT_H & 0xe000),
                Register.SOUND3CNT_X => (UInt16)(_SOUND3CNT_X & 0x4000),

                Register.SOUND4CNT_L => (UInt16)(_SOUND4CNT_L & 0xff00),
                Register.SOUND4CNT_H => (UInt16)(_SOUND4CNT_H & 0x40ff),

                Register.SOUNDCNT_L => _SOUNDCNT_L,
                Register.SOUNDCNT_H => (UInt16)(_SOUNDCNT_H & 0x770f),
                Register.SOUNDCNT_X => _SOUNDCNT_X,

                Register.SOUNDBIAS => _SOUNDBIAS,

                Register.WAVE_RAM0_L => _WAVE_RAM0_L,
                Register.WAVE_RAM0_H => _WAVE_RAM0_H,

                Register.WAVE_RAM1_L => _WAVE_RAM1_L,
                Register.WAVE_RAM1_H => _WAVE_RAM1_H,

                Register.WAVE_RAM2_L => _WAVE_RAM2_L,
                Register.WAVE_RAM2_H => _WAVE_RAM2_H,

                Register.WAVE_RAM3_L => _WAVE_RAM3_L,
                Register.WAVE_RAM3_H => _WAVE_RAM3_H,

                // should never happen
                _ => throw new Exception("Iris.GBA.Sound: Register read error"),
            };
        }

        internal void WriteRegister(Register register, UInt16 value, Memory.RegisterWriteMode mode)
        {
            switch (register)
            {
                case Register.SOUND1CNT_L:
                    Memory.WriteRegisterHelper(ref _SOUND1CNT_L, (UInt16)(value & 0x007f), mode);
                    break;
                case Register.SOUND1CNT_H:
                    Memory.WriteRegisterHelper(ref _SOUND1CNT_H, value, mode);
                    break;
                case Register.SOUND1CNT_X:
                    Memory.WriteRegisterHelper(ref _SOUND1CNT_X, (UInt16)(value & 0xc7ff), mode);
                    break;

                case Register.SOUND2CNT_L:
                    Memory.WriteRegisterHelper(ref _SOUND2CNT_L, value, mode);
                    break;
                case Register.SOUND2CNT_H:
                    Memory.WriteRegisterHelper(ref _SOUND2CNT_H, (UInt16)(value & 0xc7ff), mode);
                    break;

                case Register.SOUND3CNT_L:
                    Memory.WriteRegisterHelper(ref _SOUND3CNT_L, (UInt16)(value & 0x00e0), mode);
                    break;
                case Register.SOUND3CNT_H:
                    Memory.WriteRegisterHelper(ref _SOUND3CNT_H, (UInt16)(value & 0xe0ff), mode);
                    break;
                case Register.SOUND3CNT_X:
                    Memory.WriteRegisterHelper(ref _SOUND3CNT_X, (UInt16)(value & 0xc7ff), mode);
                    break;

                case Register.SOUND4CNT_L:
                    Memory.WriteRegisterHelper(ref _SOUND4CNT_L, (UInt16)(value & 0xff3f), mode);
                    break;
                case Register.SOUND4CNT_H:
                    Memory.WriteRegisterHelper(ref _SOUND4CNT_H, (UInt16)(value & 0xc0ff), mode);
                    break;

                case Register.SOUNDCNT_L:
                    Memory.WriteRegisterHelper(ref _SOUNDCNT_L, (UInt16)(value & 0xff77), mode);
                    break;
                case Register.SOUNDCNT_H:
                    Memory.WriteRegisterHelper(ref _SOUNDCNT_H, (UInt16)(value & 0xff0f), mode);
                    break;
                case Register.SOUNDCNT_X:
                    Memory.WriteRegisterHelper(ref _SOUNDCNT_X, (UInt16)((value & 0x0080) | (_SOUNDCNT_X & 0x000f)), mode);
                    break;

                case Register.SOUNDBIAS:
                    Memory.WriteRegisterHelper(ref _SOUNDBIAS, (UInt16)(value & 0xc3ff), mode);
                    break;

                case Register.WAVE_RAM0_L:
                    Memory.WriteRegisterHelper(ref _WAVE_RAM0_L, value, mode);
                    break;
                case Register.WAVE_RAM0_H:
                    Memory.WriteRegisterHelper(ref _WAVE_RAM0_H, value, mode);
                    break;

                case Register.WAVE_RAM1_L:
                    Memory.WriteRegisterHelper(ref _WAVE_RAM1_L, value, mode);
                    break;
                case Register.WAVE_RAM1_H:
                    Memory.WriteRegisterHelper(ref _WAVE_RAM1_H, value, mode);
                    break;

                case Register.WAVE_RAM2_L:
                    Memory.WriteRegisterHelper(ref _WAVE_RAM2_L, value, mode);
                    break;
                case Register.WAVE_RAM2_H:
                    Memory.WriteRegisterHelper(ref _WAVE_RAM2_H, value, mode);
                    break;

                case Register.WAVE_RAM3_L:
                    Memory.WriteRegisterHelper(ref _WAVE_RAM3_L, value, mode);
                    break;
                case Register.WAVE_RAM3_H:
                    Memory.WriteRegisterHelper(ref _WAVE_RAM3_H, value, mode);
                    break;

                case Register.FIFO_A_L:
                    Memory.WriteRegisterHelper(ref _FIFO_A_L, value, mode);
                    break;
                case Register.FIFO_A_H:
                    Memory.WriteRegisterHelper(ref _FIFO_A_H, value, mode);
                    break;

                case Register.FIFO_B_L:
                    Memory.WriteRegisterHelper(ref _FIFO_B_L, value, mode);
                    break;
                case Register.FIFO_B_H:
                    Memory.WriteRegisterHelper(ref _FIFO_B_H, value, mode);
                    break;

                // should never happen
                default:
                    throw new Exception("Iris.GBA.Sound: Register write error");
            }
        }
    }
}
