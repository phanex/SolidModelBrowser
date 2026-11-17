# Implementation Plan: UI & Settings Overhaul

## Phase 1: Menu Items & Toolbar Icons Polish
- [x] Task: Standardize Menu Syntax & Shortcuts
  - [x] Update `MainWindow.xaml` ContextMenus (`MenuMore`, `MenuShading`, `MenuExternalApps`, `MenuCenterCamera`).
  - [x] Convert manual text shortcuts like `[C]`, `[M]`, `[1]`, `[O]` to native `InputGestureText`.
  - [x] Rename `Open Settings...` to `Settings...` and ensure standard Windows ellipsis conventions.
- [x] Task: Redesign Camera Projection & Shading Toolbar Icons
  - [x] Implement crisp camera contour vectors for `PathCameraPerspective` (camera body/lens).
  - [x] Implement parallel grid/projection icon for `Orthographic`.
  - [x] Implement lens/aperture icon for `Fisheye`.
  - [x] Update `ButtonWireframe` cube states: filled solid vs edge-highlighted vs wireframe.
  - [x] Apply `SnapsToDevicePixels="True"` and `UseLayoutRounding="True"` across all toolbar buttons.
- [x] Task: Phase Verification & Checkpoint (Verify icon contrast & menu layout)

---

## Phase 2: Model Info HUD & File Navigation Polish
- [x] Task: HUD Card Layout & Units Refactor
  - [x] Refactor `infoContainer` to use a structured `Grid` / `StackPanel` with defined line heights and muted label styles.
  - [x] Format file size into readable units: bytes `< 1024` $\to$ B, `< 1 MB` $\to$ KB, `≥ 1 MB` $\to$ MB with one decimal place.
  - [x] Group metrics logically: File Header $\to$ Bounding Dimensions $\to$ Volume/Weight $\to$ Geometry Stats.
- [x] Task: File Browser Iconography Harmony
  - [x] Harmonize drive icons, folder icons, and file type badges with consistent stroke weights and margins.
- [x] Task: Phase Verification & Checkpoint

---

## Phase 3: External Applications & File Associations Architecture
- [x] Task: Multi-App Configuration in Settings
  - [x] Extend `Settings.cs` to support a collection of `ExternalAppEntry` (Name, Path, Arguments, Extensions).
  - [x] Implement INI serialization/deserialization for multiple external apps with backwards compatibility for legacy `ExternalApp`.
  - [x] Add auto-detection logic for standard slicer installation paths (Bambu Studio, OrcaSlicer, PrusaSlicer, Cura).
- [x] Task: Windows File Associations Manager
  - [x] Create `FileAssociationManager.cs` using `Microsoft.Win32.Registry.CurrentUser` (`HKCU\Software\Classes`).
  - [x] Implement registration and unregistration for `.stl`, `.3mf`, `.obj`, `.skp`, `.ply`, `.gcode`.
  - [x] Implement check for current association status.
- [x] Task: Unit Tests for Settings & File Associations
  - [x] Write unit tests in `SolidModelBrowser.Tests` for `ExternalAppEntry` serialization and filtering.
- [x] Task: Phase Verification & Checkpoint

---

## Phase 4: Settings Dialog Redesign
- [x] Task: Settings Window Layout & Left Sidebar
  - [x] Refactor `SettingsWindow.xaml` into a two-pane layout (Left Category Sidebar + Right Content Area).
  - [x] Implement standard bottom button bar (`Reset to Defaults`, `Open INI`, `Save & Close`, `Cancel`).
  - [x] Remove obsolete title-bar glyphs (`DEF`, editor icon).
- [x] Task: Category Views Implementation
  - [x] Page 1: General & Interface (Theme, sorting, panel behavior).
  - [x] Page 2: Viewport & Camera (FOV, angles, ground grid, axes).
  - [x] Page 3: Materials & Lighting (Clean color swatches without redundant labels, lighting toggles).
  - [x] Page 4: G-Code Import (Single note "Reload model to apply changes", parameters).
  - [x] Page 5: External Apps Manager (Add/Remove apps, file dialog selector, extension checkboxes).
  - [x] Page 6: File Associations (Checkboxes for supported formats).
- [x] Task: Streamlined Color Picker Component
  - [x] Replace cluttered `ColorInput.xaml` with direct swatch click and clean `#RRGGBB` input.
- [x] Task: Phase Verification & Checkpoint

---

## Phase 5: Verification & Remote Build
- [x] Task: Code Review & Local Working Tree Cleanliness
  - [x] Verify clean git diff and adherence to C# standards.
- [x] Task: Remote CI Compilation
  - [x] Trigger GitHub Actions build when requested by user.
  - [x] Verify test suite passes and download release artifact to `dist\SolidModelBrowser.exe`.
