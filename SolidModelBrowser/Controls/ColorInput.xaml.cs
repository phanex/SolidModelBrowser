using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SolidModelBrowser
{
    public partial class ColorInput : UserControl
    {
        private Color currentColor = Colors.White;
        private bool isUpdating = false;

        private static readonly string[] CadPresets =
        {
            "#9AA2AA", "#E2E6EB", "#4A90E2", "#50E3C2", "#F5A623",
            "#00AE42", "#E02020", "#F8E71C", "#9013FE", "#5D6D7E"
        };

        private static readonly string[] GrayPresets =
        {
            "#FFFFFF", "#D0D4DC", "#8C929D", "#50545C", "#303236",
            "#202226", "#B8BCC4", "#68707C", "#40444C", "#000000"
        };

        public ColorInput()
        {
            InitializeComponent();

            initPresets();
            BorderColorPreview.MouseLeftButtonUp += (s, e) => PopupPalette.IsOpen = !PopupPalette.IsOpen;

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

        private void initPresets()
        {
            foreach (var hex in CadPresets)
                WrapPanelPresets.Children.Add(createSwatch(hex));

            foreach (var hex in GrayPresets)
                WrapPanelGrays.Children.Add(createSwatch(hex));
        }

        private FrameworkElement createSwatch(string hex)
        {
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            brush.Freeze();

            var border = new Border
            {
                Width = 26,
                Height = 22,
                Margin = new Thickness(2),
                Background = brush,
                BorderBrush = (Brush)Application.Current.FindResource("Separator"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                Cursor = Cursors.Hand,
                ToolTip = hex
            };

            border.MouseLeftButtonUp += (s, e) =>
            {
                if (TryParseHex(hex, out Color parsed))
                {
                    currentColor = parsed;
                    UpdateUI(fromHexText: false);
                    ValueChanged?.Invoke(this, EventArgs.Empty);
                    PopupPalette.IsOpen = false;
                }
            };

            return border;
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
                    if (text.Length == 7) c.A = currentColor.A;
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
