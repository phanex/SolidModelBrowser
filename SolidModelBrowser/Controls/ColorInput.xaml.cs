using System;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SolidModelBrowser
{
    public partial class ColorInput : UserControl
    {
        private Color currentColor = Colors.White;
        private bool isUpdating = false;

        public ColorInput()
        {
            InitializeComponent();

            BorderColorPreview.MouseLeftButtonUp += (s, e) => OpenPalette();

            TextBoxHex.LostFocus += (s, e) => ApplyHexText();
            TextBoxHex.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    ApplyHexText();
                    e.Handled = true;
                }
            };
        }

        private void OpenPalette()
        {
            using (var cd = new System.Windows.Forms.ColorDialog())
            {
                cd.FullOpen = true;
                cd.Color = System.Drawing.Color.FromArgb(currentColor.R, currentColor.G, currentColor.B);
                if (cd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    currentColor = Color.FromArgb(currentColor.A, cd.Color.R, cd.Color.G, cd.Color.B);
                    UpdateUI(fromHexText: false);
                    ValueChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private void ApplyHexText()
        {
            if (isUpdating) return;
            string text = TextBoxHex.Text.Trim();
            if (TryParseHex(text, out Color parsed))
            {
                currentColor = parsed;
                UpdateUI(fromHexText: false);
                ValueChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                UpdateUI(fromHexText: false);
            }
        }

        private bool TryParseHex(string text, out Color color)
        {
            color = currentColor;
            if (string.IsNullOrWhiteSpace(text)) return false;
            if (!text.StartsWith("#")) text = "#" + text;
            try
            {
                var obj = ColorConverter.ConvertFromString(text);
                if (obj is Color c)
                {
                    // If user entered 6-digit hex (#RRGGBB), preserve current alpha
                    if (text.Length == 7)
                    {
                        c.A = currentColor.A;
                    }
                    color = c;
                    return true;
                }
            }
            catch { }
            return false;
        }

        private void UpdateUI(bool fromHexText)
        {
            isUpdating = true;
            try
            {
                BorderColorPreview.Background = new SolidColorBrush(currentColor);
                if (!fromHexText)
                {
                    TextBoxHex.Text = currentColor.A == 255
                        ? $"#{currentColor.R:X2}{currentColor.G:X2}{currentColor.B:X2}"
                        : $"#{currentColor.A:X2}{currentColor.R:X2}{currentColor.G:X2}{currentColor.B:X2}";
                }
            }
            finally
            {
                isUpdating = false;
            }
        }

        public event EventHandler ValueChanged;

        public Color ColorValue
        {
            get => currentColor;
            set
            {
                currentColor = value;
                UpdateUI(fromHexText: false);
            }
        }
    }
}
