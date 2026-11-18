# Implementation Plan: UI Polish, Direct Binary Export & Settings Overhaul

## Phase 1: Toolbar 20px Icons & Windows Shell Drive Icons
- [x] Task: Update Toolbar Button Dimensions & Icon Viewbox
  - [x] In `MainWindow.xaml`, change `<Viewbox Width="16" Height="16">` to `<Viewbox Width="20" Height="20">` for all 11 toolbar buttons.
  - [x] Adjust `ButtonExternalApp` and other SplitButtons to `Width="28"` so 20px icons fit with clean margins.
- [x] Task: Restore Windows Shell System Drive Icons
  - [x] In `SolidModelBrowser/Controls/FileNavigationPanel.xaml.cs`, restore `GetSystemIcon(drivePath)` for `drivesWrapPanel` items.
  - [x] Ensure 16×16 system icon dimensions and runtime DPI normalization.
- [x] Task: Phase Verification & Checkpoint (Verify toolbar scaling and drive icons)

---

## Phase 2: Direct Binary SaveFileDialog & Dead Code Cleanup
- [x] Task: Simplify `export()` in `MainWindow.xaml.cs`
  - [x] Eliminate `Utils.MessageWindow("Select export format", ...)` intermediate dialog and remove `this.Effect = new BlurEffect()`.
  - [x] Launch `Microsoft.Win32.SaveFileDialog` directly with filter: `"STL (*.stl)|*.stl|Wavefront OBJ (*.obj)|*.obj|3D Manufacturing Format (*.3mf)|*.3mf|Polygon File Format (*.ply)|*.ply"`.
  - [x] Set default `FilterIndex = 1` (STL).
  - [x] Save strictly in Binary format for STL and PLY.
- [x] Task: Purge Obsolete ASCII Exporters & Dead Code
  - [x] Remove ASCII STL / PLY export routines and any unused dialog helper methods.
- [x] Task: Phase Verification & Checkpoint (Verify export directly invokes SaveFileDialog without modal/blur)

---

## Phase 3: Settings Dialog Redesign (Two-Pane System Window)
- [x] Task: Redesign `SettingsWindow.xaml` Layout
  - [x] Set `WindowStyle="SingleBorderWindow"` (or native title bar with standard system close/minimize).
  - [x] Remove parent window `BlurEffect` upon opening `SettingsWindow`.
  - [x] Implement two-pane layout: Left plain-text category list (no icons) + Right category view area (fixed ~720×460, zero vertical scrollbars).
  - [x] Implement standard bottom command bar: `[ Reset to Defaults ]`, `[ Open INI ]`, `[ Save ]`, `[ Cancel ]`.
- [x] Task: Implement Category Panels (No AI Slop)
  - [x] **General:** `Show file panel`, `Colorize files by extension`, `Sort files by extension`, `CAD flat shading`, `Light theme`.
  - [x] **Viewport:** 2-column clean grid (FOV, Fisheye FOV, Initial shift, Rotation angle, Wireframe scale, `Center camera on mass center`).
  - [x] **Colors:** 2/3-column compact grid (Diffuse, Specular, Emissive, Backside Diffuse defaulting to bright red `#FF0000`, Ambient, Directional, Ground). Swatch + Hex box (`#RRGGBB`). Modern compact spectrum popup.
  - [x] **G-Code:** Plain text note `Changes apply after reloading model` + Layer height, Line width, Line ends extension.
  - [x] **External Apps:** App list with Add/Remove/Auto-detect and editable Name, Path, Launch Arguments (`$file$`), Extensions.
  - [x] **File Associations:** Format checkboxes + editable Launch Arguments (`"%1"` with optional flags) + Apply button.
- [x] Task: Phase Verification & Checkpoint (Verify Settings dialog compiles, opens cleanly without scrollbars, binds settings correctly)

---

## Phase 4: Build Verification & Deployment
- [x] Task: Local Build & Git Commit
  - [x] Stage and commit all changes to `master`.
  - [x] Push to GitHub and trigger GitHub Actions CI build.
- [x] Task: Update Release Binary
  - [x] Verify GitHub Actions run completes successfully.
  - [x] Download release artifact and update `dist/SolidModelBrowser.exe`.
- [x] Task: Final Track Verification
