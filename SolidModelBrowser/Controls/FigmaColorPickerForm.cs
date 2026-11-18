using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SolidModelBrowser
{
    public class FigmaColorPickerForm : Form
    {
        private double currentHue = 0;        // 0..360
        private double currentSat = 1.0;      // 0..1
        private double currentVal = 1.0;      // 0..1
        private int currentAlpha = 255;       // 0..255

        private PictureBox picSatVal;
        private PictureBox picHue;
        private PictureBox picAlpha;
        private PictureBox picPreview;
        private TextBox txtHex;
        private NumericUpDown numAlpha;
        private Button btnOk;
        private Button btnCancel;

        private bool isUpdatingUI = false;
        private bool isDraggingSatVal = false;
        private bool isDraggingHue = false;
        private bool isDraggingAlpha = false;

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

        public FigmaColorPickerForm(Color initialColor)
        {
            this.Text = "Color Picker";
            this.Size = new Size(270, 390);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Font = SystemFonts.MessageBoxFont;

            int pad = 12;
            int clientW = 230;

            // 1. 2D Saturation / Value Box
            picSatVal = new PictureBox
            {
                Location = new Point(pad, pad),
                Size = new Size(clientW, 140),
                BorderStyle = BorderStyle.FixedSingle,
                Cursor = Cursors.Cross
            };
            picSatVal.Paint += PicSatVal_Paint;
            picSatVal.MouseDown += (s, e) => { isDraggingSatVal = true; ((Control)s).Capture = true; handleSatValMouse(e); };
            picSatVal.MouseMove += (s, e) => { if (isDraggingSatVal) handleSatValMouse(e); };
            picSatVal.MouseUp += (s, e) => { isDraggingSatVal = false; ((Control)s).Capture = false; };
            this.Controls.Add(picSatVal);

            // 2. Hue Slider (Rainbow)
            picHue = new PictureBox
            {
                Location = new Point(pad, picSatVal.Bottom + 10),
                Size = new Size(clientW, 14),
                BorderStyle = BorderStyle.FixedSingle,
                Cursor = Cursors.Hand
            };
            picHue.Paint += PicHue_Paint;
            picHue.MouseDown += (s, e) => { isDraggingHue = true; ((Control)s).Capture = true; handleHueMouse(e); };
            picHue.MouseMove += (s, e) => { if (isDraggingHue) handleHueMouse(e); };
            picHue.MouseUp += (s, e) => { isDraggingHue = false; ((Control)s).Capture = false; };
            this.Controls.Add(picHue);

            // 3. Alpha Slider (Opacity Checkerboard)
            picAlpha = new PictureBox
            {
                Location = new Point(pad, picHue.Bottom + 8),
                Size = new Size(clientW, 14),
                BorderStyle = BorderStyle.FixedSingle,
                Cursor = Cursors.Hand
            };
            picAlpha.Paint += PicAlpha_Paint;
            picAlpha.MouseDown += (s, e) => { isDraggingAlpha = true; ((Control)s).Capture = true; handleAlphaMouse(e); };
            picAlpha.MouseMove += (s, e) => { if (isDraggingAlpha) handleAlphaMouse(e); };
            picAlpha.MouseUp += (s, e) => { isDraggingAlpha = false; ((Control)s).Capture = false; };
            this.Controls.Add(picAlpha);

            // 4. Hex & Opacity Row
            int rowY = picAlpha.Bottom + 12;

            picPreview = new PictureBox
            {
                Location = new Point(pad, rowY),
                Size = new Size(24, 24),
                BorderStyle = BorderStyle.FixedSingle
            };
            picPreview.Paint += (s, e) =>
            {
                drawCheckerboard(e.Graphics, picPreview.ClientRectangle);
                using (var b = new SolidBrush(SelectedColor))
                    e.Graphics.FillRectangle(b, picPreview.ClientRectangle);
            };
            this.Controls.Add(picPreview);

            Label lblHex = new Label
            {
                Text = "Hex",
                Location = new Point(picPreview.Right + 6, rowY + 3),
                AutoSize = true
            };
            this.Controls.Add(lblHex);

            txtHex = new TextBox
            {
                Location = new Point(lblHex.Right + 4, rowY),
                Size = new Size(72, 24),
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
                    }
                    catch { }
                }
            };
            this.Controls.Add(txtHex);

            numAlpha = new NumericUpDown
            {
                Location = new Point(txtHex.Right + 8, rowY),
                Size = new Size(58, 24),
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
            };
            this.Controls.Add(numAlpha);

            // 5. OK and Cancel Buttons
            int btnY = rowY + 36;
            btnOk = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new Point(clientW + pad - 146, btnY),
                Size = new Size(68, 26)
            };
            this.AcceptButton = btnOk;
            this.Controls.Add(btnOk);

            btnCancel = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(clientW + pad - 72, btnY),
                Size = new Size(68, 26)
            };
            this.CancelButton = btnCancel;
            this.Controls.Add(btnCancel);

            this.ClientSize = new Size(clientW + pad * 2, btnY + 36);

            SelectedColor = initialColor;
        }

        private void handleSatValMouse(MouseEventArgs e)
        {
            double x = Math.Max(0, Math.Min(picSatVal.Width, e.X));
            double y = Math.Max(0, Math.Min(picSatVal.Height, e.Y));
            currentSat = x / picSatVal.Width;
            currentVal = 1.0 - (y / picSatVal.Height);
            updateAllControls();
        }

        private void handleHueMouse(MouseEventArgs e)
        {
            double x = Math.Max(0, Math.Min(picHue.Width, e.X));
            currentHue = (x / picHue.Width) * 360.0;
            if (currentHue >= 360.0) currentHue = 0;
            updateAllControls();
        }

        private void handleAlphaMouse(MouseEventArgs e)
        {
            double x = Math.Max(0, Math.Min(picAlpha.Width, e.X));
            currentAlpha = (int)Math.Round((x / picAlpha.Width) * 255.0);
            updateAllControls();
        }

        private void updateAllControls()
        {
            isUpdatingUI = true;
            picSatVal.Invalidate();
            picHue.Invalidate();
            picAlpha.Invalidate();
            picPreview.Invalidate();

            var col = SelectedColor;
            txtHex.Text = $"{col.R:X2}{col.G:X2}{col.B:X2}";
            numAlpha.Value = Math.Max(0, Math.Min(100, (decimal)Math.Round(currentAlpha * 100.0 / 255.0)));
            isUpdatingUI = false;
        }

        private void PicSatVal_Paint(object sender, PaintEventArgs e)
        {
            int w = picSatVal.Width;
            int h = picSatVal.Height;
            if (w <= 0 || h <= 0) return;

            var pureHue = ColorFromHSV(currentHue, 1.0, 1.0, 255);

            // Horizontal gradient: White to Pure Hue
            using (var brushH = new LinearGradientBrush(new Point(0, 0), new Point(w, 0), Color.White, pureHue))
            {
                e.Graphics.FillRectangle(brushH, 0, 0, w, h);
            }

            // Vertical gradient: Transparent to Black
            using (var brushV = new LinearGradientBrush(new Point(0, 0), new Point(0, h), Color.FromArgb(0, 0, 0, 0), Color.FromArgb(255, 0, 0, 0)))
            {
                e.Graphics.FillRectangle(brushV, 0, 0, w, h);
            }

            // Target ring
            int cx = (int)Math.Round(currentSat * w);
            int cy = (int)Math.Round((1.0 - currentVal) * h);
            cx = Math.Max(4, Math.Min(w - 5, cx));
            cy = Math.Max(4, Math.Min(h - 5, cy));

            using (var penDark = new Pen(Color.FromArgb(180, 0, 0, 0), 2f))
                e.Graphics.DrawEllipse(penDark, cx - 4, cy - 4, 8, 8);
            using (var penWhite = new Pen(Color.White, 1.5f))
                e.Graphics.DrawEllipse(penWhite, cx - 4, cy - 4, 8, 8);
        }

        private void PicHue_Paint(object sender, PaintEventArgs e)
        {
            int w = picHue.Width;
            int h = picHue.Height;
            if (w <= 0 || h <= 0) return;

            using (var brush = new LinearGradientBrush(new Point(0, 0), new Point(w, 0), Color.Red, Color.Red))
            {
                var blend = new ColorBlend
                {
                    Positions = new float[] { 0f, 1f / 6f, 2f / 6f, 3f / 6f, 4f / 6f, 5f / 6f, 1f },
                    Colors = new Color[] { Color.Red, Color.Yellow, Color.Lime, Color.Cyan, Color.Blue, Color.Magenta, Color.Red }
                };
                brush.InterpolationColors = blend;
                e.Graphics.FillRectangle(brush, 0, 0, w, h);
            }

            // Thumb
            int tx = (int)Math.Round((currentHue / 360.0) * w);
            tx = Math.Max(2, Math.Min(w - 3, tx));
            using (var pen = new Pen(Color.White, 2f))
                e.Graphics.DrawRectangle(pen, tx - 2, 0, 4, h - 1);
            using (var pen = new Pen(Color.Black, 1f))
                e.Graphics.DrawRectangle(pen, tx - 3, 0, 6, h - 1);
        }

        private void PicAlpha_Paint(object sender, PaintEventArgs e)
        {
            int w = picAlpha.Width;
            int h = picAlpha.Height;
            if (w <= 0 || h <= 0) return;

            drawCheckerboard(e.Graphics, new Rectangle(0, 0, w, h));

            var colSolid = ColorFromHSV(currentHue, currentSat, currentVal, 255);
            var colZero = Color.FromArgb(0, colSolid.R, colSolid.G, colSolid.B);

            using (var brush = new LinearGradientBrush(new Point(0, 0), new Point(w, 0), colZero, colSolid))
            {
                e.Graphics.FillRectangle(brush, 0, 0, w, h);
            }

            // Thumb
            int tx = (int)Math.Round((currentAlpha / 255.0) * w);
            tx = Math.Max(2, Math.Min(w - 3, tx));
            using (var pen = new Pen(Color.White, 2f))
                e.Graphics.DrawRectangle(pen, tx - 2, 0, 4, h - 1);
            using (var pen = new Pen(Color.Black, 1f))
                e.Graphics.DrawRectangle(pen, tx - 3, 0, 6, h - 1);
        }

        private void drawCheckerboard(Graphics g, Rectangle rect)
        {
            int cellSize = 6;
            using (var b1 = new SolidBrush(Color.White))
            using (var b2 = new SolidBrush(Color.FromArgb(220, 220, 220)))
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
