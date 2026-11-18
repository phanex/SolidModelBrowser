using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace SolidModelBrowser
{
    public enum SettingsWindowResult { None, ResetDefaults, OpenEditor };

    internal class SettingsForm : Form
    {
        public SettingsWindowResult WindowResult { get; set; } = SettingsWindowResult.None;
        private Settings settings;
        private List<ExternalAppItem> currentApps = new List<ExternalAppItem>();

        private TabControl tabControl;

        // General
        private CheckBox chkShowFilePanel;
        private CheckBox chkColorizeFiles;
        private CheckBox chkSortFiles;
        private CheckBox chkFlatShading;
        private ComboBox cmbTheme;
        private CheckBox chkSystemWindow;

        // Viewport
        private TextBox txtFov;
        private TextBox txtFishEyeFov;
        private TextBox txtCameraInitialShift;
        private TextBox txtModelRotationAngle;
        private TextBox txtWireframeEdgeScale;
        private CheckBox chkCenterMass;

        // Colors
        private class ColorControlRow
        {
            public Label Label;
            public Button Swatch;
            public TextBox TextHex;
            public Color CurrentColor;
            public Action<Color> OnColorChanged;
        }

        private List<ColorControlRow> colorRows = new List<ColorControlRow>();
        private CheckBox chkBacksideDiffuse;

        // G-Code
        private TextBox txtGCodeLayerHeight;
        private TextBox txtGCodeLineWidth;
        private TextBox txtGCodeLineEnds;

        // External Apps
        private ListBox listApps;
        private TextBox txtAppName;
        private TextBox txtAppPath;
        private TextBox txtAppArgs;
        private CheckBox[] chkAppExts;
        private Button btnAddApp;
        private Button btnRemoveApp;
        private Button btnDetectSlicers;

        // File Associations
        private CheckBox[] chkAssocExts;
        private Button btnSelectAllAssoc;
        private Button btnDeselectAllAssoc;

        // Command Bar
        private Button btnDefaults;
        private Button btnOpenIni;
        private Button btnSave;
        private Button btnCancel;

        internal SettingsForm(Settings s)
        {
            settings = s;
            currentApps = settings.GetExternalApps();

            this.Text = "Settings";
            this.Size = new Size(740, 520);
            this.MinimumSize = new Size(680, 480);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Font = SystemFonts.MessageBoxFont;

            initLayout();
            bindData();
        }

        private void initLayout()
        {
            // 1. Bottom Command Bar
            Panel pnlBottom = new Panel { Dock = DockStyle.Bottom, Height = 48, Padding = new Padding(12, 8, 12, 8) };

            btnDefaults = new Button { Text = "Reset to Defaults", Location = new Point(12, 10), Size = new Size(130, 28) };
            btnDefaults.Click += (s, e) => { WindowResult = SettingsWindowResult.ResetDefaults; this.DialogResult = DialogResult.OK; this.Close(); };
            pnlBottom.Controls.Add(btnDefaults);

            btnOpenIni = new Button { Text = "Open INI", Location = new Point(btnDefaults.Right + 8, 10), Size = new Size(85, 28) };
            btnOpenIni.Click += (s, e) => { WindowResult = SettingsWindowResult.OpenEditor; this.DialogResult = DialogResult.OK; this.Close(); };
            pnlBottom.Controls.Add(btnOpenIni);

            btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Size = new Size(85, 28) };
            this.CancelButton = btnCancel;
            pnlBottom.Controls.Add(btnCancel);

            btnSave = new Button { Text = "Save", DialogResult = DialogResult.OK, Size = new Size(85, 28) };
            btnSave.Click += (s, e) => { saveUIToSettings(); this.Close(); };
            this.AcceptButton = btnSave;
            pnlBottom.Controls.Add(btnSave);

            pnlBottom.Resize += (s, e) =>
            {
                btnCancel.Location = new Point(pnlBottom.ClientSize.Width - btnCancel.Width - 12, 10);
                btnSave.Location = new Point(btnCancel.Left - btnSave.Width - 8, 10);
            };

            // 2. Central Native TabControl
            tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Padding = new Point(12, 6)
            };

            TabPage tabGeneral = new TabPage("General");
            TabPage tabViewport = new TabPage("Viewport");
            TabPage tabColors = new TabPage("Colors");
            TabPage tabGCode = new TabPage("G-Code");
            TabPage tabApps = new TabPage("External Apps");
            TabPage tabAssoc = new TabPage("File Associations");

            initGeneralTab(tabGeneral);
            initViewportTab(tabViewport);
            initColorsTab(tabColors);
            initGCodeTab(tabGCode);
            initAppsTab(tabApps);
            initAssocTab(tabAssoc);

            tabControl.TabPages.AddRange(new TabPage[] { tabGeneral, tabViewport, tabColors, tabGCode, tabApps, tabAssoc });

            this.Controls.Add(tabControl);
            this.Controls.Add(pnlBottom);

            pnlBottom.BringToFront();
            tabControl.SendToBack();
        }

        private void initGeneralTab(TabPage p)
        {
            p.Padding = new Padding(12);

            GroupBox grpPanel = new GroupBox { Text = "File Navigation Panel", Location = new Point(12, 12), Size = new Size(680, 115) };
            chkShowFilePanel = new CheckBox { Text = "Show file navigation panel by default", Location = new Point(16, 26), AutoSize = true };
            chkColorizeFiles = new CheckBox { Text = "Colorize file names by format extension", Location = new Point(16, 54), AutoSize = true };
            chkSortFiles = new CheckBox { Text = "Sort files by format extension in navigation panel", Location = new Point(16, 82), AutoSize = true };
            grpPanel.Controls.AddRange(new Control[] { chkShowFilePanel, chkColorizeFiles, chkSortFiles });
            p.Controls.Add(grpPanel);

            GroupBox grpShading = new GroupBox { Text = "Appearance && Theming", Location = new Point(12, 138), Size = new Size(680, 140) };
            chkFlatShading = new CheckBox { Text = "CAD Flat Shading (automatically unsmooth loaded models)", Location = new Point(16, 26), AutoSize = true };

            Label lblTheme = new Label { Text = "Default Theme:", Location = new Point(16, 60), AutoSize = true };
            cmbTheme = new ComboBox { Location = new Point(120, 56), Width = 220, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbTheme.Items.AddRange(new object[] { "System (follow Windows)", "Dark", "Light" });

            chkSystemWindow = new CheckBox { Text = "Use standard system window frame for main window", Location = new Point(16, 94), AutoSize = true };
            grpShading.Controls.AddRange(new Control[] { chkFlatShading, lblTheme, cmbTheme, chkSystemWindow });
            p.Controls.Add(grpShading);
        }

        private void initViewportTab(TabPage p)
        {
            p.Padding = new Padding(12);

            GroupBox grp = new GroupBox { Text = "Camera & Projection Parameters", Location = new Point(12, 12), Size = new Size(680, 240) };

            int y = 28;
            int h = 34;
            int lblW = 240;
            int txtW = 100;

            void addRow(string text, out TextBox tb)
            {
                Label lbl = new Label { Text = text, Location = new Point(16, y + 3), Size = new Size(lblW, 20) };
                tb = new TextBox { Location = new Point(16 + lblW + 10, y), Size = new Size(txtW, 22) };
                grp.Controls.Add(lbl);
                grp.Controls.Add(tb);
                y += h;
            }

            addRow("Perspective FOV (degrees):", out txtFov);
            addRow("Fisheye FOV (degrees):", out txtFishEyeFov);
            addRow("Camera initial distance shift:", out txtCameraInitialShift);
            addRow("Model rotation step (degrees):", out txtModelRotationAngle);
            addRow("Wireframe edge scale factor:", out txtWireframeEdgeScale);

            chkCenterMass = new CheckBox { Text = "Center camera on mass center by default", Location = new Point(16, y + 4), AutoSize = true };
            grp.Controls.Add(chkCenterMass);

            p.Controls.Add(grp);
        }

        private void initColorsTab(TabPage p)
        {
            p.Padding = new Padding(12);

            GroupBox grpMat = new GroupBox { Text = "Model Materials", Location = new Point(12, 12), Size = new Size(680, 110) };
            GroupBox grpLight = new GroupBox { Text = "Scene Lighting && Ground", Location = new Point(12, 134), Size = new Size(680, 120) };

            ColorControlRow createRow(GroupBox parent, string title, int col, int row, Action<Color> onColorChanged)
            {
                int x = 16 + col * 220;
                int y = 26 + row * 38;

                Label lbl = new Label { Text = title, Location = new Point(x, y + 2), Size = new Size(105, 18) };
                parent.Controls.Add(lbl);

                Button btnSwatch = new Button
                {
                    Location = new Point(x + 108, y),
                    Size = new Size(24, 22),
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand
                };
                btnSwatch.FlatAppearance.BorderSize = 1;
                btnSwatch.FlatAppearance.BorderColor = Color.Gray;
                parent.Controls.Add(btnSwatch);

                TextBox txtHex = new TextBox
                {
                    Location = new Point(btnSwatch.Right + 4, y),
                    Size = new Size(72, 22),
                    CharacterCasing = CharacterCasing.Upper,
                    Font = new Font("Consolas", 9f)
                };
                parent.Controls.Add(txtHex);

                var item = new ColorControlRow
                {
                    Label = lbl,
                    Swatch = btnSwatch,
                    TextHex = txtHex,
                    OnColorChanged = onColorChanged
                };

                btnSwatch.Click += (s, e) =>
                {
                    using (var dlg = new ColorDialog())
                    {
                        dlg.Color = item.CurrentColor;
                        dlg.FullOpen = true;
                        if (dlg.ShowDialog() == DialogResult.OK)
                        {
                            var newCol = dlg.Color;
                            item.CurrentColor = newCol;
                            btnSwatch.BackColor = newCol;
                            txtHex.Text = $"{newCol.R:X2}{newCol.G:X2}{newCol.B:X2}";
                            item.OnColorChanged?.Invoke(newCol);
                        }
                    }
                };

                txtHex.TextChanged += (s, e) =>
                {
                    string clean = txtHex.Text.Trim().TrimStart('#');
                    if (clean.Length == 6)
                    {
                        try
                        {
                            int r = Convert.ToInt32(clean.Substring(0, 2), 16);
                            int g = Convert.ToInt32(clean.Substring(2, 2), 16);
                            int b = Convert.ToInt32(clean.Substring(4, 2), 16);
                            var parsedCol = Color.FromArgb(item.CurrentColor.A, r, g, b);
                            item.CurrentColor = parsedCol;
                            btnSwatch.BackColor = parsedCol;
                            item.OnColorChanged?.Invoke(parsedCol);
                        }
                        catch { }
                    }
                };

                colorRows.Add(item);
                return item;
            }

            // Model Materials (row 0: Diffuse, Specular, Emissive; row 1: Backside Diffuse + Checkbox)
            createRow(grpMat, "Diffuse Color:", 0, 0, c => settings.DiffuseColor = new SColor(toMedia(c)));
            createRow(grpMat, "Specular Color:", 1, 0, c => settings.SpecularColor = new SColor(toMedia(c)));
            createRow(grpMat, "Emissive Color:", 2, 0, c => settings.EmissiveColor = new SColor(toMedia(c)));

            createRow(grpMat, "Backside Diffuse:", 0, 1, c => settings.BackDiffuseColor = new SColor(toMedia(c)));
            chkBacksideDiffuse = new CheckBox { Text = "Enable custom backside color", Location = new Point(236, 66), AutoSize = true };
            grpMat.Controls.Add(chkBacksideDiffuse);

            // Lighting (row 0: Ambient, Light 1, Light 2; row 1: Light 3, Ground Diffuse)
            createRow(grpLight, "Ambient Light:", 0, 0, c => settings.AmbientLightColor = new SColor(toMedia(c)));
            createRow(grpLight, "Light 1 Color:", 1, 0, c => settings.DirectionalLight1Color = new SColor(toMedia(c)));
            createRow(grpLight, "Light 2 Color:", 2, 0, c => settings.DirectionalLight2Color = new SColor(toMedia(c)));

            createRow(grpLight, "Light 3 Color:", 0, 1, c => settings.DirectionalLight3Color = new SColor(toMedia(c)));
            createRow(grpLight, "Ground Diffuse:", 1, 1, c => settings.GroundDiffuseColor = new SColor(toMedia(c)));

            p.Controls.Add(grpMat);
            p.Controls.Add(grpLight);
        }

        private void initGCodeTab(TabPage p)
        {
            p.Padding = new Padding(12);

            GroupBox grp = new GroupBox { Text = "G-Code Toolpath Visualization", Location = new Point(12, 12), Size = new Size(680, 160) };

            int y = 28;
            int lblW = 200;
            int txtW = 100;

            void addRow(string text, out TextBox tb)
            {
                Label lbl = new Label { Text = text, Location = new Point(16, y + 3), Size = new Size(lblW, 20) };
                tb = new TextBox { Location = new Point(16 + lblW + 10, y), Size = new Size(txtW, 22) };
                grp.Controls.Add(lbl);
                grp.Controls.Add(tb);
                y += 34;
            }

            addRow("Layer Height (mm):", out txtGCodeLayerHeight);
            addRow("Extrusion Line Width (mm):", out txtGCodeLineWidth);
            addRow("Line Ends Extension:", out txtGCodeLineEnds);

            p.Controls.Add(grp);
        }

        private void initAppsTab(TabPage p)
        {
            p.Padding = new Padding(12);

            // Left side: Apps list
            Label lblList = new Label { Text = "Configured Applications:", Location = new Point(12, 12), AutoSize = true };
            listApps = new ListBox { Location = new Point(12, 34), Size = new Size(210, 200) };
            listApps.SelectedIndexChanged += (s, e) => loadSelectedAppIntoEditor();

            btnAddApp = new Button { Text = "+ Add...", Location = new Point(12, listApps.Bottom + 8), Size = new Size(65, 26) };
            btnAddApp.Click += (s, e) =>
            {
                using (var ofd = new OpenFileDialog { Filter = "Executables (*.exe)|*.exe|All Files (*.*)|*.*" })
                {
                    if (ofd.ShowDialog(this) == DialogResult.OK)
                    {
                        var app = new ExternalAppItem
                        {
                            Name = Path.GetFileNameWithoutExtension(ofd.FileName),
                            Path = ofd.FileName,
                            Arguments = "\"%1\"",
                            Extensions = "stl,3mf,obj,skp"
                        };
                        currentApps.Add(app);
                        refreshAppsList();
                        listApps.SelectedIndex = currentApps.Count - 1;
                    }
                }
            };

            btnRemoveApp = new Button { Text = "Remove", Location = new Point(btnAddApp.Right + 6, listApps.Bottom + 8), Size = new Size(65, 26) };
            btnRemoveApp.Click += (s, e) =>
            {
                int idx = listApps.SelectedIndex;
                if (idx >= 0 && idx < currentApps.Count)
                {
                    currentApps.RemoveAt(idx);
                    refreshAppsList();
                    if (currentApps.Count > 0) listApps.SelectedIndex = Math.Min(idx, currentApps.Count - 1);
                    else clearAppEditor();
                }
            };

            btnDetectSlicers = new Button { Text = "Auto-detect", Location = new Point(btnRemoveApp.Right + 6, listApps.Bottom + 8), Size = new Size(80, 26) };
            btnDetectSlicers.Click += (s, e) =>
            {
                var detected = Settings.AutoDetectSlicers();
                foreach (var d in detected)
                {
                    if (!currentApps.Any(a => a.Path.Equals(d.Path, StringComparison.OrdinalIgnoreCase)))
                        currentApps.Add(d);
                }
                refreshAppsList();
            };

            p.Controls.AddRange(new Control[] { lblList, listApps, btnAddApp, btnRemoveApp, btnDetectSlicers });

            // Right side: GroupBox for editor
            GroupBox grpEdit = new GroupBox
            {
                Text = "Application Details",
                Location = new Point(236, 12),
                Size = new Size(456, 268)
            };

            Label lblName = new Label { Text = "Application Name:", Location = new Point(16, 26), AutoSize = true };
            txtAppName = new TextBox { Location = new Point(16, 46), Size = new Size(420, 22) };
            txtAppName.TextChanged += (s, e) => saveAppEditorBack();

            Label lblPath = new Label { Text = "Executable Path:", Location = new Point(16, 76), AutoSize = true };
            txtAppPath = new TextBox { Location = new Point(16, 96), Size = new Size(382, 22) };
            txtAppPath.TextChanged += (s, e) => saveAppEditorBack();

            Button btnBrowse = new Button { Text = "...", Location = new Point(txtAppPath.Right + 6, 95), Size = new Size(32, 24) };
            btnBrowse.Click += (s, e) =>
            {
                using (var ofd = new OpenFileDialog { Filter = "Executables (*.exe)|*.exe|All Files (*.*)|*.*" })
                {
                    if (ofd.ShowDialog(this) == DialogResult.OK)
                    {
                        txtAppPath.Text = ofd.FileName;
                        if (string.IsNullOrWhiteSpace(txtAppName.Text))
                            txtAppName.Text = Path.GetFileNameWithoutExtension(ofd.FileName);
                    }
                }
            };

            Label lblArgs = new Label { Text = "Arguments (Windows placeholder \"%1\"): ", Location = new Point(16, 126), AutoSize = true };
            txtAppArgs = new TextBox { Location = new Point(16, 146), Size = new Size(420, 22) };
            txtAppArgs.TextChanged += (s, e) => saveAppEditorBack();

            Label lblExts = new Label { Text = "Supported File Formats:", Location = new Point(16, 178), AutoSize = true };
            grpEdit.Controls.AddRange(new Control[] { lblName, txtAppName, lblPath, txtAppPath, btnBrowse, lblArgs, txtAppArgs, lblExts });

            string[] exts = { "stl", "3mf", "obj", "skp", "gcode" };
            chkAppExts = new CheckBox[exts.Length];
            int cx = 16;
            for (int i = 0; i < exts.Length; i++)
            {
                chkAppExts[i] = new CheckBox { Text = "." + exts[i], Tag = exts[i], Location = new Point(cx, 202), AutoSize = true };
                chkAppExts[i].CheckedChanged += (s, e) => saveAppEditorBack();
                grpEdit.Controls.Add(chkAppExts[i]);
                cx += 68;
            }

            p.Controls.Add(grpEdit);
        }

        private void initAssocTab(TabPage p)
        {
            p.Padding = new Padding(12);

            GroupBox grp = new GroupBox
            {
                Text = "Windows Explorer File Associations (HKCU)",
                Location = new Point(12, 12),
                Size = new Size(680, 180)
            };

            string[] exts = FileAssociationManager.SupportedExtensions;
            chkAssocExts = new CheckBox[exts.Length];
            int x = 20;
            int y = 30;
            for (int i = 0; i < exts.Length; i++)
            {
                string ext = exts[i];
                chkAssocExts[i] = new CheckBox
                {
                    Text = $"3D Model {ext.ToUpperInvariant()} ({ext})",
                    Tag = ext,
                    Location = new Point(x, y),
                    Size = new Size(260, 24),
                    Checked = FileAssociationManager.IsAssociated(ext)
                };
                chkAssocExts[i].Click += (s, e) =>
                {
                    var cb = (CheckBox)s;
                    FileAssociationManager.SetAssociation(cb.Tag as string, cb.Checked);
                    cb.Checked = FileAssociationManager.IsAssociated(cb.Tag as string);
                };
                grp.Controls.Add(chkAssocExts[i]);

                if (i % 2 == 1) { x = 20; y += 32; }
                else { x = 320; }
            }
            p.Controls.Add(grp);

            btnSelectAllAssoc = new Button { Text = "Select All", Location = new Point(12, grp.Bottom + 12), Size = new Size(100, 28) };
            btnSelectAllAssoc.Click += (s, e) =>
            {
                foreach (var cb in chkAssocExts)
                {
                    FileAssociationManager.SetAssociation(cb.Tag as string, true);
                    cb.Checked = FileAssociationManager.IsAssociated(cb.Tag as string);
                }
            };
            p.Controls.Add(btnSelectAllAssoc);

            btnDeselectAllAssoc = new Button { Text = "Deselect All", Location = new Point(btnSelectAllAssoc.Right + 10, grp.Bottom + 12), Size = new Size(100, 28) };
            btnDeselectAllAssoc.Click += (s, e) =>
            {
                foreach (var cb in chkAssocExts)
                {
                    FileAssociationManager.SetAssociation(cb.Tag as string, false);
                    cb.Checked = FileAssociationManager.IsAssociated(cb.Tag as string);
                }
            };
            p.Controls.Add(btnDeselectAllAssoc);
        }

        private void refreshAppsList()
        {
            listApps.Items.Clear();
            foreach (var a in currentApps)
                listApps.Items.Add(string.IsNullOrWhiteSpace(a.Name) ? Path.GetFileName(a.Path) : a.Name);
        }

        private void loadSelectedAppIntoEditor()
        {
            int idx = listApps.SelectedIndex;
            if (idx < 0 || idx >= currentApps.Count) { clearAppEditor(); return; }
            var app = currentApps[idx];
            txtAppName.Text = app.Name;
            txtAppPath.Text = app.Path;
            txtAppArgs.Text = app.Arguments;
            foreach (var cb in chkAppExts)
                cb.Checked = app.SupportsExtension(cb.Tag as string);
        }

        private void clearAppEditor()
        {
            txtAppName.Text = "";
            txtAppPath.Text = "";
            txtAppArgs.Text = "";
            foreach (var cb in chkAppExts) cb.Checked = false;
        }

        private void saveAppEditorBack()
        {
            int idx = listApps.SelectedIndex;
            if (idx < 0 || idx >= currentApps.Count) return;
            var app = currentApps[idx];
            app.Name = txtAppName.Text;
            app.Path = txtAppPath.Text;
            app.Arguments = txtAppArgs.Text;
            var list = new List<string>();
            foreach (var cb in chkAppExts)
                if (cb.Checked) list.Add(cb.Tag as string);
            app.Extensions = string.Join(",", list);
        }

        private void bindData()
        {
            // General
            chkShowFilePanel.Checked = settings.ShowFilePanel;
            chkColorizeFiles.Checked = settings.ColorizeFiles;
            chkSortFiles.Checked = settings.SortFilesByExtensions;
            chkFlatShading.Checked = settings.UnsmoothAfterLoading;
            cmbTheme.SelectedIndex = (int)settings.ThemeMode;
            chkSystemWindow.Checked = settings.UseSystemWindowStyle;

            // Viewport
            txtFov.Text = settings.FOV.ToString("0.#");
            txtFishEyeFov.Text = settings.FishEyeFOV.ToString("0.#");
            txtCameraInitialShift.Text = settings.CameraInitialShift.ToString("0.#");
            txtModelRotationAngle.Text = settings.ModelRotationAngle.ToString("0.#");
            txtWireframeEdgeScale.Text = settings.WireframeEdgeScale.ToString("0.###");
            chkCenterMass.Checked = settings.DefaultLookAtModelPointsAvgCenter;

            // Colors
            Color[] colors = new Color[]
            {
                toDrawing(settings.DiffuseColor.Color),
                toDrawing(settings.SpecularColor.Color),
                toDrawing(settings.EmissiveColor.Color),
                toDrawing(settings.BackDiffuseColor.Color),
                toDrawing(settings.AmbientLightColor.Color),
                toDrawing(settings.DirectionalLight1Color.Color),
                toDrawing(settings.DirectionalLight2Color.Color),
                toDrawing(settings.DirectionalLight3Color.Color),
                toDrawing(settings.GroundDiffuseColor.Color)
            };

            for (int i = 0; i < colorRows.Count && i < colors.Length; i++)
            {
                var row = colorRows[i];
                var c = colors[i];
                row.CurrentColor = c;
                row.Swatch.BackColor = c;
                row.TextHex.Text = $"{c.R:X2}{c.G:X2}{c.B:X2}";
            }

            chkBacksideDiffuse.Checked = settings.IsBackDiffuseEnabled;

            // G-Code
            txtGCodeLayerHeight.Text = settings.GCODELayerHeight.ToString("0.##");
            txtGCodeLineWidth.Text = settings.GCODELineWidth.ToString("0.##");
            txtGCodeLineEnds.Text = settings.GCODELineExtensions.ToString();

            // External Apps
            refreshAppsList();
            if (currentApps.Count > 0) listApps.SelectedIndex = 0;
        }

        private void saveUIToSettings()
        {
            // General
            settings.ShowFilePanel = chkShowFilePanel.Checked;
            settings.ColorizeFiles = chkColorizeFiles.Checked;
            settings.SortFilesByExtensions = chkSortFiles.Checked;
            settings.UnsmoothAfterLoading = chkFlatShading.Checked;
            settings.ThemeMode = (AppThemeMode)Math.Max(0, cmbTheme.SelectedIndex);
            settings.UseSystemWindowStyle = chkSystemWindow.Checked;

            // Viewport
            if (double.TryParse(txtFov.Text, out double fov)) settings.FOV = Utils.MinMax(fov, 5.0, 160.0);
            if (double.TryParse(txtFishEyeFov.Text, out double feFov)) settings.FishEyeFOV = Utils.MinMax(feFov, 5.0, 160.0);
            if (double.TryParse(txtCameraInitialShift.Text, out double shift)) settings.CameraInitialShift = Utils.MinMax(shift, 0.5, 10.0);
            if (double.TryParse(txtModelRotationAngle.Text, out double rot)) settings.ModelRotationAngle = Utils.MinMax(rot, 5.0, 180.0);
            if (double.TryParse(txtWireframeEdgeScale.Text, out double wes)) settings.WireframeEdgeScale = Utils.MinMax(wes, 0.001, 0.9);
            settings.DefaultLookAtModelPointsAvgCenter = chkCenterMass.Checked;

            // Colors
            settings.IsBackDiffuseEnabled = chkBacksideDiffuse.Checked;

            // G-Code
            if (double.TryParse(txtGCodeLayerHeight.Text, out double lh)) settings.GCODELayerHeight = lh;
            if (double.TryParse(txtGCodeLineWidth.Text, out double lw)) settings.GCODELineWidth = lw;
            if (double.TryParse(txtGCodeLineEnds.Text, out double le)) settings.GCODELineExtensions = le;

            // External Apps
            settings.SetExternalApps(currentApps);
        }

        private static Color toDrawing(System.Windows.Media.Color c) => Color.FromArgb(c.A, c.R, c.G, c.B);
        private static System.Windows.Media.Color toMedia(Color c) => System.Windows.Media.Color.FromArgb(c.A, c.R, c.G, c.B);
    }
}
