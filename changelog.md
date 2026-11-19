# Solid Model Browser, change log

## v0.7.0 (19.11.2026)

- SketchUp format import added (`.skp` binary parser via OpenSkp by [Ahsan Mehmood](https://github.com/iamahsanmehmood/openskp))
- Modern floating HUD UI architecture: File Navigation Panel and Model Info Card redesigned as frosted semi-transparent floating cards with uniform 16px window margins and rounded corners
- Z-Height Slicer alignment: vertical slider and info box margins synchronized to uniform 16px geometry grid
- Modern Windows 11 DWM Color Picker Flyout with rounded corners, native shadow, alpha channel slider, spectrum wheel, mouse wheel adjustment, and direct HEX code input
- Settings Form overhaul: modern system visual styles, External Applications manager with auto-detect (Cura, PrusaSlicer, OrcaSlicer, etc.), and file type association manager
- Direct binary export via native `SaveFileDialog` (STL, Wavefront OBJ, 3MF, PLY) without modal blocking or background blur
- Shading mode toggle on toolbar with duotone icons: Solid, Solid + Wireframe, and Wireframe
- Multi-resolution file icons compiled with complete 10-frame sets (16, 20, 24, 28, 32, 40, 48, 64, 96, and 256px) for crisp rendering in Windows Explorer
- Model Z-slicing fix: slider at maximum height bypasses mesh slicing, rendering full model directly without top-layer clipping

## v0.6.1 (23.04.2025)

- Added settings to GCODE import (Layer height, Line width)
- Added setting UseSystemWindowStyle (for compatibility issues if OS can't draw correct frameless windows)

## v0.6 (19.02.2025)

- Export to STL, 3MF, OBJ, PLY formats added
- Settings menu: reset to defaults and open in text editor buttons added
- Rotation reworked (slower, but now it applied to model geometry data)
- Axes length setting added
- Ground settings added (size, color, checker size)
- Ambient light and directional lights settings added

## v0.5 (28.01.2025)

- Settings Menu added (instead of opening settings.ini file)
- GCODE format import added (Marlin linear moves compatible, experimental)
- Model Z-axis slicing (with slider)
- Camera positioning at geometric/points_avg center buttons
- Camera default positioning DefaultLookAtModelPointsAvgCenter setting added
- Sort files by extensions setting added
- Sort folders and files for all supported filesystems (fixed)
- Open folder in filepanel and select file from command line argument (fixed)

## v0.4 (23.12.2024)

- PLY format import added (ASCII and Binary little-endian)
- Wireframe mode implemented
- WireframeEdgeScale setting added
- IgnoreOriginalNormals setting added, to skip normals if there are a lot files with broken or incorrect normals in user library
- UnsmoothAfterLoading setting added, to load all models in flat mode

## v0.3.1 (08.12.2024)

- 3MF import compatibility improvements
- Different colors for files of different types in file panel 

## v0.3 (25.10.2024)

- 3MF with components import added (extended 3MF support)
- Tool to reset meshes smooth structure to flat (button on tool panel)
- Camera front, back, top, bottom, left, right view positions added (hotkeys)
- Camera initial shift from model setting added
- Model rotation angle 45/90 deg setting added
- Ground plane added (tool panel)
- [F1] help page added
- 3MF import, no transform matrix for object bugfix
- interface updates

## v0.2 (21.10.2024)

- 3MF import class was remastered
- model file name may be passed like command line argument to open with app
- window max size calculation method was changed
- window max size can be manually selected in settings (to solve problems with non-primary displays)
- header double click expands window
- camera FOV and FishEyeFOV angles settings were added
- interface updates

## v0.1 initial release (17.10.2024)
