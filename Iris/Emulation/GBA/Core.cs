namespace Iris.Emulation.GBA
{
    internal sealed partial class Core
    {
        internal enum Keys
        {
            A = 0,
            B = 1,
            Select = 2,
            Start = 3,
            Right = 4,
            Left = 5,
            Up = 6,
            Down = 7,
            R = 8,
            L = 9,
        };

        private readonly CPU.Core _cpu;
        private readonly PPU _ppu;

        private UInt16 _SOUNDCNT_H;
        private UInt16 _SOUNDCNT_X;
        private UInt16 _SOUNDBIAS;
        private UInt16 _DMA0CNT_H;
        private UInt16 _DMA1SAD_L;
        private UInt16 _DMA1SAD_H;
        private UInt16 _DMA1DAD_L;
        private UInt16 _DMA1DAD_H;
        private UInt16 _DMA1CNT_L;
        private UInt16 _DMA1CNT_H;
        private UInt16 _DMA2SAD_H;
        private UInt16 _DMA2CNT_L;
        private UInt16 _DMA2CNT_H;
        private UInt16 _DMA3CNT_H;
        private UInt16 _TM0CNT_H;
        private UInt16 _TM1CNT_H;
        private UInt16 _TM2CNT_H;
        private UInt16 _TM3CNT_H;
        private UInt16 _SIOCNT;
        private UInt16 _KEYINPUT;
        private UInt16 _KEYCNT;
        private UInt16 _IE;
        private UInt16 _WAITCNT;
        private UInt16 _IME;

        private bool _running = false;

        internal Core(PPU.DrawFrame_Delegate drawFrameCallback)
        {
            CPU.Core.CallbackInterface cpuCallbackInterface = new()
            {
                ReadMemory8 = ReadMemory8,
                ReadMemory16 = ReadMemory16,
                ReadMemory32 = ReadMemory32,
                WriteMemory8 = WriteMemory8,
                WriteMemory16 = WriteMemory16,
                WriteMemory32 = WriteMemory32,
                HandleSWI = HandleSWI,
                HandleIRQ = HandleIRQ
            };

            _cpu = new(CPU.Core.Architecture.ARMv4T, cpuCallbackInterface);
            _ppu = new(drawFrameCallback);
        }

        internal void Init()
        {
            BIOS_Init();

            _SOUNDCNT_H = 0;
            _SOUNDCNT_X = 0;
            _SOUNDBIAS = 0;
            _DMA0CNT_H = 0;
            _DMA1SAD_L = 0;
            _DMA1SAD_H = 0;
            _DMA1DAD_L = 0;
            _DMA1DAD_H = 0;
            _DMA1CNT_L = 0;
            _DMA1CNT_H = 0;
            _DMA2SAD_H = 0;
            _DMA2CNT_L = 0;
            _DMA2CNT_H = 0;
            _DMA3CNT_H = 0;
            _TM0CNT_H = 0;
            _TM1CNT_H = 0;
            _TM2CNT_H = 0;
            _TM3CNT_H = 0;
            _SIOCNT = 0;
            _KEYINPUT = 0x03ff;
            _KEYCNT = 0;
            _IE = 0;
            _WAITCNT = 0;
            _IME = 0;

            _cpu.IRQPending = false;
        }

        internal bool IsRunning()
        {
            return _running;
        }

        internal void Run()
        {
            _running = true;

            while (_running)
            {
                _cpu.Step();
                _ppu.Step();
            }
        }

        internal void Pause()
        {
            _running = false;
        }

        internal void SetKeyStatus(Keys key, bool pressed)
        {
            int mask = 1 << (int)key;
            _KEYINPUT = (UInt16)(pressed ? (_KEYINPUT & ~mask) : (_KEYINPUT | mask));
        }
    }
}
