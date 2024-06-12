namespace Iris.UserInterface
{
    partial class MainWindow
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

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
            recentROMsToolStripMenuItem = new ToolStripMenuItem();
            recentStatesToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator5 = new ToolStripSeparator();
            quitToolStripMenuItem = new ToolStripMenuItem();
            emulationToolStripMenuItem = new ToolStripMenuItem();
            runToolStripMenuItem = new ToolStripMenuItem();
            pauseToolStripMenuItem = new ToolStripMenuItem();
            resetToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator3 = new ToolStripSeparator();
            limitFramerateToolStripMenuItem = new ToolStripMenuItem();
            automaticPauseToolStripMenuItem = new ToolStripMenuItem();
            skipIntroToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator4 = new ToolStripSeparator();
            inputSettingsToolStripMenuItem = new ToolStripMenuItem();
            displayToolStripMenuItem = new ToolStripMenuItem();
            fullScreenToolStripMenuItem = new ToolStripMenuItem();
            integerScalingToolStripMenuItem = new ToolStripMenuItem();
            bilinearFilteringToolStripMenuItem = new ToolStripMenuItem();
            fixedAspectRatioToolStripMenuItem = new ToolStripMenuItem();
            audioToolStripMenuItem = new ToolStripMenuItem();
            exclusiveModeToolStripMenuItem = new ToolStripMenuItem();
            glControl = new OpenTK.WinForms.GLControl();
            statusStrip1.SuspendLayout();
            menuStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // statusStrip1
            // 
            statusStrip1.ImageScalingSize = new Size(20, 20);
            statusStrip1.Items.AddRange(new ToolStripItem[] { statusToolStripStatusLabel, toolStripStatusLabel2, fpsToolStripStatusLabel });
            statusStrip1.Location = new Point(0, 504);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new Size(720, 22);
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
            fpsToolStripStatusLabel.Size = new Size(219, 17);
            fpsToolStripStatusLabel.Text = "FPS: 0,00 (sd: 0,00 | min: 0,00 | max: 0,00)";
            // 
            // menuStrip1
            // 
            menuStrip1.ImageScalingSize = new Size(20, 20);
            menuStrip1.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem, emulationToolStripMenuItem, displayToolStripMenuItem, audioToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(720, 24);
            menuStrip1.TabIndex = 1;
            menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { loadROMToolStripMenuItem, toolStripSeparator1, loadStateToolStripMenuItem, saveStateToolStripMenuItem, toolStripSeparator2, recentROMsToolStripMenuItem, recentStatesToolStripMenuItem, toolStripSeparator5, quitToolStripMenuItem });
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new Size(37, 20);
            fileToolStripMenuItem.Text = "File";
            // 
            // loadROMToolStripMenuItem
            // 
            loadROMToolStripMenuItem.Name = "loadROMToolStripMenuItem";
            loadROMToolStripMenuItem.Size = new Size(180, 22);
            loadROMToolStripMenuItem.Text = "Load ROM...";
            loadROMToolStripMenuItem.Click += LoadROMToolStripMenuItem_Click;
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new Size(177, 6);
            // 
            // loadStateToolStripMenuItem
            // 
            loadStateToolStripMenuItem.Enabled = false;
            loadStateToolStripMenuItem.Name = "loadStateToolStripMenuItem";
            loadStateToolStripMenuItem.Size = new Size(180, 22);
            loadStateToolStripMenuItem.Text = "Load State...";
            loadStateToolStripMenuItem.Click += LoadStateToolStripMenuItem_Click;
            // 
            // saveStateToolStripMenuItem
            // 
            saveStateToolStripMenuItem.Enabled = false;
            saveStateToolStripMenuItem.Name = "saveStateToolStripMenuItem";
            saveStateToolStripMenuItem.Size = new Size(180, 22);
            saveStateToolStripMenuItem.Text = "Save State...";
            saveStateToolStripMenuItem.Click += SaveStateToolStripMenuItem_Click;
            // 
            // toolStripSeparator2
            // 
            toolStripSeparator2.Name = "toolStripSeparator2";
            toolStripSeparator2.Size = new Size(177, 6);
            // 
            // recentROMsToolStripMenuItem
            // 
            recentROMsToolStripMenuItem.Enabled = false;
            recentROMsToolStripMenuItem.Name = "recentROMsToolStripMenuItem";
            recentROMsToolStripMenuItem.Size = new Size(180, 22);
            recentROMsToolStripMenuItem.Text = "Recent ROMs";
            // 
            // recentStatesToolStripMenuItem
            // 
            recentStatesToolStripMenuItem.Enabled = false;
            recentStatesToolStripMenuItem.Name = "recentStatesToolStripMenuItem";
            recentStatesToolStripMenuItem.Size = new Size(180, 22);
            recentStatesToolStripMenuItem.Text = "Recent States";
            // 
            // toolStripSeparator5
            // 
            toolStripSeparator5.Name = "toolStripSeparator5";
            toolStripSeparator5.Size = new Size(177, 6);
            // 
            // quitToolStripMenuItem
            // 
            quitToolStripMenuItem.Name = "quitToolStripMenuItem";
            quitToolStripMenuItem.Size = new Size(180, 22);
            quitToolStripMenuItem.Text = "Quit";
            quitToolStripMenuItem.Click += QuitToolStripMenuItem_Click;
            // 
            // emulationToolStripMenuItem
            // 
            emulationToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { runToolStripMenuItem, pauseToolStripMenuItem, resetToolStripMenuItem, toolStripSeparator3, limitFramerateToolStripMenuItem, automaticPauseToolStripMenuItem, skipIntroToolStripMenuItem, toolStripSeparator4, inputSettingsToolStripMenuItem });
            emulationToolStripMenuItem.Name = "emulationToolStripMenuItem";
            emulationToolStripMenuItem.Size = new Size(73, 20);
            emulationToolStripMenuItem.Text = "Emulation";
            // 
            // runToolStripMenuItem
            // 
            runToolStripMenuItem.Enabled = false;
            runToolStripMenuItem.Name = "runToolStripMenuItem";
            runToolStripMenuItem.Size = new Size(180, 22);
            runToolStripMenuItem.Text = "Run";
            runToolStripMenuItem.Click += RunToolStripMenuItem_Click;
            // 
            // pauseToolStripMenuItem
            // 
            pauseToolStripMenuItem.Enabled = false;
            pauseToolStripMenuItem.Name = "pauseToolStripMenuItem";
            pauseToolStripMenuItem.Size = new Size(180, 22);
            pauseToolStripMenuItem.Text = "Pause";
            pauseToolStripMenuItem.Click += PauseToolStripMenuItem_Click;
            // 
            // resetToolStripMenuItem
            // 
            resetToolStripMenuItem.Enabled = false;
            resetToolStripMenuItem.Name = "resetToolStripMenuItem";
            resetToolStripMenuItem.Size = new Size(180, 22);
            resetToolStripMenuItem.Text = "Reset";
            resetToolStripMenuItem.Click += ResetToolStripMenuItem_Click;
            // 
            // toolStripSeparator3
            // 
            toolStripSeparator3.Name = "toolStripSeparator3";
            toolStripSeparator3.Size = new Size(177, 6);
            // 
            // limitFramerateToolStripMenuItem
            // 
            limitFramerateToolStripMenuItem.Checked = true;
            limitFramerateToolStripMenuItem.CheckOnClick = true;
            limitFramerateToolStripMenuItem.CheckState = CheckState.Checked;
            limitFramerateToolStripMenuItem.Name = "limitFramerateToolStripMenuItem";
            limitFramerateToolStripMenuItem.Size = new Size(180, 22);
            limitFramerateToolStripMenuItem.Text = "Limit Framerate";
            limitFramerateToolStripMenuItem.Click += LimitFramerateToolStripMenuItem_Click;
            // 
            // automaticPauseToolStripMenuItem
            // 
            automaticPauseToolStripMenuItem.Checked = true;
            automaticPauseToolStripMenuItem.CheckOnClick = true;
            automaticPauseToolStripMenuItem.CheckState = CheckState.Checked;
            automaticPauseToolStripMenuItem.Name = "automaticPauseToolStripMenuItem";
            automaticPauseToolStripMenuItem.Size = new Size(180, 22);
            automaticPauseToolStripMenuItem.Text = "Automatic Pause";
            automaticPauseToolStripMenuItem.Click += AutomaticPauseToolStripMenuItem_Click;
            // 
            // skipIntroToolStripMenuItem
            // 
            skipIntroToolStripMenuItem.Checked = true;
            skipIntroToolStripMenuItem.CheckOnClick = true;
            skipIntroToolStripMenuItem.CheckState = CheckState.Checked;
            skipIntroToolStripMenuItem.Name = "skipIntroToolStripMenuItem";
            skipIntroToolStripMenuItem.Size = new Size(180, 22);
            skipIntroToolStripMenuItem.Text = "Skip Intro";
            skipIntroToolStripMenuItem.Click += SkipIntroToolStripMenuItem_Click;
            // 
            // toolStripSeparator4
            // 
            toolStripSeparator4.Name = "toolStripSeparator4";
            toolStripSeparator4.Size = new Size(177, 6);
            // 
            // inputSettingsToolStripMenuItem
            // 
            inputSettingsToolStripMenuItem.ImageAlign = ContentAlignment.TopRight;
            inputSettingsToolStripMenuItem.Name = "inputSettingsToolStripMenuItem";
            inputSettingsToolStripMenuItem.Size = new Size(180, 22);
            inputSettingsToolStripMenuItem.Text = "Input Settings...";
            inputSettingsToolStripMenuItem.Click += InputSettingsToolStripMenuItem_Click;
            // 
            // displayToolStripMenuItem
            // 
            displayToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { fullScreenToolStripMenuItem, integerScalingToolStripMenuItem, bilinearFilteringToolStripMenuItem, fixedAspectRatioToolStripMenuItem });
            displayToolStripMenuItem.Name = "displayToolStripMenuItem";
            displayToolStripMenuItem.Size = new Size(57, 20);
            displayToolStripMenuItem.Text = "Display";
            // 
            // fullScreenToolStripMenuItem
            // 
            fullScreenToolStripMenuItem.CheckOnClick = true;
            fullScreenToolStripMenuItem.Name = "fullScreenToolStripMenuItem";
            fullScreenToolStripMenuItem.Size = new Size(171, 22);
            fullScreenToolStripMenuItem.Text = "Full Screen";
            fullScreenToolStripMenuItem.Click += FullScreenToolStripMenuItem_Click;
            // 
            // integerScalingToolStripMenuItem
            // 
            integerScalingToolStripMenuItem.Checked = true;
            integerScalingToolStripMenuItem.CheckOnClick = true;
            integerScalingToolStripMenuItem.CheckState = CheckState.Checked;
            integerScalingToolStripMenuItem.Name = "integerScalingToolStripMenuItem";
            integerScalingToolStripMenuItem.Size = new Size(171, 22);
            integerScalingToolStripMenuItem.Text = "Integer Scaling";
            // 
            // bilinearFilteringToolStripMenuItem
            // 
            bilinearFilteringToolStripMenuItem.CheckOnClick = true;
            bilinearFilteringToolStripMenuItem.Name = "bilinearFilteringToolStripMenuItem";
            bilinearFilteringToolStripMenuItem.Size = new Size(171, 22);
            bilinearFilteringToolStripMenuItem.Text = "Bilinear Filtering";
            // 
            // fixedAspectRatioToolStripMenuItem
            // 
            fixedAspectRatioToolStripMenuItem.Checked = true;
            fixedAspectRatioToolStripMenuItem.CheckOnClick = true;
            fixedAspectRatioToolStripMenuItem.CheckState = CheckState.Checked;
            fixedAspectRatioToolStripMenuItem.Name = "fixedAspectRatioToolStripMenuItem";
            fixedAspectRatioToolStripMenuItem.Size = new Size(171, 22);
            fixedAspectRatioToolStripMenuItem.Text = "Fixed Aspect Ratio";
            // 
            // audioToolStripMenuItem
            // 
            audioToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { exclusiveModeToolStripMenuItem });
            audioToolStripMenuItem.Name = "audioToolStripMenuItem";
            audioToolStripMenuItem.Size = new Size(51, 20);
            audioToolStripMenuItem.Text = "Audio";
            // 
            // exclusiveModeToolStripMenuItem
            // 
            exclusiveModeToolStripMenuItem.CheckOnClick = true;
            exclusiveModeToolStripMenuItem.Name = "exclusiveModeToolStripMenuItem";
            exclusiveModeToolStripMenuItem.Size = new Size(156, 22);
            exclusiveModeToolStripMenuItem.Text = "Exclusive Mode";
            // 
            // glControl
            // 
            glControl.API = OpenTK.Windowing.Common.ContextAPI.OpenGL;
            glControl.APIVersion = new Version(4, 2, 0, 0);
            glControl.Dock = DockStyle.Fill;
            glControl.Flags = OpenTK.Windowing.Common.ContextFlags.Default;
            glControl.IsEventDriven = true;
            glControl.Location = new Point(0, 24);
            glControl.Name = "glControl";
            glControl.Profile = OpenTK.Windowing.Common.ContextProfile.Core;
            glControl.Size = new Size(720, 480);
            glControl.TabIndex = 3;
            glControl.Text = "glControl1";
            // 
            // MainWindow
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(720, 526);
            Controls.Add(glControl);
            Controls.Add(statusStrip1);
            Controls.Add(menuStrip1);
            MainMenuStrip = menuStrip1;
            Name = "MainWindow";
            ShowIcon = false;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Iris";
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
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
        private ToolStripMenuItem resetToolStripMenuItem;
        private ToolStripMenuItem displayToolStripMenuItem;
        private ToolStripMenuItem fullScreenToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator3;
        private ToolStripMenuItem limitFramerateToolStripMenuItem;
        private ToolStripMenuItem automaticPauseToolStripMenuItem;
        private OpenTK.WinForms.GLControl glControl;
        private ToolStripMenuItem skipIntroToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator4;
        private ToolStripMenuItem inputSettingsToolStripMenuItem;
        private ToolStripMenuItem integerScalingToolStripMenuItem;
        private ToolStripMenuItem fixedAspectRatioToolStripMenuItem;
        private ToolStripMenuItem bilinearFilteringToolStripMenuItem;
        private ToolStripMenuItem audioToolStripMenuItem;
        private ToolStripMenuItem exclusiveModeToolStripMenuItem;
        private ToolStripMenuItem recentROMsToolStripMenuItem;
        private ToolStripMenuItem recentStatesToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator5;
    }
}