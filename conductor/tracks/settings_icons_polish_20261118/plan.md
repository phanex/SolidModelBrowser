# Implementation Plan: Settings Dialog Polish & Multi-Resolution Icons

## Phase 1: Mesh Loading & Slicing Fixes
- [x] Task 1.1: Fix invocation order in `MainWindow.xaml.cs` (`initSlicer()` before `updateMeshes()`).
- [x] Task 1.2: In `slice()`, bypass `Utils.SliceMesh()` when slider is at maximum, filling the full mesh directly.
- [x] Task 1.3: Suppress modal alert on missing `settings.ini` and update startup credits.

## Phase 2: Multi-Resolution Icon Compilation (REDO — previous implementation was broken)
- [ ] Task 2.1: Regenerate all `.ico` files (`app.ico`, `icon3a.ico`, `stl.ico`, `3mf.ico`, `obj.ico`, `skp.ico`, `ply.ico`, `gcode.ico`) from the original PNGs in the user's zip archive using **proper Win32 DIB format**: sizes 16–96 encoded as `BITMAPINFOHEADER` + 32bpp BGRA bottom-up pixel data + 1-bit AND mask (alpha > 50% → opaque, else transparent); size 256 encoded as embedded PNG. The previous implementation incorrectly embedded raw PNG streams for all sizes, which Windows Explorer silently rejects for sizes < 256, falling back to a blurry downscale of the 256px frame.
- [ ] Task 2.2: Verify each generated `.ico` loads correctly via `System.Drawing.Icon` and renders the exact pixel grid of the original PNG at every size (16, 20, 24, 32, 40, 48, 64, 96, 256).
- [ ] Task 2.3: Rebuild, deploy, re-register file associations, and visually confirm crisp icons in Windows Explorer at all view sizes (Small, Medium, Large, Extra Large).

## Phase 3: Settings Dialog Visual Styles & Layout Fix
- [ ] Task 3.1: Call `Application.EnableVisualStyles()` and `Application.SetCompatibleTextRenderingDefault(false)` before any WinForms form is created. Since this is a WPF app that hosts WinForms dialogs, the call must happen early (e.g. in `App` static constructor or `OnStartup`). This fixes the Windows 95 look (beveled buttons, old checkboxes, double-line GroupBox borders).
- [ ] Task 3.2: Fix `DockStyle` Z-order in `SettingsForm.initLayout()` so the `TabControl` (DockStyle.Fill) does not cover the bottom button panel. Correct order: add `pnlBottom` first, then `pnlTop`, then `tabControl` last — WinForms Fill docking respects reverse add-order.
- [ ] Task 3.3: Review and fix control overlaps across all tabs (especially Colors and External Apps). Ensure no label text clips into adjacent controls.

## Phase 4: Replace Custom Color Picker with System ColorDialog
- [ ] Task 4.1: In `SettingsForm`, change swatch button click handler to open `System.Windows.Forms.ColorDialog` instead of `FigmaColorPickerFlyout`. On OK, update the swatch BackColor, hex TextBox, and fire the color-changed callback. This is 5 lines of code, not 400.
- [ ] Task 4.2: Delete `FigmaColorPickerControl`, `FigmaColorPickerFlyout`, and `FigmaColorPickerForm` from `Controls/FigmaColorPickerForm.cs`. Remove the file from the project if it becomes empty. No custom-drawn color picker with non-native borders, alpha sliders, and checkerboard backgrounds should remain.

## Phase 5: CI Build & Final Verification
- [ ] Task 5.1: Commit, push, verify CI build passes.
- [ ] Task 5.2: Download release binary, delete old `settings.ini`, launch and verify: icons crisp in Explorer, settings dialog renders with modern visual styles, `ColorDialog` opens on swatch click, no layout overlaps.
