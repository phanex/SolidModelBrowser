# Implementation Plan: UI & Settings Overhaul

## Phase 1: Menu Items & Toolbar Icons Polish
- [ ] Task: Standardize Menu Syntax & Shortcuts
  - [ ] Update `MainWindow.xaml` ContextMenus (`MenuMore`, `MenuShading`, `MenuExternalApps`, `MenuCenterCamera`).
  - [ ] Convert manual text shortcuts like `[C]`, `[M]`, `[1]`, `[O]` to native `InputGestureText`.
  - [ ] Rename `Open Settings...` to `Settings...` and ensure standard Windows ellipsis conventions.
- [ ] Task: Redesign Camera Projection & Shading Toolbar Icons
  - [ ] Implement crisp camera contour vectors for `PathCameraPerspective` (camera body/lens).
  - [ ] Implement parallel grid/projection icon for `Orthographic`.
  - [ ] Implement lens/aperture icon for `Fisheye`.
  - [ ] Update `ButtonWireframe` cube states: filled solid vs edge-highlighted vs wireframe.
  - [ ] Apply `SnapsToDevicePixels="True"` and `UseLayoutRounding="True"` across all toolbar buttons.
- [ ] Task: Phase Verification & Checkpoint (Verify icon contrast & menu layout)

---

## Phase 2: Model Info HUD & File Navigation Polish
- [ ] Task: HUD Card Layout & Units Refactor
  - [ ] Refactor `infoContainer` to use a structured `Grid` / `StackPanel` with defined line heights and muted label styles.
  - [ ] Format file size into readable units: bytes `< 1024` $\to$ B, `< 1 MB` $\to$ KB, `≥ 1 MB` $\to$ MB with one decimal place.
  - [ ] Group metrics logically: File Header $\to$ Bounding Dimensions $\to$ Volume/Weight $\to$ Geometry Stats.
- [ ] Task: File Browser Iconography Harmony
  - [ ] Harmonize drive icons, folder icons, and file type badges with consistent stroke weights and margins.
- [ ] Task: Phase Verification & Checkpoint

---

## Phase 3: External Applications & File Associations Architecture
- [ ] Task: Multi-App Configuration in Settings
  - [ ] Extend `Settings.cs` to support a collection of `ExternalAppEntry` (Name, Path, Arguments, Extensions).
  - [ ] Implement INI serialization/deserialization for multiple external apps with backwards compatibility for legacy `ExternalApp`.
  - [ ] Add auto-detection logic for standard slicer installation paths (Bambu Studio, OrcaSlicer, PrusaSlicer, Cura).
- [ ] Task: Windows File Associations Manager
  - [ ] Create `FileAssociationManager.cs` using `Microsoft.Win32.Registry.CurrentUser` (`HKCU\Software\Classes`).
  - [ ] Implement registration and unregistration for `.stl`, `.3mf`, `.obj`, `.skp`, `.ply`, `.gcode`.
  - [ ] Implement check for current association status.
- [ ] Task: Unit Tests for Settings & File Associations
  - [ ] Write unit tests in `SolidModelBrowser.Tests` for `ExternalAppEntry` serialization and filtering.
- [ ] Task: Phase Verification & Checkpoint

---

## Phase 4: Settings Dialog Redesign
- [ ] Task: Settings Window Layout & Left Sidebar
  - [ ] Refactor `SettingsWindow.xaml` into a two-pane layout (Left Category Sidebar + Right Content Area).
  - [ ] Implement standard bottom button bar (`Reset to Defaults`, `Open INI`, `Save & Close`, `Cancel`).
  - [ ] Remove obsolete title-bar glyphs (`DEF`, editor icon).
- [ ] Task: Category Views Implementation
  - [ ] Page 1: General & Interface (Theme, sorting, panel behavior).
  - [ ] Page 2: Viewport & Camera (FOV, angles, ground grid, axes).
  - [ ] Page 3: Materials & Lighting (Clean color swatches without redundant labels, lighting toggles).
  - [ ] Page 4: G-Code Import (Single note "Reload model to apply changes", parameters).
  - [ ] Page 5: External Apps Manager (Add/Remove apps, file dialog selector, extension checkboxes).
  - [ ] Page 6: File Associations (Checkboxes for supported formats).
- [ ] Task: Streamlined Color Picker Component
  - [ ] Replace cluttered `ColorInput.xaml` with direct swatch click and clean `#RRGGBB` input.
- [ ] Task: Phase Verification & Checkpoint

---

## Phase 5: Verification & Remote Build
- [ ] Task: Code Review & Local Working Tree Cleanliness
  - [ ] Verify clean git diff and adherence to C# standards.
- [ ] Task: Remote CI Compilation
  - [ ] Trigger GitHub Actions build when requested by user.
  - [ ] Verify test suite passes and download release artifact to `dist\SolidModelBrowser.exe`.
