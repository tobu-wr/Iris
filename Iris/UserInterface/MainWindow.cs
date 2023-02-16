using System.Drawing.Imaging;
using System.Timers;

namespace Iris.UserInterface
{
    internal sealed partial class MainWindow : Form
    {
        private static readonly Dictionary<Keys, Emulation.GBA.Core.Keys> KeyMapping = new()
        {
            { Keys.A, Emulation.GBA.Core.Keys.A },
            { Keys.Z, Emulation.GBA.Core.Keys.B},
            { Keys.Space, Emulation.GBA.Core.Keys.Select},
            { Keys.Enter, Emulation.GBA.Core.Keys.Start},
            { Keys.Right, Emulation.GBA.Core.Keys.Right},
            { Keys.Left, Emulation.GBA.Core.Keys.Left},
            { Keys.Up, Emulation.GBA.Core.Keys.Up},
            { Keys.Down, Emulation.GBA.Core.Keys.Down},
            { Keys.S, Emulation.GBA.Core.Keys.R},
            { Keys.Q, Emulation.GBA.Core.Keys.L},
        };

        private readonly Emulation.GBA.Core _gba;

        private int _frameCount = 0;
        private readonly System.Timers.Timer _performanceUpdateTimer = new(1000);

        internal MainWindow(string[] args)
        {
            InitializeComponent();
            _gba = new(DrawFrame);

            _performanceUpdateTimer.Elapsed += new ElapsedEventHandler(PerformanceUpdateTimer_Elapsed);

            if (args.Length > 0 && LoadROM(args[0]))
            {
                loadStateToolStripMenuItem.Enabled = true;
                saveStateToolStripMenuItem.Enabled = true;
                runToolStripMenuItem.Enabled = true;
                pauseToolStripMenuItem.Enabled = false;
                restartToolStripMenuItem.Enabled = true;
                toolStripStatusLabel1.Text = "Paused";
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
            pictureBox1.Invoke(() => pictureBox1.Image = bitmap);
            pictureBox1.Invalidate();

            ++_frameCount;
        }

        private bool LoadROM(string fileName)
        {
            try
            {
                _gba.LoadROM(fileName);
                _gba.Init();
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
            toolStripStatusLabel1.Text = "Running";

            Task.Run(() =>
            {
                try
                {
                    _gba.Run();
                }
                catch (Exception ex)
                {
                    Pause();
                    _gba.Init();
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            });

            _performanceUpdateTimer.Start();
        }

        private void Pause()
        {
            runToolStripMenuItem.Enabled = true;
            pauseToolStripMenuItem.Enabled = false;
            toolStripStatusLabel1.Text = "Paused";
            _gba.Pause();

            _performanceUpdateTimer.Stop();
            toolStripStatusLabel2.Text = "FPS: 0";
            _frameCount = 0;
        }

        private void LoadROMToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool running = _gba.IsRunning();
            if (running)
                Pause();

            OpenFileDialog dialog = new();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                if (LoadROM(dialog.FileName) && !running)
                {
                    loadStateToolStripMenuItem.Enabled = true;
                    saveStateToolStripMenuItem.Enabled = true;
                    runToolStripMenuItem.Enabled = true;
                    pauseToolStripMenuItem.Enabled = false;
                    restartToolStripMenuItem.Enabled = true;
                    toolStripStatusLabel1.Text = "Paused";
                }
            }

            if (running)
                Run();
        }

        private void LoadStateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool running = _gba.IsRunning();
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
            bool running = _gba.IsRunning();
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
            bool running = _gba.IsRunning();
            if (running)
                Pause();

            _gba.Init();

            if (running)
                Run();
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (KeyMapping.TryGetValue(e.KeyCode, out Emulation.GBA.Core.Keys value))
                _gba.SetKeyStatus(value, true);
        }

        private void MainWindow_KeyUp(object sender, KeyEventArgs e)
        {
            if (KeyMapping.TryGetValue(e.KeyCode, out Emulation.GBA.Core.Keys value))
                _gba.SetKeyStatus(value, false);
        }

        private void PerformanceUpdateTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            int fps = (int)(_frameCount * 1000 / _performanceUpdateTimer.Interval);
            menuStrip1.Invoke(() => toolStripStatusLabel2.Text = "FPS: " + fps);
            _frameCount = 0;
        }
    }
}
