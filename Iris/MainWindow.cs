using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Iris
{
    public partial class MainWindow : Form, IRenderer
    {
        private readonly GBA gba;

        public MainWindow(string[] args)
        {
            InitializeComponent();
            gba = new(this);
        }

        public void DrawFrame()
        {
            throw new NotImplementedException();
        }

        private void LoadROMToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                gba.LoadROM(dialog.FileName);
                gba.Run();
            }
        }

        private void SaveStateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private void QuitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}
