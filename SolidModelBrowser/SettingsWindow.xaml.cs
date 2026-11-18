using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
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

            ListCategories.SelectionChanged += (s, e) => updateCategoryPanels();
            ListCategories.SelectedIndex = 0;

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

            ButtonAddApp.Click += ButtonAddApp_Click;
            ButtonDetectSlicers.Click += (s, e) =>
            {
                var detected = Settings.AutoDetectSlicers();
                foreach (var d in detected)
                {
                    if (!currentApps.Any(a => a.Path.Equals(d.Path, StringComparison.OrdinalIgnoreCase)))
                        currentApps.Add(d);
                }
                renderExternalAppsList();
            };

            ButtonSelectAllAssoc.Click += (s, e) =>
            {
                string args = TextAssocArgs.Text.Trim();
                foreach (var ext in FileAssociationManager.SupportedExtensions)
                    FileAssociationManager.SetAssociation(ext, true, args);
                renderFileAssociationsList();
            };

            ButtonDeselectAllAssoc.Click += (s, e) =>
            {
                foreach (var ext in FileAssociationManager.SupportedExtensions)
                    FileAssociationManager.SetAssociation(ext, false);
                renderFileAssociationsList();
            };
        }

        private void updateCategoryPanels()
        {
            int idx = ListCategories.SelectedIndex;
            PanelGeneral.Visibility = idx == 0 ? Visibility.Visible : Visibility.Collapsed;
            PanelViewport.Visibility = idx == 1 ? Visibility.Visible : Visibility.Collapsed;
            PanelColors.Visibility = idx == 2 ? Visibility.Visible : Visibility.Collapsed;
            PanelGCode.Visibility = idx == 3 ? Visibility.Visible : Visibility.Collapsed;
            PanelApps.Visibility = idx == 4 ? Visibility.Visible : Visibility.Collapsed;
            PanelAssociations.Visibility = idx == 5 ? Visibility.Visible : Visibility.Collapsed;
        }

        internal void BindSettings(Settings s)
        {
            settings = s;
            currentApps = settings.GetExternalApps();

            // 1. General
            CheckShowFilePanel.IsChecked = s.ShowFilePanel;
            CheckColorizeFiles.IsChecked = s.ColorizeFiles;
            CheckSortFilesByExtensions.IsChecked = s.SortFilesByExtensions;
            CheckUnsmoothAfterLoading.IsChecked = s.UnsmoothAfterLoading;
            CheckLightTheme.IsChecked = s.LightTheme;
            CheckUseSystemWindowStyle.IsChecked = s.UseSystemWindowStyle;

            // 2. Viewport
            CheckCenterCameraAtPointsAverageCenter.IsChecked = s.DefaultLookAtModelPointsAvgCenter;
            TextFov.Text = s.FOV.ToString("0.#");
            TextFishEyeFov.Text = s.FishEyeFOV.ToString("0.#");
            TextCameraInitialShift.Text = s.CameraInitialShift.ToString("0.#");
            TextModelRotationAngle.Text = s.ModelRotationAngle.ToString("0.#");
            TextWireframeEdgeScale.Text = s.WireframeEdgeScale.ToString("0.###");

            // 3. Colors
            ColorDiffuse.ColorValue = s.DiffuseColor.Color;
            ColorSpecular.ColorValue = s.SpecularColor.Color;
            ColorEmissive.ColorValue = s.EmissiveColor.Color;
            ColorBackDiffuse.ColorValue = s.BackDiffuseColor.Color;
            ColorAmbient.ColorValue = s.AmbientLightColor.Color;
            ColorLight1.ColorValue = s.DirectionalLight1Color.Color;
            ColorLight2.ColorValue = s.DirectionalLight2Color.Color;
            ColorLight3.ColorValue = s.DirectionalLight3Color.Color;
            ColorGroundDiffuse.ColorValue = s.GroundDiffuseColor.Color;
            CheckBacksideDiffuseMaterial.IsChecked = s.IsBackDiffuseEnabled;

            // 4. G-Code
            TextGCodeLayerHeight.Text = s.GCODELayerHeight.ToString("0.##");
            TextGCodeLineWidth.Text = s.GCODELineWidth.ToString("0.##");
            TextGCodeLineEnds.Text = s.GCODELineExtensions.ToString();

            // 5. File Associations
            TextAssocArgs.Text = string.IsNullOrWhiteSpace(s.FileAssociationArguments) ? "\"%1\"" : s.FileAssociationArguments;

            renderExternalAppsList();
            renderFileAssociationsList();
        }

        private void saveUIToSettings()
        {
            if (settings == null) return;

            // 1. General
            settings.ShowFilePanel = CheckShowFilePanel.IsChecked == true;
            settings.ColorizeFiles = CheckColorizeFiles.IsChecked == true;
            settings.SortFilesByExtensions = CheckSortFilesByExtensions.IsChecked == true;
            settings.UnsmoothAfterLoading = CheckUnsmoothAfterLoading.IsChecked == true;
            settings.LightTheme = CheckLightTheme.IsChecked == true;
            settings.UseSystemWindowStyle = CheckUseSystemWindowStyle.IsChecked == true;

            // 2. Viewport
            settings.DefaultLookAtModelPointsAvgCenter = CheckCenterCameraAtPointsAverageCenter.IsChecked == true;
            if (double.TryParse(TextFov.Text, out double fov)) settings.FOV = Utils.MinMax(fov, 5.0, 160.0);
            if (double.TryParse(TextFishEyeFov.Text, out double feFov)) settings.FishEyeFOV = Utils.MinMax(feFov, 5.0, 160.0);
            if (double.TryParse(TextCameraInitialShift.Text, out double shift)) settings.CameraInitialShift = Utils.MinMax(shift, 0.5, 10.0);
            if (double.TryParse(TextModelRotationAngle.Text, out double rot)) settings.ModelRotationAngle = Utils.MinMax(rot, 5.0, 180.0);
            if (double.TryParse(TextWireframeEdgeScale.Text, out double wes)) settings.WireframeEdgeScale = Utils.MinMax(wes, 0.001, 0.9);

            // 3. Colors
            settings.DiffuseColor = new SColor(ColorDiffuse.ColorValue);
            settings.SpecularColor = new SColor(ColorSpecular.ColorValue);
            settings.EmissiveColor = new SColor(ColorEmissive.ColorValue);
            settings.BackDiffuseColor = new SColor(ColorBackDiffuse.ColorValue);
            settings.AmbientLightColor = new SColor(ColorAmbient.ColorValue);
            settings.DirectionalLight1Color = new SColor(ColorLight1.ColorValue);
            settings.DirectionalLight2Color = new SColor(ColorLight2.ColorValue);
            settings.DirectionalLight3Color = new SColor(ColorLight3.ColorValue);
            settings.GroundDiffuseColor = new SColor(ColorGroundDiffuse.ColorValue);
            settings.IsBackDiffuseEnabled = CheckBacksideDiffuseMaterial.IsChecked == true;

            // 4. G-Code
            if (double.TryParse(TextGCodeLayerHeight.Text, out double lh)) settings.GCODELayerHeight = lh;
            if (double.TryParse(TextGCodeLineWidth.Text, out double lw)) settings.GCODELineWidth = lw;
            if (double.TryParse(TextGCodeLineEnds.Text, out double le)) settings.GCODELineExtensions = le;

            // 5. External Apps
            settings.SetExternalApps(currentApps);

            // 6. File Associations
            settings.FileAssociationArguments = TextAssocArgs.Text.Trim();
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
                    Arguments = "$file$",
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
                    Text = "No external applications configured. Click '+ Add Application...' or 'Auto-detect Slicers'.",
                    FontSize = 11,
                    Foreground = (Brush)Application.Current.FindResource("WindowFore"),
                    Opacity = 0.6,
                    Margin = new Thickness(0, 8, 0, 8)
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
                    Padding = new Thickness(10, 8, 10, 8),
                    Margin = new Thickness(0, 0, 0, 8)
                };

                var sp = new StackPanel();

                // Row 1: Name + Remove Button
                var topRow = new DockPanel();
                var btnRemove = new Button
                {
                    Content = "✕",
                    Width = 22,
                    Height = 22,
                    Padding = new Thickness(0),
                    ToolTip = "Remove application"
                };
                DockPanel.SetDock(btnRemove, Dock.Right);
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
                    Height = 24,
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
                    Margin = new Thickness(0, 3, 0, 4),
                    ToolTip = app.Path
                };
                pathTb.SetResourceReference(TextBlock.ForegroundProperty, "WindowFore");
                sp.Children.Add(pathTb);

                // Row 3: Launch Arguments
                var argsDock = new DockPanel { Margin = new Thickness(0, 2, 0, 4) };
                var argsLabel = new TextBlock
                {
                    Text = "Arguments:",
                    FontSize = 11,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 6, 0)
                };
                argsLabel.SetResourceReference(TextBlock.ForegroundProperty, "WindowFore");
                DockPanel.SetDock(argsLabel, Dock.Left);
                argsDock.Children.Add(argsLabel);

                var txtArgs = new TextBox
                {
                    Text = app.Arguments,
                    Height = 22,
                    FontSize = 11,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    ToolTip = "Command line arguments ($file$ is replaced by model file path)"
                };
                txtArgs.TextChanged += (s, e) => app.Arguments = txtArgs.Text;
                argsDock.Children.Add(txtArgs);
                sp.Children.Add(argsDock);

                // Row 4: Extension checkboxes
                var extsWrap = new WrapPanel { Margin = new Thickness(0, 2, 0, 0) };
                string[] allExts = { "stl", "3mf", "obj", "skp", "gcode" };
                foreach (var ext in allExts)
                {
                    var cb = new CheckBox
                    {
                        Content = "." + ext,
                        IsChecked = app.SupportsExtension(ext),
                        Margin = new Thickness(0, 0, 10, 0),
                        FontSize = 11
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
            string args = TextAssocArgs.Text.Trim();
            foreach (var ext in FileAssociationManager.SupportedExtensions)
            {
                string currentExt = ext;
                var cb = new CheckBox
                {
                    Content = $"3D Model {currentExt.ToUpperInvariant()} ({currentExt})",
                    IsChecked = FileAssociationManager.IsAssociated(currentExt),
                    Margin = new Thickness(0, 4, 16, 4),
                    FontSize = 11
                };
                cb.Click += (s, e) =>
                {
                    bool want = cb.IsChecked == true;
                    FileAssociationManager.SetAssociation(currentExt, want, args);
                    cb.IsChecked = FileAssociationManager.IsAssociated(currentExt);
                };
                PanelAssociationsList.Children.Add(cb);
            }
        }
    }
}
