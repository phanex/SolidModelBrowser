using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Media3D;
using System.Windows.Shell;
using System.Windows.Threading;

namespace SolidModelBrowser
{
    public partial class MainWindow : Window
    {
        const string version = "0.6.1";

        MeshGeometry3D meshBase, meshWireframe;
        Point3D modelCenter = new Point3D();
        Point3D modelPointsAvgCenter = new Point3D();
        Vector3D modelSize = new Vector3D();

        string lastFilename;

        DispatcherTimer loadingTimer = new DispatcherTimer() { Interval = new TimeSpan(0, 0, 0, 0, 100), IsEnabled = false };

        Settings settings = new Settings();

        public MainWindow()
        {
            InitializeComponent();
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture; // to avoid "." "," mess while parsing

            ButtonReloadModel.Click += (s, e) => { clearView(); loadFile(lastFilename); };
            ButtonSaveImage.Click += (s, e) => Utils.SaveImagePNG(scene.Viewport3D, settings.SaveImageDPI);
            ButtonOpenExtApp.Click += (s, e) => Utils.RunExternalApp(settings.ExternalApp, settings.ExternalAppArguments, lastFilename);
            ButtonOpenExtAppDropdown.Click += (s, e) =>
            {
                populateExternalAppsMenu();
                ButtonOpenExtAppDropdown.ContextMenu.PlacementTarget = ButtonOpenExtAppDropdown;
                ButtonOpenExtAppDropdown.ContextMenu.IsOpen = true;
            };
            MenuSaveSnapshotPNG.Click += (s, e) => Utils.SaveImagePNG(scene.Viewport3D, settings.SaveImageDPI);

            ButtonRotateX.Click += (s, e) => addRotation(settings.ModelRotationAngle, 0, 0);
            ButtonRotateY.Click += (s, e) => addRotation(0, settings.ModelRotationAngle, 0);
            ButtonRotateZ.Click += (s, e) => addRotation(0, 0, settings.ModelRotationAngle);

            ButtonWireframe.Click += (s, e) => updateMeshes(false);
            ButtonShadingDropdown.Click += (s, e) =>
            {
                MenuBacksideColor.IsChecked = ButtonBacksideDiffuseMaterial.IsChecked == true;
                ButtonShadingDropdown.ContextMenu.PlacementTarget = ButtonShadingDropdown;
                ButtonShadingDropdown.ContextMenu.IsOpen = true;
            };
            MenuRecreateUnsmoothed.Click += (s, e) => { meshBase = Utils.UnsmoothMesh(meshBase); ButtonWireframe.IsChecked = false; updateMeshes(true); };
            MenuWireframeOnly.Click += (s, e) => { ButtonWireframe.IsChecked = true; updateMeshes(false); };
            MenuBacksideColor.Click += (s, e) => { ButtonBacksideDiffuseMaterial.IsChecked = MenuBacksideColor.IsChecked; applyMaterials(); };

            ButtonSelectCamera.Click += (s, e) => updateCameraModes();
            ButtonGround.Click += (s, e) => scene.IsGroundVisible = ButtonGround.IsChecked.Value;
            ButtonShowModelInfo.Click += (s, e) => updateModelInfo();

            ButtonMore.Click += (s, e) =>
            {
                MenuAxes.IsChecked = ButtonAxes.IsChecked == true;
                MenuFishEye.IsChecked = ButtonFishEyeFOV.IsChecked == true;
                ButtonMore.ContextMenu.PlacementTarget = ButtonMore;
                ButtonMore.ContextMenu.IsOpen = true;
            };
            MenuAxes.Click += (s, e) => { ButtonAxes.Toggle(); scene.IsAxesVisible = ButtonAxes.IsChecked.Value; };
            MenuCenterGC.Click += (s, e) => scene.Camera.TurnAt(modelCenter);
            MenuCenterMC.Click += (s, e) => scene.Camera.TurnAt(modelPointsAvgCenter);
            MenuFishEye.Click += (s, e) => { ButtonFishEyeFOV.Toggle(); updateCameraModes(); };
            MenuSwapNormals.Click += (s, e) => { Utils.InvertNormals(meshBase); updateMeshes(true); };
            MenuRemoveNormals.Click += (s, e) => { meshBase.Normals.Clear(); updateMeshes(true); };
            MenuFlipFaces.Click += (s, e) => { Utils.InvertVertexOrder(meshBase); updateMeshes(true); };
            MenuSettings.Click += (s, e) => runSettings();

            ButtonDiffuseMaterial.Click += (s, e) => applyMaterials();
            ButtonSpecularMaterial.Click += (s, e) => applyMaterials();
            ButtonEmissiveMaterial.Click += (s, e) => applyMaterials();
            ButtonBacksideDiffuseMaterial.Click += (s, e) => applyMaterials();
            ButtonCenterCameraAtModelGC.Click += (s, e) => scene.Camera.TurnAt(modelCenter);
            ButtonCenterCameraAtModelMC.Click += (s, e) => scene.Camera.TurnAt(modelPointsAvgCenter);
            ButtonFishEyeFOV.Click += (s, e) => updateCameraModes();
            ButtonAxes.Click += (s, e) => scene.IsAxesVisible = ButtonAxes.IsChecked.Value;
            ButtonSwapNormals.Click += (s, e) => { Utils.InvertNormals(meshBase); updateMeshes(true); };
            ButtonRemoveNormals.Click += (s, e) => { meshBase.Normals.Clear(); updateMeshes(true); };
            ButtonChangeVerticesOrder.Click += (s, e) => { Utils.InvertVertexOrder(meshBase); updateMeshes(true); };
            ButtonRecreateUnsmoothed.Click += (s, e) => { meshBase = Utils.UnsmoothMesh(meshBase); updateMeshes(true); };
            ButtonSettings.Click += (s, e) => runSettings();
            ButtonExport.Click += (s, e) => export();

            ButtonChangeTheme.Click += (s, e) => { settings.LightTheme = !settings.LightTheme; setAppTheme(); };
            ButtonMinimize.Click += (s, e) => WindowState = WindowState.Minimized;
            ButtonMaximize.Click += (s, e) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            ButtonClose.Click += (s, e) => Close();

            scene.ShiftAndDrag += (s, e) => this.DragMove();
            sliderSliceHeight.ValueChanged += (s, e) => slice();

            ButtonToggleFilePanel.Click += (s, e) => setFilePanelVisibility(ButtonToggleFilePanel.IsChecked == true);

            filePanel.IsIgnoringLetterKeysNavigation = true;
            filePanel.Extensions = new List<string>();
            foreach (Import importer in Import.Imports) filePanel.Extensions.AddRange(importer.Extensions);
            filePanel.Opacity = 1.0;
            filePanel.SelectionChanged += (s, e) => clearView();
            filePanel.FileNavigated += (s, filename) => loadFile(filename);
            filePanel.DirectoryChanged += (s, e) =>
            {
                settings.SelectedPath = filePanel.Path;
                settings.Save();
            };

            this.PreviewKeyDown += MainWindow_PreviewKeyDown;
                        
            loadingTimer.Tick += LoadingTimer_Tick;

            loadSettings();
            this.Closing += (s, e) => saveSettings();
            Import.BindSettings(settings);

            // set max window size to avoid taskbar overlay
            var scrsize = Utils.GetCurrentScreenSize(this);
            MaxWidth = settings.MaxWidth < 1 ? scrsize.Width : Math.Max(settings.MaxWidth, MinWidth);
            MaxHeight = settings.MaxHeight < 1 ? scrsize.Height : Math.Max(settings.MaxHeight, MinHeight);

            showInfo($"Solid Model Browser v {version}\r\nSTL, OBJ, 3MF, PLY, SKP, GCODE formats supported\r\n\r\nquestfulcat 2025 (C)\r\nhttps://github.com/questfulcat/solidmodelbrowser\r\nDistributed under MIT License\r\n\r\nF1 - keymap help");
            this.ContentRendered += windowRendered;
        }

