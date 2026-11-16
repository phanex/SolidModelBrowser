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
- [x] Verify test suite passes green for SKP parsing.

## Phase 3: CAD Shading & Slicer Polish
- [x] Set Flat Shading (unsmoothed mesh) as default on model load (`UnsmoothAfterLoading = true`).
- [x] Implement G-code exception rule: G-code toolpaths retain smooth normals by default (`Utils.ShouldUnsmoothMesh`).
- [x] Implement circular Thumb style for the vertical Z-slicer slider.
- [x] Wire concise Model Info HUD (toggleable via `I`).

## Phase 4: Toolbar & SplitButtons Architecture
- [x] Build Shading SplitButton (Flat CAD $\leftrightarrow$ Wireframe, dropdown for smooth/overlays).
- [x] Build External App SplitButton (icon-only 38px, context menu with app choices + separator + Save PNG `[F6]`).
- [x] Build Camera Button (Persp $\leftrightarrow$ Ortho, dropdown for Fish-Eye & Center).
- [x] Implement compact X/Y/Z rotation button cluster.
- [x] Embed pixel-perfect vector icons in toolbar buttons.

## Phase 5: Opaque Sidebar & Window Chrome
- [x] Finalize `FileNavigationPanel`: solid background, hidden by default (`ShowFilePanel = false`, `Opacity = 1.0`), toggled via Tab / Ctrl+B.
- [x] Single-load fix in `windowRendered` for file association launches.
- [x] Restore Windows 11 Snap Layouts hover on Maximize button (`WM_NCHITTEST` / `HTMAXBUTTON`).

## Phase 6: Live Verification & Cloud Build
- [x] Update `conductor/tracks/modernize_ui_and_skp_20261116/preview.html` with real XAML-matched layout.
- [x] Run full CI build with tests in GitHub Actions and verify downloadable release artifact.
