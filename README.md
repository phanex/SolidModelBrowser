# Solid Model Browser

A lightweight, portable 3D model viewer and format converter for Windows (STL, 3MF, OBJ, PLY, SKP, G-Code).
Browse 3D models and g-code files effortlessly with smooth navigation, floating HUD panels, and instant visual inspection.

![cover01](https://github.com/user-attachments/assets/c996f67c-7100-4a8b-92c1-af75fd2d44bb)

## Requirements

.NET Framework 4.8, WPF (Windows 7 ... Windows 11)

## Installation

No installation required — single portable executable. Settings file (`settings.ini`) is created automatically on first startup in the app directory or `%LocalAppData%`.

## Features

- **Supported 3D Formats:**
  - **STL** (Binary and ASCII)
  - **3MF** (3D Manufacturing Format, full mesh and components)
  - **OBJ** (Wavefront OBJ with triangulated faces)
  - **PLY** (Polygon File Format, ASCII and binary little-endian)
  - **SKP** (SketchUp binary models via OpenSkp parser)
  - **G-Code** (Marlin / linear movement toolpaths visualization)

- **Direct Export:**
  - Fast binary export via native `SaveFileDialog` (STL, Wavefront OBJ, 3MF, PLY).

- **Modern Floating HUD Interface:**
  - Frosted semi-transparent floating cards for File Navigation and Model Metadata.
  - Symmetrical 16px window margins and unified rounded corners.
  - Z-Height Slicer with circular knob for layer-by-layer inspection (bypassed at 100% for full mesh rendering).

- **Shading Modes & Viewport:**
  - Instant toggle between **Solid**, **Solid + Wireframe**, and **Wireframe** shading.
  - Flat/CAD shading toggle (`Ctrl+F`).
  - Perspective and Orthographic camera projection modes, plus Fish-Eye FOV model.
  - Ground plane and coordinate XYZ axes.

- **Lighting & Color Customization:**
  - Windows 11 DWM Color Picker with alpha slider, mouse wheel adjustments, and direct HEX input.
  - Configurable Diffuse, Specular, Emissive, Ambient, Directional lights, and Backside coloring.
  - Dark and Light application themes.

- **CLI & Automation:**
  - Open model directly: `SolidModelBrowser.exe "model.stl"`
  - Render off-screen snapshot to PNG: `SolidModelBrowser.exe "model.stl" --screenshot "preview.png" [--sidebar]`

## Credits & Acknowledgments

- **Original Author:** Grigoriy E. (`questfulcat`), 2025 (C)
- **Enhanced by:** Desmond
- **SketchUp (.skp) Parser:** OpenSkp by [Ahsan Mehmood](https://github.com/ahsan-mehmood)
- **Icons:** Coolicons by Vlad Shagov
- **License:** Distributed under the MIT License
