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
        private readonly SystemControl _systemControl = new();
        private readonly InterruptControl _interruptControl = new();
        private readonly BIOS _bios = new();
        private readonly Memory _memory = new();
        private readonly PPU _ppu;

        private bool _running;

        public GBA_System(DrawFrame_Delegate drawFrame)
        {
            CPU_Core.CallbackInterface cpuCallbackInterface = new(_memory.ReadMemory8, _memory.ReadMemory16, _memory.ReadMemory32, _memory.WriteMemory8, _memory.WriteMemory16, _memory.WriteMemory32, _bios.HandleSWI, _bios.HandleIRQ);
            _cpu = new(CPU_Core.Model.ARM7TDMI, cpuCallbackInterface);

            PPU.CallbackInterface ppuCallbackInterface = new(drawFrame, () => _interruptControl.RequestInterrupt(Interrupt.VBlank));
            _ppu = new(_scheduler, ppuCallbackInterface);

            _interruptControl.Init(_cpu);
            _bios.Init(_cpu, _memory);
            _memory.Init(_communication, _timer, _sound, _dma, _keyInput, _systemControl, _interruptControl, _bios, _ppu);
        }

        public void Reset()
        {
            _scheduler.Reset();

            _communication.Reset();
            _timer.Reset();
            _sound.Reset();
            _dma.Reset();
            _keyInput.Reset();
            _systemControl.Reset();
            _interruptControl.Reset();
            _bios.Reset();
            _memory.Reset();
            _ppu.Reset();
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
