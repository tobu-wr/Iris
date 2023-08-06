using Iris.Common;
using Iris.CPU;
using static Iris.Common.ISystem;
using static Iris.GBA.InterruptControl;

namespace Iris.GBA
{
    public sealed partial class GBA_System : ISystem
    {
        private readonly Scheduler _scheduler = new(1);

        private readonly CPU_Core _cpu;
        private readonly Communication _communication = new();
        private readonly Timer _timer = new();
        private readonly Sound _sound = new();
        private readonly DMA _dma = new();
        private readonly KeyInput _keyInput = new();
        private readonly InterruptControl _interruptControl;
        private readonly BIOS _bios;
        private readonly PPU _ppu;

        private UInt16 _WAITCNT;

        private bool _running;

        public GBA_System(DrawFrame_Delegate drawFrame)
        {
            CPU_Core.CallbackInterface cpuCallbackInterface = new()
            {
                ReadMemory8 = ReadMemory8,
                ReadMemory16 = ReadMemory16,
                ReadMemory32 = ReadMemory32,
                WriteMemory8 = WriteMemory8,
                WriteMemory16 = WriteMemory16,
                WriteMemory32 = WriteMemory32,
                HandleSWI = (UInt32 value) => _bios!.HandleSWI(value),
                HandleIRQ = () => _bios!.HandleIRQ()
            };

            PPU.CallbackInterface ppuCallbackInterface = new()
            {
                DrawFrame = drawFrame,
                RequestVBlankInterrupt = () => _interruptControl!.RequestInterrupt(Interrupt.VBlank)
            };

            _cpu = new(CPU_Core.Model.ARM7TDMI, cpuCallbackInterface);
            _interruptControl = new(_cpu);
            _bios = new(_cpu, this);
            _ppu = new(_scheduler, ppuCallbackInterface);

            InitPageTables();
        }

        public void Reset()
        {
            _scheduler.Reset();

            _communication.Reset();
            _timer.Reset();
            _sound.Reset();
            _dma.Reset();
            _keyInput.Reset();
            _interruptControl.Reset();
            _bios.Reset();
            _ppu.Reset();

            _WAITCNT = 0;
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

        public void SetKeyStatus(Key key, KeyStatus status)
        {
            _keyInput.SetKeyStatus(key, status);
        }
    }
}
