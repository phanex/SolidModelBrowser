using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace SolidModelBrowser
{
    public partial class PropertyPanel : UserControl
    {
        public PropertyPanel()
        {
            InitializeComponent();
        }

        Thickness vmargin = new Thickness(0, 8, 0, 8);
        Thickness hmargin = new Thickness(8, 0, 8, 0);
        Thickness categoryMargin = new Thickness(0, 24, 0, 8);
        Thickness categoryPadding = new Thickness(8, 4, 0, 4);
        string lastCategory = "";

        public bool ShowCategoryHeaders { get; set; } = true;

        void addProperty(PropertyInfoAttribute p)
        {
            var type = p.Property.PropertyType;

            if (ShowCategoryHeaders && p.Category != lastCategory)
            {
                Border b = new Border() { Margin = categoryMargin, Padding = categoryPadding, CornerRadius = new CornerRadius(4) };
                b.SetResourceReference(Border.BackgroundProperty, "PanelBack");
                TextBlock tb = new TextBlock() { Text = p.Category, FontWeight = FontWeights.Bold };
                b.Child = tb;
                panel.Children.Add(b);

                lastCategory = p.Category;
            }

            if (type == typeof(string))
            {
                TextBox tb = new TextBox() { Text = (string)p.Value };
                tb.TextChanged += valueChanged;
                tb.Tag = p;

                StackPanel sp = new StackPanel() { Orientation = Orientation.Vertical, Margin = vmargin };
                sp.Children.Add(new TextBlock() { Text = p.MenuLabel, ToolTip = p.Description });
                sp.Children.Add(tb);
                panel.Children.Add(sp);
            }
            if (type == typeof(bool))
            {
                CheckBox cb = new CheckBox() { IsChecked = (bool)p.Value };
                cb.Checked += valueChanged;
                cb.Unchecked += valueChanged;
                cb.Tag = p;

                StackPanel sp = new StackPanel() { Orientation = Orientation.Horizontal, Margin = vmargin };
                sp.Children.Add(cb);
                sp.Children.Add(new TextBlock() { Text = p.MenuLabel, ToolTip = p.Description });
                panel.Children.Add(sp);
            }
            if (type == typeof(int))
            {
                NumericBox nb = new NumericBox() { Value = (int)p.Value, Width = 100, RoundDigits = 0, MinValue = p.Min, MaxValue = p.Max, Increment = p.Increment };
                nb.ValueChanged += valueChanged;
                nb.Tag = p;

                StackPanel sp = new StackPanel() { Orientation = Orientation.Horizontal, Margin = vmargin };
                sp.Children.Add(nb);
                sp.Children.Add(new TextBlock() { Text = p.MenuLabel, Margin = hmargin, ToolTip = p.Description });
                panel.Children.Add(sp);
            }
            if (type == typeof(double))
            {
                NumericBox nb = new NumericBox() { Value = (double)p.Value, Width = 100, RoundDigits = 3, MinValue = p.Min, MaxValue = p.Max, Increment = p.Increment };
                nb.ValueChanged += valueChanged;
                nb.Tag = p;

                StackPanel sp = new StackPanel() { Orientation = Orientation.Horizontal, Margin = vmargin };
                sp.Children.Add(nb);
                sp.Children.Add(new TextBlock() { Text = p.MenuLabel, Margin = hmargin, ToolTip = p.Description });
                panel.Children.Add(sp);
            }
            if (type == typeof(SColor))
            {
                var curCol = ((SColor)p.Value).Color;
                var btn = new Button { Content = curCol.ToString(), Margin = vmargin };
                btn.Click += (s, e) =>
                {
                    var flyout = new ColorPickerFlyout(
                        System.Drawing.Color.FromArgb(curCol.R, curCol.G, curCol.B),
                        c =>
                        {
                            var mediaCol = System.Windows.Media.Color.FromArgb(curCol.A, c.R, c.G, c.B);
                            curCol = mediaCol;
                            p.Property.SetValue(p.Object, new SColor(mediaCol));
                            btn.Content = mediaCol.ToString();
                        });
                    var pScreen = btn.PointToScreen(new System.Windows.Point(0, btn.ActualHeight + 2));
                    flyout.ShowAt(new System.Drawing.Point((int)pScreen.X, (int)pScreen.Y));
                };

                StackPanel sp = new StackPanel() { Orientation = Orientation.Vertical, Margin = vmargin };
                sp.Children.Add(new TextBlock() { Text = p.MenuLabel, ToolTip = p.Description });
                sp.Children.Add(btn);
                panel.Children.Add(sp);
            }
            if (type == typeof(Rect))
            {
                DoubleValuesInput dvi = new DoubleValuesInput(new string[] { "X", "Y", "W", "H" }, -10000.0, 10000, 0);
                var r = (Rect)p.Value;
                dvi[0] = r.X; dvi[1] = r.Y; dvi[2] = r.Width; dvi[3] = r.Height;
                dvi.SetMinValue(2, 10.0);
                dvi.SetMinValue(3, 10.0);
                dvi.ValueChanged += valueChanged;
                dvi.Tag = p;

                StackPanel sp = new StackPanel() { Orientation = Orientation.Vertical, Margin = vmargin };
                sp.Children.Add(new TextBlock() { Text = p.MenuLabel, ToolTip = p.Description });
                sp.Children.Add(dvi);
                panel.Children.Add(sp);
            }
            if (type == typeof(Vector3D))
            {
                DoubleValuesInput dvi = new DoubleValuesInput(new string[] { "X", "Y", "Z" }, -10000.0, 10000, 0);
                var r = (Vector3D)p.Value;
                dvi[0] = r.X; dvi[1] = r.Y; dvi[2] = r.Z;
                dvi.ValueChanged += valueChanged;
                dvi.Tag = p;

                StackPanel sp = new StackPanel() { Orientation = Orientation.Vertical, Margin = vmargin };
                sp.Children.Add(new TextBlock() { Text = p.MenuLabel, ToolTip = p.Description });
                sp.Children.Add(dvi);
                panel.Children.Add(sp);
            }
        }

        public void SetObject(object o) => SetObject(o, null);

        public void SetObject(object o, params string[] categories)
        {
            try
            {
                panel.Children.Clear();
                lastCategory = "";
                var query = PropertyInfoAttribute.GetPropertiesInfoList(o, false).Where(p => p.MenuLabel != null);
                if (categories != null && categories.Length > 0)
                {
                    query = query.Where(p => categories.Contains(p.Category));
                }
                var props = query.OrderBy(p => p.Category + p.SortPrefix + p.MenuLabel);
                foreach (var p in props) addProperty(p);
            }
            catch (Exception exc) { Utils.MessageWindow($"Settings enumeration failed with message\r\n{exc.Message}"); }
        }

        void valueChanged(object Sender, EventArgs e)
        {
            FrameworkElement fwe = Sender as FrameworkElement;
            if (fwe == null) return;
            var p = fwe.Tag as PropertyInfoAttribute;
            if (p == null) return;
            Type t = p.Property.PropertyType;

            try
            {
                if (t == typeof(string)) p.Property.SetValue(p.Object, (Sender as TextBox).Text);
                if (t == typeof(bool)) p.Property.SetValue(p.Object, (Sender as CheckBox).IsChecked);
                if (t == typeof(int)) p.Property.SetValue(p.Object, (int)((Sender as NumericBox).Value));
                if (t == typeof(double)) p.Property.SetValue(p.Object, (Sender as NumericBox).Value);
                if (t == typeof(Rect)) p.Property.SetValue(p.Object, new Rect((Sender as DoubleValuesInput)[0], (Sender as DoubleValuesInput)[1], (Sender as DoubleValuesInput)[2], (Sender as DoubleValuesInput)[3]));
                if (t == typeof(Vector3D)) p.Property.SetValue(p.Object, new Vector3D((Sender as DoubleValuesInput)[0], (Sender as DoubleValuesInput)[1], (Sender as DoubleValuesInput)[2]));
            }
            catch (Exception exc) { Utils.MessageWindow($"Setting '{p.Name}' value change failed with message\r\n{exc.Message}"); }
        }
    }
}
