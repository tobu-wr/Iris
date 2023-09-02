namespace Iris.GBA
{
    internal sealed class Sound
    {
        internal UInt16 _SOUND1CNT_L;
        internal UInt16 _SOUND1CNT_H;
        internal UInt16 _SOUND1CNT_X;

        internal UInt16 _SOUND2CNT_L;
        internal UInt16 _SOUND2CNT_H;

        internal UInt16 _SOUND3CNT_L;
        internal UInt16 _SOUND3CNT_H;
        internal UInt16 _SOUND3CNT_X;

        internal UInt16 _SOUND4CNT_L;
        internal UInt16 _SOUND4CNT_H;

        internal UInt16 _SOUNDCNT_L;
        internal UInt16 _SOUNDCNT_H;
        internal UInt16 _SOUNDCNT_X;

        internal UInt16 _SOUNDBIAS;

        internal UInt16 _WAVE_RAM0_L;
        internal UInt16 _WAVE_RAM0_H;

        internal UInt16 _WAVE_RAM1_L;
        internal UInt16 _WAVE_RAM1_H;

        internal UInt16 _WAVE_RAM2_L;
        internal UInt16 _WAVE_RAM2_H;

        internal UInt16 _WAVE_RAM3_L;
        internal UInt16 _WAVE_RAM3_H;

        internal UInt16 _FIFO_A_L;
        internal UInt16 _FIFO_A_H;

        internal UInt16 _FIFO_B_L;
        internal UInt16 _FIFO_B_H;

        internal void Reset()
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
    }
}
