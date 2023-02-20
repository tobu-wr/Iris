namespace Iris.Emulation.NDS
{
    internal sealed partial class Core
    {
        private readonly CPU.Core _cpu;
        private readonly PPU _ppu;

        private bool _running = false;

        internal Core(PPU.DrawFrame_Delegate drawFrameCallback)
        {
            CPU.Core.CallbackInterface cpuCallbackInterface = new()
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

            _cpu = new(CPU.Core.Architecture.ARMv5TE, cpuCallbackInterface);
            _ppu = new(drawFrameCallback);
        }

        internal void Reset()
        {
            BIOS_Reset();

            _cpu.NIRQ = CPU.Core.Signal.High;
        }

        internal bool IsRunning()
        {
            return _running;
        }

        internal void Run()
        {
            _running = true;

            while (_running)
            {
                _cpu.Step();
                _ppu.Step();
            }
        }

        internal void Pause()
        {
            _running = false;
        }
    }
}
