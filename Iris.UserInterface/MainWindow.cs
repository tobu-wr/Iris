namespace Iris.UserInterface
{
    public partial class MainWindow : Form
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

        private readonly Emulation.GBA.Core _GBA;

        private int _frameCount = 0;
        private readonly System.Timers.Timer _performanceUpdateTimer = new(1000);

        public MainWindow()
        {
            InitializeComponent();

            _GBA = new(DrawFrame);
        }

        private void DrawFrame(UInt16[] frameBuffer)
        {

        }

        private void loadROMToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void loadStateToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void saveStateToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void quitToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void runToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void pauseToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void restartToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }
    }
}