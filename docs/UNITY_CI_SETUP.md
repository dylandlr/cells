# Unity validation

Use **Unity 6000.6.5f1** and Test Framework **1.8.0**, matching the installed editor's
resolved built-in package. `Packages/packages-lock.json` and Unity-generated `.meta`
files are committed for reproducible imports. Legacy input is enabled for the existing
microscope debug overlay.

## Local commands

With the Unity CLI available (on this Windows host it is bundled under Unity Hub):

```powershell
unity run . --editor-version 6000.6.5f1 --timeout 600 -- -executeMethod Cells.Editor.DemoProjectSetup.CreateScene -logFile Logs/setup.log
unity test . --editor-version 6000.6.5f1 --mode EditMode --output TestResults/editmode.xml --timeout 600 -- -logFile Logs/editmode.log
unity test . --editor-version 6000.6.5f1 --mode PlayMode --output TestResults/playmode.xml --timeout 600 -- -logFile Logs/playmode.log
```

Or open Window > General > Test Runner in Unity and run both suites. A failing or
undiscovered suite is not a pass. Consult NUnit XML as well as process exit status;
CLI summaries can highlight unrelated licensing warnings ahead of the real failure.

PlayMode tests load the saved demo scene, check feeding/injury, pause and pipette
behavior, renderer lifecycle, camera rendering, and local rupture under directional
toxin exposure. The controlled-rupture test also writes `Artifacts/membrane-local-rupture.png`
and its scenario provenance; transport and reaction are deliberately paused in that test. The render test writes
`Artifacts/microscope-camera.png` and a provenance text file. That image is an actual
Unity PlayMode camera render. It excludes the IMGUI overlay and is not a recording
of the Editor window, a video, or proof of manual interaction/accessibility testing.

## Hosted CI

The GameCI workflow runs both modes on Unity 6000.6.5f1. It requires a compatible
container image and configured Unity activation. A local licensed Windows pass does
not prove hosted Linux CI works. Hosted execution and activation remain unverified.
Do not commit credentials. Use a supported GameCI licensing method or a suitably
licensed self-hosted runner if hosted activation is unavailable.

See `INTEGRATION_STATUS.md` for the actual results of this development pass.

For a Windows development player: `unity run . --editor-version 6000.6.5f1 --timeout 600 -- -executeMethod Cells.Editor.DemoProjectSetup.BuildWindows -logFile Logs/build.log`. Output: `Build/Cells.exe`.

Read-only static structure checks: `python tools/verify_project.py`.
