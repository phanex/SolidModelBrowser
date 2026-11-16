# Product Definition

## Vision
**SolidModelBrowser (Modernized Fork)** is an ultra-lightweight, standalone 3D model and G-code viewer/converter for Windows (Win32 / WPF). 
Originally developed by questfulcat, this modernized fork introduces:
1. Native support for SketchUp (`.skp`) files via the open-source `OpenSkp` engine.
2. A refreshed, modern, and ergonomic UI with pixel-perfect icons, distinct folder/file visual hierarchy in the file navigator, and calm dark/light color themes (Zinc/Slate palette).
3. Native Windows 11 integration (proper title bar, dark mode window chrome, and full Snap Layouts flyout support on the Maximize button via `WM_NCHITTEST` / `HTMAXBUTTON`).
4. Automated cloud CI/CD via GitHub Actions for seamless standalone binary builds (`.exe` artifact) without local build bloat.

## Target Audience
Engineers, 3D printing enthusiasts, and makers who need an instant, no-nonsense desktop viewer for 3D meshes (STL, 3MF, OBJ, PLY), SketchUp models (SKP), and toolpaths (G-code) that starts in milliseconds without heavy CAD suite overhead or store dependencies.
