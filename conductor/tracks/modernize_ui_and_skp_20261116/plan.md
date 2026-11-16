# Implementation Plan: Modernize UI & SKP Support

## Phase 1: CI/CD Setup & Build Baseline
- [ ] Create `.github/workflows/build.yml` with MSBuild and NuGet restore.
- [ ] Verify clean build on GitHub Actions runner.
- [ ] Configure automatic artifact generation for Release binaries.

## Phase 2: SKP Format Support
- [ ] Add `OpenSkp` NuGet package to `SolidModelBrowser.csproj` / `packages.config`.
- [ ] Create `Imports/ImportSKP.cs` inheriting from `Imports/Import`.
- [ ] Implement geometry extraction (vertices, normals, triangles) into `Positions`, `Indices`, `Normals`.
- [ ] Test loading sample `.skp` files and verify proper viewport orientation and scaling.

## Phase 3: Modern UI & Icon Integration
- [ ] Add user's pixel-perfect icons from `Assets/Icons/` to `SolidModelBrowser.csproj` as WPF Resources.
- [ ] Refactor icon loading from legacy `TextEncodedImage.cs` to resource-based vectors/bitmaps.
- [ ] Update `Colors.xaml` with modern Dark Slate and Clean Light palettes.
- [ ] Update `FileNavigationPanel.xaml`:
  - Add icons for folders vs supported file types.
  - Implement badge styling for `.stl`, `.3mf`, `.skp`, `.obj`, `.ply`, `.gcode`.

## Phase 4: Native Windows 11 Chrome & Snap Layouts
- [ ] Implement `WM_NCHITTEST` interop in `MainWindow.xaml.cs`.
- [ ] Map Maximize button rectangle to `HTMAXBUTTON` (9) to trigger Windows 11 Snap Layouts flyout.
- [ ] Verify minimize, restore, maximize, and drag interactions on Windows 10 & 11.

## Phase 5: Verification & Packaging
- [ ] Trigger CI build and download artifact.
- [ ] Verify standalone execution without external dependencies.
- [ ] Update documentation and `README.md` with screenshots and features.
