# Windows player workflow

The project is pinned to Unity **6000.6.5f1**. Install that Editor with Windows Build
Support (Mono), activate it through Unity Hub, and close the project before running
batch commands. Development on Linux does not change the Windows player target.

From PowerShell in the repository root:

```powershell
./tools/windows/windows.ps1
# For an Editor installed somewhere else:
./tools/windows/windows.ps1 -UnityEditor 'D:/Unity/6000.6.5f1/Editor/Unity.exe'
```

The script runs EditMode and PlayMode tests, requires passing XML reports with a
nonzero test count, builds Windows x64, checks the player files and packages the
whole output folder. Each invocation gets a new Artifacts directory so a failed
build cannot be mistaken for an older successful executable. `-SkipTests` is
available for iteration; it does not count as a validated release.

In the Editor, **Cells > Build Windows player** builds to `Build/Windows/Cells.exe`.
This menu saves modified scenes only with your confirmation. It does not run tests.
The build method does not open or regenerate scenes. Both microscope scenes must
exist. Mono is selected for the build and the previous backend is restored afterward.

For a licensed Linux Editor with Windows Mono support:

```bash
/path/to/Unity -batchmode -quit -projectPath "$PWD" -buildTarget Win64 \
  -executeMethod Cells.Editor.DemoProjectSetup.BuildWindows -logFile windows-build.log
```

Optional `CELLS_BUILD_OUTPUT` selects an `.exe` output path. Keep Cells.exe,
UnityPlayer.dll, Cells_Data and all other generated dependencies together.

## Windows acceptance check

Extract the complete player ZIP on Windows, launch Cells.exe, confirm the culture
renders and chemistry updates. Click a cell, pause, pan with middle mouse drag or
arrow keys, zoom, dose nutrient/toxin, resume and confirm inspection responds.
Camera panning must not move organisms or advance paused chemistry. Close and
relaunch; inspect Player.log for exceptions. Compilation and Linux Editor tests do
not replace this Windows player check.

## Current limitations

This remains an adherent chemistry sandbox: no learned locomotion, reproduction,
or evolutionary training is implemented. The v3.3 Python building-block engine has
not been ported by this change. No Windows executable is claimed until a successful
Unity build; no Windows runtime pass is claimed until the player is actually run.
