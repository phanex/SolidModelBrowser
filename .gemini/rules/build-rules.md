# Build and Compilation Rule

- **Do NOT compile locally:** Never search for `csc.exe`, `msbuild.exe`, `dotnet`, or attempt local builds. The local machine does not host the compiler toolchain.
- **Remote CI only:** Compilation is handled strictly via GitHub Actions (`.github/workflows/build.yml`).
- **User permission required:** Do NOT trigger a build, push commits to origin, or run GitHub Actions workflows unless the USER explicitly requests a build.
- **Fresh config per new version:** Every new downloaded build must start with fresh defaults: always delete `settings.ini` in the binary directory upon downloading/updating a release so the new executable generates a clean config cleanly and silently.
