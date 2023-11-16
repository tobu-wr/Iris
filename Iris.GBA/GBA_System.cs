using Iris.Common;
using Iris.CPU;
using static Iris.GBA.InterruptControl;

namespace Iris.GBA
{
    public sealed class GBA_System : Common.System
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
        private readonly Memory _memory = new();
        private readonly Video _video;

        private readonly BIOS _bios = new BIOS_LLE("D:\\dev\\Iris\\ROMs\\GBA\\gba_bios.bin");

        private bool _running;

        public GBA_System(DrawFrame_Delegate drawFrame)
        {
            CPU_Core.CallbackInterface cpuCallbackInterface = new(_memory.Read8, _memory.Read16, _memory.Read32, _memory.Write8, _memory.Write16, _memory.Write32, _bios.HandleSWI, _bios.HandleIRQ);
            _cpu = new(CPU_Core.Model.ARM7TDMI, cpuCallbackInterface);

            Video.CallbackInterface ppuCallbackInterface = new(drawFrame, () => _interruptControl.RequestInterrupt(Interrupt.VBlank));
            _video = new(_scheduler, ppuCallbackInterface);

            _interruptControl.Initialize(_cpu);
            _memory.Initialize(_communication, _timer, _sound, _dma, _keyInput, _systemControl, _interruptControl, _bios, _video);
            _video.Initialize(_memory);
            _bios.Initialize(_cpu, _memory);
        }

        public override void Reset()
        {
            _scheduler.Reset();

            _cpu.Reset();
            _communication.Reset();
            _timer.Reset();
            _sound.Reset();
            _dma.Reset();
            _keyInput.Reset();
            _systemControl.Reset();
            _interruptControl.Reset();
            _memory.Reset();
            _video.Reset();

            _bios.Reset();
        }

        public override void LoadROM(string filename)
        {
            _memory.LoadROM(filename);
        }

        public override bool IsRunning()
        {
            return _running;
        }

        public override void Run()
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

        public override void Pause()
        {
            _running = false;
        }

        public override void SetKeyStatus(Key key, KeyStatus status)
        {
            _keyInput.SetKeyStatus(key, status);
        }
    }
}
