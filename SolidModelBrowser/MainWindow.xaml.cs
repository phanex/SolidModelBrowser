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
    public enum ShadingMode
    {
        Solid,
        SolidAndWireframe,
        Wireframe
    }

    public partial class MainWindow : Window
    {
        const string version = "0.6.1";

        MeshGeometry3D meshBase, meshWireframe;
        Point3D modelCenter = new Point3D();
        Point3D modelPointsAvgCenter = new Point3D();
        Vector3D modelSize = new Vector3D();

        ShadingMode currentShadingMode = ShadingMode.Solid;
        CameraProjection currentCameraProjection = CameraProjection.Perspective;

        string lastFilename;

        DispatcherTimer loadingTimer = new DispatcherTimer() { Interval = new TimeSpan(0, 0, 0, 0, 100), IsEnabled = false };
        string pendingScreenshotPath = null;

        Settings settings = new Settings();

        public MainWindow()
        {
            InitializeComponent();
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture; // to avoid "." "," mess while parsing

            ButtonReloadModel.Click += (s, e) => { clearView(); loadFile(lastFilename); };
            ButtonSaveImage.Click += (s, e) => Utils.SaveImagePNG(scene.Viewport3D, settings.SaveImageDPI);
            ButtonOpenExtApp.Click += (s, e) =>
            {
                string ext = string.IsNullOrEmpty(lastFilename) ? "" : Path.GetExtension(lastFilename).TrimStart('.');
                var app = settings.GetExternalApps().FirstOrDefault(a => a.SupportsExtension(ext)) ?? settings.GetExternalApps().FirstOrDefault();
                if (app != null)
                    Utils.RunExternalApp(app.Path, app.Arguments, lastFilename);
                else if (File.Exists(lastFilename))
                    System.Diagnostics.Process.Start("rundll32.exe", $"shell32.dll,OpenAs_RunDLL \"{lastFilename}\"");
            };
            ButtonOpenExtAppDropdown.Click += (s, e) =>
            {
                populateExternalAppsMenu();
                ButtonOpenExtAppDropdown.ContextMenu.PlacementTarget = ButtonOpenExtAppDropdown;
                ButtonOpenExtAppDropdown.ContextMenu.IsOpen = true;
            };
            MenuSaveSnapshotPNG.Click += (s, e) => Utils.SaveImagePNG(scene.Viewport3D, settings.SaveImageDPI);

            MenuRotateX.Click += (s, e) => addRotation(settings.ModelRotationAngle, 0, 0);
            MenuRotateY.Click += (s, e) => addRotation(0, settings.ModelRotationAngle, 0);
            MenuRotateZ.Click += (s, e) => addRotation(0, 0, settings.ModelRotationAngle);

            ButtonWireframe.Click += (s, e) =>
            {
                var next = currentShadingMode == ShadingMode.Solid ? ShadingMode.SolidAndWireframe
                         : currentShadingMode == ShadingMode.SolidAndWireframe ? ShadingMode.Wireframe
                         : ShadingMode.Solid;
                setShadingMode(next);
            };
            ButtonShadingDropdown.Click += (s, e) =>
            {
                MenuSolid.IsChecked = currentShadingMode == ShadingMode.Solid;
                MenuSolidAndWireframe.IsChecked = currentShadingMode == ShadingMode.SolidAndWireframe;
                MenuWireframeOnly.IsChecked = currentShadingMode == ShadingMode.Wireframe;
                MenuBacksideColor.IsChecked = ButtonBacksideDiffuseMaterial.IsChecked == true;
                MenuRecreateUnsmoothed.IsChecked = !settings.UnsmoothAfterLoading;

                ButtonShadingDropdown.ContextMenu.PlacementTarget = ButtonShadingDropdown;
                ButtonShadingDropdown.ContextMenu.IsOpen = true;
            };
            MenuSolid.Click += (s, e) => setShadingMode(ShadingMode.Solid);
            MenuSolidAndWireframe.Click += (s, e) => setShadingMode(ShadingMode.SolidAndWireframe);
            MenuWireframeOnly.Click += (s, e) => setShadingMode(ShadingMode.Wireframe);
            MenuRecreateUnsmoothed.Click += (s, e) =>
            {
                if (meshBase == null) return;
                settings.UnsmoothAfterLoading = !MenuRecreateUnsmoothed.IsChecked;
                if (settings.UnsmoothAfterLoading)
                    meshBase = Utils.UnsmoothMesh(meshBase);
                else if (!string.IsNullOrEmpty(lastFilename))
                    loadFile(lastFilename);
                updateMeshes(true);
            };
            MenuBacksideColor.Click += (s, e) =>
            {
                ButtonBacksideDiffuseMaterial.IsChecked = MenuBacksideColor.IsChecked;
                applyMaterials();
            };

            ButtonSelectCamera.Click += (s, e) =>
            {
                var next = currentCameraProjection == CameraProjection.Perspective ? CameraProjection.Orthographic
                         : currentCameraProjection == CameraProjection.Orthographic ? CameraProjection.Fisheye
                         : CameraProjection.Perspective;
                setCameraProjection(next);
            };

            ButtonCenterCamera.Click += (s, e) =>
            {
                if (meshBase != null) scene.Camera.TurnAt(settings.DefaultLookAtModelPointsAvgCenter ? modelPointsAvgCenter : modelCenter);
            };
            ButtonCenterDropdown.Click += (s, e) =>
            {
                ButtonCenterDropdown.ContextMenu.PlacementTarget = ButtonCenterDropdown;
                ButtonCenterDropdown.ContextMenu.IsOpen = true;
            };
            MenuCenterMC.Click += (s, e) =>
            {
                if (meshBase != null)
                {
                    settings.DefaultLookAtModelPointsAvgCenter = true;
                    scene.Camera.TurnAt(modelPointsAvgCenter);
                }
            };
            MenuCenterGC.Click += (s, e) =>
            {
                if (meshBase != null)
                {
                    settings.DefaultLookAtModelPointsAvgCenter = false;
                    scene.Camera.TurnAt(modelCenter);
                }
            };

            ButtonGround.Click += (s, e) => scene.IsGroundVisible = ButtonGround.IsChecked.Value;
            ButtonShowModelInfo.Click += (s, e) => updateModelInfo();

            ButtonMore.Click += (s, e) =>
            {
                MenuAxes.IsChecked = ButtonAxes.IsChecked == true;
                ButtonMore.ContextMenu.PlacementTarget = ButtonMore;
                ButtonMore.ContextMenu.IsOpen = true;
            };
            MenuAxes.Click += (s, e) => { ButtonAxes.Toggle(); scene.IsAxesVisible = ButtonAxes.IsChecked.Value; };
            MenuFishEye.Click += (s, e) => setCameraProjection(CameraProjection.Fisheye);
            MenuSwapNormals.Click += (s, e) => { if (meshBase == null) return; Utils.InvertNormals(meshBase); updateMeshes(true); };
            MenuRemoveNormals.Click += (s, e) => { if (meshBase == null) return; meshBase.Normals.Clear(); updateMeshes(true); };
            MenuFlipFaces.Click += (s, e) => { if (meshBase == null) return; Utils.InvertVertexOrder(meshBase); updateMeshes(true); };
            MenuSettings.Click += (s, e) => runSettings();

            ButtonDiffuseMaterial.Click += (s, e) => applyMaterials();
            ButtonSpecularMaterial.Click += (s, e) => applyMaterials();
            ButtonEmissiveMaterial.Click += (s, e) => applyMaterials();
            ButtonBacksideDiffuseMaterial.Click += (s, e) => applyMaterials();
            ButtonCenterCameraAtModelGC.Click += (s, e) => { if (meshBase != null) scene.Camera.TurnAt(modelCenter); };
            ButtonCenterCameraAtModelMC.Click += (s, e) => { if (meshBase != null) scene.Camera.TurnAt(modelPointsAvgCenter); };
            ButtonFishEyeFOV.Click += (s, e) => updateCameraModes();
            ButtonAxes.Click += (s, e) => scene.IsAxesVisible = ButtonAxes.IsChecked.Value;
            ButtonSwapNormals.Click += (s, e) => { if (meshBase == null) return; Utils.InvertNormals(meshBase); updateMeshes(true); };
            ButtonRemoveNormals.Click += (s, e) => { if (meshBase == null) return; meshBase.Normals.Clear(); updateMeshes(true); };
            ButtonChangeVerticesOrder.Click += (s, e) => { if (meshBase == null) return; Utils.InvertVertexOrder(meshBase); updateMeshes(true); };
            ButtonRecreateUnsmoothed.Click += (s, e) => { if (meshBase == null) return; meshBase = Utils.UnsmoothMesh(meshBase); updateMeshes(true); };
            ButtonSettings.Click += (s, e) => runSettings();
            ButtonExport.Click += (s, e) => export();

            ButtonChangeTheme.Click += (s, e) => { currentSessionLightTheme = !currentSessionLightTheme; applySessionTheme(currentSessionLightTheme); };
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

            showInfo($"Solid Model Browser v {version}\r\nSTL, OBJ, 3MF, PLY, SKP, GCODE formats supported\r\n\r\nOriginal author: questfulcat (C) 2025\r\nEnhanced by: desmond\r\nSKP parser: OpenSkp by Ahsan Mehmood\r\nIcons: Coolicons by Vlad Shagov\r\nDistributed under MIT License\r\n\r\nF1 - keymap help");
            this.ContentRendered += windowRendered;
        }

        void windowRendered(object s, EventArgs a)
        {
            var args = Environment.GetCommandLineArgs();
            string targetFile = null;
            for (int i = 1; i < args.Length; i++)
            {
                if (args[i].StartsWith("--", StringComparison.OrdinalIgnoreCase))
                {
                    if (args[i].Equals("--screenshot", StringComparison.OrdinalIgnoreCase)) i++;
                    continue;
                }
                if (File.Exists(args[i]) && Import.SelectImporter(args[i]))
                {
                    targetFile = Path.GetFullPath(args[i]);
                    break;
                }
            }

            if (targetFile != null)
            {
                setFilePanelVisibility(false);
                filePanel.SelectFile(targetFile, false);
                loadFile(targetFile);
            }
            else
            {
                setFilePanelVisibility(true);
            }
            this.ContentRendered -= windowRendered;

            // CLI Screenshot handler: --screenshot <outPath> [--sidebar]
            for (int i = 1; i < args.Length; i++)
            {
                if (args[i].Equals("--screenshot", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    pendingScreenshotPath = Path.GetFullPath(args[i + 1]);
                    bool withSidebar = false;
                    for (int j = 1; j < args.Length; j++)
                    {
                        if (args[j].Equals("--sidebar", StringComparison.OrdinalIgnoreCase)) withSidebar = true;
                    }
                    if (withSidebar) setFilePanelVisibility(true);

                    if (targetFile == null)
                    {
                        this.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            this.UpdateLayout();
                            try
                            {
                                Utils.RenderElementToPNG(this, pendingScreenshotPath, 96);
                            }
                            catch { }
                            Application.Current.Shutdown();
                        }), System.Windows.Threading.DispatcherPriority.Loaded);
                    }
                    break;
                }
            }
        }
        

        void showInfo(string info) { textBlockInfo.Text = info; infoContainer.Visibility = Visibility.Visible; }
        void hideInfo() => infoContainer.Visibility = Visibility.Collapsed;
        void showModelInfo()
        {
            if (meshBase == null || string.IsNullOrEmpty(lastFilename))
            {
                hideInfo();
                return;
            }

            string fname = Path.GetFileName(lastFilename);
            string sizeStr = "";
            try
            {
                if (File.Exists(lastFilename))
                {
                    long bytes = new FileInfo(lastFilename).Length;
                    if (bytes >= 1024 * 1024 * 1024)
                        sizeStr = $"{bytes / (1024.0 * 1024.0 * 1024.0):0.1} GB";
                    else if (bytes >= 1024 * 1024)
                        sizeStr = $"{bytes / (1024.0 * 1024.0):0.1} MB";
                    else if (bytes >= 1024)
                        sizeStr = $"{bytes / 1024.0:0.1} KB";
                    else
                        sizeStr = $"{bytes} B";
                }
            }
            catch { }

            textBlockInfo.Inlines.Clear();

            // Header line: **Filename.ext** (Size)
            textBlockInfo.Inlines.Add(new System.Windows.Documents.Bold(new System.Windows.Documents.Run(fname)));
            if (!string.IsNullOrEmpty(sizeStr))
                textBlockInfo.Inlines.Add(new System.Windows.Documents.Run($" ({sizeStr})"));
            textBlockInfo.Inlines.Add(new System.Windows.Documents.LineBreak());

            // Dimensions: X × Y × Z mm
            textBlockInfo.Inlines.Add(new System.Windows.Documents.Bold(new System.Windows.Documents.Run("Dimensions: ")));
            textBlockInfo.Inlines.Add(new System.Windows.Documents.Run($"{modelSize.X:0.0} × {modelSize.Y:0.0} × {modelSize.Z:0.0} mm"));
            textBlockInfo.Inlines.Add(new System.Windows.Documents.LineBreak());

            if (Import.Metadata.ContainsKey("PrintTime") || Import.Metadata.ContainsKey("Filament"))
            {
                if (Import.Metadata.ContainsKey("PrintTime"))
                {
                    textBlockInfo.Inlines.Add(new System.Windows.Documents.Bold(new System.Windows.Documents.Run("Print Time: ")));
                    textBlockInfo.Inlines.Add(new System.Windows.Documents.Run(Import.Metadata["PrintTime"]));
                    textBlockInfo.Inlines.Add(new System.Windows.Documents.LineBreak());
                }
                if (Import.Metadata.ContainsKey("Filament"))
                {
                    textBlockInfo.Inlines.Add(new System.Windows.Documents.Bold(new System.Windows.Documents.Run("Filament: ")));
                    textBlockInfo.Inlines.Add(new System.Windows.Documents.Run(Import.Metadata["Filament"]));
                    textBlockInfo.Inlines.Add(new System.Windows.Documents.LineBreak());
                }
            }
            else
            {
                Utils.CalculateMeshStats(meshBase, out double volCm3, out double areaCm2);
                if (volCm3 > 0.001)
                {
                    double wallThickness = 0.08; // ~0.8mm wall (2 perimeters)
                    double wallVol = areaCm2 * wallThickness;
                    double coreVol = Math.Max(0.0, volCm3 - wallVol);
                    double printVol = Math.Min(volCm3, wallVol + coreVol * 0.15); // 15% infill
                    double printWeight = printVol * 1.24;

                    textBlockInfo.Inlines.Add(new System.Windows.Documents.Bold(new System.Windows.Documents.Run("Volume: ")));
                    textBlockInfo.Inlines.Add(new System.Windows.Documents.Run($"{volCm3:N1} cm³"));
                    textBlockInfo.Inlines.Add(new System.Windows.Documents.LineBreak());

                    textBlockInfo.Inlines.Add(new System.Windows.Documents.Bold(new System.Windows.Documents.Run("Filament 15%: ")));
                    textBlockInfo.Inlines.Add(new System.Windows.Documents.Run($"{printWeight:0.0} g"));
                    textBlockInfo.Inlines.Add(new System.Windows.Documents.LineBreak());
                }
            }

            textBlockInfo.Inlines.Add(new System.Windows.Documents.Bold(new System.Windows.Documents.Run("Triangles: ")));
            textBlockInfo.Inlines.Add(new System.Windows.Documents.Run($"{meshBase.TriangleIndices.Count / 3:N0}   "));
            textBlockInfo.Inlines.Add(new System.Windows.Documents.Bold(new System.Windows.Documents.Run("Vertices: ")));
            textBlockInfo.Inlines.Add(new System.Windows.Documents.Run($"{meshBase.Positions.Count:N0}"));

            infoContainer.Visibility = Visibility.Visible;
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
                    if (!string.IsNullOrEmpty(pendingScreenshotPath))
                    {
                        string targetPng = pendingScreenshotPath;
                        pendingScreenshotPath = null;
                        this.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            this.UpdateLayout();
                            try { Utils.RenderElementToPNG(this, targetPng, 96); } catch { }
                            Application.Current.Shutdown();
                        }), System.Windows.Threading.DispatcherPriority.Loaded);
                    }
                    return;
                }

                if (settings.IgnoreOriginalNormals) Import.Normals.Clear();
                meshBase = Utils.FillMeshFromImport();
                meshWireframe = null;
                if (Utils.ShouldUnsmoothMesh(lastFilename, settings.UnsmoothAfterLoading))
                    meshBase = Utils.UnsmoothMesh(meshBase);
                Utils.GetModelCenterAndSize(meshBase, out modelCenter, out modelPointsAvgCenter, out modelSize);
                initSlicer();
                updateMeshes(true);
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

                if (!string.IsNullOrEmpty(pendingScreenshotPath))
                {
                    string targetPng = pendingScreenshotPath;
                    pendingScreenshotPath = null;
                    this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        this.UpdateLayout();
                        try
                        {
                            Utils.RenderElementToPNG(this, targetPng, 96);
                        }
                        catch { }
                        Application.Current.Shutdown();
                    }), System.Windows.Threading.DispatcherPriority.Loaded);
                }
            }
            catch (Exception exc)
            {
                loadingTimer.Stop();
                clearView();
                showInfo(exc.Message);
                if (!string.IsNullOrEmpty(pendingScreenshotPath))
                {
                    string targetPng = pendingScreenshotPath;
                    pendingScreenshotPath = null;
                    this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        this.UpdateLayout();
                        try { Utils.RenderElementToPNG(this, targetPng, 96); } catch { }
                        Application.Current.Shutdown();
                    }), System.Windows.Threading.DispatcherPriority.Loaded);
                }
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
                if (currentTask != null && (currentTask.IsCompleted || currentTask.IsFaulted || currentTask.IsCanceled))
                {
                    try { currentTask.Dispose(); } catch { }
                }

                if (!Import.SelectImporter(filename))
                {
                    clearView();
                    if (!string.IsNullOrEmpty(pendingScreenshotPath))
                    {
                        string targetPng = pendingScreenshotPath;
                        pendingScreenshotPath = null;
                        this.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            this.UpdateLayout();
                            try { Utils.RenderElementToPNG(this, targetPng, 96); } catch { }
                            Application.Current.Shutdown();
                        }), System.Windows.Threading.DispatcherPriority.Loaded);
                    }
                    return;
                }

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
            if (meshBase == null) return;
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
                if (e.Key == Key.D1 || e.Key == Key.NumPad1) { setShadingMode(ShadingMode.Solid); e.Handled = true; return; }
                if (e.Key == Key.D2 || e.Key == Key.NumPad2) { setShadingMode(ShadingMode.SolidAndWireframe); e.Handled = true; return; }
                if (e.Key == Key.D3 || e.Key == Key.NumPad3) { setShadingMode(ShadingMode.Wireframe); e.Handled = true; return; }

                double ra = settings.ModelRotationAngle;
                if (e.Key == Key.X) { addRotation(ra, 0, 0); e.Handled = true; return; }
                if (e.Key == Key.Y) { addRotation(0, ra, 0); e.Handled = true; return; }
                if (e.Key == Key.Z) { addRotation(0, 0, ra); e.Handled = true; return; }
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
                if (e.Key == Key.F8)
                {
                    var next = currentCameraProjection == CameraProjection.Perspective ? CameraProjection.Orthographic
                             : currentCameraProjection == CameraProjection.Orthographic ? CameraProjection.Fisheye
                             : CameraProjection.Perspective;
                    setCameraProjection(next);
                }
                if (e.Key == Key.C) scene.Camera.TurnAt(modelCenter);
                if (e.Key == Key.M) scene.Camera.TurnAt(modelPointsAvgCenter);
                if (e.Key == Key.O) { ButtonAxes.Toggle(); scene.IsAxesVisible = ButtonAxes.IsChecked.Value; }
                if (e.Key == Key.P) { ButtonGround.Toggle(); scene.IsGroundVisible = ButtonGround.IsChecked.Value; }
                if (e.Key == Key.I) { ButtonShowModelInfo.Toggle(); updateModelInfo(); }
            }

            if (CAS == "S") // SHIFT
            {
                double ra = settings.ModelRotationAngle;
                if (e.Key == Key.X) { addRotation(-ra, 0, 0); e.Handled = true; return; }
                if (e.Key == Key.Y) { addRotation(0, -ra, 0); e.Handled = true; return; }
                if (e.Key == Key.Z) { addRotation(0, 0, -ra); e.Handled = true; return; }
            }

            if (CAS == "C") // CTRL
            {
                if (e.Key == Key.B) { setFilePanelVisibility(filePanel.Visibility != Visibility.Visible); e.Handled = true; return; }
                if (e.Key == Key.F) { meshBase = Utils.UnsmoothMesh(meshBase); updateMeshes(true); }
                if (e.Key == Key.W)
                {
                    var next = currentShadingMode == ShadingMode.Solid ? ShadingMode.SolidAndWireframe
                             : currentShadingMode == ShadingMode.SolidAndWireframe ? ShadingMode.Wireframe
                             : ShadingMode.Solid;
                    setShadingMode(next);
                    e.Handled = true;
                    return;
                }
                if (e.Key == Key.S) export();
            }
        }
        
        void clearView()
        {
            textBlockTitle.Text = "Solid Model Browser";
            this.Title = "Solid Model Browser";
            if (meshBase != null)
            {
                meshBase.TriangleIndices.Clear();
                meshBase.Positions.Clear();
                meshBase.Normals.Clear();
            }
            meshWireframe = null;
            scene.Mesh.Positions.Clear();
            scene.Mesh.TriangleIndices.Clear();
            scene.Mesh.Normals.Clear();
            scene.WireframeMesh.Positions.Clear();
            scene.WireframeMesh.TriangleIndices.Clear();
            scene.WireframeMesh.Normals.Clear();
            hideInfo();
        }

        void applyMaterials()
        {
            if (!(ButtonDiffuseMaterial.IsChecked.Value || ButtonSpecularMaterial.IsChecked.Value || ButtonEmissiveMaterial.IsChecked.Value)) ButtonDiffuseMaterial.IsChecked = true;
            scene.ApplyMaterials(ButtonDiffuseMaterial.IsChecked.Value, ButtonSpecularMaterial.IsChecked.Value, ButtonEmissiveMaterial.IsChecked.Value, ButtonBacksideDiffuseMaterial.IsChecked.Value);
            scene.UpdateWireframeColor(currentShadingMode == ShadingMode.Wireframe, currentSessionLightTheme, settings.DiffuseColor.Color);
        }

        void updateCameraModes()
        {
            setCameraProjection(currentCameraProjection);
        }

        void setCameraProjection(CameraProjection proj)
        {
            currentCameraProjection = proj;
            scene.Camera.SetProjection(proj, settings.FOV, settings.FishEyeFOV);

            CanvasCameraPerspective.Visibility = proj == CameraProjection.Perspective ? Visibility.Visible : Visibility.Collapsed;
            CanvasCameraOrtho.Visibility = proj == CameraProjection.Orthographic ? Visibility.Visible : Visibility.Collapsed;
            CanvasCameraFishEye.Visibility = proj == CameraProjection.Fisheye ? Visibility.Visible : Visibility.Collapsed;

            if (proj == CameraProjection.Perspective)
            {
                ButtonSelectCamera.ToolTip = "Camera: Perspective (Click for Orthographic) [F8]";
            }
            else if (proj == CameraProjection.Orthographic)
            {
                ButtonSelectCamera.ToolTip = "Camera: Orthographic (Click for Fisheye) [F8]";
            }
            else // Fisheye
            {
                ButtonSelectCamera.ToolTip = "Camera: Fisheye (Click for Perspective) [F8]";
            }
        }

        void setShadingMode(ShadingMode mode)
        {
            currentShadingMode = mode;

            CanvasShadingSolid.Visibility = mode == ShadingMode.Solid ? Visibility.Visible : Visibility.Collapsed;
            CanvasShadingSolidAndWireframe.Visibility = mode == ShadingMode.SolidAndWireframe ? Visibility.Visible : Visibility.Collapsed;
            CanvasShadingWireframe.Visibility = mode == ShadingMode.Wireframe ? Visibility.Visible : Visibility.Collapsed;

            if (MenuSolid != null) MenuSolid.IsChecked = mode == ShadingMode.Solid;
            if (MenuSolidAndWireframe != null) MenuSolidAndWireframe.IsChecked = mode == ShadingMode.SolidAndWireframe;
            if (MenuWireframeOnly != null) MenuWireframeOnly.IsChecked = mode == ShadingMode.Wireframe;

            if (mode == ShadingMode.Solid)
            {
                scene.SetShadingVisibility(true, false);
                ButtonWireframe.ToolTip = "Switch to Solid + Wireframe [Ctrl+W / 1,2,3]";
            }
            else if (mode == ShadingMode.SolidAndWireframe)
            {
                scene.SetShadingVisibility(true, true);
                ButtonWireframe.ToolTip = "Switch to Wireframe [Ctrl+W / 1,2,3]";
            }
            else // Wireframe
            {
                scene.SetShadingVisibility(false, true);
                ButtonWireframe.ToolTip = "Switch to Solid [Ctrl+W / 1,2,3]";
            }
            scene.UpdateWireframeColor(mode == ShadingMode.Wireframe, currentSessionLightTheme, settings.DiffuseColor.Color);
            updateMeshes(false);
        }

        private void Handler_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2) { WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized; return; }
            if (e.ChangedButton == MouseButton.Left) this.DragMove();
        }

        private bool currentSessionLightTheme;

        void setAppTheme()
        {
            currentSessionLightTheme = settings.ResolveIsLightTheme();
            applySessionTheme(currentSessionLightTheme);
        }

        void applySessionTheme(bool isLight)
        {
            if (isLight) App.SetTheme("ColorsLight.xaml");
            else App.SetTheme("Colors.xaml");
            scene.UpdateWireframeColor(currentShadingMode == ShadingMode.Wireframe, isLight, settings.DiffuseColor.Color);
            filePanel.ExtensionsColors.Clear();
            if (settings.ColorizeFiles) { Import.FillColorsDictionary(filePanel.ExtensionsColors, isLight); filePanel.Refresh(); }
        }

        void updateModelInfo()
        {
            if (ButtonShowModelInfo.IsChecked.Value) showModelInfo();
            else hideInfo();
        }

        private class Win32WindowOwner : System.Windows.Forms.IWin32Window
        {
            public IntPtr Handle { get; }
            public Win32WindowOwner(IntPtr handle) { Handle = handle; }
        }

        void runSettings()
        {
            var owner = new Win32WindowOwner(new System.Windows.Interop.WindowInteropHelper(this).Handle);
            using (var settingsForm = new SettingsForm(settings))
            {
                var result = settingsForm.ShowDialog(owner);
                if (settingsForm.WindowResult == SettingsWindowResult.ResetDefaults)
                {
                    settings = new Settings();
                    saveSettings();
                    loadSettings();
                }
                else if (result == System.Windows.Forms.DialogResult.OK)
                {
                    saveSettings();
                    loadSettings();
                }
                else if (settingsForm.WindowResult == SettingsWindowResult.OpenEditor)
                {
                    settings.StartProcess();
                    Close();
                }
            }
        }

        void updateMeshes(bool forceUpdateWireframe)
        {
            if (meshBase == null) return;
            if (currentShadingMode == ShadingMode.Solid)
            {
                scene.SetShadingVisibility(true, false);
                Utils.FillMesh(meshBase, scene.Mesh);
            }
            else if (currentShadingMode == ShadingMode.SolidAndWireframe)
            {
                scene.SetShadingVisibility(true, true);
                Utils.FillMesh(meshBase, scene.Mesh);
                if (meshWireframe == null || forceUpdateWireframe)
                    meshWireframe = Utils.ConvertToWireframe(meshBase, settings.WireframeEdgeScale);
                Utils.FillMesh(meshWireframe, scene.WireframeMesh);
            }
            else // Wireframe
            {
                scene.SetShadingVisibility(false, true);
                if (meshWireframe == null || forceUpdateWireframe)
                    meshWireframe = Utils.ConvertToWireframe(meshBase, settings.WireframeEdgeScale);
                Utils.FillMesh(meshWireframe, scene.WireframeMesh);
            }
            slice();
        }

        void initSlicer()
        {
            skipslice = true;
            double maxZ = modelCenter.Z + modelSize.Z / 2.0;
            double minZ = modelCenter.Z - modelSize.Z / 2.0;
            sliderSliceHeight.Minimum = minZ;
            sliderSliceHeight.Maximum = maxZ;
            sliderSliceHeight.Value = maxZ;
            sliderSliceHeight.LargeChange = modelSize.Z > 0 ? modelSize.Z * 0.05 : 0.2;
            skipslice = false;
        }

        bool skipslice = false;
        void slice()
        {
            if (skipslice || meshBase == null) return;
            double h = sliderSliceHeight.Value;
            bool isAtTop = h >= sliderSliceHeight.Maximum - 1e-4;

            if (currentShadingMode == ShadingMode.Solid)
            {
                if (isAtTop)
                {
                    Utils.FillMesh(meshBase, scene.Mesh);
                }
                else
                {
                    Utils.SliceMesh(meshBase, scene.Mesh, h);
                    if (scene.Mesh.TriangleIndices.Count == 0) scene.Mesh.TriangleIndices.Add(0);
                }
            }
            else if (currentShadingMode == ShadingMode.Wireframe)
            {
                if (meshWireframe != null)
                {
                    if (isAtTop)
                    {
                        Utils.FillMesh(meshWireframe, scene.WireframeMesh);
                    }
                    else
                    {
                        Utils.SliceMesh(meshWireframe, scene.WireframeMesh, h);
                        if (scene.WireframeMesh.TriangleIndices.Count == 0) scene.WireframeMesh.TriangleIndices.Add(0);
                    }
                }
            }
            else // SolidAndWireframe
            {
                if (isAtTop)
                {
                    Utils.FillMesh(meshBase, scene.Mesh);
                    if (meshWireframe != null) Utils.FillMesh(meshWireframe, scene.WireframeMesh);
                }
                else
                {
                    Utils.SliceMesh(meshBase, scene.Mesh, h);
                    if (scene.Mesh.TriangleIndices.Count == 0) scene.Mesh.TriangleIndices.Add(0);
                    if (meshWireframe != null)
                    {
                        Utils.SliceMesh(meshWireframe, scene.WireframeMesh, h);
                        if (scene.WireframeMesh.TriangleIndices.Count == 0) scene.WireframeMesh.TriangleIndices.Add(0);
                    }
                }
            }
        }

        void populateExternalAppsMenu()
        {
            MenuExternalApps.Items.Clear();

            string ext = string.IsNullOrEmpty(lastFilename) ? "" : Path.GetExtension(lastFilename).TrimStart('.');
            var allApps = settings.GetExternalApps();
            var relevantApps = !string.IsNullOrEmpty(ext) ? allApps.Where(a => a.SupportsExtension(ext)).ToList() : allApps;
            if (relevantApps.Count == 0) relevantApps = allApps;

            bool isFirst = true;
            foreach (var app in relevantApps)
            {
                if (File.Exists(app.Path))
                {
                    string targetPath = app.Path;
                    string targetArgs = app.Arguments;
                    var item = new MenuItem { Header = app.Name };
                    if (isFirst) { item.InputGestureText = "F7"; isFirst = false; }
                    item.Click += (s, e) => Utils.RunExternalApp(targetPath, targetArgs, lastFilename);
                    MenuExternalApps.Items.Add(item);
                }
            }

            var itemChoose = new MenuItem { Header = "Choose App..." };
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

            var itemPng = new MenuItem { Header = "Save Snapshot PNG", InputGestureText = "F6" };
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
            if (meshBase == null || scene.Mesh == null || scene.Mesh.Positions.Count == 0) return;
            var exportOrder = new[] { "stl", "obj", "3mf", "ply" };
            var sortedExports = Export.Exports
                .OrderBy(e => { int idx = Array.IndexOf(exportOrder, e.Extension.ToLowerInvariant()); return idx >= 0 ? idx : 999; })
                .ToList();
            var filter = string.Join("|", sortedExports.Select(e => $"{e.Description}|*.{e.Extension}"));
            var sfd = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Export Model",
                Filter = filter,
                FilterIndex = 1,
                FileName = string.IsNullOrEmpty(lastFilename) ? "model" : Path.GetFileNameWithoutExtension(lastFilename)
            };
            if (sfd.ShowDialog(this) != true) return;

            int selectedIndex = sfd.FilterIndex - 1;
            var exp = (selectedIndex >= 0 && selectedIndex < sortedExports.Count)
                ? sortedExports[selectedIndex]
                : sortedExports.FirstOrDefault(e => sfd.FileName.EndsWith("." + e.Extension, StringComparison.OrdinalIgnoreCase)) ?? sortedExports[0];

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
