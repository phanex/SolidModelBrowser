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

            ButtonLoadDefaults.Click += ButtonLoadDefaults_Click;
            ButtonOpenInTextEditor.Click += ButtonOpenInTextEditor_Click;

            ButtonSave.Click += (s, e) =>
            {
                settings.SetExternalApps(currentApps);
                DialogResult = true;
                Close();
            };
            ButtonCancel.Click += (s, e) => Close();

            ListCategories.SelectionChanged += ListCategories_SelectionChanged;

            ButtonAddApp.Click += ButtonAddApp_Click;
            ButtonAutoDetectSlicers.Click += ButtonAutoDetectSlicers_Click;

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

        public void BindSettings(Settings s)
        {
            settings = s;
            currentApps = settings.GetExternalApps();
            ListCategories.SelectedIndex = 0;
            switchCategory(0);
        }

        private void ListCategories_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (settings == null) return;
            switchCategory(ListCategories.SelectedIndex);
        }

        private void switchCategory(int index)
        {
            switch (index)
            {
                case 0: // General & Interface
                    TextCategoryTitle.Text = "General & Interface";
                    ScrollViewerProperties.Visibility = Visibility.Visible;
                    GridExternalApps.Visibility = Visibility.Collapsed;
                    GridFileAssociations.Visibility = Visibility.Collapsed;
                    BannerGCodeNotice.Visibility = Visibility.Collapsed;
                    propertyPanel.ShowCategoryHeaders = false;
                    propertyPanel.SetObject(settings, "Interface", "Main Window");
                    break;
                case 1: // Viewport & Camera
                    TextCategoryTitle.Text = "Viewport & Camera";
                    ScrollViewerProperties.Visibility = Visibility.Visible;
                    GridExternalApps.Visibility = Visibility.Collapsed;
                    GridFileAssociations.Visibility = Visibility.Collapsed;
                    BannerGCodeNotice.Visibility = Visibility.Collapsed;
                    propertyPanel.ShowCategoryHeaders = false;
                    propertyPanel.SetObject(settings, "Camera", "Model");
                    break;
                case 2: // Materials & Lights
                    TextCategoryTitle.Text = "Materials & Lighting";
                    ScrollViewerProperties.Visibility = Visibility.Visible;
                    GridExternalApps.Visibility = Visibility.Collapsed;
                    GridFileAssociations.Visibility = Visibility.Collapsed;
                    BannerGCodeNotice.Visibility = Visibility.Collapsed;
                    propertyPanel.ShowCategoryHeaders = false;
                    propertyPanel.SetObject(settings, "Materials", "Scene Lights");
                    break;
                case 3: // G-Code Import
                    TextCategoryTitle.Text = "G-Code Import";
                    ScrollViewerProperties.Visibility = Visibility.Visible;
                    GridExternalApps.Visibility = Visibility.Collapsed;
                    GridFileAssociations.Visibility = Visibility.Collapsed;
                    BannerGCodeNotice.Visibility = Visibility.Visible;
                    propertyPanel.ShowCategoryHeaders = false;
                    propertyPanel.SetObject(settings, "Import GCODE");
                    break;
                case 4: // External Apps
                    TextCategoryTitle.Text = "External Applications";
                    ScrollViewerProperties.Visibility = Visibility.Collapsed;
                    GridExternalApps.Visibility = Visibility.Visible;
                    GridFileAssociations.Visibility = Visibility.Collapsed;
                    renderExternalAppsList();
                    break;
                case 5: // File Associations
                    TextCategoryTitle.Text = "File Associations";
                    ScrollViewerProperties.Visibility = Visibility.Collapsed;
                    GridExternalApps.Visibility = Visibility.Collapsed;
                    GridFileAssociations.Visibility = Visibility.Visible;
                    renderFileAssociationsList();
                    break;
            }
        }

        private void ButtonAddApp_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new System.Windows.Forms.OpenFileDialog
            {
                Filter = "Executable files (*.exe)|*.exe|All files (*.*)|*.*",
                Title = "Select Application Executable"
            };
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK && File.Exists(ofd.FileName))
            {
                string name = Path.GetFileNameWithoutExtension(ofd.FileName);
                currentApps.Add(new ExternalAppItem
                {
                    Name = name,
                    Path = ofd.FileName,
                    Arguments = "$file$",
                    Extensions = "stl,3mf,obj,skp,ply,gcode"
                });
                renderExternalAppsList();
            }
        }

        private void ButtonAutoDetectSlicers_Click(object sender, RoutedEventArgs e)
        {
            var detected = Settings.AutoDetectSlicers();
            int added = 0;
            foreach (var d in detected)
            {
                if (!currentApps.Any(a => a.Path.Equals(d.Path, StringComparison.OrdinalIgnoreCase)))
                {
                    currentApps.Add(d);
                    added++;
                }
            }
            if (added > 0)
            {
                renderExternalAppsList();
                Utils.MessageWindow($"Added {added} detected application(s).");
            }
            else
            {
                Utils.MessageWindow("No new slicers detected in standard program directories.");
            }
        }

        private void renderExternalAppsList()
        {
            PanelAppsList.Children.Clear();
            if (currentApps.Count == 0)
            {
                PanelAppsList.Children.Add(new TextBlock
                {
                    Text = "No external applications configured.\nClick '+ Add Application...' or 'Auto-Detect Slicers' above.",
                    FontSize = 11,
                    Foreground = (Brush)FindResource("WindowFore"),
                    Opacity = 0.6,
                    Margin = new Thickness(0, 10, 0, 0)
                });
                return;
            }

            foreach (var app in currentApps)
            {
                var border = new Border
                {
                    Background = (Brush)FindResource("PanelBack"),
                    BorderBrush = (Brush)FindResource("Separator"),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(12, 10, 12, 10),
                    Margin = new Thickness(0, 0, 0, 8)
                };

                var sp = new StackPanel();

                var row1 = new DockPanel();
                var btnRemove = new Button
                {
                    Content = "✕",
                    Width = 24,
                    Height = 24,
                    ToolTip = "Remove application"
                };
                DockPanel.SetDock(btnRemove, Dock.Right);
                var appRef = app;
                btnRemove.Click += (s, e) =>
                {
                    currentApps.Remove(appRef);
                    renderExternalAppsList();
                };
                row1.Children.Add(btnRemove);

                var txtName = new TextBlock
                {
                    Text = app.Name,
                    FontWeight = FontWeights.SemiBold,
                    FontSize = 12,
                    Foreground = (Brush)FindResource("WindowFore"),
                    VerticalAlignment = VerticalAlignment.Center
                };
                row1.Children.Add(txtName);
                sp.Children.Add(row1);

                var txtPath = new TextBlock
                {
                    Text = app.Path,
                    FontSize = 11,
                    Foreground = (Brush)FindResource("WindowFore"),
                    Opacity = 0.65,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    Margin = new Thickness(0, 2, 0, 6)
                };
                sp.Children.Add(txtPath);

                var extsPanel = new WrapPanel { Margin = new Thickness(0, 4, 0, 0) };
                string[] standardExts = { "stl", "3mf", "obj", "skp", "ply", "gcode" };
                foreach (var ext in standardExts)
                {
                    var cb = new CheckBox
                    {
                        Content = ext.ToUpperInvariant(),
                        IsChecked = app.SupportsExtension(ext),
                        Margin = new Thickness(0, 0, 12, 0),
                        FontSize = 11
                    };
                    string eRef = ext;
                    cb.Checked += (s, e) => updateAppExtension(appRef, eRef, true);
                    cb.Unchecked += (s, e) => updateAppExtension(appRef, eRef, false);
                    extsPanel.Children.Add(cb);
                }
                sp.Children.Add(extsPanel);

                border.Child = sp;
                PanelAppsList.Children.Add(border);
            }
        }

        private void updateAppExtension(ExternalAppItem app, string ext, bool enable)
        {
            var list = app.Extensions.ToLowerInvariant().Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            if (enable && !list.Contains(ext)) list.Add(ext);
            else if (!enable && list.Contains(ext)) list.Remove(ext);
            app.Extensions = string.Join(",", list);
        }

        private void renderFileAssociationsList()
        {
            PanelAssociationsList.Children.Clear();
            foreach (var ext in FileAssociationManager.SupportedExtensions)
            {
                bool isAssoc = FileAssociationManager.IsAssociated(ext);
                var cb = new CheckBox
                {
                    Content = $"{ext} ({GetFormatDescription(ext)})",
                    IsChecked = isAssoc,
                    Margin = new Thickness(0, 4, 0, 6),
                    FontSize = 11
                };
                string targetExt = ext;
                cb.Checked += (s, e) => FileAssociationManager.SetAssociation(targetExt, true);
                cb.Unchecked += (s, e) => FileAssociationManager.SetAssociation(targetExt, false);
                PanelAssociationsList.Children.Add(cb);
            }
        }

        private string GetFormatDescription(string ext)
        {
            switch (ext.ToLowerInvariant())
            {
                case ".stl": return "Stereolithography Mesh";
                case ".3mf": return "3D Manufacturing Format";
                case ".obj": return "Wavefront 3D Object";
                case ".skp": return "SketchUp Model";
                case ".ply": return "Polygon File Format";
                case ".gcode": return "G-Code Toolpath";
                default: return "3D Model";
            }
        }

        private void ButtonOpenInTextEditor_Click(object sender, RoutedEventArgs e)
        {
            if (Utils.MessageWindow("Close app and open settings.ini in default editor?\r\n(You have to associate ini files with Notepad++ or other text editor before using this option)", this, "YES,Cancel", Orientation.Horizontal) != "YES") return;
            WindowResult = SettingsWindowResult.OpenEditor;
            Close();
        }

        private void ButtonLoadDefaults_Click(object sender, RoutedEventArgs e)
        {
            if (Utils.MessageWindow("Are you sure you want to reset all settings to default values?", this, "YES,Cancel", Orientation.Horizontal) != "YES") return;
            WindowResult = SettingsWindowResult.ResetDefaults;
            Close();
        }
    }
}
