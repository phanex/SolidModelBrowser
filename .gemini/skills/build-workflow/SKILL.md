---
name: build-workflow
description: Instructions for building and compiling SolidModelBrowser exclusively via GitHub Actions CI. Trigger only when explicitly instructed by the user.
---

# Build and Compilation Workflow for SolidModelBrowser

## CRITICAL RULES - READ BEFORE ATTEMPTING ANY BUILD
1. **NO LOCAL BUILDS OR COMPILER PROBING:**
   - NEVER search the local machine for `msbuild.exe`, `csc.exe`, `dotnet`, or Visual Studio.
   - Do NOT attempt to build locally using legacy system .NET Framework tools or shell commands. The local environment is not configured with the full modern C# compiler toolchain.

2. **COMPILATION HAPPENS EXCLUSIVELY VIA GITHUB ACTIONS:**
   - All builds run remotely on the GitHub Actions Windows runner (`windows-latest`) using `.github/workflows/build.yml`.
   - The workflow handles NuGet restore, MSBuild (C# latest), and packaging.

3. **TRIGGER ONLY ON EXPLICIT USER INSTRUCTION:**
   - NEVER trigger CI builds or push commits to trigger a build automatically on your own.
   - Wait until the user explicitly says to build (e.g. "зроби білд", "компілюй", "push and build").

4. **WHEN USER REQUESTS A BUILD:**
   - Stage and commit the local changes cleanly with a descriptive English commit message.
   - Push to `origin master` (or trigger workflow via `gh workflow run build.yml`).
   - Monitor the run with `gh run list` / `gh run watch`.
   - Download the release artifact (`SolidModelBrowser-Release`) cleanly.
   - Place ONLY the final executable in `dist/` (or clean path as requested), without breeding nested `dist` or temporary directories.
   - **FRESH CONFIG PER NEW VERSION:** Always delete `settings.ini` in the executable folder upon updating/downloading a new build so that the application initializes a clean default configuration silently.
