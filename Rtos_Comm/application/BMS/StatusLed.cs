using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Rtos_Comm
{
    public class StatusLed : Control
    {
        private bool _active = false;
        public bool Active
        {
            get => _active;
            set { _active = value; this.Invalidate(); }
        }

        public Color ActiveColor { get; set; } = Color.FromArgb(239, 68, 68); // Red
        public Color InactiveColor { get; set; } = Color.FromArgb(63, 63, 70); // Dark Gray

        public StatusLed()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            this.Size = new Size(16, 16);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            Color colorToDraw = _active ? ActiveColor : InactiveColor;

            using (var brush = new SolidBrush(colorToDraw))
            {
                e.Graphics.FillEllipse(brush, this.ClientRectangle);
            }
        }
    }
}