# Specification: UI & Settings Overhaul

## 1. Overview
This track delivers a comprehensive overhaul of SolidModelBrowser's settings experience and UI polish:
1. Complete redesign of the **Settings Dialog** with a category navigation sidebar, eliminating the endless vertical scroll, tiny title-bar buttons, and cluttered property panels.
2. Multi-application manager for **External Apps** (Bambu, OrcaSlicer, Prusa, Blender, FreeCAD, etc.) with per-extension filtering and dynamic display in the toolbar dropdown.
3. Windows **File Associations** manager in Settings (registering `.stl`, `.3mf`, `.obj`, `.skp`, `.ply`, `.gcode` via `HKCU` without admin prompt).
4. Streamlined, modern **Color Picker** without redundant palette buttons or duplicated "Hex: #" labels.
5. Pixel-perfect toolbar icons with distinct visual identities for **Shading Modes** vs **Camera Projections**.
6. Standard Windows menu conventions (proper `InputGestureText`, standard `Settings...` naming, no bracket slop).
7. Unified file browser iconography and structured **Model Info HUD** with clean multi-column metric formatting.

---

## 2. Functional Requirements

### 2.1 Settings Window Overhaul
- **Layout:** Standard two-pane window:
  - Left pane: Clean navigation list for categories:
    - *General & Interface* (window style, theme, file panel behavior, sorting)
    - *Viewport & Camera* (FOV, fisheye, rotation step, ground grid, axes)
    - *Materials & Lighting* (diffuse, specular, emissive colors, ambient/directional lights)
    - *G-Code Import* (single clean note: "Reload model to apply changes", line width, layer height, line extension)
    - *External Apps* (list of configured tools, add/remove, executable path, supported extensions)
    - *File Associations* (checkboxes to associate `.stl`, `.3mf`, `.obj`, `.skp`, `.ply`, `.gcode` in `HKCU`)
  - Bottom command bar: Standard Windows dialog buttons:
    - `Reset to Defaults`
    - `Open INI`
    - `Save & Close`
    - `Cancel`
- **Backward Compatibility:** Must run seamlessly on Windows 10 and Windows 11 without requiring WinUI 3 or external runtime dependencies.

### 2.2 External Applications Manager
- Support multiple external applications in `settings.ini` (e.g. `App1_Name`, `App1_Path`, `App1_Exts`).
- Auto-detect common slicers if installed at standard locations (Bambu Studio, OrcaSlicer, PrusaSlicer, Cura).
- Allow user to add custom apps via standard file dialog and check which extensions apply (`stl`, `3mf`, `obj`, `skp`, `gcode`).
- On the main toolbar: The `Open with...` split button dropdown dynamically displays only applications supporting the current file's extension.

### 2.3 Windows File Associations (`HKCU`)
- Add user-level registration under `HKEY_CURRENT_USER\Software\Classes`:
  - Registers programmatic identifiers (`SolidModelBrowser.<ext>`).
  - Sets application executable command `"<exePath>" "%1"`.
  - Sets file type icon.
- No UAC / Administrator prompt required.

### 2.4 Color Picker Streamlining
- Click directly on the color swatch to open the color selection tool.
- Remove redundant palette button.
- Hex input box labeled simply `#` with 6-digit hex code (`RRGGBB`).
- Clean RGBA / Hue controls.

### 2.5 Pixel-Perfect Toolbar & Icon Distinction
- **Shading Mode Icon:** Isometric cube:
  - *Solid:* Solid filled faces.
  - *Solid + Wireframe:* Solid faces with dark/light edge contours.
  - *Wireframe:* Pure edge strokes without face fills.
- **Camera Projection Icon:** Clearly distinct from cubes:
  - *Perspective:* Camcorder / classic camera contour with perspective rays.
  - *Orthographic:* Isometric grid / parallel projection symbol.
  - *Fisheye:* Convex lens / wide-angle aperture.
- **Snapping:** Ensure `SnapsToDevicePixels="True"`, `UseLayoutRounding="True"`, and integer vector coordinates to avoid blurriness.

### 2.6 Menu Item & Shortcut Conventions
- Remove arbitrary bracketed shortcuts like `[C]` or `[W]` from menu item headers.
- Use WPF `InputGestureText="C"` so shortcuts right-align natively in accordance with Windows guidelines.
- Rename action items opening dialogs to standard Windows syntax: `Settings...`, `Open with...`, `Export...` (drop unnecessary verbs like `Open Settings...`).

### 2.7 Model Info HUD Card Polish
- Replace single text block info-dump with structured grid / card:
  - Header: Filename, Extension badge, File size in readable units (`6.4 MB` instead of `6551 KB`).
  - Row 1: Dimensions ($X \times Y \times Z\text{ mm}$).
  - Row 2: Volume ($\text{cm}^3$) & Surface Area ($\text{cm}^2$).
  - Row 3: PLA Weight Estimation ($\sim X\text{ g}$ @ 15% infill) & Solid weight.
  - Row 4 (for G-code): Print Time & Filament usage.
  - Footer: Triangles & Vertices.
- Generous line height and clear visual hierarchy.

---

## 3. Non-Functional Requirements
- **Framework:** .NET Framework 4.8.
- **Packaging:** Single standalone executable without external DLL installer requirements.
- **Build Target:** Clean compilation via GitHub Actions CI workflow.

---

## 4. Acceptance Criteria
1. Settings dialog opens with left category navigation and clear bottom buttons; no controls are cut off or jammed into title bar.
2. Multiple external apps can be configured; only relevant apps show in the `Open with...` dropdown for the loaded file.
3. User can associate file formats in HKCU with one click.
4. Toolbar icons for Camera Projection and Shading are immediately distinguishable at normal resolution.
5. All menu shortcuts use right-aligned `InputGestureText`.
6. Model Info HUD renders clean numbers, readable units (MB/KB), and structured metrics.
