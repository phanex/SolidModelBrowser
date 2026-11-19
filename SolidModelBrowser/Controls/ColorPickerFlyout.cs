using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SolidModelBrowser
{
    internal class ColorPickerFlyout : Form
    {
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("dwmapi.dll")]
        private static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS pMarInset);

        [StructLayout(LayoutKind.Sequential)]
        private struct MARGINS
        {
            public int cxLeftWidth;
            public int cxRightWidth;
            public int cyTopHeight;
            public int cyBottomHeight;
        }

        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWCP_ROUND = 2;

        private double currentHue = 0;        // 0..360
        private double currentSat = 1.0;      // 0..1
        private double currentVal = 1.0;      // 0..1
        private int currentAlpha = 255;       // 0..255

        private PictureBox picSatVal;
        private PictureBox picHue;
        private PictureBox picAlpha;
        private PictureBox picPreview;
        private TextBox txtHex;
        private Label lblAlpha;
        private NumericUpDown numAlpha;

        private bool isUpdatingUI = false;
        private bool isDraggingSatVal = false;
        private bool isDraggingHue = false;
        private bool isDraggingAlpha = false;

        public event Action<Color> ColorChanged;

        public Color SelectedColor
        {
            get => ColorFromHSV(currentHue, currentSat, currentVal, currentAlpha);
            set
            {
                ColorToHSV(value, out currentHue, out currentSat, out currentVal);
                currentAlpha = value.A;
                updateAllControls();
            }
        }

        public ColorPickerFlyout(Color initialColor, Action<Color> onColorChanged)
        {
            this.ColorChanged = onColorChanged;

            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.Manual;
            this.DoubleBuffered = true;
            this.Font = SystemFonts.MessageBoxFont;
            this.BackColor = Color.FromArgb(251, 251, 252);
            this.Size = new Size(252, 222);

            int pad = 10;
            int clientW = 232;

            // 1. 2D Saturation / Value Canvas
            picSatVal = new PictureBox
            {
                Location = new Point(pad, pad),
                Size = new Size(clientW, 116),
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
                Size = new Size(clientW, 14),
                Cursor = Cursors.Hand
            };
            picHue.Paint += PicHue_Paint;
            picHue.MouseDown += (s, e) => { isDraggingHue = true; ((Control)s).Capture = true; handleHueMouse(e); };
            picHue.MouseMove += (s, e) => { if (isDraggingHue) handleHueMouse(e); };
            picHue.MouseUp += (s, e) => { isDraggingHue = false; ((Control)s).Capture = false; };
            this.Controls.Add(picHue);

            // 3. Alpha Slider with Checkerboard
            picAlpha = new PictureBox
            {
                Location = new Point(pad, picHue.Bottom + 6),
                Size = new Size(clientW, 14),
                Cursor = Cursors.Hand
            };
            picAlpha.Paint += PicAlpha_Paint;
            picAlpha.MouseDown += (s, e) => { isDraggingAlpha = true; ((Control)s).Capture = true; handleAlphaMouse(e); };
            picAlpha.MouseMove += (s, e) => { if (isDraggingAlpha) handleAlphaMouse(e); };
            picAlpha.MouseUp += (s, e) => { isDraggingAlpha = false; ((Control)s).Capture = false; };
            picAlpha.MouseWheel += (s, e) =>
            {
                int step = (e.Delta > 0 ? 13 : -13);
                currentAlpha = Math.Max(0, Math.Min(255, currentAlpha + step));
                updateAllControls();
                ColorChanged?.Invoke(SelectedColor);
            };
            this.Controls.Add(picAlpha);

            // 4. Hex & Numeric Alpha Row
            int rowY = picAlpha.Bottom + 8;

            picPreview = new PictureBox
            {
                Location = new Point(pad, rowY),
                Size = new Size(24, 24)
            };
            picPreview.Paint += (s, e) =>
            {
                drawCheckerboard(e.Graphics, picPreview.ClientRectangle);
                using (var b = new SolidBrush(SelectedColor))
                    e.Graphics.FillRectangle(b, picPreview.ClientRectangle);
                using (var pen = new Pen(Color.FromArgb(215, 218, 222), 1f))
                    e.Graphics.DrawRectangle(pen, 0, 0, picPreview.Width - 1, picPreview.Height - 1);
            };
            this.Controls.Add(picPreview);

            txtHex = new TextBox
            {
                Location = new Point(picPreview.Right + 6, rowY),
                Size = new Size(82, 24),
                CharacterCasing = CharacterCasing.Upper,
                Font = new Font("Consolas", 9.5f)
            };
            txtHex.TextChanged += (s, e) =>
            {
                if (isUpdatingUI) return;
                string clean = txtHex.Text.Trim().TrimStart('#');
                if (clean.Length == 6)
                {
                    try
                    {
                        int r = Convert.ToInt32(clean.Substring(0, 2), 16);
                        int g = Convert.ToInt32(clean.Substring(2, 2), 16);
                        int b = Convert.ToInt32(clean.Substring(4, 2), 16);
                        var c = Color.FromArgb(currentAlpha, r, g, b);
                        ColorToHSV(c, out currentHue, out currentSat, out currentVal);
                        picSatVal.Invalidate();
                        picHue.Invalidate();
                        picAlpha.Invalidate();
                        picPreview.Invalidate();
                        ColorChanged?.Invoke(SelectedColor);
                    }
                    catch { }
                }
                else if (clean.Length == 8)
                {
                    try
                    {
                        int a = Convert.ToInt32(clean.Substring(0, 2), 16);
                        int r = Convert.ToInt32(clean.Substring(2, 2), 16);
                        int g = Convert.ToInt32(clean.Substring(4, 2), 16);
                        int b = Convert.ToInt32(clean.Substring(6, 2), 16);
                        currentAlpha = a;
                        var c = Color.FromArgb(currentAlpha, r, g, b);
                        ColorToHSV(c, out currentHue, out currentSat, out currentVal);
                        updateAllControls();
                        ColorChanged?.Invoke(SelectedColor);
                    }
                    catch { }
                }
            };
            this.Controls.Add(txtHex);

            lblAlpha = new Label
            {
                Text = "Alpha:",
                Location = new Point(txtHex.Right + 8, rowY + 4),
                AutoSize = true,
                ForeColor = Color.FromArgb(90, 95, 100)
            };
            this.Controls.Add(lblAlpha);

            numAlpha = new NumericUpDown
            {
                Location = new Point(lblAlpha.Right + 2, rowY),
                Size = new Size(54, 24),
                Minimum = 0,
                Maximum = 100,
                Increment = 5,
                Value = 100
            };
            numAlpha.ValueChanged += (s, e) =>
            {
                if (isUpdatingUI) return;
                currentAlpha = (int)Math.Round((double)numAlpha.Value * 255.0 / 100.0);
                picAlpha.Invalidate();
                picPreview.Invalidate();
                updateHexFieldOnly();
                ColorChanged?.Invoke(SelectedColor);
            };
            this.Controls.Add(numAlpha);

            SelectedColor = initialColor;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                const int CS_DROPSHADOW = 0x00020000;
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= CS_DROPSHADOW;
                return cp;
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            try
            {
                int preference = DWMWCP_ROUND;
                DwmSetWindowAttribute(this.Handle, DWMWA_WINDOW_CORNER_PREFERENCE, ref preference, sizeof(int));

                var margins = new MARGINS { cxLeftWidth = 1, cxRightWidth = 1, cyTopHeight = 1, cyBottomHeight = 1 };
                DwmExtendFrameIntoClientArea(this.Handle, ref margins);
            }
            catch { }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            // Subtle 1px Windows 11 flyout border
            using (var pen = new Pen(Color.FromArgb(226, 228, 232), 1f))
            {
                e.Graphics.DrawRectangle(pen, 0, 0, this.Width - 1, this.Height - 1);
            }
        }

        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);
            this.Close();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape || keyData == Keys.Enter)
            {
                this.Close();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        public void ShowBelow(Control control)
        {
            Point screenPos = control.PointToScreen(new Point(0, control.Height + 2));
            var screen = Screen.FromControl(control).WorkingArea;
            if (screenPos.X + this.Width > screen.Right)
                screenPos.X = screen.Right - this.Width - 4;
            if (screenPos.Y + this.Height > screen.Bottom)
                screenPos.Y = control.PointToScreen(Point.Empty).Y - this.Height - 2;

            this.Location = screenPos;
            this.Show(control.FindForm());
        }

        public void ShowAt(Point screenPoint)
        {
            var screen = Screen.FromPoint(screenPoint).WorkingArea;
            if (screenPoint.X + this.Width > screen.Right)
                screenPoint.X = screen.Right - this.Width - 4;
            if (screenPoint.Y + this.Height > screen.Bottom)
                screenPoint.Y = screen.Bottom - this.Height - 4;

            this.Location = screenPoint;
            this.Show();
        }

        private void handleSatValMouse(MouseEventArgs e)
        {
            int w = picSatVal.Width;
            int h = picSatVal.Height;
            if (w <= 0 || h <= 0) return;

            int x = Math.Max(0, Math.Min(w, e.X));
            int y = Math.Max(0, Math.Min(h, e.Y));

            currentSat = (double)x / w;
            currentVal = 1.0 - ((double)y / h);

            picSatVal.Invalidate();
            picPreview.Invalidate();
            updateHexFieldOnly();
            ColorChanged?.Invoke(SelectedColor);
        }

        private void handleHueMouse(MouseEventArgs e)
        {
            int w = picHue.Width;
            if (w <= 0) return;

            int x = Math.Max(0, Math.Min(w, e.X));
            currentHue = ((double)x / w) * 360.0;
            if (currentHue >= 360.0) currentHue = 0.0;

            picHue.Invalidate();
            picSatVal.Invalidate();
            picAlpha.Invalidate();
            picPreview.Invalidate();
            updateHexFieldOnly();
            ColorChanged?.Invoke(SelectedColor);
        }

        private void handleAlphaMouse(MouseEventArgs e)
        {
            int w = picAlpha.Width;
            if (w <= 0) return;

            int x = Math.Max(0, Math.Min(w, e.X));
            currentAlpha = (int)Math.Round(((double)x / w) * 255.0);
            currentAlpha = Math.Max(0, Math.Min(255, currentAlpha));

            picAlpha.Invalidate();
            picPreview.Invalidate();
            updateHexFieldOnly();
            isUpdatingUI = true;
            numAlpha.Value = Math.Max(0, Math.Min(100, (decimal)Math.Round(currentAlpha * 100.0 / 255.0)));
            isUpdatingUI = false;
            ColorChanged?.Invoke(SelectedColor);
        }

        private void updateHexFieldOnly()
        {
            isUpdatingUI = true;
            var col = SelectedColor;
            txtHex.Text = currentAlpha == 255
                ? $"{col.R:X2}{col.G:X2}{col.B:X2}"
                : $"{currentAlpha:X2}{col.R:X2}{col.G:X2}{col.B:X2}";
            isUpdatingUI = false;
        }

        private void updateAllControls()
        {
            isUpdatingUI = true;
            picSatVal.Invalidate();
            picHue.Invalidate();
            picAlpha.Invalidate();
            picPreview.Invalidate();

            var col = SelectedColor;
            txtHex.Text = currentAlpha == 255
                ? $"{col.R:X2}{col.G:X2}{col.B:X2}"
                : $"{currentAlpha:X2}{col.R:X2}{col.G:X2}{col.B:X2}";
            numAlpha.Value = Math.Max(0, Math.Min(100, (decimal)Math.Round(currentAlpha * 100.0 / 255.0)));
            isUpdatingUI = false;
        }

        private void PicSatVal_Paint(object sender, PaintEventArgs e)
        {
            int w = picSatVal.Width;
            int h = picSatVal.Height;
            if (w <= 0 || h <= 0) return;

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, w, h);

            Color hueColor = ColorFromHSV(currentHue, 1.0, 1.0);

            using (var brushH = new LinearGradientBrush(rect, Color.White, hueColor, LinearGradientMode.Horizontal))
            {
                g.FillRectangle(brushH, rect);
            }

            using (var brushV = new LinearGradientBrush(rect, Color.FromArgb(0, 0, 0, 0), Color.FromArgb(255, 0, 0, 0), LinearGradientMode.Vertical))
            {
                g.FillRectangle(brushV, rect);
            }

            // Subtle border
            using (var pen = new Pen(Color.FromArgb(215, 218, 222), 1f))
            {
                g.DrawRectangle(pen, 0, 0, w - 1, h - 1);
            }

            // Target ring
            int cx = (int)Math.Round(currentSat * (w - 1));
            int cy = (int)Math.Round((1.0 - currentVal) * (h - 1));

            using (var penDark = new Pen(Color.FromArgb(160, 0, 0, 0), 2f))
                g.DrawEllipse(penDark, cx - 4, cy - 4, 8, 8);
            using (var penWhite = new Pen(Color.White, 1.5f))
                g.DrawEllipse(penWhite, cx - 4, cy - 4, 8, 8);
        }

        private void PicHue_Paint(object sender, PaintEventArgs e)
        {
            int w = picHue.Width;
            int h = picHue.Height;
            if (w <= 0 || h <= 0) return;

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, w, h);

            using (var brush = new LinearGradientBrush(rect, Color.Red, Color.Red, LinearGradientMode.Horizontal))
            {
                var blend = new ColorBlend
                {
                    Positions = new float[] { 0f, 1f / 6f, 2f / 6f, 3f / 6f, 4f / 6f, 5f / 6f, 1f },
                    Colors = new Color[] { Color.Red, Color.Yellow, Color.Lime, Color.Cyan, Color.Blue, Color.Magenta, Color.Red }
                };
                brush.InterpolationColors = blend;
                g.FillRectangle(brush, rect);
            }

            // Subtle border
            using (var pen = new Pen(Color.FromArgb(215, 218, 222), 1f))
            {
                g.DrawRectangle(pen, 0, 0, w - 1, h - 1);
            }

            // Thumb
            int tx = (int)Math.Round((currentHue / 360.0) * (w - 1));
            using (var penW = new Pen(Color.White, 2f))
                g.DrawRectangle(penW, tx - 2, 0, 4, h - 1);
            using (var penB = new Pen(Color.FromArgb(50, 50, 50), 1f))
                g.DrawRectangle(penB, tx - 2, 0, 4, h - 1);
        }

        private void PicAlpha_Paint(object sender, PaintEventArgs e)
        {
            int w = picAlpha.Width;
            int h = picAlpha.Height;
            if (w <= 0 || h <= 0) return;

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, w, h);

            drawCheckerboard(g, rect);

            var colSolid = ColorFromHSV(currentHue, currentSat, currentVal, 255);
            var colZero = Color.FromArgb(0, colSolid.R, colSolid.G, colSolid.B);

            using (var brush = new LinearGradientBrush(rect, colZero, colSolid, LinearGradientMode.Horizontal))
            {
                g.FillRectangle(brush, rect);
            }

            // Subtle border
            using (var pen = new Pen(Color.FromArgb(215, 218, 222), 1f))
            {
                g.DrawRectangle(pen, 0, 0, w - 1, h - 1);
            }

            // Thumb
            int tx = (int)Math.Round((currentAlpha / 255.0) * (w - 1));
            using (var penW = new Pen(Color.White, 2f))
                g.DrawRectangle(penW, tx - 2, 0, 4, h - 1);
            using (var penB = new Pen(Color.FromArgb(50, 50, 50), 1f))
                g.DrawRectangle(penB, tx - 2, 0, 4, h - 1);
        }

        private void drawCheckerboard(Graphics g, Rectangle rect)
        {
            int cellSize = 5;
            using (var b1 = new SolidBrush(Color.White))
            using (var b2 = new SolidBrush(Color.FromArgb(224, 226, 230)))
            {
                for (int y = rect.Top; y < rect.Bottom; y += cellSize)
                {
                    for (int x = rect.Left; x < rect.Right; x += cellSize)
                    {
                        var b = ((x / cellSize) + (y / cellSize)) % 2 == 0 ? b1 : b2;
                        g.FillRectangle(b, x, y, Math.Min(cellSize, rect.Right - x), Math.Min(cellSize, rect.Bottom - y));
                    }
                }
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

        public static Color ColorFromHSV(double hue, double saturation, double value, int alpha = 255)
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

            if (hi == 0) return Color.FromArgb(alpha, v, t, p);
            else if (hi == 1) return Color.FromArgb(alpha, q, v, p);
            else if (hi == 2) return Color.FromArgb(alpha, p, v, t);
            else if (hi == 3) return Color.FromArgb(alpha, p, q, v);
            else if (hi == 4) return Color.FromArgb(alpha, t, p, v);
            else return Color.FromArgb(alpha, v, p, q);
        }
    }
}
