using Iris.Common;
using Iris.CPU;

namespace Iris.NDS
{
    public sealed partial class NDS_System : ISystem
    {
        private readonly CPU_Core _cpu;
        private readonly PPU _ppu;

        private bool _running = false;

        public NDS_System(ISystem.DrawFrame_Delegate drawFrameCallback)
        {
            CPU_Core.CallbackInterface cpuCallbackInterface = new(ReadMemory8, ReadMemory16, ReadMemory32, WriteMemory8, WriteMemory16, WriteMemory32, HandleSWI, HandleIRQ);

            _cpu = new(CPU_Core.Model.ARM946ES, cpuCallbackInterface);
            _ppu = new(drawFrameCallback);
        }

        public void Reset()
        {
            BIOS_Reset();

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
                UInt32 cycles = _cpu.Step();

                for (UInt32 i = 0; i < cycles; ++i)
                    _ppu.Step();
            }
        }

        public void Pause()
        {
            _running = false;
        }

        public void SetKeyStatus(ISystem.Key key, ISystem.KeyStatus status)
        {
            // TODO
        }
    }
}
