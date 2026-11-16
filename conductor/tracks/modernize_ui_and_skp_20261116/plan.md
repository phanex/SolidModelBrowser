# Implementation Plan: Modernize UI & SKP Support

## Phase 1: CI/CD & TDD Test Project Setup
- [x] Create `.github/workflows/build.yml` with MSBuild and NuGet restore.
- [x] Create `SolidModelBrowser.Tests/SolidModelBrowser.Tests.csproj` (NUnit / net48) and link to `SolidModelBrowser.sln`.
- [x] Add unit tests for `Settings` defaults (opaque file panel, hidden by default).
- [x] Add unit tests for `ImportSKP` contract (positions, triangles, error resilience).
- [x] Update `build.yml` to run `dotnet test` on GitHub Actions runner.

## Phase 2: Verified SKP Format Implementation
- [x] Add `PackageReference Include="OpenSkp" Version="1.5.0"` to `SolidModelBrowser.csproj` (clean PackageReference, remove obsolete packages.config).
- [x] Implement `Imports/ImportSKP.cs` using verified `OpenSkp.Scene.GlbPrimitives` flat float arrays.
- [ ] Verify test suite passes green for SKP parsing.

## Phase 3: CAD Shading & Slicer Polish
- [x] Set Flat Shading (unsmoothed mesh) as default on model load (`UnsmoothAfterLoading = true`).
- [x] Implement circular Thumb style for the vertical Z-slicer slider.
- [ ] Wire optional Model Info HUD (toggleable via `I`).

## Phase 4: Toolbar & SplitButtons Architecture
- [ ] Build Shading SplitButton (Flat CAD $\leftrightarrow$ Wireframe, dropdown for smooth/overlays).
- [ ] Build External App SplitButton (icon-only 38px, context menu with app choices + separator + Save PNG `[F6]`).
- [ ] Build Camera Button (Persp $\leftrightarrow$ Ortho, dropdown for Fish-Eye & Center).
- [ ] Implement compact X/Y/Z rotation button cluster.
- [ ] Embed pixel-perfect 16x16 vector/duotone icons from `Assets/Icons/`.

## Phase 5: Opaque Sidebar & Window Chrome
- [ ] Finalize `FileNavigationPanel`: solid background, zero startup flash, sharp 1:1 folder/file icons.
- [ ] Single-load fix in `windowRendered` for file association launches.
- [ ] Restore Windows 11 Snap Layouts hover on Maximize button (`WM_NCHITTEST` / `HTMAXBUTTON`).

## Phase 6: Live Verification & Cloud Build
- [ ] Update `conductor/tracks/modernize_ui_and_skp_20261116/preview.html` with real XAML-matched layout.
- [ ] Run full CI build with tests in GitHub Actions and verify downloadable release artifact.
