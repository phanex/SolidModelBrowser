# Specification: Settings Dialog Polish & Multi-Resolution Icons

## Overview
This track addresses several UI/UX issues in SolidModelBrowser:
1. Slicer height bug: newly opened models had their top vertices or wireframe tubes cut off due to obsolete slicing coordinates and premature `slice()` calls on load.
2. Settings layout: replace docked ListBox with native Win32 `TabControl`, add search filter bar, and fix overlapping controls in External Apps.
3. Colors UX: remove developer label text, replace bulky buttons with compact color swatches and adjacent editable Hex TextBoxes, and replace modal dialog with a non-modal popup flyout.
4. Multi-resolution Windows Explorer icons: pack authentic 16, 20, 24, 32, 40, 48, 64, 96, 256 pixel-art layers into `.ico` files for both the application and all associated 3D formats (`stl`, `3mf`, `obj`, `skp`, `ply`, `gcode`).
5. Silent initialization: suppress startup modal message when `settings.ini` is created, and credit project authors and icon designer in startup HUD.

## Functional Requirements
- **FR-1:** Slicing is bypassed when the height slider is at 100% (maximum), displaying pristine complete model geometry on load.
- **FR-2:** `initSlicer()` is invoked before `updateMeshes()` on new file load, ensuring valid Z bounds.
- **FR-3:** Settings dialog uses native `TabControl` with 6 standard tabs (`General`, `Viewport`, `Colors`, `G-Code`, `External Apps`, `File Associations`).
- **FR-4:** Color settings display a 24x22 swatch and an adjacent `#RRGGBB` TextBox for direct manual input or copy-paste.
- **FR-5:** Clicking a color swatch opens a non-modal `ToolStripDropDown` flyout directly below the swatch that closes on click-outside or Esc.
- **FR-6:** External Apps tab layout uses proper spacing across the entire dialog width without overlapping controls.
- **FR-7:** Multi-resolution `.ico` files contain 16, 20, 24, 32, 40, 48, 64, 96, 256 frames, eliminating blur in Explorer Large / Extra Large icon views.
- **FR-8:** `settings.ini` is created silently on missing file without modal error dialogs.
- **FR-9:** Startup HUD credits `questfulcat (C) 2025`, contributor `desmond`, and icon pack `Coolicons` by Vlad Shagov.

## Acceptance Criteria
- [x] Loading models of varying heights opens them at 100% full height without clipped faces or wireframe edges.
- [x] Settings dialog tabs display cleanly with native margins and zero overlapping controls.
- [x] Color hex codes can be typed/pasted directly into text boxes or picked via flyout.
- [x] Windows Explorer displays sharp pixel-perfect icons for `.stl`, `.skp`, `.3mf`, `.obj`, `.ply`, `.gcode` in Small, Medium, Large, and Extra Large views.
- [x] App starts silently without warnings when `settings.ini` is deleted.