        void windowRendered(object s, EventArgs a)
        {
            var args = Environment.GetCommandLineArgs();
            bool hasFileArg = args.Length >= 2 && File.Exists(args[1]) && Import.SelectImporter(args[1]);
            if (hasFileArg)
            {
                setFilePanelVisibility(false);
                filePanel.SelectFile(args[1], false);
                loadFile(args[1]);
            }
            else
            {
                setFilePanelVisibility(true);
            }
            this.ContentRendered -= windowRendered;
        }
        

        void showInfo(string info) { textBlockInfo.Text = info; infoContainer.Visibility = Visibility.Visible; }
        void hideInfo() => infoContainer.Visibility = Visibility.Collapsed;
        void showModelInfo()
        {
            string fname = string.IsNullOrEmpty(lastFilename) ? "" : Path.GetFileName(lastFilename);
            int tris = scene.Mesh != null ? scene.Mesh.TriangleIndices.Count / 3 : 0;
            int verts = scene.Mesh != null ? scene.Mesh.Positions.Count : 0;
            showInfo($"{fname}\r\nDimensions: {modelSize.X:0.0} × {modelSize.Y:0.0} × {modelSize.Z:0.0} mm\r\nTriangles: {tris:N0}  |  Vertices: {verts:N0}");
        }


