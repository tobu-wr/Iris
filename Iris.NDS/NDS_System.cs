namespace Iris.NDS
{
    public sealed partial class NDS_System : Common.System
    {
        private readonly CPU.CPU_Core _cpu;
        private readonly PPU _ppu;

        private bool _running;
        private bool _disposed;

        public NDS_System(PollInput_Delegate pollInputCallback, PresentFrame_Delegate presentFrameCallback)
        {
            CPU.CPU_Core.CallbackInterface cpuCallbackInterface = new(ReadMemory8, ReadMemory16, ReadMemory32, WriteMemory8, WriteMemory16, WriteMemory32, HandleSWI, HandleIRQ);
            _cpu = new(CPU.CPU_Core.Model.ARM946ES, cpuCallbackInterface);
            _ppu = new(presentFrameCallback);
        }

        public override void Dispose()
        {
            if (_disposed)
                return;

            // TODO

            _disposed = true;
        }

        public override void ResetState(bool skipIntro)
        {
            BIOS_Reset();

            _cpu.NIRQ = CPU.CPU_Core.Signal.High;
        }

        public override void LoadState(BinaryReader reader)
        {
            // TODO
        }

        public override void SaveState(BinaryWriter writer)
        {
            // TODO
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
                UInt64 cycles = _cpu.Step();

                for (UInt64 i = 0; i < cycles; ++i)
                    _ppu.Step();
            }
        }

        public override void Pause()
        {
            _running = false;
        }

        public override void SetKeyStatus(Key key, KeyStatus status)
        {
            // TODO
        }
    }
}
