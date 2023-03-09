using System.Drawing.Drawing2D;

namespace Iris.UserInterface
{
    public class ScreenBox : PictureBox
    {
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.PixelOffsetMode = PixelOffsetMode.Half;
            e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
            base.OnPaint(e);
        }
    }
}
