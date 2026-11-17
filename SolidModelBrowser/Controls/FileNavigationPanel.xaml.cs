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
    public partial class FileNavigationPanel : UserControl
    {
        public event SelectionChangedEventHandler SelectionChanged;

        public delegate void FileNavigationEventHandler(object Sender, string filename);
        public event FileNavigationEventHandler FileNavigated;
        public event EventHandler DirectoryChanged;


        public FileNavigationPanel()
        {
            InitializeComponent();

            listBoxFiles.SelectionChanged += ListBoxFiles_SelectionChanged;
            listBoxFiles.PreviewMouseDoubleClick += ListBoxFiles_PreviewMouseDoubleClick;
            listBoxFiles.PreviewKeyDown += ListBoxFiles_PreviewKeyDown;

            fill();
            foreach (DriveInfo d in DriveInfo.GetDrives())
            {
                string drivePath = d.Name.ToUpper();
                RadioButton rb = new RadioButton();
                rb.GroupName = "drivesgroup";
                rb.Tag = drivePath;
                rb.BorderThickness = new Thickness(0);
                rb.Margin = new Thickness(2, 2, 4, 2);
                rb.ToolTip = $"Drive {drivePath}";

                var stack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(6, 2, 6, 2) };
                
                // Hard Drive Icon
                var vb = new Viewbox { Width = 14, Height = 14, Margin = new Thickness(0, 0, 4, 0), VerticalAlignment = VerticalAlignment.Center };
                var p = new System.Windows.Shapes.Path
                {
                    Data = Geometry.Parse("M2,4 A1,1 0 0,1 3,3 L13,3 A1,1 0 0,1 14,4 L14,12 A1,1 0 0,1 13,13 L3,13 A1,1 0 0,1 2,12 Z M4,10.5 L6,10.5 M11.5,10.5 A0.75,0.75 0 1,0 11.51,10.5"),
                    Stroke = (Brush)Application.Current.FindResource("WindowFore"),
                    StrokeThickness = 1.1
                };
                vb.Child = p;
                stack.Children.Add(vb);

                var tb = new TextBlock
                {
                    Text = drivePath,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 11
                };
                tb.SetResourceReference(TextBlock.ForegroundProperty, "WindowFore");
                stack.Children.Add(tb);

                rb.Content = stack;
                rb.Checked += (sender, args) =>
                {
                    string drvPath = rb.Tag as string;
                    if (!string.IsNullOrEmpty(drvPath) && Directory.Exists(drvPath))
                    {
                        path = drvPath;
                        fill();
                        DirectoryChanged?.Invoke(this, EventArgs.Empty);
                    }
                };

                drivesWrapPanel.Children.Add(rb);
            }

            Path = Settings.GetDefaultInitialDirectory();
            watchForDirectory();
        }

        ~FileNavigationPanel()
        {
            watcher?.Dispose();
        }

        private string getSelectedItemPath()
        {
            if (listBoxFiles.SelectedItem == null) return null;
            if (listBoxFiles.SelectedItem is FrameworkElement fe) return fe.Tag as string;
            return null;
        }

        private void ListBoxFiles_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            DependencyObject temp = (DependencyObject)e.OriginalSource;
            while ((temp = VisualTreeHelper.GetParent(temp)) != null)
                if (temp is ListBoxItem lbi)
                {
                    listBoxFiles.SelectedItem = lbi.Content;
                    TryEnterSelectedFolder();
                    e.Handled = true;
                    break;
                }
        }

        bool dontRaiseSelectionChanged = false;
        private void ListBoxFiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dontRaiseSelectionChanged) return;
            SelectionChanged?.Invoke(this, e);

            string p = getSelectedItemPath();
            if (p == null) return;
            string ext = System.IO.Path.GetExtension(p).Trim('.').ToLower();
            if (!UseExtensionFilter || extensions.Contains(ext)) FileNavigated?.Invoke(this, p);
        }

        private void ListBoxFiles_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) TryEnterSelectedFolder();
            if (e.Key == Key.Back && listBoxFiles.Items.Count > 0)
            {
                if ((listBoxFiles.Items[0] as FrameworkElement)?.Tag as string == "..")
                {
                    listBoxFiles.SelectedIndex = 0;
                    TryEnterSelectedFolder();
                }
            }

            if (IsIgnoringLetterKeysNavigation && e.Key >= Key.A && e.Key <= Key.Z) e.Handled = true;
        }

        private void TryEnterSelectedFolder()
        {
            try
            {
                if (listBoxFiles.SelectedItem is FrameworkElement fe)
                {
                    string itemTag = fe.Tag as string;
                    if (itemTag == "..")
                    {
                        DirectoryInfo dir = new DirectoryInfo(path);
                        string dirname = dir.Name;
                        if (dir.Parent != null)
                        {
                            path = dir.Parent.FullName;
                            fill();
                            DirectoryChanged?.Invoke(this, EventArgs.Empty);
                            foreach (var li in listBoxFiles.Items)
                            {
                                if ((li as FrameworkElement)?.Tag as string == System.IO.Path.Combine(path, dirname))
                                {
                                    listBoxFiles.SelectedItem = li;
                                    break;
                                }
                            }
                        }
                    }
                    else if (!string.IsNullOrEmpty(itemTag) && Directory.Exists(itemTag))
                    {
                        path = itemTag;
                        fill();
                        DirectoryChanged?.Invoke(this, EventArgs.Empty);
                    }
                }
                if (listBoxFiles.Items.Count > 0)
                {
                    if (listBoxFiles.SelectedItem == null) listBoxFiles.SelectedIndex = 0;
                    listBoxFiles.UpdateLayout();
                    listBoxFiles.ScrollIntoView(listBoxFiles.SelectedItem);
                    ListBoxItem temp = (listBoxFiles.ItemContainerGenerator.ContainerFromItem(listBoxFiles.SelectedItem) as ListBoxItem);
                    if (temp != null) temp.Focus();
                }
            }
            catch { }
        }

        static readonly Dictionary<string, System.Windows.Media.Imaging.BitmapImage> iconCache = new Dictionary<string, System.Windows.Media.Imaging.BitmapImage>();
        private static System.Windows.Media.Imaging.BitmapImage getExtensionIcon(string ext)
        {
            if (iconCache.TryGetValue(ext, out var cached)) return cached;
            try
            {
                var uri = new Uri($"pack://application:,,,/Icons/{ext}.png", UriKind.Absolute);
                var bmp = new System.Windows.Media.Imaging.BitmapImage(uri);
                bmp.Freeze();
                iconCache[ext] = bmp;
                return bmp;
            }
            catch
            {
                return null;
            }
        }

        private FrameworkElement createItem(string fullPath, string displayText, bool isFolder, string ext = null)
        {
            var sp = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(2, 2, 2, 2),
                Background = Brushes.Transparent,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Tag = fullPath
            };

            if (isFolder)
            {
                var vb = new Viewbox { Width = 16, Height = 16, Margin = new Thickness(0, 0, 6, 0), VerticalAlignment = VerticalAlignment.Center };
                var canvas = new Canvas { Width = 16, Height = 16 };
                var p = new System.Windows.Shapes.Path
                {
                    Data = Geometry.Parse("M1.5,3.5 L6,3.5 L7.5,5.5 L14.5,5.5 A1,1 0 0,1 15.5,6.5 L15.5,13.5 A1,1 0 0,1 14.5,14.5 L1.5,14.5 A1,1 0 0,1 0.5,13.5 L0.5,4.5 A1,1 0 0,1 1.5,3.5 Z"),
                    Fill = new SolidColorBrush(Color.FromRgb(225, 175, 70))
                };
                canvas.Children.Add(p);
                vb.Child = canvas;
                sp.Children.Add(vb);
            }
            else
            {
                var bmp = getExtensionIcon(ext);
                if (bmp != null)
                {
                    var img = new Image
                    {
                        Source = bmp,
                        Width = 16,
                        Height = 16,
                        Margin = new Thickness(0, 0, 6, 0),
                        VerticalAlignment = VerticalAlignment.Center,
                        SnapsToDevicePixels = true
                    };
                    RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.NearestNeighbor);
                    sp.Children.Add(img);
                }
            }

            var tb = new TextBlock
            {
                Text = displayText,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            tb.SetResourceReference(TextBlock.ForegroundProperty, "WindowFore");

            sp.Children.Add(tb);
            return sp;
        }

        private void fill() => fill(false);
        private void fill(bool keepSelectedItem)
        {
            if (!Directory.Exists(path)) return;

            string si = getSelectedItemPath();

            listBoxFiles.Items.Clear();

            if ((new DirectoryInfo(path)).Parent != null)
            {
                listBoxFiles.Items.Add(createItem("..", "..", true));
            }

            string[] dirs = Directory.GetDirectories(path).OrderBy(d => d).ToArray();
            for (int c = 0; c < dirs.Length; c++)
            {
                string dirName = System.IO.Path.GetFileName(dirs[c]);
                listBoxFiles.Items.Add(createItem(dirs[c], dirName, true));
            }

            string[] files = Directory.GetFiles(path);
            if (SortFilesByExtensions) files = files.OrderBy(f => System.IO.Path.GetExtension(f) + System.IO.Path.GetFileNameWithoutExtension(f)).ToArray();
            else files = files.OrderBy(f => f).ToArray();
            for (int c = 0; c < files.Length; c++)
            {
                string ext = System.IO.Path.GetExtension(files[c]).Trim('.').ToLower();
                if (extensions.Contains(ext) || !UseExtensionFilter)
                {
                    string fileName = System.IO.Path.GetFileName(files[c]);
                    listBoxFiles.Items.Add(createItem(files[c], fileName, false, ext));
                }
            }

            if (keepSelectedItem)
            {
                var li = listBoxFiles.Items.OfType<FrameworkElement>().FirstOrDefault(t => t.Tag?.ToString() == si);
                listBoxFiles.SelectedItem = li;
            }
        }

        public void SelectFile(string filepath, bool focusControl)
        {
            if (!File.Exists(filepath)) return;
            Path = System.IO.Path.GetDirectoryName(filepath);
            foreach (var f in listBoxFiles.Items)
            {
                if (f is FrameworkElement fe && (fe.Tag as string) == filepath)
                {
                    listBoxFiles.SelectedItem = f;
                    if (focusControl)
                    {
                        listBoxFiles.UpdateLayout();
                        var listBoxItem = (ListBoxItem)listBoxFiles.ItemContainerGenerator.ContainerFromItem(listBoxFiles.SelectedItem);
                        listBoxItem?.Focus();
                    }
                    break;
                }
            }
        }

        public void Reselect()
        {
            if (listBoxFiles.SelectedItem != null)
            {
                object sel = listBoxFiles.SelectedItem;
                listBoxFiles.SelectedItem = null;
                listBoxFiles.SelectedItem = sel;
            }
        }

        public void Refresh()
        {
            dontRaiseSelectionChanged = true;
            fill(true);
            dontRaiseSelectionChanged = false;
            if (listBoxFiles.SelectedItem == null) ListBoxFiles_SelectionChanged(this, null);
        }

        FileSystemWatcher watcher = new FileSystemWatcher();
        void watchForDirectory()
        {
            watcher.Path = path;
            watcher.Filter = "*.*";
            watcher.Changed += (s, e) => Application.Current.Dispatcher.Invoke(() => Refresh());
            watcher.Created += (s, e) => Application.Current.Dispatcher.Invoke(() => Refresh());
            watcher.Deleted += (s, e) => Application.Current.Dispatcher.Invoke(() => Refresh());
            watcher.Renamed += (s, e) => Application.Current.Dispatcher.Invoke(() => Refresh());
            watcher.EnableRaisingEvents = true;
        }

        string path = Settings.GetDefaultInitialDirectory();
        public string Path
        {
            get
            {
                return path;
            }
            set
            {
                if (value == path) return;
                if (!Directory.Exists(value)) return;
                
                string drv = value.Substring(0, 1).ToUpper();
                var rbutton = drivesWrapPanel.Children.OfType<RadioButton>().FirstOrDefault(rb => (rb.Tag as string)?.StartsWith(drv) == true);
                if (rbutton != null) rbutton.IsChecked = true;
                
                path = value;
                fill();
                watcher.Path = path;
                DirectoryChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public string FullName
        {
            get
            {
                string fname = (listBoxFiles.SelectedItem as TextBlock)?.Text;
                if (fname != null && fname != "..") return System.IO.Path.Combine(path, fname);
                return null;
            }
        }

        public bool SortFilesByExtensions { get; set; } = false;
        public bool UseExtensionFilter { get; set; } = true;

        List<string> extensions = new List<string>();
        public List<string> Extensions
        {
            get
            {
                return extensions;
            }
            set
            {
                extensions = value;
                fill(true);
            }
        }

        Dictionary<string, SolidColorBrush> extensionsColors = new Dictionary<string, SolidColorBrush>();
        public Dictionary<string, SolidColorBrush> ExtensionsColors
        {
            get
            {
                return extensionsColors;
            }
            set
            {
                extensionsColors = value;
                fill(true);
            }
        }

        public bool IsIgnoringLetterKeysNavigation { get; set; }
    }
}
