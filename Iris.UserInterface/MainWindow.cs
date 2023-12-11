using Iris.GBA;
using System.Collections.Frozen;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Iris.UserInterface
{
    internal partial class MainWindow : Form
    {
        private static readonly FrozenDictionary<Keys, Common.System.Key> s_keyboardMapping = new Dictionary<Keys, Common.System.Key>()
        {
            { Keys.A, Common.System.Key.A },
            { Keys.Z, Common.System.Key.B },
            { Keys.Space, Common.System.Key.Select },
            { Keys.Enter, Common.System.Key.Start },
            { Keys.Right, Common.System.Key.Right },
            { Keys.Left, Common.System.Key.Left },
            { Keys.Up, Common.System.Key.Up },
            { Keys.Down, Common.System.Key.Down },
            { Keys.S, Common.System.Key.R },
            { Keys.Q, Common.System.Key.L },
            { Keys.E, Common.System.Key.X },
            { Keys.R, Common.System.Key.Y },
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
        private readonly XboxController _xboxController = new();
        private FormWindowState _previousWindowState;

        private int _frameCounter;
        private readonly System.Timers.Timer _frameCounterTimer = new(1000);

        private bool _framerateLimiterEnabled = true;
        private readonly Stopwatch _framerateLimiterStopwatch = Stopwatch.StartNew();
        private long _framerateLimiterExtraSleepTime;

        internal MainWindow(string[] args)
        {
            InitializeComponent();

            _system = new GBA_System(DrawFrame);

            _xboxController.ButtonDown += XboxController_ButtonDown;
            _xboxController.ButtonUp += XboxController_ButtonUp;

            _frameCounterTimer.Elapsed += FrameCounterTimer_Elapsed;

            if (args.Length > 0 && LoadROM(args[0]))
            {
                loadStateToolStripMenuItem.Enabled = true;
                saveStateToolStripMenuItem.Enabled = true;
                runToolStripMenuItem.Enabled = true;
                pauseToolStripMenuItem.Enabled = false;
                restartToolStripMenuItem.Enabled = true;
                statusToolStripStatusLabel.Text = "Paused";
            }
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

            ++_frameCounter;

            _xboxController.Poll();

            if (_framerateLimiterEnabled)
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                long GetElapsedUs() => 1_000_000 * _framerateLimiterStopwatch.ElapsedTicks / Stopwatch.Frequency;

                const double TargetFrameRate = 59.727;
                const long TargetFrameDuration = (long)(1_000_000 / TargetFrameRate);

                long frameDuration = GetElapsedUs();

                if (frameDuration < TargetFrameDuration)
                {
                    long sleepTime = TargetFrameDuration - frameDuration;

                    if (sleepTime > _framerateLimiterExtraSleepTime)
                    {
                        sleepTime -= _framerateLimiterExtraSleepTime;
                        Thread.Sleep((int)(sleepTime / 1000));
                        _framerateLimiterExtraSleepTime = GetElapsedUs() - frameDuration - sleepTime;
                    }
                    else
                    {
                        _framerateLimiterExtraSleepTime -= sleepTime;
                    }
                }

                _framerateLimiterStopwatch.Restart();
            }
        }

        private bool LoadROM(string fileName)
        {
            try
            {
                _system.LoadROM(fileName);
                _system.ResetState();
                return true;
            }
            catch
            {
                MessageBox.Show("Could not load ROM", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private void Run()
        {
            runToolStripMenuItem.Enabled = false;
            pauseToolStripMenuItem.Enabled = true;
            statusToolStripStatusLabel.Text = "Running";

            Task.Run(() =>
            {
                try
                {
                    _system.Run();
                }
                catch (Exception ex)
                {
                    Pause();
                    _system.ResetState();
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            });

            _frameCounterTimer.Start();
        }

        private void Pause()
        {
            runToolStripMenuItem.Enabled = true;
            pauseToolStripMenuItem.Enabled = false;
            statusToolStripStatusLabel.Text = "Paused";
            _system.Pause();

            _frameCounterTimer.Stop();
            fpsToolStripStatusLabel.Text = "FPS: 0";
            _frameCounter = 0;
        }

        private void LoadROMToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool running = _system.IsRunning();

            if (running)
                Pause();

            OpenFileDialog dialog = new()
            {
                Filter = "GBA ROM files (*.gba)|*.gba|NDS ROM files (*.nds)|*.nds"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                if (LoadROM(dialog.FileName) && !running)
                {
                    loadStateToolStripMenuItem.Enabled = true;
                    saveStateToolStripMenuItem.Enabled = true;
                    runToolStripMenuItem.Enabled = true;
                    pauseToolStripMenuItem.Enabled = false;
                    restartToolStripMenuItem.Enabled = true;
                    statusToolStripStatusLabel.Text = "Paused";
                }
            }

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
                // TODO: filter according to current system
                Filter = "GBA State Save files (*.gss)|*.gss|NDS State Save files (*.nss)|*.nss"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    _system.LoadState(dialog.FileName);
                }
                catch
                {
                    MessageBox.Show("Could not load state", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                // TODO: filter according to current system
                Filter = "GBA State Save files (*.gss)|*.gss|NDS State Save files (*.nss)|*.nss"
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

        private void RunToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Run();
        }

        private void PauseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Pause();
        }

        private void RestartToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool running = _system.IsRunning();

            if (running)
                Pause();

            _system.ResetState();

            if (running)
                Run();
        }

        private void FrameCounterTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            int fps = (int)(_frameCounter * 1000 / _frameCounterTimer.Interval);
            menuStrip1.Invoke(() => fpsToolStripStatusLabel.Text = "FPS: " + fps);
            Console.WriteLine("[UserInterface.MainWindow] FPS: {0}", fps);
            _frameCounter = 0;
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (s_keyboardMapping.TryGetValue(e.KeyCode, out Common.System.Key value))
                _system.SetKeyStatus(value, Common.System.KeyStatus.Input);
        }

        private void MainWindow_KeyUp(object sender, KeyEventArgs e)
        {
            if (s_keyboardMapping.TryGetValue(e.KeyCode, out Common.System.Key value))
                _system.SetKeyStatus(value, Common.System.KeyStatus.NoInput);
        }

        private void XboxController_ButtonDown(object? sender, XboxController.ButtonEventArgs e)
        {
            if (s_xboxControllerMapping.TryGetValue(e.Button, out Common.System.Key value))
                _system.SetKeyStatus(value, Common.System.KeyStatus.Input);
        }

        private void XboxController_ButtonUp(object? sender, XboxController.ButtonEventArgs e)
        {
            if (s_xboxControllerMapping.TryGetValue(e.Button, out Common.System.Key value))
                _system.SetKeyStatus(value, Common.System.KeyStatus.NoInput);
        }

        private void FullScreenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (fullScreenToolStripMenuItem.Checked)
            {
                FormBorderStyle = FormBorderStyle.None;
                _previousWindowState = WindowState;
                WindowState = FormWindowState.Normal;
                WindowState = FormWindowState.Maximized;
            }
            else
            {
                FormBorderStyle = FormBorderStyle.Sizable;
                WindowState = _previousWindowState;
            }
        }

        private void LimitFramerateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _framerateLimiterEnabled = limitFramerateToolStripMenuItem.Checked;
        }
    }
}
