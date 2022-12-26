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
            const PixelFormat FORMAT = PixelFormat.Format24bppRgb;
            Bitmap bitmap = new(SCREEN_WIDTH, SCREEN_HEIGHT, FORMAT);
            BitmapData data = bitmap.LockBits(new Rectangle(0, 0, SCREEN_WIDTH, SCREEN_HEIGHT), ImageLockMode.WriteOnly, FORMAT);

            const int BPP = 3;
            const int bufferSize = SCREEN_WIDTH * SCREEN_HEIGHT * BPP;
            byte[] buffer = new byte[bufferSize];

            for (int i = 0; i < SCREEN_WIDTH * SCREEN_HEIGHT; ++i)
            {
                UInt16 gbaColor = frameBuffer[i];
                buffer[i * BPP + 2] = (byte)(((gbaColor >> 0) & 0x1f) * 0xff / 0x1f); // red
                buffer[i * BPP + 1] = (byte)(((gbaColor >> 5) & 0x1f) * 0xff / 0x1f); // green
                buffer[i * BPP + 0] = (byte)(((gbaColor >> 10) & 0x1f) * 0xff / 0x1f); // blue
            }

            System.Runtime.InteropServices.Marshal.Copy(buffer, 0, data.Scan0, bufferSize);
            bitmap.UnlockBits(data);
            pictureBox1.Image = bitmap;
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
            int fps = (int)((frameCount * 1000) / performanceUpdateTimer.Interval);
            toolStripStatusLabel2.Text = "FPS: " + fps;
            frameCount = 0;
        }
    }
}