        private void LoadingTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                //textBlockProgress.Text = Import.Progress.ToString();
                borderProgressContainer.Visibility = Visibility.Visible;
                borderProgress.Width = Import.Progress < 0 ? 0 : (Import.Progress > 100 ? 100 : Import.Progress);

                if (currentTask?.Status == TaskStatus.Running) return;
                loadingTimer.Stop();
                borderProgressContainer.Visibility = Visibility.Hidden;

                if (Import.ExceptionMessage != null)
                {
                    clearView();
                    showInfo(Import.ExceptionMessage);
                    return;
                }

                if (settings.IgnoreOriginalNormals) Import.Normals.Clear();
                meshBase = Utils.FillMeshFromImport();
                meshWireframe = null;
                if (Utils.ShouldUnsmoothMesh(lastFilename, settings.UnsmoothAfterLoading))
                    meshBase = Utils.UnsmoothMesh(meshBase);
                Utils.FillMesh(meshBase, scene.Mesh);
                Utils.GetModelCenterAndSize(meshBase, out modelCenter, out modelPointsAvgCenter, out modelSize);
                scene.Camera.LookCenter = settings.DefaultLookAtModelPointsAvgCenter ? modelPointsAvgCenter : modelCenter;
                scene.Camera.DefaultPosition(modelSize.Length, settings.CameraInitialShift);

                if (!string.IsNullOrEmpty(lastFilename))
                {
                    string bareName = Path.GetFileName(lastFilename);
                    textBlockTitle.Text = bareName;
                    this.Title = bareName;
                }

                if (Import.CurrentImport.InitialRotationX != 0.0) addRotation(Import.CurrentImport.InitialRotationX, 0, 0);
                if (ButtonShowModelInfo.IsChecked.Value) showModelInfo();
                else hideInfo();

