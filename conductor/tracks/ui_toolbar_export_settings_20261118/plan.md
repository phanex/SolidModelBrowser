# Implementation Plan: UI Polish, Direct Binary Export & Settings Overhaul

## Phase 1: Toolbar 20px Icons & Windows Shell Drive Icons
- [ ] Task: Update Toolbar Button Dimensions & Icon Viewbox
  - [ ] In `MainWindow.xaml`, change `<Viewbox Width="16" Height="16">` to `<Viewbox Width="20" Height="20">` for all 11 toolbar buttons.
  - [ ] Adjust `ButtonExternalApp` and other SplitButtons to `Width="28"` so 20px icons fit with clean margins.
- [ ] Task: Restore Windows Shell System Drive Icons
  - [ ] In `SolidModelBrowser/Controls/FileNavigationPanel.xaml.cs`, restore `GetSystemIcon(drivePath)` for `drivesWrapPanel` items.
  - [ ] Ensure 16×16 system icon dimensions and runtime DPI normalization.
- [ ] Task: Phase Verification & Checkpoint (Verify toolbar scaling and drive icons)

---

## Phase 2: Direct Binary SaveFileDialog & Dead Code Cleanup
- [ ] Task: Simplify `export()` in `MainWindow.xaml.cs`
  - [ ] Eliminate `Utils.MessageWindow("Select export format", ...)` intermediate dialog and remove `this.Effect = new BlurEffect()`.
  - [ ] Launch `Microsoft.Win32.SaveFileDialog` directly with filter: `"STL (*.stl)|*.stl|Wavefront OBJ (*.obj)|*.obj|3D Manufacturing Format (*.3mf)|*.3mf|Polygon File Format (*.ply)|*.ply"`.
  - [ ] Set default `FilterIndex = 1` (STL).
  - [ ] Save strictly in Binary format for STL and PLY.
- [ ] Task: Purge Obsolete ASCII Exporters & Dead Code
  - [ ] Remove ASCII STL / PLY export routines and any unused dialog helper methods.
- [ ] Task: Phase Verification & Checkpoint (Verify export directly invokes SaveFileDialog without modal/blur)

---

## Phase 3: Settings Dialog Redesign (Two-Pane System Window)
- [ ] Task: Redesign `SettingsWindow.xaml` Layout
  - [ ] Set `WindowStyle="SingleBorderWindow"` (or native title bar with standard system close/minimize).
  - [ ] Remove parent window `BlurEffect` upon opening `SettingsWindow`.
  - [ ] Implement two-pane layout: Left plain-text category list (no icons) + Right category view area (fixed ~720×460, zero vertical scrollbars).
  - [ ] Implement standard bottom command bar: `[ Reset to Defaults ]`, `[ Open INI ]`, `[ Save ]`, `[ Cancel ]`.
- [ ] Task: Implement Category Panels (No AI Slop)
  - [ ] **General:** `Show file panel`, `Colorize files by extension`, `Sort files by extension`, `CAD flat shading`, `Light theme`.
  - [ ] **Viewport:** 2-column clean grid (FOV, Fisheye FOV, Initial shift, Rotation angle, Wireframe scale, `Center camera on mass center`).
  - [ ] **Colors:** 2/3-column compact grid (Diffuse, Specular, Emissive, Backside Diffuse defaulting to bright red `#FF0000`, Ambient, Directional, Ground). Swatch + Hex box (`#RRGGBB`). Modern compact spectrum popup.
  - [ ] **G-Code:** Plain text note `Changes apply after reloading model` + Layer height, Line width, Line ends extension.
  - [ ] **External Apps:** App list with Add/Remove/Auto-detect and editable Name, Path, Launch Arguments (`$file$`), Extensions.
  - [ ] **File Associations:** Format checkboxes + editable Launch Arguments (`"%1"` with optional flags) + Apply button.
- [ ] Task: Phase Verification & Checkpoint (Verify Settings dialog compiles, opens cleanly without scrollbars, binds settings correctly)

---

## Phase 4: Build Verification & Deployment
- [ ] Task: Local Build & Git Commit
  - [ ] Stage and commit all changes to `master`.
  - [ ] Push to GitHub and trigger GitHub Actions CI build.
- [ ] Task: Update Release Binary
  - [ ] Verify GitHub Actions run completes successfully.
  - [ ] Download release artifact and update `dist/SolidModelBrowser.exe`.
- [ ] Task: Final Track Verification
