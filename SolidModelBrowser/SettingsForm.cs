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

        private ListBox listCategories;
        private Panel panelContainer;
        private Panel[] categoryPanels;

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
        private Color btnDiffuseColor;
        private Color btnSpecularColor;
        private Color btnEmissiveColor;
        private Color btnBackDiffuseColor;
        private Color btnAmbientColor;
        private Color btnLight1Color;
        private Color btnLight2Color;
        private Color btnLight3Color;
        private Color btnGroundDiffuseColor;
        private CheckBox chkBacksideDiffuse;

        private Button btnColorDiffuse;
        private Button btnColorSpecular;
        private Button btnColorEmissive;
        private Button btnColorBackDiffuse;
        private Button btnColorAmbient;
        private Button btnColorLight1;
        private Button btnColorLight2;
        private Button btnColorLight3;
        private Button btnColorGroundDiffuse;

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
            this.Size = new Size(720, 480);
            this.MinimumSize = new Size(680, 440);
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
            // Bottom Command Bar
            Panel pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 44,
                Padding = new Padding(12, 8, 12, 8)
            };
            this.Controls.Add(pnlBottom);

            btnDefaults = new Button { Text = "Reset to Defaults", Location = new Point(12, 8), Size = new Size(120, 26) };
            btnDefaults.Click += (s, e) => { WindowResult = SettingsWindowResult.ResetDefaults; this.DialogResult = DialogResult.OK; this.Close(); };
            pnlBottom.Controls.Add(btnDefaults);

            btnOpenIni = new Button { Text = "Open INI", Location = new Point(btnDefaults.Right + 8, 8), Size = new Size(80, 26) };
            btnOpenIni.Click += (s, e) => { WindowResult = SettingsWindowResult.OpenEditor; this.DialogResult = DialogResult.OK; this.Close(); };
            pnlBottom.Controls.Add(btnOpenIni);

            btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Anchor = AnchorStyles.Top | AnchorStyles.Right };
            btnCancel.Size = new Size(75, 26);
            btnCancel.Location = new Point(pnlBottom.Width - btnCancel.Width - 14, 8);
            this.CancelButton = btnCancel;
            pnlBottom.Controls.Add(btnCancel);

            btnSave = new Button { Text = "Save", DialogResult = DialogResult.OK, Anchor = AnchorStyles.Top | AnchorStyles.Right };
            btnSave.Size = new Size(75, 26);
            btnSave.Location = new Point(btnCancel.Left - btnSave.Width - 8, 8);
            btnSave.Click += (s, e) => { saveUIToSettings(); this.Close(); };
            this.AcceptButton = btnSave;
            pnlBottom.Controls.Add(btnSave);

            // Left Navigation Sidebar
            listCategories = new ListBox
            {
                Dock = DockStyle.Left,
                Width = 150,
                IntegralHeight = false,
                ItemHeight = 28,
                Font = new Font(this.Font.FontFamily, 9.5f)
            };
            listCategories.Items.AddRange(new object[] {
                "General",
                "Viewport",
                "Colors",
                "G-Code",
                "External Apps",
                "File Associations"
            });
            listCategories.SelectedIndexChanged += (s, e) => switchCategory(listCategories.SelectedIndex);
            this.Controls.Add(listCategories);

            // Right Container
            panelContainer = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16, 12, 16, 12)
            };
            this.Controls.Add(panelContainer);

            // Category Panels
            categoryPanels = new Panel[6];
            for (int i = 0; i < 6; i++)
            {
                categoryPanels[i] = new Panel { Dock = DockStyle.Fill, Visible = false };
                panelContainer.Controls.Add(categoryPanels[i]);
            }

            initGeneralPanel(categoryPanels[0]);
            initViewportPanel(categoryPanels[1]);
            initColorsPanel(categoryPanels[2]);
            initGCodePanel(categoryPanels[3]);
            initAppsPanel(categoryPanels[4]);
            initAssociationsPanel(categoryPanels[5]);

            listCategories.SelectedIndex = 0;
        }

        private void switchCategory(int index)
        {
            if (index < 0 || index >= categoryPanels.Length) return;
            for (int i = 0; i < categoryPanels.Length; i++)
                categoryPanels[i].Visible = (i == index);
        }

        private void initGeneralPanel(Panel p)
        {
            Label lblTitle = new Label { Text = "General & Interface", Font = new Font(this.Font, FontStyle.Bold), Location = new Point(10, 10), AutoSize = true };
            p.Controls.Add(lblTitle);

            GroupBox grpFile = new GroupBox { Text = "File Panel", Location = new Point(10, 36), Size = new Size(500, 100) };
            chkShowFilePanel = new CheckBox { Text = "Show file panel", Location = new Point(16, 22), AutoSize = true };
            chkColorizeFiles = new CheckBox { Text = "Colorize files by extension", Location = new Point(16, 46), AutoSize = true };
            chkSortFiles = new CheckBox { Text = "Sort files by extension", Location = new Point(16, 70), AutoSize = true };
            grpFile.Controls.AddRange(new Control[] { chkShowFilePanel, chkColorizeFiles, chkSortFiles });
            p.Controls.Add(grpFile);

            GroupBox grpApp = new GroupBox { Text = "Appearance & Shading", Location = new Point(10, grpFile.Bottom + 10), Size = new Size(500, 120) };
            chkFlatShading = new CheckBox { Text = "CAD flat shading", Location = new Point(16, 22), AutoSize = true };

            Label lblTheme = new Label { Text = "Default Theme:", Location = new Point(16, 52), AutoSize = true };
            cmbTheme = new ComboBox { Location = new Point(120, 48), Size = new Size(180, 24), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbTheme.Items.AddRange(new object[] { "System (follow Windows)", "Dark", "Light" });

            chkSystemWindow = new CheckBox { Text = "Use system style for main window", Location = new Point(16, 82), AutoSize = true };

            grpApp.Controls.AddRange(new Control[] { chkFlatShading, lblTheme, cmbTheme, chkSystemWindow });
            p.Controls.Add(grpApp);
        }

        private void initViewportPanel(Panel p)
        {
            Label lblTitle = new Label { Text = "Viewport & Camera", Font = new Font(this.Font, FontStyle.Bold), Location = new Point(10, 10), AutoSize = true };
            p.Controls.Add(lblTitle);

            GroupBox grp = new GroupBox { Text = "Camera Settings", Location = new Point(10, 36), Size = new Size(500, 200) };

            int y = 26;
            int h = 30;
            int lblW = 200;
            int txtW = 100;

            void addRow(string label, out TextBox tb)
            {
                Label l = new Label { Text = label, Location = new Point(16, y + 3), Size = new Size(lblW, 20) };
                tb = new TextBox { Location = new Point(lblW + 20, y), Size = new Size(txtW, 22) };
                grp.Controls.Add(l);
                grp.Controls.Add(tb);
                y += h;
            }

            addRow("Normal FOV (degrees):", out txtFov);
            addRow("Fisheye FOV (degrees):", out txtFishEyeFov);
            addRow("Camera initial shift:", out txtCameraInitialShift);
            addRow("Rotation angle (degrees):", out txtModelRotationAngle);
            addRow("Wireframe edge scale:", out txtWireframeEdgeScale);

            chkCenterMass = new CheckBox { Text = "Center camera on mass center", Location = new Point(16, y + 4), AutoSize = true };
            grp.Controls.Add(chkCenterMass);

            p.Controls.Add(grp);
        }

        private void initColorsPanel(Panel p)
        {
            Label lblTitle = new Label { Text = "Colors & Materials", Font = new Font(this.Font, FontStyle.Bold), Location = new Point(10, 10), AutoSize = true };
            p.Controls.Add(lblTitle);

            Label lblHint = new Label { Text = "Click a color box to open the Figma color picker.", Location = new Point(10, 34), AutoSize = true, ForeColor = SystemColors.GrayText };
            p.Controls.Add(lblHint);

            int startY = 60;
            int colW = 160;
            int rowH = 58;

            Button createColorItem(string title, int col, int row, Color initialColor, Action<Color> onColorChanged)
            {
                int x = 10 + col * colW;
                int y = startY + row * rowH;

                Label lbl = new Label { Text = title, Location = new Point(x, y), Size = new Size(colW - 10, 16) };
                p.Controls.Add(lbl);

                Button btn = new Button
                {
                    Location = new Point(x, y + 18),
                    Size = new Size(colW - 20, 28),
                    BackColor = initialColor,
                    Text = $"#{initialColor.R:X2}{initialColor.G:X2}{initialColor.B:X2}",
                    ForeColor = (0.299 * initialColor.R + 0.587 * initialColor.G + 0.114 * initialColor.B) > 125 ? Color.Black : Color.White,
                    FlatStyle = FlatStyle.Flat
                };
                btn.Click += (s, e) =>
                {
                    using (var dlg = new FigmaColorPickerForm(btn.BackColor))
                    {
                        if (dlg.ShowDialog(this) == DialogResult.OK)
                        {
                            btn.BackColor = dlg.SelectedColor;
                            btn.Text = $"#{dlg.SelectedColor.R:X2}{dlg.SelectedColor.G:X2}{dlg.SelectedColor.B:X2}";
                            btn.ForeColor = (0.299 * dlg.SelectedColor.R + 0.587 * dlg.SelectedColor.G + 0.114 * dlg.SelectedColor.B) > 125 ? Color.Black : Color.White;
                            onColorChanged(dlg.SelectedColor);
                        }
                    }
                };
                p.Controls.Add(btn);
                return btn;
            }

            btnColorDiffuse = createColorItem("Model Diffuse", 0, 0, Color.FromArgb(154, 162, 170), c => btnDiffuseColor = c);
            btnColorSpecular = createColorItem("Model Specular", 1, 0, Color.FromArgb(48, 48, 48), c => btnSpecularColor = c);
            btnColorEmissive = createColorItem("Model Emissive", 2, 0, Color.FromArgb(16, 18, 21), c => btnEmissiveColor = c);

            btnColorBackDiffuse = createColorItem("Backside (Inverted)", 0, 1, Color.FromArgb(255, 0, 0), c => btnBackDiffuseColor = c);
            btnColorAmbient = createColorItem("Ambient Light", 1, 1, Color.FromArgb(48, 50, 54), c => btnAmbientColor = c);
            btnColorLight1 = createColorItem("Directional Light 1", 2, 1, Color.FromArgb(184, 188, 196), c => btnLight1Color = c);

            btnColorLight2 = createColorItem("Directional Light 2", 0, 2, Color.FromArgb(104, 112, 124), c => btnLight2Color = c);
            btnColorLight3 = createColorItem("Directional Light 3", 1, 2, Color.FromArgb(64, 68, 76), c => btnLight3Color = c);
            btnColorGroundDiffuse = createColorItem("Ground Diffuse", 2, 2, Color.FromArgb(255, 255, 255), c => btnGroundDiffuseColor = c);

            chkBacksideDiffuse = new CheckBox { Text = "Show backside in custom color", Location = new Point(10, startY + 3 * rowH + 4), AutoSize = true };
            p.Controls.Add(chkBacksideDiffuse);
        }

        private void initGCodePanel(Panel p)
        {
            Label lblTitle = new Label { Text = "G-Code Import", Font = new Font(this.Font, FontStyle.Bold), Location = new Point(10, 10), AutoSize = true };
            p.Controls.Add(lblTitle);

            Label lblNote = new Label { Text = "Changes apply after reloading model", Location = new Point(10, 32), AutoSize = true, ForeColor = SystemColors.GrayText };
            p.Controls.Add(lblNote);

            GroupBox grp = new GroupBox { Text = "Parameters", Location = new Point(10, 56), Size = new Size(500, 130) };

            int y = 24;
            int lblW = 200;
            int txtW = 100;

            void addRow(string label, out TextBox tb)
            {
                Label l = new Label { Text = label, Location = new Point(16, y + 3), Size = new Size(lblW, 20) };
                tb = new TextBox { Location = new Point(lblW + 20, y), Size = new Size(txtW, 22) };
                grp.Controls.Add(l);
                grp.Controls.Add(tb);
                y += 30;
            }

            addRow("Layer height (mm):", out txtGCodeLayerHeight);
            addRow("Line width (mm):", out txtGCodeLineWidth);
            addRow("Line ends extension:", out txtGCodeLineEnds);

            p.Controls.Add(grp);
        }

        private void initAppsPanel(Panel p)
        {
            Label lblTitle = new Label { Text = "External Applications", Font = new Font(this.Font, FontStyle.Bold), Location = new Point(10, 10), AutoSize = true };
            p.Controls.Add(lblTitle);

            listApps = new ListBox { Location = new Point(10, 34), Size = new Size(220, 160) };
            listApps.SelectedIndexChanged += (s, e) => loadSelectedAppIntoEditor();
            p.Controls.Add(listApps);

            btnAddApp = new Button { Text = "+ Add...", Location = new Point(10, listApps.Bottom + 8), Size = new Size(70, 24) };
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
            p.Controls.Add(btnAddApp);

            btnRemoveApp = new Button { Text = "Remove", Location = new Point(btnAddApp.Right + 6, listApps.Bottom + 8), Size = new Size(70, 24) };
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
            p.Controls.Add(btnRemoveApp);

            btnDetectSlicers = new Button { Text = "Auto-detect", Location = new Point(btnRemoveApp.Right + 6, listApps.Bottom + 8), Size = new Size(80, 24) };
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
            p.Controls.Add(btnDetectSlicers);

            // Editor controls on the right
            int exX = 246;
            int exW = 270;

            Label lblName = new Label { Text = "App Name:", Location = new Point(exX, 34), AutoSize = true };
            txtAppName = new TextBox { Location = new Point(exX, 52), Size = new Size(exW, 22) };
            txtAppName.TextChanged += (s, e) => saveAppEditorBack();

            Label lblPath = new Label { Text = "Executable Path:", Location = new Point(exX, 80), AutoSize = true };
            txtAppPath = new TextBox { Location = new Point(exX, 98), Size = new Size(exW - 30, 22) };
            txtAppPath.TextChanged += (s, e) => saveAppEditorBack();

            Button btnBrowsePath = new Button { Text = "...", Location = new Point(exX + exW - 28, 97), Size = new Size(28, 24) };
            btnBrowsePath.Click += (s, e) =>
            {
                using (var ofd = new OpenFileDialog { Filter = "Executables (*.exe)|*.exe|All Files (*.*)|*.*" })
                {
                    if (ofd.ShowDialog(this) == DialogResult.OK)
                    {
                        txtAppPath.Text = ofd.FileName;
                        if (string.IsNullOrWhiteSpace(txtAppName.Text)) txtAppName.Text = Path.GetFileNameWithoutExtension(ofd.FileName);
                    }
                }
            };

            Label lblArgs = new Label { Text = "Arguments (e.g. \"%1\"):", Location = new Point(exX, 126), AutoSize = true };
            txtAppArgs = new TextBox { Location = new Point(exX, 144), Size = new Size(exW, 22) };
            txtAppArgs.TextChanged += (s, e) => saveAppEditorBack();

            Label lblExts = new Label { Text = "Supported Extensions:", Location = new Point(exX, 172), AutoSize = true };

            p.Controls.AddRange(new Control[] { lblName, txtAppName, lblPath, txtAppPath, btnBrowsePath, lblArgs, txtAppArgs, lblExts });

            string[] exts = { "stl", "3mf", "obj", "skp", "gcode" };
            chkAppExts = new CheckBox[exts.Length];
            int cx = exX;
            for (int i = 0; i < exts.Length; i++)
            {
                chkAppExts[i] = new CheckBox { Text = "." + exts[i], Tag = exts[i], Location = new Point(cx, 192), AutoSize = true };
                chkAppExts[i].CheckedChanged += (s, e) => saveAppEditorBack();
                p.Controls.Add(chkAppExts[i]);
                cx += 52;
            }
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

        private void initAssociationsPanel(Panel p)
        {
            Label lblTitle = new Label { Text = "Windows File Associations (HKCU)", Font = new Font(this.Font, FontStyle.Bold), Location = new Point(10, 10), AutoSize = true };
            p.Controls.Add(lblTitle);

            Label lblSubtitle = new Label { Text = "Associate 3D file formats with SolidModelBrowser in the current user profile.", Location = new Point(10, 32), AutoSize = true, ForeColor = SystemColors.GrayText };
            p.Controls.Add(lblSubtitle);

            GroupBox grp = new GroupBox { Text = "File Formats", Location = new Point(10, 60), Size = new Size(500, 140) };

            string[] exts = FileAssociationManager.SupportedExtensions;
            chkAssocExts = new CheckBox[exts.Length];
            int x = 16;
            int y = 26;
            for (int i = 0; i < exts.Length; i++)
            {
                string ext = exts[i];
                chkAssocExts[i] = new CheckBox
                {
                    Text = $"3D Model {ext.ToUpperInvariant()} ({ext})",
                    Tag = ext,
                    Location = new Point(x, y),
                    Size = new Size(220, 24),
                    Checked = FileAssociationManager.IsAssociated(ext)
                };
                chkAssocExts[i].Click += (s, e) =>
                {
                    var cb = (CheckBox)s;
                    FileAssociationManager.SetAssociation(cb.Tag as string, cb.Checked);
                    cb.Checked = FileAssociationManager.IsAssociated(cb.Tag as string);
                };
                grp.Controls.Add(chkAssocExts[i]);

                if (i % 2 == 1) { x = 16; y += 28; }
                else { x = 250; }
            }
            p.Controls.Add(grp);

            btnSelectAllAssoc = new Button { Text = "Select All", Location = new Point(10, grp.Bottom + 10), Size = new Size(90, 26) };
            btnSelectAllAssoc.Click += (s, e) =>
            {
                foreach (var cb in chkAssocExts)
                {
                    FileAssociationManager.SetAssociation(cb.Tag as string, true);
                    cb.Checked = FileAssociationManager.IsAssociated(cb.Tag as string);
                }
            };
            p.Controls.Add(btnSelectAllAssoc);

            btnDeselectAllAssoc = new Button { Text = "Deselect All", Location = new Point(btnSelectAllAssoc.Right + 8, grp.Bottom + 10), Size = new Size(90, 26) };
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
            void setBtnCol(Button btn, Color c, out Color target)
            {
                target = c;
                btn.BackColor = c;
                btn.Text = $"#{c.R:X2}{c.G:X2}{c.B:X2}";
                btn.ForeColor = (0.299 * c.R + 0.587 * c.G + 0.114 * c.B) > 125 ? Color.Black : Color.White;
            }

            setBtnCol(btnColorDiffuse, toDrawing(settings.DiffuseColor.Color), out btnDiffuseColor);
            setBtnCol(btnColorSpecular, toDrawing(settings.SpecularColor.Color), out btnSpecularColor);
            setBtnCol(btnColorEmissive, toDrawing(settings.EmissiveColor.Color), out btnEmissiveColor);
            setBtnCol(btnColorBackDiffuse, toDrawing(settings.BackDiffuseColor.Color), out btnBackDiffuseColor);
            setBtnCol(btnColorAmbient, toDrawing(settings.AmbientLightColor.Color), out btnAmbientColor);
            setBtnCol(btnColorLight1, toDrawing(settings.DirectionalLight1Color.Color), out btnLight1Color);
            setBtnCol(btnColorLight2, toDrawing(settings.DirectionalLight2Color.Color), out btnLight2Color);
            setBtnCol(btnColorLight3, toDrawing(settings.DirectionalLight3Color.Color), out btnLight3Color);
            setBtnCol(btnColorGroundDiffuse, toDrawing(settings.GroundDiffuseColor.Color), out btnGroundDiffuseColor);
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
            settings.DiffuseColor = new SColor(toMedia(btnDiffuseColor));
            settings.SpecularColor = new SColor(toMedia(btnSpecularColor));
            settings.EmissiveColor = new SColor(toMedia(btnEmissiveColor));
            settings.BackDiffuseColor = new SColor(toMedia(btnBackDiffuseColor));
            settings.AmbientLightColor = new SColor(toMedia(btnAmbientColor));
            settings.DirectionalLight1Color = new SColor(toMedia(btnLight1Color));
            settings.DirectionalLight2Color = new SColor(toMedia(btnLight2Color));
            settings.DirectionalLight3Color = new SColor(toMedia(btnLight3Color));
            settings.GroundDiffuseColor = new SColor(toMedia(btnGroundDiffuseColor));
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
