using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace Iris
{
    public partial class MainWindow : Form, IRenderer
    {
        private readonly GBA gba;

        private int frameCount = 0;
        private readonly System.Timers.Timer performanceUpdateTimer = new(1000);

        public MainWindow(string[] args)
        {
            InitializeComponent();
            gba = new(this);

            performanceUpdateTimer.Elapsed += new ElapsedEventHandler(PerformanceUpdateTimer_Elapsed);

            if (args.Length > 0 && LoadROM(args[0]))
            {
                loadStateToolStripMenuItem.Enabled = true;
                saveStateToolStripMenuItem.Enabled = true;
                runToolStripMenuItem.Enabled = true;
                pauseToolStripMenuItem.Enabled = false;
                toolStripStatusLabel1.Text = "Paused";
            }
        }

        public void DrawFrame(UInt16[] frameBuffer)
        {
            const int SCREEN_WIDTH = 240;
            const int SCREEN_HEIGHT = 160;
            const PixelFormat PIXEL_FORMAT = PixelFormat.Format16bppRgb555;

            Bitmap bitmap = new(SCREEN_WIDTH, SCREEN_HEIGHT, PIXEL_FORMAT);
            BitmapData data = bitmap.LockBits(new Rectangle(0, 0, SCREEN_WIDTH, SCREEN_HEIGHT), ImageLockMode.WriteOnly, PIXEL_FORMAT);

            const int PIXEL_COUNT = SCREEN_WIDTH * SCREEN_HEIGHT;
            Int16[] buffer = new Int16[PIXEL_COUNT];
            for (int i = 0; i < PIXEL_COUNT; ++i)
            {
                UInt16 gbaColor = frameBuffer[i]; // BGR format
                Byte red = (Byte)((gbaColor >> 0) & 0x1f);
                Byte green = (Byte)((gbaColor >> 5) & 0x1f);
                Byte blue = (Byte)((gbaColor >> 10) & 0x1f);
                buffer[i] = (Int16)((red << 10) | (green << 5) | blue);
            }

            System.Runtime.InteropServices.Marshal.Copy(buffer, 0, data.Scan0, PIXEL_COUNT);
            bitmap.UnlockBits(data);
            pictureBox1.Invoke((MethodInvoker)delegate
            {
                pictureBox1.Image = bitmap;
            });
            pictureBox1.Invalidate();

            ++frameCount;
        }

        private bool LoadROM(string fileName)
        {
            try
            {
                gba.LoadROM(fileName);
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
            Task.Run(() => gba.Run());

            performanceUpdateTimer.Start();
        }

        private void Pause()
        {
            runToolStripMenuItem.Enabled = true;
            pauseToolStripMenuItem.Enabled = false;
            toolStripStatusLabel1.Text = "Paused";
            gba.Pause();

            performanceUpdateTimer.Stop();
            toolStripStatusLabel2.Text = "FPS: 0";
            frameCount = 0;
        }

        private void LoadROMToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool running = gba.IsRunning();
            if (running) Pause();

            OpenFileDialog dialog = new();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                if (LoadROM(dialog.FileName) && !running)
                {
                    loadStateToolStripMenuItem.Enabled = true;
                    saveStateToolStripMenuItem.Enabled = true;
                    runToolStripMenuItem.Enabled = true;
                    pauseToolStripMenuItem.Enabled = false;
                    toolStripStatusLabel1.Text = "Paused";
                }
            }

            if (running) Run();
        }

        private void LoadStateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool running = gba.IsRunning();
            if (running) Pause();

            OpenFileDialog dialog = new();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                // TODO
            }

            if (running) Run();
        }

        private void SaveStateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool running = gba.IsRunning();
            if (running) Pause();

            SaveFileDialog dialog = new();
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                // TODO
            }

            if (running) Run();
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

        private void PerformanceUpdateTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            int fps = (int)(frameCount * 1000 / performanceUpdateTimer.Interval);
            menuStrip1.Invoke((MethodInvoker)delegate
            {
                toolStripStatusLabel2.Text = "FPS: " + fps;
            });
            frameCount = 0;
        }
    }
}
