# Implementation Plan: Settings Dialog Polish & Multi-Resolution Icons

## Phase 1: Mesh Loading & Slicing Fixes
- [x] Task 1.1: Fix invocation order in `MainWindow.xaml.cs` (`initSlicer()` before `updateMeshes()`).
- [x] Task 1.2: In `slice()`, bypass `Utils.SliceMesh()` when slider is at maximum, filling the full mesh directly.
- [x] Task 1.3: Suppress modal alert on missing `settings.ini` and update startup credits.

## Phase 2: Multi-Resolution Icon Compilation
- [x] Task 2.1: Pack 9-frame (16..256px) ICO files for `app.ico` and format icons (`stl`, `3mf`, `obj`, `skp`, `ply`, `gcode`).
- [x] Task 2.2: Embed multi-resolution `.ico` files as assembly resources in `SolidModelBrowser.csproj`.
- [x] Task 2.3: Update `FileAssociationManager.cs` to extract embedded multi-resolution `.ico` files directly to LocalAppData.

## Phase 3: Native Settings Dialog & Flyout Color Picker
- [x] Task 3.1: Convert `FigmaColorPickerForm` to `FigmaColorPickerControl` and `FigmaColorPickerFlyout : ToolStripDropDown`.
- [x] Task 3.2: Rebuild `SettingsForm` with standard Win32 `TabControl`, search filter, swatch + Hex TextBox rows, and clean External Apps layout.

## Phase 4: CI Verification & Local Deployment
- [ ] Task 4.1: Commit all changes and push to `origin master`.
- [ ] Task 4.2: Verify GitHub Actions CI build and unit tests pass.
- [ ] Task 4.3: Download and update `dist/SolidModelBrowser.exe` and verify associations.
