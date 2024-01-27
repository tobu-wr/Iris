using Iris.CPU;

namespace Iris.NDS
{
    public sealed partial class NDS_System : Common.System
    {
        private readonly CPU_Core _cpu;
        private readonly PPU _ppu;

        private bool _running;
        private bool _disposed;

        public NDS_System(PollInput_Delegate pollInputCallback, DrawFrame_Delegate drawFrameCallback)
        {
            CPU_Core.CallbackInterface cpuCallbackInterface = new(ReadMemory8, ReadMemory16, ReadMemory32, WriteMemory8, WriteMemory16, WriteMemory32, HandleSWI, HandleIRQ);
            _cpu = new(CPU_Core.Model.ARM946ES, cpuCallbackInterface);
            _ppu = new(drawFrameCallback);
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                // TODO
            }

            _disposed = true;
        }

        public override void ResetState()
        {
            BIOS_Reset();

            _cpu.NIRQ = CPU_Core.Signal.High;
        }

        public override void LoadState(string filename)
        {
            // TODO
        }

        public override void SaveState(string filename)
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
                UInt32 cycles = _cpu.Step();

                for (UInt32 i = 0; i < cycles; ++i)
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
