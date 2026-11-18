# Specification: UI Polish, Direct Binary Export & Settings Overhaul

## 1. Overview
This track addresses long-standing UI inconsistencies, streamlines model export, and completely redesigns the Settings dialog according to user specifications:
1. **Toolbar Icon Tuning:** Resize coolicons from 16px to 20px with adjusted button padding for optimal clarity in WPF Direct3D rendering.
2. **System Drive Icons:** Restore genuine Windows Shell drive icons in `FileNavigationPanel`.
3. **Streamlined Binary Export:** Eliminate intermediate modal window and background blur; launch native `SaveFileDialog` directly with pure Binary STL (default), OBJ, 3MF, and Binary PLY (dropping obsolete ASCII formats).
4. **Settings Dialog Redesign:** Replace the cluttered, custom-framed scrolling view with a clean, two-pane system window (left plain-text category list, right no-scroll category panels, 2/3-column color grid, red backface default, no AI slop).
5. **Configurable Launch Parameters:** Support custom launch arguments for both External Applications (`$file$`) and Windows File Associations (`"%1"` with optional flags).

---

## 2. Functional Requirements

### 2.1 Toolbar 20px Icons
- Update all 11 toolbar buttons in `MainWindow.xaml`:
  - Set `<Viewbox Width="20" Height="20">`.
  - Adjust SplitButton main button width to 28px (`Width="28"`) to maintain consistent 4–6px margins.
  - Maintain `SnapsToDevicePixels="True"` and `UseLayoutRounding="True"`.

### 2.2 Shell System Drive Icons
- In `FileNavigationPanel.xaml.cs`:
  - Restore `GetSystemIcon(drivePath)` for drive items in `drivesWrapPanel`.
  - Maintain 16×16 system icon dimensions and runtime DPI normalization.

### 2.3 Direct Binary Export & Dead Code Removal
- In `MainWindow.xaml.cs` (`export()` method):
  - Remove `Utils.MessageWindow("Select export format", ...)` and parent `BlurEffect`.
  - Directly open `Microsoft.Win32.SaveFileDialog`.
  - Filter string:
    `"STL (*.stl)|*.stl|Wavefront OBJ (*.obj)|*.obj|3D Manufacturing Format (*.3mf)|*.3mf|Polygon File Format (*.ply)|*.ply"`
  - `FilterIndex = 1` (STL by default).
  - Export strictly in Binary format for STL and PLY.
  - Remove ASCII export implementations and associated dead code.

### 2.4 Settings Dialog Complete Overhaul
- **Window Frame & Form Factor:**
  - Standard native Windows dialog window (`WindowStyle="SingleBorderWindow"` or standard system title bar, native `—`, `✕` controls).
  - No background blur (`BlurEffect = null`) on parent window.
  - Fixed, comfortable dimensions (~720×460 px) where **every category panel fits entirely on screen without any vertical scrollbar**.
- **Left Navigation Sidebar:**
  - Clean, plain text list of categories (no emojis, no icons):
    - `General`
    - `Viewport`
    - `Colors`
    - `G-Code`
    - `External Apps`
    - `File Associations`
  - Subtle highlight / bold text for the active category.
- **Right Content Panels:**
  - **General:**
    - `[✓] Show file panel`
    - `[✓] Colorize files by extension`
    - `[✓] Sort files by extension`
    - `[✓] CAD flat shading`
    - `[ ] Light theme`
  - **Viewport:**
    - Clean 2-column property grid:
      - Normal FOV (degrees): `[ 45 ]`
      - Fisheye FOV (degrees): `[ 120 ]`
      - Camera initial shift: `[ 1.5 ]`
      - Rotation angle (deg): `[ 90 ]`
      - Wireframe edge scale: `[ 0.03 ]`
      - `[✓] Center camera on mass center`
  - **Colors (2-3 Column Grid):**
    - Compact grid layout (2 or 3 items per row) avoiding long single-column scroll:
      - Model Diffuse | Model Specular | Model Emissive
      - Backside Diffuse (default bright red `#FF0000`) | Ambient Light | Directional Light
      - Ground Diffuse | Ground Emissive
    - Each item: label + swatch box + `#RRGGBB` input field (no "Hex:" prefix).
    - Swatch click opens a clean, modern compact color picker.
  - **G-Code:**
    - Plain text note under header: `Changes apply after reloading model` (no italics, no icons, no [F5]).
    - Layer height (mm): `[ 0.2 ]`
    - Line width (mm): `[ 0.5 ]`
    - Line ends extension: `[ 0.0 ]`
  - **External Apps:**
    - Clean list of configured external tools.
    - Fields:
      - App Name: `[ e.g. OrcaSlicer ]`
      - Path: `[ ...\orca-slicer.exe ]` `[ Browse... ]`
      - Arguments: `[ $file$ ]` (custom flags supported)
      - Extensions: `[ stl, 3mf, obj ]`
    - Action buttons: `[ Add ]`, `[ Remove ]`, `[ Auto-detect Slicers ]`.
  - **File Associations:**
    - Checkboxes: `[ ] .stl  [ ] .3mf  [ ] .obj  [ ] .skp  [ ] .ply  [ ] .gcode`.
    - Launch arguments input: `[ "%1" ]` (allows adding custom parameters/flags for registered shell commands).
    - Button: `[ Apply Associations (HKCU) ]`.
- **Bottom Command Bar:**
  - Left: `[ Reset to Defaults ]` `[ Open INI ]`
  - Right: `[ Save ]` `[ Cancel ]`

---

## 3. Non-Functional Requirements
- .NET Framework 4.8 compatibility.
- Clean compilation without external heavyweight dependencies.
- Zero UI regressions on 96 DPI and high-DPI monitors.
