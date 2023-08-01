using Iris.Common;
using Iris.GBA;
using System.Drawing.Imaging;

namespace Iris.UserInterface
{
    public partial class MainWindow : Form
    {
        private static readonly Dictionary<Keys, ISystem.Key> KeyMapping = new()
        {
            { Keys.A, ISystem.Key.A },
            { Keys.Z, ISystem.Key.B },
            { Keys.Space, ISystem.Key.Select },
            { Keys.Enter, ISystem.Key.Start },
            { Keys.Right, ISystem.Key.Right },
            { Keys.Left, ISystem.Key.Left },
            { Keys.Up, ISystem.Key.Up },
            { Keys.Down, ISystem.Key.Down },
            { Keys.S, ISystem.Key.R },
            { Keys.Q, ISystem.Key.L },
            { Keys.E, ISystem.Key.X },
            { Keys.R, ISystem.Key.Y },
        };

        private readonly ISystem _system;
        private int _frameCount = 0;
        private readonly System.Timers.Timer _performanceUpdateTimer = new(1000);

        public MainWindow(string[] args)
        {
            InitializeComponent();

            _system = new GBA_System(DrawFrame);
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
                _system.LoadROM(fileName);
                _system.Reset();
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
                    _system.Reset();
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
            _system.Pause();

            _performanceUpdateTimer.Stop();
            fpsToolStripStatusLabel.Text = "FPS: 0";
            _frameCount = 0;
        }

        private void LoadROMToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool running = _system.IsRunning();

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
            bool running = _system.IsRunning();

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
            bool running = _system.IsRunning();

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
            bool running = _system.IsRunning();

            if (running)
                Pause();

            _system.Reset();

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
            if (KeyMapping.TryGetValue(e.KeyCode, out ISystem.Key value))
                _system.SetKeyStatus(value, ISystem.KeyStatus.Input);
        }

        private void MainWindow_KeyUp(object sender, KeyEventArgs e)
        {
            if (KeyMapping.TryGetValue(e.KeyCode, out ISystem.Key value))
                _system.SetKeyStatus(value, ISystem.KeyStatus.NoInput);
        }
    }
}