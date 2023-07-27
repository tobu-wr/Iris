using Iris.Common;
using Iris.EmulationCore.Common;

namespace Iris.EmulationCore.NDS
{
    public sealed partial class Core : ICore
    {
        private readonly CPU _cpu;
        private readonly PPU _ppu;

        private bool _running = false;

        public Core(DrawFrame_Delegate drawFrameCallback)
        {
            CPU.CallbackInterface cpuCallbackInterface = new()
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

            _cpu = new(CPU.Architecture.ARMv5TE, cpuCallbackInterface);
            _ppu = new(drawFrameCallback);
        }

        public void Reset()
        {
            BIOS_Reset();

            _cpu.NIRQ = CPU.Signal.High;
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
                _cpu.Step();
                _ppu.Step();
            }
        }

        public void Pause()
        {
            _running = false;
        }

        public void SetKeyStatus(ICore.Key key, ICore.KeyStatus status)
        {
            // TODO
        }
    }
}
