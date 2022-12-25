using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
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

        public MainWindow(string[] args)
        {
            InitializeComponent();

            gba = new(this);
            if (args.Length > 0)
            {
                LoadROM(args[0]);
            }
        }

        public void DrawFrame()
        {
            // TODO
        }

        private void LoadROM(string fileName)
        {
            try
            {
                gba.LoadROM(fileName);
            }
            catch
            {
                MessageBox.Show("Could not load ROM", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // gba.Run();
        }

        private void LoadROMToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                LoadROM(dialog.FileName);
            }
        }

        private void SaveStateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog dialog = new();
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                // TODO
            }
        }

        private void QuitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}
