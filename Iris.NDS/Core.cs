using Iris.Common;

namespace Iris.NDS
{
    public sealed partial class Core : ISystemCore
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
                Byte cycles = _CPU.Step();

                for (Byte i = 0; i < cycles; ++i)
                    _PPU.Step();
            }
        }

        public void Pause()
        {
            _running = false;
        }

        public void SetKeyStatus(ISystemCore.Key key, ISystemCore.KeyStatus status)
        {
            // TODO
        }
    }
}