                initSlicer();
            }
            catch (Exception exc)
            {
                loadingTimer.Stop();
                clearView();
                showInfo(exc.Message);
            }
        }
                

        Task currentTask = null;
        void loadFile(string filename)
        {
            if (!File.Exists(filename)) return;
            string dir = Path.GetDirectoryName(filename);
            if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir)) settings.SelectedPath = dir;
            lastFilename = null;
            try
            {
                if (currentTask?.Status == TaskStatus.Running)
                {
                    Import.StopFlag = true;
                    currentTask.Wait(3000);
                }
                currentTask?.Dispose();

                if (!Import.SelectImporter(filename)) { clearView(); return; }

                currentTask = new Task(() =>
                {
                    try
                    {
                        Import.CurrentImport.Load(filename);
                    }
                    catch (Exception exc) { Import.ExceptionMessage = exc.Message; }
                });

                lastFilename = filename;
                loadingTimer.Start();
                showInfo("loading...");

                currentTask.Start();
            }
            catch (Exception exc)
            {
                showInfo(exc.Message);
            }
        }

        void addRotation(double ax, double ay, double az)
        {
            Utils.RotateMesh(meshBase, settings.DefaultLookAtModelPointsAvgCenter ? modelPointsAvgCenter : modelCenter, ax, ay, az);
            Utils.GetModelCenterAndSize(meshBase, out modelCenter, out modelPointsAvgCenter, out modelSize);
            initSlicer();
            updateMeshes(true);
        }

        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            string CAS = Utils.GetKeyboardModifiers();
            
            if (CAS == "") // no modifier keys
            {
                if (e.Key == Key.Tab || e.Key == Key.F9) { setFilePanelVisibility(filePanel.Visibility != Visibility.Visible); e.Handled = true; return; }

                double ra = settings.ModelRotationAngle;
                if (e.Key == Key.A) addRotation(0, 0, ra);
                if (e.Key == Key.D) addRotation(0, 0, -ra);
                if (e.Key == Key.S) addRotation(ra, 0, 0);
                if (e.Key == Key.W) addRotation(-ra, 0, 0);
                if (e.Key == Key.E) addRotation(0, ra, 0);
                if (e.Key == Key.Q) addRotation(0, -ra, 0);

                double cis = settings.CameraInitialShift;
                if (e.Key == Key.G) scene.Camera.RelativePosition(modelSize.Length, 0, -cis, 0);
                if (e.Key == Key.R) scene.Camera.RelativePosition(modelSize.Length, 0, cis, 0);
                if (e.Key == Key.F) scene.Camera.RelativePosition(modelSize.Length, -cis, 0, 0);
                if (e.Key == Key.H) scene.Camera.RelativePosition(modelSize.Length, cis, 0, 0);
                if (e.Key == Key.T) scene.Camera.RelativePosition(modelSize.Length, 0, -0.0001, cis);
                if (e.Key == Key.B) scene.Camera.RelativePosition(modelSize.Length, 0, -0.0001, -cis);

                if (e.Key == Key.F1)
                {
                    ButtonShowModelInfo.IsChecked = false;
                    if (textBlockInfo.Text == help && infoContainer.Visibility == Visibility.Visible) hideInfo();
                    else showInfo(help);
                }
                if (e.Key == Key.F2) { ButtonDiffuseMaterial.Toggle(); applyMaterials(); }
                if (e.Key == Key.F3) { ButtonSpecularMaterial.Toggle(); applyMaterials(); }
                if (e.Key == Key.F4) { ButtonEmissiveMaterial.Toggle(); applyMaterials(); }
                if (e.Key == Key.F5) { clearView(); loadFile(lastFilename); }
                if (e.Key == Key.F6) Utils.SaveImagePNG(scene.Viewport3D, settings.SaveImageDPI);
                if (e.Key == Key.F7) Utils.RunExternalApp(settings.ExternalApp, settings.ExternalAppArguments, lastFilename);
                if (e.Key == Key.F8) { ButtonSelectCamera.Toggle(); scene.Camera.SelectType(ButtonSelectCamera.IsChecked.Value); }
                if (e.Key == Key.C) scene.Camera.TurnAt(modelCenter);
                if (e.Key == Key.M) scene.Camera.TurnAt(modelPointsAvgCenter);
                if (e.Key == Key.O) { ButtonAxes.Toggle(); scene.IsAxesVisible = ButtonAxes.IsChecked.Value; }
                if (e.Key == Key.P) { ButtonGround.Toggle(); scene.IsGroundVisible = ButtonGround.IsChecked.Value; }
                if (e.Key == Key.I) { ButtonShowModelInfo.Toggle(); updateModelInfo(); }
            }

            if (CAS == "C") // CTRL
            {
                if (e.Key == Key.B) { setFilePanelVisibility(filePanel.Visibility != Visibility.Visible); e.Handled = true; return; }
                if (e.Key == Key.F) { meshBase = Utils.UnsmoothMesh(meshBase); updateMeshes(true); }
                if (e.Key == Key.W) { ButtonWireframe.Toggle(); updateMeshes(false); }
                if (e.Key == Key.S) export();
            }
        }
        
        void clearView()
        {
            textBlockTitle.Text = "Solid Model Browser";
            this.Title = "Solid Model Browser";
            if (meshBase == null) return;
            meshBase.TriangleIndices.Clear();
            meshBase.Positions.Clear();
            meshBase.Normals.Clear();
            ButtonWireframe.IsChecked = false;
            updateMeshes(false);
            hideInfo();
        }

        void applyMaterials()
        {
            if (!(ButtonDiffuseMaterial.IsChecked.Value || ButtonSpecularMaterial.IsChecked.Value || ButtonEmissiveMaterial.IsChecked.Value)) ButtonDiffuseMaterial.IsChecked = true;
            scene.ApplyMaterials(ButtonDiffuseMaterial.IsChecked.Value, ButtonSpecularMaterial.IsChecked.Value, ButtonEmissiveMaterial.IsChecked.Value, ButtonBacksideDiffuseMaterial.IsChecked.Value);
        }

        void updateCameraModes()
        {
            scene.Camera.SelectType(ButtonSelectCamera.IsChecked.Value);
            scene.Camera.FOV = ButtonFishEyeFOV.IsChecked.Value ? settings.FishEyeFOV : settings.FOV;
        }



        private void Handler_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2) { WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized; return; }
            if (e.ChangedButton == MouseButton.Left) this.DragMove();
        }

        void setAppTheme()
        {
            if (settings.LightTheme) App.SetTheme("ColorsLight.xaml");
            else App.SetTheme("Colors.xaml");
            filePanel.ExtensionsColors.Clear();
            if (settings.ColorizeFiles) { Import.FillColorsDictionary(filePanel.ExtensionsColors, settings.LightTheme); filePanel.Refresh(); }
        }

        
        void updateModelInfo()
        {
            if (ButtonShowModelInfo.IsChecked.Value) showModelInfo();
            else hideInfo();
        }

        void runSettings()
        {
            var settingsWindow = new SettingsWindow();
            settingsWindow.Owner = this;
            Utils.SetWindowStyle(settingsWindow, settings.UseSystemWindowStyle, settingsWindow.ButtonClose);
            settingsWindow.propertyPanel.SetObject(settings);
            this.Effect = new BlurEffect() { Radius = 4.0 };
            settingsWindow.ShowDialog();
            this.Effect = null;
            if (settingsWindow.WindowResult == SettingsWindowResult.ResetDefaults) { settings = new Settings(); }
            saveSettings();
            loadSettings();
            if (settingsWindow.WindowResult == SettingsWindowResult.OpenEditor) { settings.StartProcess(); Close(); }
        }

        void updateMeshes(bool forceUpdateWireframe)
        {
            var wfmode = ButtonWireframe.IsChecked.Value;
            if (wfmode)
            {
                if (meshWireframe == null || forceUpdateWireframe)
                    meshWireframe = Utils.ConvertToWireframe(meshBase, settings.WireframeEdgeScale);
            }
            else meshWireframe = null;
            Utils.FillMesh(wfmode ? meshWireframe : meshBase, scene.Mesh);
            slice();
        }

        void initSlicer()
        {
            skipslice = true;
            sliderSliceHeight.Maximum = modelCenter.Z + modelSize.Z / 2;
            sliderSliceHeight.Minimum = modelCenter.Z - modelSize.Z / 2;
            sliderSliceHeight.Value = sliderSliceHeight.Maximum;
            sliderSliceHeight.LargeChange = 0.2;
            skipslice = false;
        }

        //int slicescount = 0;
        bool skipslice = false;
        void slice()
        {
            if (skipslice) return;
            Utils.SliceMesh(ButtonWireframe.IsChecked.Value ? meshWireframe : meshBase, scene.Mesh, sliderSliceHeight.Value);
            if (scene.Mesh.TriangleIndices.Count == 0) { scene.Mesh.TriangleIndices.Add(0); }
            //showInfo($"ms: {Utils.sw.ElapsedMilliseconds}\r\nticks: {Utils.sw.ElapsedTicks}\r\nheight: {sliderSliceHeight.Value}, slicescount {++slicescount}, tris: {mesh.TriangleIndices.Count}");
        }

        void populateExternalAppsMenu()
        {
            MenuExternalApps.Items.Clear();

            if (!string.IsNullOrWhiteSpace(settings.ExternalApp) && File.Exists(settings.ExternalApp))
            {
                string appName = Path.GetFileNameWithoutExtension(settings.ExternalApp);
                var itemExt = new MenuItem { Header = $"Open with {appName} [F7]" };
                itemExt.Click += (s, e) => Utils.RunExternalApp(settings.ExternalApp, settings.ExternalAppArguments, lastFilename);
                MenuExternalApps.Items.Add(itemExt);
            }

            var itemChoose = new MenuItem { Header = "Open with... (Choose app)" };
            itemChoose.Click += (s, e) =>
            {
                if (File.Exists(lastFilename))
                {
                    try
                    {
                        System.Diagnostics.Process.Start("rundll32.exe", $"shell32.dll,OpenAs_RunDLL \"{lastFilename}\"");
                    }
                    catch (Exception ex)
                    {
                        Utils.MessageWindow(ex.Message);
                    }
                }
            };
            MenuExternalApps.Items.Add(itemChoose);

            MenuExternalApps.Items.Add(new Separator());

            var itemPng = new MenuItem { Header = "Save snapshot to PNG image [F6]" };
            itemPng.Click += (s, e) => Utils.SaveImagePNG(scene.Viewport3D, settings.SaveImageDPI);
            MenuExternalApps.Items.Add(itemPng);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            HwndSource source = PresentationSource.FromVisual(this) as HwndSource;
            source?.AddHook(WndProc);
        }

        private const int WM_NCHITTEST = 0x0084;
        private const int HTMAXBUTTON = 9;
        private const int WM_NCLBUTTONUP = 0x00A2;

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_NCHITTEST && ButtonMaximize != null && ButtonMaximize.IsVisible)
            {
                try
                {
                    int x = unchecked((short)(long)lParam);
                    int y = unchecked((short)((long)lParam >> 16));
                    Point screenPoint = new Point(x, y);

                    Point buttonPos = ButtonMaximize.PointToScreen(new Point(0, 0));
                    Rect buttonRect = new Rect(buttonPos.X, buttonPos.Y, ButtonMaximize.ActualWidth, ButtonMaximize.ActualHeight);

                    if (buttonRect.Contains(screenPoint))
                    {
                        handled = true;
                        return new IntPtr(HTMAXBUTTON);
                    }
                }
                catch { }
            }
            else if (msg == WM_NCLBUTTONUP && wParam.ToInt32() == HTMAXBUTTON)
            {
                WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
                handled = true;
            }
            return IntPtr.Zero;
        }

        void export()
        {
            string exporters = string.Join(",", Export.Exports.Select(e => e.Description).OrderBy(e => e));
            string r = Utils.MessageWindow("Select export format", this, exporters, Orientation.Vertical);
            var exp = Export.Exports.FirstOrDefault(e => e.Description == r);
            if (exp == null) return;
            var sfd = new System.Windows.Forms.SaveFileDialog() { DefaultExt = exp.Extension, Filter = $"{exp.Description}|*.{exp.Extension}" };
            if (sfd.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;
            try
            {
                MeshGeometry3D m = scene.Mesh;
                if (exp.InitialRotationX != 0.0) { m = scene.Mesh.Clone(); Utils.RotateMesh(m, settings.DefaultLookAtModelPointsAvgCenter ? modelPointsAvgCenter : modelCenter, exp.InitialRotationX, 0, 0); }
                exp.Save(m, sfd.FileName);
                showInfo($"Exported to file {sfd.FileName}");
            }
            catch (Exception exc) { Utils.MessageWindow($"Export failed with message\r\n{exc.Message}"); }
        }
    }
}
