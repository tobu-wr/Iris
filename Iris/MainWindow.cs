﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace Iris
{
    public partial class MainWindow : Form, IRenderer
    {
        private readonly GBA gba;

        private UInt32 frameCount = 0;
        private readonly System.Windows.Forms.Timer performanceUpdateTimer = new();

        public MainWindow(string[] args)
        {
            InitializeComponent();
            gba = new(this);

            performanceUpdateTimer.Interval = 1000; // each second
            performanceUpdateTimer.Tick += new EventHandler(PerformanceUpdateTimer_Tick);

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

            Bitmap bitmap = new(SCREEN_WIDTH, SCREEN_HEIGHT);
            for (int x = 0; x < SCREEN_WIDTH; ++x)
            {
                for (int y = 0; y < SCREEN_HEIGHT; ++y)
                {
                    UInt16 gbaColor = frameBuffer[y * SCREEN_WIDTH + x];
                    Color color = Color.FromArgb(
                        ((gbaColor >> 0) & 0x1f) << 3,
                        ((gbaColor >> 5) & 0x1f) << 3,
                        ((gbaColor >> 10) & 0x1f) << 3
                    );
                    bitmap.SetPixel(x, y, color);
                }
            }

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

        private void PerformanceUpdateTimer_Tick(object? sender, EventArgs e)
        {
            toolStripStatusLabel2.Text = "FPS: " + frameCount;
            frameCount = 0;
        }
    }
}
