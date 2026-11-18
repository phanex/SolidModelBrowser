using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SolidModelBrowser
{
    internal class ColorPickerControl : UserControl
    {
        private double currentHue = 0;   // 0..360
        private double currentSat = 1.0; // 0..1
        private double currentVal = 1.0; // 0..1

        private PictureBox picSatVal;
        private PictureBox picHue;

        private bool isDraggingSatVal = false;
        private bool isDraggingHue = false;

        public event Action<Color> ColorChanged;

        public Color SelectedColor
        {
            get => ColorFromHSV(currentHue, currentSat, currentVal);
            set
            {
                ColorToHSV(value, out currentHue, out currentSat, out currentVal);
                picSatVal.Invalidate();
                picHue.Invalidate();
            }
        }

        public ColorPickerControl(Color initialColor)
        {
            this.DoubleBuffered = true;
            this.Font = SystemFonts.MessageBoxFont;
            this.BackColor = Color.FromArgb(248, 249, 250);
            this.Size = new Size(224, 154);

            int pad = 8;
            int clientW = 208;

            // 1. Saturation / Value 2D Box
            picSatVal = new PictureBox
            {
                Location = new Point(pad, pad),
                Size = new Size(clientW, 114),
                Cursor = Cursors.Cross
            };
            picSatVal.Paint += PicSatVal_Paint;
            picSatVal.MouseDown += (s, e) => { isDraggingSatVal = true; ((Control)s).Capture = true; handleSatValMouse(e); };
            picSatVal.MouseMove += (s, e) => { if (isDraggingSatVal) handleSatValMouse(e); };
            picSatVal.MouseUp += (s, e) => { isDraggingSatVal = false; ((Control)s).Capture = false; };
            this.Controls.Add(picSatVal);

            // 2. Hue Slider
            picHue = new PictureBox
            {
                Location = new Point(pad, picSatVal.Bottom + 8),
                Size = new Size(clientW, 16),
                Cursor = Cursors.Hand
            };
            picHue.Paint += PicHue_Paint;
            picHue.MouseDown += (s, e) => { isDraggingHue = true; ((Control)s).Capture = true; handleHueMouse(e); };
            picHue.MouseMove += (s, e) => { if (isDraggingHue) handleHueMouse(e); };
            picHue.MouseUp += (s, e) => { isDraggingHue = false; ((Control)s).Capture = false; };
            this.Controls.Add(picHue);

            SelectedColor = initialColor;
        }

        private void handleSatValMouse(MouseEventArgs e)
        {
            int w = picSatVal.Width - 1;
            int h = picSatVal.Height - 1;
            int x = Math.Max(0, Math.Min(w, e.X));
            int y = Math.Max(0, Math.Min(h, e.Y));

            currentSat = (double)x / w;
            currentVal = 1.0 - ((double)y / h);

            picSatVal.Invalidate();
            ColorChanged?.Invoke(SelectedColor);
        }

        private void handleHueMouse(MouseEventArgs e)
        {
            int w = picHue.Width - 1;
            int x = Math.Max(0, Math.Min(w, e.X));

            currentHue = ((double)x / w) * 360.0;
            if (currentHue >= 360.0) currentHue = 0.0;

            picHue.Invalidate();
            picSatVal.Invalidate();
            ColorChanged?.Invoke(SelectedColor);
        }

        private void PicSatVal_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int w = picSatVal.Width;
            int h = picSatVal.Height;
            var rect = new Rectangle(0, 0, w, h);

            // Base Hue fill
            Color hueColor = ColorFromHSV(currentHue, 1.0, 1.0);

            // Horizontal white gradient
            using (var brushH = new LinearGradientBrush(rect, Color.White, hueColor, LinearGradientMode.Horizontal))
            {
                g.FillRectangle(brushH, rect);
            }

            // Vertical black gradient with blend
            using (var brushV = new LinearGradientBrush(rect, Color.FromArgb(0, 0, 0, 0), Color.FromArgb(255, 0, 0, 0), LinearGradientMode.Vertical))
            {
                g.FillRectangle(brushV, rect);
            }

            // Subtle border
            using (var p = new Pen(Color.FromArgb(200, 204, 209), 1f))
            {
                g.DrawRectangle(p, 0, 0, w - 1, h - 1);
            }

            // Target reticle
            int cx = (int)(currentSat * (w - 1));
            int cy = (int)((1.0 - currentVal) * (h - 1));

            using (var penW = new Pen(Color.White, 2f))
                g.DrawEllipse(penW, cx - 4, cy - 4, 8, 8);
            using (var penB = new Pen(Color.FromArgb(40, 40, 40), 1f))
                g.DrawEllipse(penB, cx - 4, cy - 4, 8, 8);
        }

        private void PicHue_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int w = picHue.Width;
            int h = picHue.Height;
            var rect = new Rectangle(0, 0, w, h);

            using (var brush = new LinearGradientBrush(rect, Color.Red, Color.Red, LinearGradientMode.Horizontal))
            {
                var cb = new ColorBlend
                {
                    Positions = new[] { 0f, 1f / 6f, 2f / 6f, 3f / 6f, 4f / 6f, 5f / 6f, 1f },
                    Colors = new[]
                    {
                        Color.Red,
                        Color.Yellow,
                        Color.Lime,
                        Color.Cyan,
                        Color.Blue,
                        Color.Magenta,
                        Color.Red
                    }
                };
                brush.InterpolationColors = cb;
                g.FillRectangle(brush, rect);
            }

            // Subtle border
            using (var p = new Pen(Color.FromArgb(200, 204, 209), 1f))
            {
                g.DrawRectangle(p, 0, 0, w - 1, h - 1);
            }

            // Hue cursor marker
            int hx = (int)((currentHue / 360.0) * (w - 1));
            using (var brush = new SolidBrush(Color.White))
            using (var pen = new Pen(Color.FromArgb(40, 40, 40), 1f))
            {
                g.FillRectangle(brush, hx - 2, 0, 4, h);
                g.DrawRectangle(pen, hx - 2, 0, 4, h - 1);
            }
        }

        public static void ColorToHSV(Color color, out double hue, out double saturation, out double value)
        {
            int max = Math.Max(color.R, Math.Max(color.G, color.B));
            int min = Math.Min(color.R, Math.Min(color.G, color.B));

            hue = color.GetHue();
            saturation = (max == 0) ? 0 : 1d - (1d * min / max);
            value = max / 255d;
        }

        public static Color ColorFromHSV(double hue, double saturation, double value)
        {
            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            double f = hue / 60 - Math.Floor(hue / 60);

            value = value * 255;
            int v = Convert.ToInt32(value);
            int p = Convert.ToInt32(value * (1 - saturation));
            int q = Convert.ToInt32(value * (1 - f * saturation));
            int t = Convert.ToInt32(value * (1 - (1 - f) * saturation));

            v = Math.Max(0, Math.Min(255, v));
            p = Math.Max(0, Math.Min(255, p));
            q = Math.Max(0, Math.Min(255, q));
            t = Math.Max(0, Math.Min(255, t));

            if (hi == 0) return Color.FromArgb(255, v, t, p);
            else if (hi == 1) return Color.FromArgb(255, q, v, p);
            else if (hi == 2) return Color.FromArgb(255, p, v, t);
            else if (hi == 3) return Color.FromArgb(255, p, q, v);
            else if (hi == 4) return Color.FromArgb(255, t, p, v);
            else return Color.FromArgb(255, v, p, q);
        }
    }

    internal class ColorPickerFlyout : ToolStripDropDown
    {
        private ColorPickerControl pickerControl;
        private ToolStripControlHost host;

        public Color SelectedColor
        {
            get => pickerControl.SelectedColor;
            set => pickerControl.SelectedColor = value;
        }

        public ColorPickerFlyout(Color initialColor, Action<Color> onColorChanged)
        {
            pickerControl = new ColorPickerControl(initialColor);
            pickerControl.ColorChanged += onColorChanged;
            host = new ToolStripControlHost(pickerControl)
            {
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                AutoSize = false,
                Size = pickerControl.Size
            };
            this.Items.Add(host);
            this.Padding = new Padding(1);
            this.BackColor = Color.FromArgb(220, 224, 230);
            this.DropShadowEnabled = true;
            this.AutoClose = true;
        }

        public void ShowBelow(Control control)
        {
            Point screenPos = control.PointToScreen(new Point(0, control.Height + 2));
            var screen = Screen.FromControl(control).WorkingArea;
            if (screenPos.X + pickerControl.Width > screen.Right)
                screenPos.X = screen.Right - pickerControl.Width - 4;
            if (screenPos.Y + pickerControl.Height > screen.Bottom)
                screenPos.Y = control.PointToScreen(Point.Empty).Y - pickerControl.Height - 2;

            this.Show(screenPos);
        }
    }
}
