# Specification: Modernize UI & SKP Support

## 1. Overview & Goals
SolidModelBrowser is an ultra-fast, lightweight 3D model viewer for Windows built with .NET Framework 4.8 and WPF.
This track delivers a comprehensive engineering facelift and format expansion:
1. **SketchUp (.skp) Support**: Pure-managed integration via `OpenSkp` (NuGet) extracting world-space triangulated geometry without SketchUp installation or native SDK dependencies.
2. **CAD-Grade Flat Shading by Default**: Unsmooth/flat polygon rendering by default on import to eliminate distorted/mangled shading on raw CAD/STL models, with optional smooth normals.
3. **Streamlined Engineering Toolbar & SplitButtons**:
   - Compact **Shading SplitButton** (38px): Click toggles Flat CAD Shaded $\leftrightarrow$ Wireframe; dropdown controls Shaded+Wireframe, Smooth Normals, and Backside color.
   - Compact **External App SplitButton** (38px, icon only): Context-filtered apps by file extension (e.g. OrcaSlicer, SketchUp) + dropdown separator with **Save Snapshot to PNG [F6]**.
   - Compact **Camera button** (30px): Quick Persp $\leftrightarrow$ Ortho toggle (`F8`), with Fish-Eye FOV and Center at Model in dropdown.
   - Rotations X / Y / Z compact colored block.
   - Rare actions (mesh inversion, settings) housed in an overflow `[ ⋯ ]` More menu.
4. **Opaque & Hidden-by-Default File Navigation Panel**:
   - Solid background (`#252528` dark / `#F0F0F0` light) with `Opacity = 1.0` (no dimming).
   - Hidden by default (`ShowFilePanel = false`) for seamless file association launching.
   - Instant toggle via `Tab` / `Ctrl+B` or toolbar button.
   - Sharp 1:1 pixel-perfect folder and format icons from `Assets/Icons/`.
5. **Interactive Height Slicer (Z-Cross-Section)**:
   - Kept right-aligned in viewport with modern circular Thumb styling.
6. **Optional Model Info HUD**:
   - Hidden by default, toggled via `I` key showing concise Bounding Box dimensions in mm ($X \times Y \times Z$), triangle count, and manifold status.
7. **Clean Titlebar**:
   - Bare file name (`autoramka_test.3mf`), Windows 11 Snap Layouts via `WM_NCHITTEST` on Maximize button.
8. **Automated CI/CD & Test Baseline (TDD)**:
   - Separate test project `SolidModelBrowser.Tests` running in GitHub Actions workflow.

---

## 2. Technical Contracts & Requirements

### 2.1 SKP Importer (`Imports/ImportSKP.cs`)
- Subclass `Imports/Import`.
- Uses `OpenSkp.SkpFile.BuildScene(filename)` returning `OpenSkp.Scene`.
- Iterates `scene.GlbPrimitives`:
  - `float[] Positions`: 3-tuple floats mapped to `Positions.Add(new Point3D(x, y, z))`.
  - `float[] Normals`: 3-tuple floats mapped to `Normals.Add(new Vector3D(nx, ny, nz))`.
  - `uint[] Indices`: Triangle vertex indices mapped with `baseIndex` offsets.
- Progress updates per primitive, error resilience with `ExceptionMessage`.

### 2.2 Shading Logic
- In `loadFile()`: default mesh post-processing uses `Utils.UnsmoothMesh(meshBase)` to guarantee clean flat facets unless smooth shading is explicitly requested.

### 2.3 External App SplitButton
- Configurable dictionary mapping extensions to executable candidates.
- Primary click launches current default application for extension.
- Dropdown menu lists alternative tools followed by `Separator` and `Save Image to PNG... (F6)`.

### 2.4 File Panel & State
- Default `ShowFilePanel = false`.
- Default `FilePanelIdleOpacity = 1.0`.
- Initial XAML layout set to `Width="0"` and `Visibility="Collapsed"` to prevent startup flicker.
- `SelectFile` coordinates selection without triggering duplicate file loads.

### 2.5 Tests (`SolidModelBrowser.Tests`)
- Target: `net48`, NUnit.
- Tests verify:
  1. `ImportSKP` parsing valid geometry, triangle count divisibility by 3, and failure handling on corrupted inputs.
  2. `Settings` defaults (`ShowFilePanel == false`, `FilePanelIdleOpacity == 1.0`).
  3. Single-load invariant on command-line argument dispatch.
