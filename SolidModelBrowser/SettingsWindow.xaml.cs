using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SolidModelBrowser
{
    public enum SettingsWindowResult { None, ResetDefaults, OpenEditor };

    public partial class SettingsWindow : Window
    {
        public SettingsWindowResult WindowResult { get; set; } = SettingsWindowResult.None;
        private Settings settings;
        private List<ExternalAppItem> currentApps = new List<ExternalAppItem>();

        public SettingsWindow()
        {
            InitializeComponent();

            ButtonClose.Click += (s, e) => Close();
            BorderHeader.MouseDown += (s, e) => { if (e.ChangedButton == MouseButton.Left) this.DragMove(); };

            ButtonLoadDefaults.Click += (s, e) =>
            {
                WindowResult = SettingsWindowResult.ResetDefaults;
                DialogResult = true;
                Close();
            };

            ButtonOpenInTextEditor.Click += (s, e) =>
            {
                WindowResult = SettingsWindowResult.OpenEditor;
                DialogResult = true;
                Close();
            };

            ButtonSave.Click += (s, e) =>
            {
                saveUIToSettings();
                DialogResult = true;
                Close();
            };

            ButtonCancel.Click += (s, e) => Close();

            TextBoxSearch.TextChanged += (s, e) => filterSettings(TextBoxSearch.Text);
            ButtonClearSearch.Click += (s, e) => TextBoxSearch.Text = "";

            ButtonAddApp.Click += ButtonAddApp_Click;

            ButtonSelectAllAssoc.Click += (s, e) =>
            {
                foreach (var ext in FileAssociationManager.SupportedExtensions)
                    FileAssociationManager.SetAssociation(ext, true);
                renderFileAssociationsList();
            };

            ButtonDeselectAllAssoc.Click += (s, e) =>
            {
                foreach (var ext in FileAssociationManager.SupportedExtensions)
                    FileAssociationManager.SetAssociation(ext, false);
                renderFileAssociationsList();
            };
        }

        internal void BindSettings(Settings s)
        {
            settings = s;
            currentApps = settings.GetExternalApps();

            // General & Interface
            CheckShowFilePanel.IsChecked = s.ShowFilePanel;
            CheckColorizeFiles.IsChecked = s.ColorizeFiles;
            CheckSortFilesByExtensions.IsChecked = s.SortFilesByExtensions;
            CheckUseSystemWindowStyle.IsChecked = s.UseSystemWindowStyle;
            CheckUnsmoothAfterLoading.IsChecked = s.UnsmoothAfterLoading;

            // Camera & Viewport
            CheckCenterCameraAtPointsAverageCenter.IsChecked = s.CenterCameraAtPointsAverageCenter;
            TextFov.Text = s.FOV.ToString("0.#");
            TextFishEyeFov.Text = s.FishEyeFOV.ToString("0.#");
            TextCameraInitialShift.Text = s.CameraInitialShift.ToString("0.#");
            TextModelRotationAngle.Text = s.ModelRotationAngle.ToString("0.#");

            // G-Code Import
            TextGCodeLineEnds.Text = s.GcodeLineEndsExt.ToString();
            TextGCodeLayerHeight.Text = s.GcodeLayerHeight.ToString("0.##");
            TextGCodeLineWidth.Text = s.GcodeLineWidth.ToString("0.##");

            // Colors & Materials
            ColorDiffuse.ColorValue = s.DiffuseColor.Color;
            ColorSpecular.ColorValue = s.SpecularColor.Color;
            ColorAmbient.ColorValue = s.AmbientLightColor.Color;
            ColorLight1.ColorValue = s.DirectionalLight1Color.Color;
            ColorLight2.ColorValue = s.DirectionalLight2Color.Color;
            ColorLight3.ColorValue = s.DirectionalLight3Color.Color;
            CheckBacksideDiffuseMaterial.IsChecked = s.IsBackDiffuseEnabled;

            renderExternalAppsList();
            renderFileAssociationsList();
        }

        private void saveUIToSettings()
        {
            if (settings == null) return;

            // General & Interface
            settings.ShowFilePanel = CheckShowFilePanel.IsChecked == true;
            settings.ColorizeFiles = CheckColorizeFiles.IsChecked == true;
            settings.SortFilesByExtensions = CheckSortFilesByExtensions.IsChecked == true;
            settings.UseSystemWindowStyle = CheckUseSystemWindowStyle.IsChecked == true;
            settings.UnsmoothAfterLoading = CheckUnsmoothAfterLoading.IsChecked == true;

            // Camera & Viewport
            settings.CenterCameraAtPointsAverageCenter = CheckCenterCameraAtPointsAverageCenter.IsChecked == true;
            if (double.TryParse(TextFov.Text, out double fov)) settings.FOV = Utils.MinMax(fov, 5.0, 160.0);
            if (double.TryParse(TextFishEyeFov.Text, out double feFov)) settings.FishEyeFOV = Utils.MinMax(feFov, 5.0, 160.0);
            if (double.TryParse(TextCameraInitialShift.Text, out double shift)) settings.CameraInitialShift = Utils.MinMax(shift, 0.5, 10.0);
            if (double.TryParse(TextModelRotationAngle.Text, out double rot)) settings.ModelRotationAngle = Utils.MinMax(rot, 5.0, 180.0);

            // G-Code Import
            if (int.TryParse(TextGCodeLineEnds.Text, out int le)) settings.GcodeLineEndsExt = le;
            if (double.TryParse(TextGCodeLayerHeight.Text, out double lh)) settings.GcodeLayerHeight = lh;
            if (double.TryParse(TextGCodeLineWidth.Text, out double lw)) settings.GcodeLineWidth = lw;

            // Colors & Materials
            settings.DiffuseColor = new SColor(ColorDiffuse.ColorValue);
            settings.SpecularColor = new SColor(ColorSpecular.ColorValue);
            settings.AmbientLightColor = new SColor(ColorAmbient.ColorValue);
            settings.DirectionalLight1Color = new SColor(ColorLight1.ColorValue);
            settings.DirectionalLight2Color = new SColor(ColorLight2.ColorValue);
            settings.DirectionalLight3Color = new SColor(ColorLight3.ColorValue);
            settings.IsBackDiffuseEnabled = CheckBacksideDiffuseMaterial.IsChecked == true;

            settings.SetExternalApps(currentApps);
        }

        private void filterSettings(string query)
        {
            string q = (query ?? "").Trim().ToLowerInvariant();
            GroupBox[] groups = { GroupInterface, GroupCamera, GroupGCode, GroupMaterials, GroupApps, GroupAssociations };

            if (string.IsNullOrEmpty(q))
            {
                foreach (var g in groups) g.Visibility = Visibility.Visible;
                return;
            }

            foreach (var g in groups)
            {
                bool matches = false;
                string header = (g.Header as string) ?? "";
                if (header.ToLowerInvariant().Contains(q))
                {
                    matches = true;
                }
                else
                {
                    // Check text of child elements
                    matches = checkChildMatches(g, q);
                }
                g.Visibility = matches ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private bool checkChildMatches(DependencyObject parent, string q)
        {
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is TextBlock tb && tb.Text.ToLowerInvariant().Contains(q)) return true;
                if (child is CheckBox cb && (cb.Content as string ?? "").ToLowerInvariant().Contains(q)) return true;
                if (checkChildMatches(child, q)) return true;
            }
            return false;
        }

        private void ButtonAddApp_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Executables (*.exe)|*.exe|All Files (*.*)|*.*",
                Title = "Select Application Executable"
            };
            if (ofd.ShowDialog() == true)
            {
                string exeName = Path.GetFileNameWithoutExtension(ofd.FileName);
                currentApps.Add(new ExternalAppItem
                {
                    Name = exeName,
                    Path = ofd.FileName,
                    Arguments = "\"%1\"",
                    Extensions = "stl,3mf,obj,skp"
                });
                renderExternalAppsList();
            }
        }

        private void renderExternalAppsList()
        {
            PanelAppsList.Children.Clear();
            if (currentApps.Count == 0)
            {
                PanelAppsList.Children.Add(new TextBlock
                {
                    Text = "No external applications configured. Click '+ Add Application...' to add one.",
                    FontStyle = FontStyles.Italic,
                    FontSize = 11,
                    Foreground = (Brush)Application.Current.FindResource("WindowFore"),
                    Opacity = 0.6,
                    Margin = new Thickness(0, 4, 0, 4)
                });
                return;
            }

            for (int i = 0; i < currentApps.Count; i++)
            {
                int index = i;
                var app = currentApps[i];

                var border = new Border
                {
                    Background = (Brush)Application.Current.FindResource("PanelBack"),
                    BorderBrush = (Brush)Application.Current.FindResource("Separator"),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(8, 6, 8, 6),
                    Margin = new Thickness(0, 0, 0, 6)
                };

                var sp = new StackPanel();

                // Row 1: Name + Remove
                var topRow = new DockPanel();
                var btnRemove = new Button
                {
                    Content = "✕",
                    Width = 20,
                    Height = 20,
                    Padding = new Thickness(0),
                    ToolTip = "Remove application",
                    DockPanel.Dock = DockPanel.Dock.Right
                };
                btnRemove.Click += (s, e) =>
                {
                    currentApps.RemoveAt(index);
                    renderExternalAppsList();
                };
                topRow.Children.Add(btnRemove);

                var txtName = new TextBox
                {
                    Text = app.Name,
                    FontWeight = FontWeights.SemiBold,
                    Height = 22,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 8, 0)
                };
                txtName.TextChanged += (s, e) => app.Name = txtName.Text;
                topRow.Children.Add(txtName);
                sp.Children.Add(topRow);

                // Row 2: Path
                var pathTb = new TextBlock
                {
                    Text = app.Path,
                    FontSize = 10,
                    Opacity = 0.7,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    Margin = new Thickness(0, 2, 0, 4),
                    ToolTip = app.Path
                };
                pathTb.SetResourceReference(TextBlock.ForegroundProperty, "WindowFore");
                sp.Children.Add(pathTb);

                // Row 3: Extension checkboxes
                var extsWrap = new WrapPanel { Margin = new Thickness(0, 2, 0, 0) };
                string[] allExts = { "stl", "3mf", "obj", "skp", "gcode" };
                foreach (var ext in allExts)
                {
                    var cb = new CheckBox
                    {
                        Content = "." + ext,
                        IsChecked = app.SupportsExtension(ext),
                        Margin = new Thickness(0, 0, 8, 0),
                        FontSize = 10
                    };
                    cb.Checked += (s, e) => updateAppExtensions(app, ext, true);
                    cb.Unchecked += (s, e) => updateAppExtensions(app, ext, false);
                    extsWrap.Children.Add(cb);
                }
                sp.Children.Add(extsWrap);

                border.Child = sp;
                PanelAppsList.Children.Add(border);
            }
        }

        private void updateAppExtensions(ExternalAppItem app, string ext, bool enable)
        {
            var list = (app.Extensions ?? "").Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim().TrimStart('.').ToLowerInvariant())
                .Distinct()
                .ToList();

            if (enable && !list.Contains(ext)) list.Add(ext);
            else if (!enable && list.Contains(ext)) list.Remove(ext);

            app.Extensions = string.Join(",", list);
        }

        private void renderFileAssociationsList()
        {
            PanelAssociationsList.Children.Clear();
            foreach (var ext in FileAssociationManager.SupportedExtensions)
            {
                string currentExt = ext;
                var cb = new CheckBox
                {
                    Content = $"3D Model {currentExt.ToUpperInvariant()} ({currentExt})",
                    IsChecked = FileAssociationManager.IsAssociated(currentExt),
                    Margin = new Thickness(0, 2, 0, 2)
                };
                cb.Click += (s, e) =>
                {
                    bool want = cb.IsChecked == true;
                    FileAssociationManager.SetAssociation(currentExt, want);
                    cb.IsChecked = FileAssociationManager.IsAssociated(currentExt);
                };
                PanelAssociationsList.Children.Add(cb);
            }
        }
    }
}
