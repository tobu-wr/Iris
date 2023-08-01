using System.Drawing.Imaging;
using Iris.Common;
using Iris.GBA;

namespace Iris.UserInterface
{
    public partial class MainWindow : Form
    {
        private static readonly Dictionary<Keys, ISystemCore.Key> KeyMapping = new()
        {
            { Keys.A, ISystemCore.Key.A },
            { Keys.Z, ISystemCore.Key.B },
            { Keys.Space, ISystemCore.Key.Select },
            { Keys.Enter, ISystemCore.Key.Start },
            { Keys.Right, ISystemCore.Key.Right },
            { Keys.Left, ISystemCore.Key.Left },
            { Keys.Up, ISystemCore.Key.Up },
            { Keys.Down, ISystemCore.Key.Down },
            { Keys.S, ISystemCore.Key.R },
            { Keys.Q, ISystemCore.Key.L },
            { Keys.E, ISystemCore.Key.X },
            { Keys.R, ISystemCore.Key.Y },
        };

        private readonly ISystemCore _core;
        private int _frameCount = 0;
        private readonly System.Timers.Timer _performanceUpdateTimer = new(1000);

        public MainWindow(string[] args)
        {
            InitializeComponent();

            _core = new Core(DrawFrame);
            _performanceUpdateTimer.Elapsed += PerformanceUpdateTimer_Elapsed;

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
#if !RELEASE_NODISPLAY
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

            System.Runtime.InteropServices.Marshal.Copy(buffer, 0, data.Scan0, PixelCount);
            bitmap.UnlockBits(data);

            screenBox.Invoke(() => screenBox.Image = bitmap);
            screenBox.Invalidate();
#endif

            ++_frameCount;
        }

        private bool LoadROM(string fileName)
        {
            try
            {
                _core.LoadROM(fileName);
                _core.Reset();
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
                    _core.Run();
                }
                catch (Exception ex)
                {
                    Pause();
                    _core.Reset();
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            });

            _performanceUpdateTimer.Start();
        }

        private void Pause()
        {
            runToolStripMenuItem.Enabled = true;
            pauseToolStripMenuItem.Enabled = false;
            statusToolStripStatusLabel.Text = "Paused";
            _core.Pause();

            _performanceUpdateTimer.Stop();
            fpsToolStripStatusLabel.Text = "FPS: 0";
            _frameCount = 0;
        }

        private void LoadROMToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool running = _core.IsRunning();

            if (running)
                Pause();

            OpenFileDialog dialog = new()
            {
                Filter = "GBA ROM files (*.gba)|*.gba"
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
            bool running = _core.IsRunning();

            if (running)
                Pause();

            OpenFileDialog dialog = new();

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                // TODO
            }

            if (running)
                Run();
        }

        private void SaveStateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool running = _core.IsRunning();

            if (running)
                Pause();

            SaveFileDialog dialog = new();

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                // TODO
            }

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
            bool running = _core.IsRunning();

            if (running)
                Pause();

            _core.Reset();

            if (running)
                Run();
        }

        private void PerformanceUpdateTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            int fps = (int)(_frameCount * 1000 / _performanceUpdateTimer.Interval);
            menuStrip1.Invoke(() => fpsToolStripStatusLabel.Text = "FPS: " + fps);
            Console.WriteLine("UserInterface.MainWindow: FPS: {0}", fps);
            _frameCount = 0;
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (KeyMapping.TryGetValue(e.KeyCode, out ISystemCore.Key value))
                _core.SetKeyStatus(value, ISystemCore.KeyStatus.Input);
        }

        private void MainWindow_KeyUp(object sender, KeyEventArgs e)
        {
            if (KeyMapping.TryGetValue(e.KeyCode, out ISystemCore.Key value))
                _core.SetKeyStatus(value, ISystemCore.KeyStatus.NoInput);
        }
    }
}