# Specification: Settings Dialog Polish & Multi-Resolution Icons

## Overview
This track addresses several UI/UX issues in SolidModelBrowser:
1. Slicer height bug: newly opened models had their top vertices or wireframe tubes cut off due to obsolete slicing coordinates and premature `slice()` calls on load.
2. Settings layout: replace docked ListBox with native Win32 `TabControl`, add search filter bar, and fix overlapping controls in External Apps.
3. Colors UX: remove developer label text, replace bulky buttons with compact color swatches and adjacent editable Hex TextBoxes, and use system `ColorDialog` for picking (not a custom-drawn monstrosity).
4. Multi-resolution Windows Explorer icons: pack authentic 16, 20, 24, 32, 40, 48, 64, 96, 256 pixel-art layers into `.ico` files using **proper Win32 DIB format** (BITMAPINFOHEADER + 32bpp BGRA + 1-bit AND mask) for sizes 16–96, PNG only for the 256px frame.
5. Silent initialization: suppress startup modal message when `settings.ini` is created, and credit project authors and icon designer in startup HUD.

## Functional Requirements
- **FR-1:** Slicing is bypassed when the height slider is at 100% (maximum), displaying pristine complete model geometry on load.
- **FR-2:** `initSlicer()` is invoked before `updateMeshes()` on new file load, ensuring valid Z bounds.
- **FR-3:** Settings dialog uses native `TabControl` with 6 standard tabs (`General`, `Viewport`, `Colors`, `G-Code`, `External Apps`, `File Associations`), rendered with **modern visual styles** (not Windows 95 ComCtl v5).
- **FR-4:** Color settings display a 24×22 swatch and an adjacent `#RRGGBB` TextBox for direct manual input or copy-paste.
- **FR-5:** Clicking a color swatch opens the **standard Windows `ColorDialog`** — no custom-drawn picker with non-native borders, alpha sliders, or jarring visual style.
- **FR-6:** External Apps tab layout uses proper spacing across the entire dialog width without overlapping controls.
- **FR-7:** Multi-resolution `.ico` files contain 16, 20, 24, 32, 40, 48, 64, 96, 256 frames. Sizes 16–96 use **Win32 DIB format** (BITMAPINFOHEADER + BGRA pixel data + 1-bit AND mask); 256 uses embedded PNG. This ensures Windows Explorer picks the correct pixel-perfect frame at every view size.
- **FR-8:** `settings.ini` is created silently on missing file without modal error dialogs.
- **FR-9:** Startup HUD credits `questfulcat (C) 2025`, contributor `desmond`, and icon pack `Coolicons` by Vlad Shagov.
- **FR-10:** `Application.EnableVisualStyles()` must be called before any WinForms dialog is shown, so controls render with the modern Windows theme (rounded buttons, flat checkboxes, proper GroupBox lines).

## Acceptance Criteria
- [x] Loading models of varying heights opens them at 100% full height without clipped faces or wireframe edges.
- [x] Settings dialog controls render with **modern Windows visual styles** (not 3D beveled Windows 95 look).
- [x] Settings dialog TabControl does not overlap or obscure the bottom button bar.
- [x] Color hex codes can be typed/pasted directly into text boxes; clicking swatch opens **standard `ColorDialog`**.
- [x] No custom-drawn `FigmaColorPickerControl` / `FigmaColorPickerFlyout` / `FigmaColorPickerForm` remains in the codebase.
- [x] Colors tab: labels, swatches, and text boxes are laid out cleanly without any overlap or clipping.
- [x] Windows Explorer displays **sharp pixel-perfect icons** for `.stl`, `.skp`, `.3mf`, `.obj`, `.ply`, `.gcode` in Small, Medium, Large, and Extra Large views — verified visually.
- [x] App starts silently without warnings when `settings.ini` is deleted.
- [x] Startup HUD shows correct credits.
