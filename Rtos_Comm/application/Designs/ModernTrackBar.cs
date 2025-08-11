// FILE: ModernTrackBar.cs (Corrected Version)

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Rtos_Comm
{
    public class ModernTrackBar : Control
    {
        private int _value = 50;
        private int _minimum = 0;
        private int _maximum = 100;
        private bool _isDragging = false;

        public event EventHandler ValueChanged;

        // --- New Properties for better control ---
        public int ThumbSize { get; set; } = 18;
        public int TrackHeight { get; set; } = 4;

        public int Value
        {
            get => _value;
            set
            {
                int clampedValue = Math.Max(_minimum, Math.Min(value, _maximum));
                if (_value != clampedValue)
                {
                    _value = clampedValue;
                    ValueChanged?.Invoke(this, EventArgs.Empty);
                    this.Invalidate();
                }
            }
        }
        public int Minimum { get => _minimum; set { _minimum = value; this.Invalidate(); } }
        public int Maximum { get => _maximum; set { _maximum = value; this.Invalidate(); } }

        public Color TrackColor { get; set; } = Color.FromArgb(63, 63, 70);
        public Color ThumbColor { get; set; } = Color.FromArgb(59, 130, 246);
        public Color TrackProgressColor { get; set; } = Color.FromArgb(59, 130, 246);

        public ModernTrackBar()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            this.Size = new Size(150, 28); // Give it a default reasonable height
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // --- REDESIGNED DRAWING LOGIC ---

            // Vertically center the track and thumb inside the control's bounds
            int trackY = (this.Height - this.TrackHeight) / 2;
            int thumbY = (this.Height - this.ThumbSize) / 2;

            // Define the track rectangle. It starts after half a thumb and ends before half a thumb.
            Rectangle trackRect = new Rectangle(this.ThumbSize / 2, trackY, this.Width - this.ThumbSize, this.TrackHeight);

            // If the control is too small to draw, exit to prevent errors.
            if (trackRect.Width <= 0) return;

            // Calculate thumb X position
            float percentage = (_maximum - _minimum == 0) ? 0 : (float)(_value - _minimum) / (_maximum - _minimum);
            int thumbX = trackRect.X + (int)(percentage * trackRect.Width);

            Rectangle thumbRect = new Rectangle(thumbX - this.ThumbSize / 2, thumbY, this.ThumbSize, this.ThumbSize);

            // 1. Draw track background
            using (var brush = new SolidBrush(TrackColor))
            using (var path = GetRoundedRect(trackRect, this.TrackHeight / 2.0f))
            {
                g.FillPath(brush, path);
            }

            // 2. Draw track progress
            if (_value > _minimum)
            {
                Rectangle progressRect = new Rectangle(trackRect.X, trackRect.Y, thumbX - trackRect.X, trackRect.Height);
                using (var brush = new SolidBrush(TrackProgressColor))
                using (var path = GetRoundedRect(progressRect, this.TrackHeight / 2.0f))
                {
                    g.FillPath(brush, path);
                }
            }

            // 3. Draw thumb
            using (var brush = new SolidBrush(ThumbColor))
            {
                g.FillEllipse(brush, thumbRect);
            }
        }

        // This method is now safe and correct
        private GraphicsPath GetRoundedRect(Rectangle rect, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            if (rect.Width <= 0 || rect.Height <= 0)
            {
                path.AddRectangle(rect);
                return path;
            }
            if (radius <= 0)
            {
                path.AddRectangle(rect);
                return path;
            }

            float diameter = radius * 2;
            if (diameter > rect.Width) diameter = rect.Width;
            if (diameter > rect.Height) diameter = rect.Height;

            path.StartFigure();
            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                _isDragging = true;
                UpdateValueFromMouse(e.X);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_isDragging)
            {
                UpdateValueFromMouse(e);
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _isDragging = false;
        }

        // Overload to handle both mouse move and mouse down
        private void UpdateValueFromMouse(MouseEventArgs e)
        {
            UpdateValueFromMouse(e.X);
        }

        private void UpdateValueFromMouse(int mouseX)
        {
            // The clickable area is the track itself
            int trackXStart = this.ThumbSize / 2;
            int trackWidth = this.Width - this.ThumbSize;
            if (trackWidth <= 0) return;

            // Calculate percentage based on mouse position relative to the track
            float percentage = (float)(mouseX - trackXStart) / trackWidth;
            int newValue = _minimum + (int)Math.Round(percentage * (_maximum - _minimum));

            // The Value property will automatically clamp the value between min and max
            this.Value = newValue;
        }
    }
}