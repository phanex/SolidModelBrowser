# Specification: Modernize UI & SKP Support

## 1. Overview & Goals
SolidModelBrowser is an ultra-fast, lightweight 3D model viewer for Windows built with .NET Framework 4.8 and WPF.
This track accomplishes four major enhancements:
1. **SketchUp (.skp) Support**: Integrate pure-managed `.skp` reading via `OpenSkp` (NuGet) without requiring SketchUp or heavyweight native CAD SDKs.
2. **Icon & Visual Facelift**: Replace legacy 1-bit ASCII string icons (`TextEncodedImage.cs`) with pixel-perfect modern vector/raster icons (16px to 96px/256px) created for file formats (`STL`, `3MF`, `SKP`, `OBJ`, `PLY`, `GCODE`) and application chrome.
3. **Themes & Navigation Improvements**: Update `Colors.xaml` with modern dark slate and crisp light palettes; enhance `FileNavigationPanel` so folders and files are instantly distinguishable with format badges.
4. **Windows 11 Window Chrome Fix**: Restore native Windows 11 Snap Layouts hover menu on the maximize button while keeping the custom minimalist titlebar.
5. **Zero-Install Cloud Build**: Configure GitHub Actions workflow to build release-ready standalone binaries on GitHub runners.

---

## 2. Detailed Requirements

### 2.1 SKP Importer (`Imports/ImportSKP.cs`)
- Subclass `Imports/Import` base class.
- The base class automatically discovers importers via reflection:
  ```csharp
  var types = System.Reflection.Assembly.GetExecutingAssembly().GetTypes()
      .Where(t => t.IsSubclassOf(typeof(Import)));
  ```
- File extensions: `.skp`.
- Extraction of triangle meshes (vertices, normals, indices) and population of `Positions`, `Indices`, and `Normals`.
- Managed package: `OpenSkp` via NuGet.

### 2.2 Visual Assets & Pixel-Perfect Icons
- Pixel-perfect icons stored in `Assets/Icons/`:
  - `App` (16, 20, 24, 32, 40, 48, 64, 96 px)
  - `STL` (16, 20, 24, 32, 40, 48, 64, 96 px)
  - `3MF` (16, 20, 24, 32, 40, 48, 64, 96 px)
  - `SKP` (16, 20, 24, 32, 40, 48, 64, 96 px)
  - `OBJ` (16, 20, 24, 32, 40, 48, 64, 96 px)
  - `PLY` (16, 20, 24, 32, 40, 48, 64, 96 px)
  - `Gcode` (16, 20, 24, 32, 40, 48, 64, 96 px)
  - High-res app / file type branding (256 px).
- Replace hardcoded 1-bit ASCII strings in `TextEncodedImage.cs` with clean WPF `BitmapImage` or `DrawingImage` resources.

### 2.3 UI Themes & File Tree (`FileNavigationPanel.xaml`)
- Update `Colors.xaml`:
  - **Dark Mode**: Neutral slate tones (`#18181B` base, `#27272A` panels, `#3F3F46` borders, `#3B82F6` accents).
  - **Light Mode**: Clean minimalist neutrals (`#F8FAFC` base, `#FFFFFF` panels, `#E2E8F0` borders).
- Enhance TreeView / ListView in `FileNavigationPanel.xaml`:
  - Differentiate folder nodes (folder icon) from 3D model files (format-specific badge/icon).
  - Clean font hierarchy, subtle selection highlights.

### 2.4 Windows 11 Snap Layouts Support
- The app uses `WindowStyle="None"` with custom window controls.
- To allow Windows 11 Snap Layouts to open when hovering over the Maximize button:
  - Hook `HwndSource.AddHook` to handle `WM_NCHITTEST` (`0x0084`).
  - When the cursor coordinate falls within the Maximize button bounds, return `HTMAXBUTTON` (`9`).
  - Handle `WM_NCLBUTTONDOWN` / `WM_NCLBUTTONUP` or coordinate state transitions properly.

### 2.5 Automated CI/CD (`.github/workflows/build.yml`)
- Trigger on `push` and `workflow_dispatch`.
- Runner: `windows-latest`.
- Steps:
  1. Checkout repository.
  2. Setup MSBuild & NuGet.
  3. Restore NuGet packages.
  4. Build `SolidModelBrowser.sln` in Release mode.
  5. Upload compiled executable artifact (`SolidModelBrowser.exe`).
