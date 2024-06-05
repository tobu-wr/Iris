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
            toolStripStatusLabel1 = new ToolStripStatusLabel();
            renderingLoadToolStripStatusLabel = new ToolStripStatusLabel();
            menuStrip1 = new MenuStrip();
            fileToolStripMenuItem = new ToolStripMenuItem();
            loadROMToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator1 = new ToolStripSeparator();
            loadStateToolStripMenuItem = new ToolStripMenuItem();
            saveStateToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator2 = new ToolStripSeparator();
            quitToolStripMenuItem = new ToolStripMenuItem();
            viewToolStripMenuItem = new ToolStripMenuItem();
            fullScreenToolStripMenuItem = new ToolStripMenuItem();
            emulationToolStripMenuItem = new ToolStripMenuItem();
            runToolStripMenuItem = new ToolStripMenuItem();
            pauseToolStripMenuItem = new ToolStripMenuItem();
            resetToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator3 = new ToolStripSeparator();
            limitFramerateToolStripMenuItem = new ToolStripMenuItem();
            automaticPauseToolStripMenuItem = new ToolStripMenuItem();
            glControl = new OpenTK.WinForms.GLControl();
            statusStrip1.SuspendLayout();
            menuStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // statusStrip1
            // 
            statusStrip1.ImageScalingSize = new Size(20, 20);
            statusStrip1.Items.AddRange(new ToolStripItem[] { statusToolStripStatusLabel, toolStripStatusLabel2, fpsToolStripStatusLabel, toolStripStatusLabel1, renderingLoadToolStripStatusLabel });
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
            fpsToolStripStatusLabel.Size = new Size(219, 17);
            fpsToolStripStatusLabel.Text = "FPS: 0,00 (sd: 0,00 | min: 0,00 | max: 0,00)";
            // 
            // toolStripStatusLabel1
            // 
            toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            toolStripStatusLabel1.Size = new Size(12, 17);
            toolStripStatusLabel1.Text = "-";
            // 
            // renderingLoadToolStripStatusLabel
            // 
            renderingLoadToolStripStatusLabel.Name = "renderingLoadToolStripStatusLabel";
            renderingLoadToolStripStatusLabel.Size = new Size(263, 17);
            renderingLoadToolStripStatusLabel.Text = "Rendering Load: 0% (sd: 0% | min: 0% | max: 0%)";
            // 
            // menuStrip1
            // 
            menuStrip1.ImageScalingSize = new Size(20, 20);
            menuStrip1.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem, viewToolStripMenuItem, emulationToolStripMenuItem });
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
            // viewToolStripMenuItem
            // 
            viewToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { fullScreenToolStripMenuItem });
            viewToolStripMenuItem.Name = "viewToolStripMenuItem";
            viewToolStripMenuItem.Size = new Size(44, 20);
            viewToolStripMenuItem.Text = "&View";
            // 
            // fullScreenToolStripMenuItem
            // 
            fullScreenToolStripMenuItem.CheckOnClick = true;
            fullScreenToolStripMenuItem.Name = "fullScreenToolStripMenuItem";
            fullScreenToolStripMenuItem.Size = new Size(131, 22);
            fullScreenToolStripMenuItem.Text = "&Full Screen";
            fullScreenToolStripMenuItem.Click += FullScreenToolStripMenuItem_Click;
            // 
            // emulationToolStripMenuItem
            // 
            emulationToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { runToolStripMenuItem, pauseToolStripMenuItem, resetToolStripMenuItem, toolStripSeparator3, limitFramerateToolStripMenuItem, automaticPauseToolStripMenuItem });
            emulationToolStripMenuItem.Name = "emulationToolStripMenuItem";
            emulationToolStripMenuItem.Size = new Size(73, 20);
            emulationToolStripMenuItem.Text = "&Emulation";
            // 
            // runToolStripMenuItem
            // 
            runToolStripMenuItem.Enabled = false;
            runToolStripMenuItem.Name = "runToolStripMenuItem";
            runToolStripMenuItem.Size = new Size(164, 22);
            runToolStripMenuItem.Text = "R&un";
            runToolStripMenuItem.Click += RunToolStripMenuItem_Click;
            // 
            // pauseToolStripMenuItem
            // 
            pauseToolStripMenuItem.Enabled = false;
            pauseToolStripMenuItem.Name = "pauseToolStripMenuItem";
            pauseToolStripMenuItem.Size = new Size(164, 22);
            pauseToolStripMenuItem.Text = "&Pause";
            pauseToolStripMenuItem.Click += PauseToolStripMenuItem_Click;
            // 
            // resetToolStripMenuItem
            // 
            resetToolStripMenuItem.Enabled = false;
            resetToolStripMenuItem.Name = "resetToolStripMenuItem";
            resetToolStripMenuItem.Size = new Size(164, 22);
            resetToolStripMenuItem.Text = "R&eset";
            resetToolStripMenuItem.Click += ResetToolStripMenuItem_Click;
            // 
            // toolStripSeparator3
            // 
            toolStripSeparator3.Name = "toolStripSeparator3";
            toolStripSeparator3.Size = new Size(161, 6);
            // 
            // limitFramerateToolStripMenuItem
            // 
            limitFramerateToolStripMenuItem.Checked = true;
            limitFramerateToolStripMenuItem.CheckOnClick = true;
            limitFramerateToolStripMenuItem.CheckState = CheckState.Checked;
            limitFramerateToolStripMenuItem.Name = "limitFramerateToolStripMenuItem";
            limitFramerateToolStripMenuItem.Size = new Size(164, 22);
            limitFramerateToolStripMenuItem.Text = "Limit Framerate";
            limitFramerateToolStripMenuItem.Click += LimitFramerateToolStripMenuItem_Click;
            // 
            // automaticPauseToolStripMenuItem
            // 
            automaticPauseToolStripMenuItem.Checked = true;
            automaticPauseToolStripMenuItem.CheckOnClick = true;
            automaticPauseToolStripMenuItem.CheckState = CheckState.Checked;
            automaticPauseToolStripMenuItem.Name = "automaticPauseToolStripMenuItem";
            automaticPauseToolStripMenuItem.Size = new Size(164, 22);
            automaticPauseToolStripMenuItem.Text = "Automatic Pause";
            automaticPauseToolStripMenuItem.Click += AutomaticPauseToolStripMenuItem_Click;
            // 
            // glControl
            // 
            glControl.API = OpenTK.Windowing.Common.ContextAPI.OpenGL;
            glControl.APIVersion = new Version(3, 3, 0, 0);
            glControl.Dock = DockStyle.Fill;
            glControl.Flags = OpenTK.Windowing.Common.ContextFlags.Default;
            glControl.IsEventDriven = true;
            glControl.Location = new Point(0, 24);
            glControl.Name = "glControl";
            glControl.Profile = OpenTK.Windowing.Common.ContextProfile.Core;
            glControl.Size = new Size(800, 404);
            glControl.TabIndex = 3;
            glControl.Text = "glControl1";
            // 
            // MainWindow
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
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
        private ToolStripMenuItem viewToolStripMenuItem;
        private ToolStripMenuItem fullScreenToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator3;
        private ToolStripMenuItem limitFramerateToolStripMenuItem;
        private ToolStripStatusLabel toolStripStatusLabel1;
        private ToolStripStatusLabel renderingLoadToolStripStatusLabel;
        private ToolStripMenuItem automaticPauseToolStripMenuItem;
        private OpenTK.WinForms.GLControl glControl;
    }
}