using System.Collections.Frozen;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Iris.UserInterface
{
    internal partial class MainWindow : Form
    {
        private static readonly FrozenDictionary<Keyboard.Key, Common.System.Key> s_keyboardMapping = new Dictionary<Keyboard.Key, Common.System.Key>()
        {
            { Keyboard.Key.A, Common.System.Key.A },
            { Keyboard.Key.Z, Common.System.Key.B },
            { Keyboard.Key.Space, Common.System.Key.Select },
            { Keyboard.Key.Return, Common.System.Key.Start },
            { Keyboard.Key.Right, Common.System.Key.Right },
            { Keyboard.Key.Left, Common.System.Key.Left },
            { Keyboard.Key.Up, Common.System.Key.Up },
            { Keyboard.Key.Down, Common.System.Key.Down },
            { Keyboard.Key.S, Common.System.Key.R },
            { Keyboard.Key.Q, Common.System.Key.L },
            { Keyboard.Key.E, Common.System.Key.X },
            { Keyboard.Key.R, Common.System.Key.Y },
        }.ToFrozenDictionary();

        private static readonly FrozenDictionary<XboxController.Button, Common.System.Key> s_xboxControllerMapping = new Dictionary<XboxController.Button, Common.System.Key>()
        {
            { XboxController.Button.A, Common.System.Key.A },
            { XboxController.Button.B, Common.System.Key.B },
            { XboxController.Button.Back, Common.System.Key.Select },
            { XboxController.Button.Start, Common.System.Key.Start },
            { XboxController.Button.DPadRight, Common.System.Key.Right },
            { XboxController.Button.DPadLeft, Common.System.Key.Left },
            { XboxController.Button.DPadUp, Common.System.Key.Up },
            { XboxController.Button.DPadDown, Common.System.Key.Down },
            { XboxController.Button.RightShoulder, Common.System.Key.R },
            { XboxController.Button.LeftShoulder, Common.System.Key.L },
            { XboxController.Button.X, Common.System.Key.X },
            { XboxController.Button.Y, Common.System.Key.Y },
        }.ToFrozenDictionary();

        private Common.System _system;
        private Task _systemTask;

        private readonly Keyboard _keyboard;
        private readonly XboxController _xboxController;

        private FormWindowState _previousWindowState;

        private int _framerateCounter;
        private readonly System.Windows.Forms.Timer _framerateCounterTimer = new();
        private readonly Stopwatch _framerateCounterStopwatch = new();

        private bool _framerateLimiterEnabled = true;
        private readonly Stopwatch _framerateLimiterStopwatch = Stopwatch.StartNew();
        private long _framerateLimiterExtraSleepTime;

        [StructLayout(LayoutKind.Sequential)]
        private readonly struct TimeCaps
        {
            internal readonly uint _periodMin;
            private readonly uint _periodMax; // not used
        }

        private readonly TimeCaps _timeCaps = new();

        [LibraryImport("winmm.dll", EntryPoint = "timeGetDevCaps")]
        private static partial uint TimeGetDevCaps(ref TimeCaps tc, uint size);

        [LibraryImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
        private static partial uint TimeBeginPeriod(uint period);

        [LibraryImport("winmm.dll", EntryPoint = "timeEndPeriod")]
        private static partial uint TimeEndPeriod(uint period);

        internal MainWindow(string[] args)
        {
            TimeGetDevCaps(ref _timeCaps, (uint)Unsafe.SizeOf<TimeCaps>());

            _ = TimeBeginPeriod(_timeCaps._periodMin);

            InitializeComponent();

#if DEBUG
            Text += " (DEBUG)";
#elif RELEASE_DEV
            Text += " (DEV)";
#endif

            _keyboard = new(Keyboard_KeyDown, Keyboard_KeyUp);
            _xboxController = new(XboxController_ButtonDown, XboxController_ButtonUp);

            _framerateCounterTimer.Interval = 1000;
            _framerateCounterTimer.Tick += FramerateCounterTimer_Tick;

            if (args.Length > 0)
                LoadROM(args[0]);
        }

        ~MainWindow()
        {
            _ = TimeEndPeriod(_timeCaps._periodMin);
        }

        private void PollInput()
        {
            _keyboard.PollInput();
            _xboxController.PollInput();
        }

        private void DrawFrame(UInt16[] frameBuffer)
        {
            const int ScreenWidth = 240;
            const int ScreenHeight = 160;
            const int PixelCount = ScreenWidth * ScreenHeight;
            const PixelFormat PixelFormat = PixelFormat.Format16bppRgb555;

            Bitmap bitmap = new(ScreenWidth, ScreenHeight, PixelFormat);
            BitmapData data = bitmap.LockBits(new Rectangle(0, 0, ScreenWidth, ScreenHeight), ImageLockMode.WriteOnly, PixelFormat);

            Int16[] buffer = new Int16[PixelCount];

            for (int i = 0; i < PixelCount; ++i)
            {
                UInt16 gbaColor = frameBuffer[i]; // BGR format
                Byte red = (Byte)((gbaColor >> 0) & 0x1f);
                Byte green = (Byte)((gbaColor >> 5) & 0x1f);
                Byte blue = (Byte)((gbaColor >> 10) & 0x1f);
                buffer[i] = (Int16)((red << 10) | (green << 5) | blue);
            }

            Marshal.Copy(buffer, 0, data.Scan0, PixelCount);
            bitmap.UnlockBits(data);

            screenBox.Invoke(() => screenBox.Image = bitmap);
            screenBox.Invalidate();

            Interlocked.Increment(ref _framerateCounter);

            if (_framerateLimiterEnabled)
            {
                const double TargetFrameRate = 59.737411711095921;

                long targetFrameDuration = (long)Math.Round(Stopwatch.Frequency / TargetFrameRate, MidpointRounding.AwayFromZero);
                long frameDuration = _framerateLimiterStopwatch.ElapsedTicks;

                if (frameDuration < targetFrameDuration)
                {
                    long sleepTime = targetFrameDuration - frameDuration;

                    if (sleepTime > _framerateLimiterExtraSleepTime)
                    {
                        sleepTime -= _framerateLimiterExtraSleepTime;
                        Thread.Sleep((int)Math.Round(1000.0 * sleepTime / Stopwatch.Frequency, MidpointRounding.AwayFromZero));
                        _framerateLimiterExtraSleepTime = _framerateLimiterStopwatch.ElapsedTicks - frameDuration - sleepTime;
                    }
                    else
                    {
                        _framerateLimiterExtraSleepTime -= sleepTime;
                    }
                }

                _framerateLimiterStopwatch.Restart();
            }
        }

        private void LoadROM(string fileName)
        {
            Common.System system = fileName.EndsWith(".gba") ? new GBA.GBA_System(PollInput, DrawFrame) : new NDS.NDS_System(PollInput, DrawFrame);

            try
            {
                system.LoadROM(fileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                system.Dispose();
                return;
            }

            _system?.Dispose();
            _system = system;
            _system.ResetState();

            loadStateToolStripMenuItem.Enabled = true;
            saveStateToolStripMenuItem.Enabled = true;

            runToolStripMenuItem.Enabled = true;
            pauseToolStripMenuItem.Enabled = false;
            resetToolStripMenuItem.Enabled = true;

            statusToolStripStatusLabel.Text = "Paused";
        }

        private void Run()
        {
            _framerateCounter = 0;
            _framerateCounterTimer.Start();
            _framerateCounterStopwatch.Restart();

            runToolStripMenuItem.Enabled = false;
            pauseToolStripMenuItem.Enabled = true;

            statusToolStripStatusLabel.Text = "Running";

            _systemTask = Task.Run(() =>
            {
                try
                {
                    _system.Run();
                }
                catch (Exception ex)
                {
                    _system.Pause();
                    _system.ResetState();

                    _framerateCounterTimer.Stop();

                    runToolStripMenuItem.Enabled = true;
                    pauseToolStripMenuItem.Enabled = false;

                    statusToolStripStatusLabel.Text = "Paused";
                    fpsToolStripStatusLabel.Text = "FPS: 0,00";

                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            });
        }

        private void Pause()
        {
            _system.Pause();

            while (!_systemTask.IsCompleted)
                Application.DoEvents();

            _framerateCounterTimer.Stop();

            runToolStripMenuItem.Enabled = true;
            pauseToolStripMenuItem.Enabled = false;

            statusToolStripStatusLabel.Text = "Paused";
            fpsToolStripStatusLabel.Text = "FPS: 0,00";
        }

        private void LoadROMToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool running = (_system != null) && _system.IsRunning();

            if (running)
                Pause();

            OpenFileDialog dialog = new()
            {
#if RELEASE_FINAL
                Filter = "GBA files (*.gba)|*.gba|All ROM files (*.gba)|*.gba"
#else
                Filter = "GBA files (*.gba)|*.gba|NDS files (*.nds)|*.nds|All ROM files (*.gba;*.nds)|*.gba;*.nds"
#endif
            };

            if (dialog.ShowDialog() == DialogResult.OK)
                LoadROM(dialog.FileName);

            if (running)
                Run();
        }

        private void LoadStateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool running = _system.IsRunning();

            if (running)
                Pause();

            OpenFileDialog dialog = new()
            {
                Filter = (_system is GBA.GBA_System) ? "GBA State Save files (*.gss)|*.gss" : "NDS State Save files (*.nss)|*.nss"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    _system.LoadState(dialog.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            if (running)
                Run();
        }

        private void SaveStateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool running = _system.IsRunning();

            if (running)
                Pause();

            SaveFileDialog dialog = new()
            {
                Filter = (_system is GBA.GBA_System) ? "GBA State Save files (*.gss)|*.gss" : "NDS State Save files (*.nss)|*.nss"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
                _system.SaveState(dialog.FileName);

            if (running)
                Run();
        }

        private void QuitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void FullScreenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (fullScreenToolStripMenuItem.Checked)
            {
                FormBorderStyle = FormBorderStyle.None;
                _previousWindowState = WindowState;
                WindowState = FormWindowState.Normal; // mandatory
                WindowState = FormWindowState.Maximized;
            }
            else
            {
                FormBorderStyle = FormBorderStyle.Sizable;
                WindowState = _previousWindowState;
            }
        }

        private void RunToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Run();
        }

        private void PauseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Pause();
        }

        private void ResetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool running = _system.IsRunning();

            if (running)
                Pause();

            _system.ResetState();

            if (running)
                Run();
        }

        private void LimitFramerateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _framerateLimiterEnabled = limitFramerateToolStripMenuItem.Checked;
        }

        private void Keyboard_KeyDown(Keyboard.Key key)
        {
            if (s_keyboardMapping.TryGetValue(key, out Common.System.Key value))
                _system.SetKeyStatus(value, Common.System.KeyStatus.Input);
        }

        private void Keyboard_KeyUp(Keyboard.Key key)
        {
            if (s_keyboardMapping.TryGetValue(key, out Common.System.Key value))
                _system.SetKeyStatus(value, Common.System.KeyStatus.NoInput);
        }

        private void XboxController_ButtonDown(XboxController.Button button)
        {
            if (s_xboxControllerMapping.TryGetValue(button, out Common.System.Key value))
                _system.SetKeyStatus(value, Common.System.KeyStatus.Input);
        }

        private void XboxController_ButtonUp(XboxController.Button button)
        {
            if (s_xboxControllerMapping.TryGetValue(button, out Common.System.Key value))
                _system.SetKeyStatus(value, Common.System.KeyStatus.NoInput);
        }

        private void FramerateCounterTimer_Tick(object sender, EventArgs e)
        {
            double fps = Math.Round((double)_framerateCounter * Stopwatch.Frequency / _framerateCounterStopwatch.ElapsedTicks, 2, MidpointRounding.AwayFromZero);
            fpsToolStripStatusLabel.Text = "FPS: " + fps.ToString("F2");

            _framerateCounter = 0;
            _framerateCounterStopwatch.Restart();
        }
    }
}
