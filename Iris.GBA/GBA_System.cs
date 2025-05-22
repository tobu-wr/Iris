using System.Security.Cryptography;

namespace Iris.GBA
{
    public sealed class GBA_System : Common.System
    {
        internal enum TaskId
        {
            // ---- Timer ----
            StartTimer_Channel0,
            StartTimer_Channel1,
            StartTimer_Channel2,
            StartTimer_Channel3,

            HandleTimerOverflow_Channel0,
            HandleTimerOverflow_Channel1,
            HandleTimerOverflow_Channel2,
            HandleTimerOverflow_Channel3,

            // ---- DMA ----
            StartDMA_Channel0,
            StartDMA_Channel1,
            StartDMA_Channel2,
            StartDMA_Channel3,

            PerformVBlankTransfers,
            PerformHBlankTransfers,
            PerformVideoTransfer,
            PerformVideoTransferEnd,

            // ---- KeyInput ----
            CheckKeyInterrupt,

            // ---- Video ----
            StartHBlank,
            StartScanline
        }

        private static readonly int s_taskIdCount = Enum.GetNames<TaskId>().Length;
        private readonly Common.Scheduler _scheduler = new(s_taskIdCount, 2 * s_taskIdCount);

        private readonly CPU.CPU_Core _cpu;
        private readonly Communication _communication = new();
        private readonly Timer _timer;
        private readonly Sound _sound = new();
        private readonly DMA _dma;
        private readonly KeyInput _keyInput;
        private readonly SystemControl _systemControl = new();
        private readonly InterruptControl _interruptControl = new();
        private readonly Memory _memory = new();
        private readonly Video _video;
        private readonly BIOS _bios = new();

        private string _romHash;
        private bool _running;

        private const string StateSaveMagic = "IRISGBA";
        private const int StateSaveVersion = 1;

        public GBA_System(PollInput_Delegate pollInputCallback, PresentFrame_Delegate presentFrameCallback)
        {
            CPU.CPU_Core.CallbackInterface cpuCallbackInterface = new
            (
                _memory.Read8,
                _memory.Read16,
                _memory.Read32,
                _memory.Write8,
                _memory.Write16,
                _memory.Write32,
                _bios.HandleSWI,
                _bios.HandleIRQ
            );

            _cpu = new(CPU.CPU_Core.Model.ARM7TDMI, cpuCallbackInterface);
            _timer = new(_scheduler);
            _dma = new(_scheduler);
            _keyInput = new(_scheduler, pollInputCallback);
            _video = new(_scheduler, presentFrameCallback);

            _communication.Initialize(_interruptControl);
            _timer.Initialize(_interruptControl);
            _dma.Initialize(_interruptControl, _memory);
            _keyInput.Initialize(_interruptControl);
            _interruptControl.Initialize(_cpu);
            _memory.Initialize(_communication, _timer, _sound, _dma, _keyInput, _systemControl, _interruptControl, _video);
            _video.Initialize(_dma, _interruptControl, _memory);
            _bios.Initialize(_cpu, _memory);
        }

        public override void Dispose()
        {
            _memory.Dispose();
            _video.Dispose();
            _bios.Dispose();
        }

        public override void ResetState(bool skipIntro)
        {
            _scheduler.ResetState();

            _cpu.ResetState();
            _communication.ResetState();
            _timer.ResetState();
            _sound.ResetState();
            _dma.ResetState();
            _keyInput.ResetState();
            _systemControl.ResetState();
            _interruptControl.ResetState();
            _memory.ResetState();
            _video.ResetState();

            _bios.Reset(skipIntro);
        }

        public override void LoadState(BinaryReader reader)
        {
            if (reader.ReadString() != StateSaveMagic)
                throw new Exception("Iris.GBA.GBA_System: Wrong state save magic");

            if (reader.ReadInt32() != StateSaveVersion)
                throw new Exception("Iris.GBA.GBA_System: Wrong state save version");

            if (reader.ReadString() != _romHash)
                throw new Exception("Iris.GBA.GBA_System: Wrong state save ROM hash");

            int dataLength = reader.ReadInt32();
            byte[] data = reader.ReadBytes(dataLength);

            if (reader.ReadString() != Convert.ToHexString(MD5.HashData(data)))
                throw new Exception("Iris.GBA.GBA_System: Wrong state save data hash");

            using MemoryStream dataStream = new(data, false);
            using BinaryReader dataReader = new(dataStream, System.Text.Encoding.UTF8, false);

            _scheduler.LoadState(dataReader);
            _cpu.LoadState(dataReader);
            _communication.LoadState(dataReader);
            _timer.LoadState(dataReader);
            _sound.LoadState(dataReader);
            _dma.LoadState(dataReader);
            _keyInput.LoadState(dataReader);
            _systemControl.LoadState(dataReader);
            _interruptControl.LoadState(dataReader);
            _memory.LoadState(dataReader);
            _video.LoadState(dataReader);
        }

        public override void SaveState(BinaryWriter writer)
        {
            writer.Write(StateSaveMagic);
            writer.Write(StateSaveVersion);
            writer.Write(_romHash);

            using MemoryStream dataStream = new();
            using BinaryWriter dataWriter = new(dataStream, System.Text.Encoding.UTF8, false);

            _scheduler.SaveState(dataWriter);
            _cpu.SaveState(dataWriter);
            _communication.SaveState(dataWriter);
            _timer.SaveState(dataWriter);
            _sound.SaveState(dataWriter);
            _dma.SaveState(dataWriter);
            _keyInput.SaveState(dataWriter);
            _systemControl.SaveState(dataWriter);
            _interruptControl.SaveState(dataWriter);
            _memory.SaveState(dataWriter);
            _video.SaveState(dataWriter);

            byte[] data = dataStream.GetBuffer();

            writer.Write(data.Length);
            writer.Write(data);
            writer.Write(Convert.ToHexString(MD5.HashData(data)));
        }

        public override UInt16[] GetFrameBuffer()
        {
            return _video.GetFrameBuffer();
        }

        public override void LoadROM(byte[] data)
        {
            _memory.LoadROM(data);
            _romHash = Convert.ToHexString(MD5.HashData(data));
        }

        public override void SetKeyStatus(Key key, KeyStatus status)
        {
            _keyInput.SetKeyStatus(key, status);
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
                UInt64 cycleCount = _cpu.Step();
                _scheduler.AdvanceCycleCounter(cycleCount);
            }
        }

        public override void Pause()
        {
            _running = false;
        }
    }
}
