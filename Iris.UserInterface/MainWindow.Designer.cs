namespace Iris.UserInterface
{
    partial class MainWindow
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            statusStrip1 = new StatusStrip();
            statusToolStripStatusLabel = new ToolStripStatusLabel();
            toolStripStatusLabel2 = new ToolStripStatusLabel();
            fpsToolStripStatusLabel = new ToolStripStatusLabel();
            menuStrip1 = new MenuStrip();
            fileToolStripMenuItem = new ToolStripMenuItem();
            loadROMToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator1 = new ToolStripSeparator();
            loadStateToolStripMenuItem = new ToolStripMenuItem();
            saveStateToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator2 = new ToolStripSeparator();
            quitToolStripMenuItem = new ToolStripMenuItem();
            emulationToolStripMenuItem = new ToolStripMenuItem();
            runToolStripMenuItem = new ToolStripMenuItem();
            pauseToolStripMenuItem = new ToolStripMenuItem();
            restartToolStripMenuItem = new ToolStripMenuItem();
            screenBox = new ScreenBox();
            statusStrip1.SuspendLayout();
            menuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)screenBox).BeginInit();
            SuspendLayout();
            // 
            // statusStrip1
            // 
            statusStrip1.Items.AddRange(new ToolStripItem[] { statusToolStripStatusLabel, toolStripStatusLabel2, fpsToolStripStatusLabel });
            statusStrip1.Location = new Point(0, 428);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new Size(800, 22);
            statusStrip1.TabIndex = 0;
            statusStrip1.Text = "statusStrip1";
            // 
            // statusToolStripStatusLabel
            // 
            statusToolStripStatusLabel.Name = "statusToolStripStatusLabel";
            statusToolStripStatusLabel.Size = new Size(92, 17);
            statusToolStripStatusLabel.Text = "No ROM loaded";
            // 
            // toolStripStatusLabel2
            // 
            toolStripStatusLabel2.Name = "toolStripStatusLabel2";
            toolStripStatusLabel2.Size = new Size(12, 17);
            toolStripStatusLabel2.Text = "-";
            // 
            // fpsToolStripStatusLabel
            // 
            fpsToolStripStatusLabel.Name = "fpsToolStripStatusLabel";
            fpsToolStripStatusLabel.Size = new Size(38, 17);
            fpsToolStripStatusLabel.Text = "FPS: 0";
            // 
            // menuStrip1
            // 
            menuStrip1.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem, emulationToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(800, 24);
            menuStrip1.TabIndex = 1;
            menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { loadROMToolStripMenuItem, toolStripSeparator1, loadStateToolStripMenuItem, saveStateToolStripMenuItem, toolStripSeparator2, quitToolStripMenuItem });
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new Size(37, 20);
            fileToolStripMenuItem.Text = "&File";
            // 
            // loadROMToolStripMenuItem
            // 
            loadROMToolStripMenuItem.Name = "loadROMToolStripMenuItem";
            loadROMToolStripMenuItem.Size = new Size(139, 22);
            loadROMToolStripMenuItem.Text = "Load &ROM...";
            loadROMToolStripMenuItem.Click += LoadROMToolStripMenuItem_Click;
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new Size(136, 6);
            // 
            // loadStateToolStripMenuItem
            // 
            loadStateToolStripMenuItem.Enabled = false;
            loadStateToolStripMenuItem.Name = "loadStateToolStripMenuItem";
            loadStateToolStripMenuItem.Size = new Size(139, 22);
            loadStateToolStripMenuItem.Text = "&Load state...";
            loadStateToolStripMenuItem.Click += LoadStateToolStripMenuItem_Click;
            // 
            // saveStateToolStripMenuItem
            // 
            saveStateToolStripMenuItem.Enabled = false;
            saveStateToolStripMenuItem.Name = "saveStateToolStripMenuItem";
            saveStateToolStripMenuItem.Size = new Size(139, 22);
            saveStateToolStripMenuItem.Text = "&Save state...";
            saveStateToolStripMenuItem.Click += SaveStateToolStripMenuItem_Click;
            // 
            // toolStripSeparator2
            // 
            toolStripSeparator2.Name = "toolStripSeparator2";
            toolStripSeparator2.Size = new Size(136, 6);
            // 
            // quitToolStripMenuItem
            // 
            quitToolStripMenuItem.Name = "quitToolStripMenuItem";
            quitToolStripMenuItem.Size = new Size(139, 22);
            quitToolStripMenuItem.Text = "&Quit";
            quitToolStripMenuItem.Click += QuitToolStripMenuItem_Click;
            // 
            // emulationToolStripMenuItem
            // 
            emulationToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { runToolStripMenuItem, pauseToolStripMenuItem, restartToolStripMenuItem });
            emulationToolStripMenuItem.Name = "emulationToolStripMenuItem";
            emulationToolStripMenuItem.Size = new Size(73, 20);
            emulationToolStripMenuItem.Text = "&Emulation";
            // 
            // runToolStripMenuItem
            // 
            runToolStripMenuItem.Enabled = false;
            runToolStripMenuItem.Name = "runToolStripMenuItem";
            runToolStripMenuItem.Size = new Size(110, 22);
            runToolStripMenuItem.Text = "R&un";
            runToolStripMenuItem.Click += RunToolStripMenuItem_Click;
            // 
            // pauseToolStripMenuItem
            // 
            pauseToolStripMenuItem.Enabled = false;
            pauseToolStripMenuItem.Name = "pauseToolStripMenuItem";
            pauseToolStripMenuItem.Size = new Size(110, 22);
            pauseToolStripMenuItem.Text = "&Pause";
            pauseToolStripMenuItem.Click += PauseToolStripMenuItem_Click;
            // 
            // restartToolStripMenuItem
            // 
            restartToolStripMenuItem.Enabled = false;
            restartToolStripMenuItem.Name = "restartToolStripMenuItem";
            restartToolStripMenuItem.Size = new Size(110, 22);
            restartToolStripMenuItem.Text = "R&estart";
            restartToolStripMenuItem.Click += RestartToolStripMenuItem_Click;
            // 
            // screenBox
            // 
            screenBox.Dock = DockStyle.Fill;
            screenBox.Location = new Point(0, 24);
            screenBox.Name = "screenBox";
            screenBox.Size = new Size(800, 404);
            screenBox.SizeMode = PictureBoxSizeMode.StretchImage;
            screenBox.TabIndex = 2;
            screenBox.TabStop = false;
            // 
            // MainWindow
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(screenBox);
            Controls.Add(statusStrip1);
            Controls.Add(menuStrip1);
            DoubleBuffered = true;
            MainMenuStrip = menuStrip1;
            Name = "MainWindow";
            ShowIcon = false;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Iris";
            KeyDown += MainWindow_KeyDown;
            KeyUp += MainWindow_KeyUp;
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)screenBox).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private StatusStrip statusStrip1;
        private ToolStripStatusLabel statusToolStripStatusLabel;
        private ToolStripStatusLabel toolStripStatusLabel2;
        private ToolStripStatusLabel fpsToolStripStatusLabel;
        private MenuStrip menuStrip1;
        private ToolStripMenuItem fileToolStripMenuItem;
        private ToolStripMenuItem loadROMToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripMenuItem loadStateToolStripMenuItem;
        private ToolStripMenuItem saveStateToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator2;
        private ToolStripMenuItem quitToolStripMenuItem;
        private ToolStripMenuItem emulationToolStripMenuItem;
        private ToolStripMenuItem runToolStripMenuItem;
        private ToolStripMenuItem pauseToolStripMenuItem;
        private ToolStripMenuItem restartToolStripMenuItem;
        private ScreenBox screenBox;
    }
}