using Iris.Common;
using Iris.CPU;

namespace Iris.GBA
{
    public sealed partial class GBA_System : ISystem
    {
        private readonly Scheduler _scheduler = new(1);

        private readonly CPU_Core _cpu;
        private readonly Communication _communication = new();
        private readonly Timer _timer = new();
        private readonly Sound _sound = new();
        private readonly PPU _ppu;

        private UInt16 _WAITCNT;

        private bool _running;

        public GBA_System(ISystem.DrawFrame_Delegate drawFrame)
        {
            CPU_Core.CallbackInterface cpuCallbackInterface = new()
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

            PPU.CallbackInterface ppuCallbackInterface = new()
            {
                DrawFrame = drawFrame,
                RequestVBlankInterrupt = () => RequestInterrupt(Interrupt.VBlank)
            };

            _cpu = new(CPU_Core.Model.ARM7TDMI, cpuCallbackInterface);
            _ppu = new(_scheduler, ppuCallbackInterface);

            InitPageTables();
        }

        public void Reset()
        {
            _scheduler.Reset();

            _communication.Reset();
            _timer.Reset();
            _sound.Reset();
            _ppu.Reset();
            BIOS_Reset();

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
            _KEYINPUT = 0x03ff;
            _KEYCNT = 0;
            _IE = 0;
            _IF = 0;
            _WAITCNT = 0;
            _IME = 0;

            _cpu.NIRQ = CPU_Core.Signal.High;
        }

        public bool IsRunning()
        {
            return _running;
        }

        public void Run()
        {
            _running = true;

            while (_running)
            {
                while (!_scheduler.HasTaskReady())
                {
                    UInt32 cycleCount = _cpu.Step();
                    _scheduler.AdvanceCycleCounter(cycleCount);
                }

                _scheduler.ProcessTasks();
            }
        }

        public void Pause()
        {
            _running = false;
        }
    }
}
