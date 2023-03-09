using System.Drawing.Imaging;

namespace Iris.UserInterface
{
    public partial class MainWindow : Form
    {
        private static readonly Dictionary<Keys, EmulationCore.GBA.Core.Keys> KeyMapping = new()
        {
            { Keys.A, EmulationCore.GBA.Core.Keys.A },
            { Keys.Z, EmulationCore.GBA.Core.Keys.B},
            { Keys.Space, EmulationCore.GBA.Core.Keys.Select},
            { Keys.Enter, EmulationCore.GBA.Core.Keys.Start},
            { Keys.Right, EmulationCore.GBA.Core.Keys.Right},
            { Keys.Left, EmulationCore.GBA.Core.Keys.Left},
            { Keys.Up, EmulationCore.GBA.Core.Keys.Up},
            { Keys.Down, EmulationCore.GBA.Core.Keys.Down},
            { Keys.S, EmulationCore.GBA.Core.Keys.R},
            { Keys.Q, EmulationCore.GBA.Core.Keys.L},
        };

        private readonly EmulationCore.GBA.Core _GBA;

        private int _frameCount = 0;
        private readonly System.Timers.Timer _performanceUpdateTimer = new(1000);

        public MainWindow(string[] args)
        {
            InitializeComponent();

            _GBA = new(DrawFrame);

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
            const int ScreenWidth = 240;
            const int ScreenHeight = 160;
            const PixelFormat PixelFormat = PixelFormat.Format16bppRgb555;

            Bitmap bitmap = new(ScreenWidth, ScreenHeight, PixelFormat);
            BitmapData data = bitmap.LockBits(new Rectangle(0, 0, ScreenWidth, ScreenHeight), ImageLockMode.WriteOnly, PixelFormat);

            const int PixelCount = ScreenWidth * ScreenHeight;
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

            ++_frameCount;
        }

        private bool LoadROM(string fileName)
        {
            try
            {
                _GBA.LoadROM(fileName);
                _GBA.Reset();
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
                    _GBA.Run();
                }
                catch (Exception ex)
                {
                    Pause();
                    _GBA.Reset();
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
            _GBA.Pause();

            _performanceUpdateTimer.Stop();
            fpsToolStripStatusLabel.Text = "FPS: 0";
            _frameCount = 0;
        }

        private void LoadROMToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool running = _GBA.IsRunning();
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
            bool running = _GBA.IsRunning();
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
            bool running = _GBA.IsRunning();
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
            bool running = _GBA.IsRunning();
            if (running)
                Pause();

            _GBA.Reset();

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
            if (KeyMapping.TryGetValue(e.KeyCode, out EmulationCore.GBA.Core.Keys value))
                _GBA.SetKeyStatus(value, EmulationCore.GBA.Core.KeyStatus.Input);
        }

        private void MainWindow_KeyUp(object sender, KeyEventArgs e)
        {
            if (KeyMapping.TryGetValue(e.KeyCode, out EmulationCore.GBA.Core.Keys value))
                _GBA.SetKeyStatus(value, EmulationCore.GBA.Core.KeyStatus.NoInput);
        }
    }
}