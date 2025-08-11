using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Rtos_Comm
{
    public class ModernProgressBar : Control
    {
        private int _value = 0;
        private int _maximum = 100;
        private Color _progressColor = Color.FromArgb(59, 130, 246);
        private string _customText = "";

        public int Value
        {
            get => _value;
            set { _value = Math.Max(0, Math.Min(value, _maximum)); this.Invalidate(); }
        }

        public int Maximum
        {
            get => _maximum;
            set { _maximum = value; this.Invalidate(); }
        }

        public Color ProgressColor
        {
            get => _progressColor;
            set { _progressColor = value; this.Invalidate(); }
        }

        public string CustomText
        {
            get => _customText;
            set { _customText = value; this.Invalidate(); }
        }

        public ModernProgressBar()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            this.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            this.ForeColor = Color.White;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            float cornerRadius = 8.0f;
            using (var path = new GraphicsPath())
            {
                // Background
                path.AddArc(0, 0, cornerRadius, cornerRadius, 180, 90);
                path.AddArc(this.Width - cornerRadius, 0, cornerRadius, cornerRadius, 270, 90);
                path.AddArc(this.Width - cornerRadius, this.Height - cornerRadius, cornerRadius, cornerRadius, 0, 90);
                path.AddArc(0, this.Height - cornerRadius, cornerRadius, cornerRadius, 90, 90);
                path.CloseFigure();
                g.FillPath(new SolidBrush(Color.FromArgb(63, 63, 70)), path);

                // Progress
                if (Value > 0)
                {
                    float progressWidth = (float)this.Value / this.Maximum * this.Width;
                    if (progressWidth > cornerRadius) // Ensure width is enough to draw curves
                    {
                        using (var progressPath = new GraphicsPath())
                        {
                            progressPath.AddArc(0, 0, cornerRadius, cornerRadius, 180, 90);
                            progressPath.AddArc(progressWidth - cornerRadius, 0, cornerRadius, cornerRadius, 270, 90);
                            progressPath.AddArc(progressWidth - cornerRadius, this.Height - cornerRadius, cornerRadius, cornerRadius, 0, 90);
                            progressPath.AddArc(0, this.Height - cornerRadius, cornerRadius, cornerRadius, 90, 90);
                            progressPath.CloseFigure();
                            g.FillPath(new SolidBrush(this.ProgressColor), progressPath);
                        }
                    }
                }
            }

            // Text
            TextRenderer.DrawText(g, this.CustomText, this.Font, this.ClientRectangle, this.ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }
}