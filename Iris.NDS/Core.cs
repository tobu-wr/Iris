using Iris.Common;
using Iris.CPU;

namespace Iris.NDS
{
    public sealed partial class Core : ISystem
    {
        private readonly CPU.CPU _cpu;
        private readonly PPU _ppu;

        private bool _running = false;

        public Core(DrawFrame_Delegate drawFrameCallback)
        {
            CPU.CPU.CallbackInterface cpuCallbackInterface = new()
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

            _cpu = new(CPU.CPU.Architecture.ARMv5TE, cpuCallbackInterface);
            _ppu = new(drawFrameCallback);
        }

        public void Reset()
        {
            BIOS_Reset();

            _cpu.NIRQ = CPU.CPU.Signal.High;
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
