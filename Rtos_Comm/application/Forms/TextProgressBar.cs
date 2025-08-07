// FILE: TextProgressBar.cs

using System.Drawing;
using System.Windows.Forms;

namespace Rtos_Comm
{
    // A custom ProgressBar that can draw text over itself.
    public class TextProgressBar : ProgressBar
    {
        public string CustomText { get; set; }

        public TextProgressBar()
        {
            // Enable custom painting and double buffering to prevent flicker.
            this.SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Rectangle rect = this.ClientRectangle;
            Graphics g = e.Graphics;

            ProgressBarRenderer.DrawHorizontalBar(g, this.ClientRectangle);

            double percentage = (double)(this.Value - this.Minimum) / (double)(this.Maximum - this.Minimum);
            rect.Width = (int)(rect.Width * percentage);

            if (rect.Width > 0)
            {
                // Draw the filled portion with the specified ForeColor.
                using (var brush = new SolidBrush(this.ForeColor))
                {
                    g.FillRectangle(brush, 2, 2, rect.Width - 4, rect.Height - 4);
                }
            }

            string text = string.IsNullOrEmpty(CustomText) ? $"{this.Value}%" : CustomText;
            using (Font font = new Font("Segoe UI", 9, FontStyle.Bold))
            {
                SizeF textSize = g.MeasureString(text, font);
                Point location = new Point(
                    (int)((this.Width / 2) - (textSize.Width / 2)),
                    (int)((this.Height / 2) - (textSize.Height / 2))
                );

                // Draw text with a subtle shadow for better readability.
                g.DrawString(text, font, Brushes.Black, location.X + 1, location.Y + 1);
                g.DrawString(text, font, Brushes.White, location);
            }
        }
    }
}