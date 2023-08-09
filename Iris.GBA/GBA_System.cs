using Iris.Common;
using Iris.CPU;
using static Iris.Common.ISystem;
using static Iris.GBA.InterruptControl;

namespace Iris.GBA
{
    public sealed class GBA_System : ISystem
    {
        private readonly Scheduler _scheduler = new(2);

        private readonly CPU_Core _cpu;
        private readonly Communication _communication = new();
        private readonly Timer _timer = new();
        private readonly Sound _sound = new();
        private readonly DMA _dma = new();
        private readonly KeyInput _keyInput = new();
        private readonly InterruptControl _interruptControl = new();
        private readonly BIOS _bios = new();
        private readonly Memory _memory = new();
        private readonly PPU _ppu;

        internal UInt16 _WAITCNT;

        private bool _running;

        public GBA_System(DrawFrame_Delegate drawFrame)
        {
            CPU_Core.CallbackInterface cpuCallbackInterface = new()
            {
                ReadMemory8 = _memory.ReadMemory8,
                ReadMemory16 = _memory.ReadMemory16,
                ReadMemory32 = _memory.ReadMemory32,
                WriteMemory8 = _memory.WriteMemory8,
                WriteMemory16 = _memory.WriteMemory16,
                WriteMemory32 = _memory.WriteMemory32,
                HandleSWI = _bios.HandleSWI,
                HandleIRQ = _bios.HandleIRQ
            };

            _cpu = new(CPU_Core.Model.ARM7TDMI, cpuCallbackInterface);

            PPU.CallbackInterface ppuCallbackInterface = new()
            {
                DrawFrame = drawFrame,
                RequestVBlankInterrupt = () => _interruptControl.RequestInterrupt(Interrupt.VBlank)
            };

            _ppu = new(_scheduler, ppuCallbackInterface);

            _interruptControl.Init(_cpu);
            _bios.Init(_cpu, _memory);
            _memory.Init(_communication, _timer, _sound, _dma, _keyInput, _interruptControl, _bios, _ppu, this);
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

        public void LoadROM(string filename)
        {
            _memory.LoadROM(filename);
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
