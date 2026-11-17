# Build and Compilation Rule

- **Do NOT compile locally:** Never search for `csc.exe`, `msbuild.exe`, `dotnet`, or attempt local builds. The local machine does not host the compiler toolchain.
- **Remote CI only:** Compilation is handled strictly via GitHub Actions (`.github/workflows/build.yml`).
- **User permission required:** Do NOT trigger a build, push commits to origin, or run GitHub Actions workflows unless the USER explicitly requests a build.
