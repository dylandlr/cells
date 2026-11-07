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
toxin exposure, whole-scene nutrient/byproduct conservation, and passive nutrient leakage
after death. The controlled-rupture test also writes `Artifacts/membrane-local-rupture.png`
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


## TODO — Configure Unity license for gameplay recording (2026-10-19)

- [ ] Confirm whether the development account uses Unity Personal or Unity Pro.
- [ ] Follow current [GameCI activation guidance](https://game.ci/docs/github/activation/) and obtain valid Unity activation credentials or configure a licensed self-hosted runner.
- [ ] Add the required Unity activation values as **GitHub Actions repository secrets**, not committed files. Never commit or share a license file, password, or serial number.
- [ ] Re-run the **Unity Gameplay Demo** workflow on `feat/substance-ontology`.
- [ ] Verify Edit Mode/Play Mode tests, video encoding, and the downloadable `cells-unity-gameplay` artifact; investigate any build/render failures.

## Membrane research export — local fallback

When GitHub Unity activation is unavailable, run the full membrane assay
suite in your locally installed Unity 6000.6.5f1 Editor:

1. Open the `cells` project in Unity.
2. Choose **Cells > Research > Export Membrane Constriction Trace**.
3. Inspect `artifacts/membrane-assay.json`,
   `artifacts/membrane-convergence.json`, and
   `artifacts/membrane-comparison.json` inside the Unity project directory.
4. On Windows, from a terminal at the project root run:

```powershell
python tools/capture/analyze_membrane_assay.py artifacts/membrane-assay.json artifacts
python tools/capture/analyze_membrane_convergence.py artifacts/membrane-convergence.json artifacts
python tools/capture/analyze_membrane_comparison.py artifacts/membrane-comparison.json artifacts
```

The exporter resolves relative output paths from `Application.dataPath`'s
parent (the Unity project root), independent of the Editor process's
working directory. `CELLS_ASSAY_OUTPUT` may override the filename; the
other two reports are written beside that file.

The exporter is **not** a gameplay reproduction test. Outputs are
numerical geometry experiments under effective 2D forces, not calibrated
biological data or proof of membrane scission. Do not accept JSON output
until the three Python analyzers have checked it.

## One-command Windows membrane assay

For a locally activated Unity Editor, run from the repository root:

```powershell
powershell -ExecutionPolicy Bypass -File tools/capture/run_membrane_assay.ps1
```

If the Editor is installed outside the Unity Hub default paths, supply
`-UnityExe "C:\path\to\Unity.exe"`. The script runs the export through
Unity batch mode, removes previous JSON reports first (so stale output
cannot masquerade as a new run), checks the Unity exit code and verifies
all three newly generated JSON reports are nonempty before running the
Python analysis scripts. It generates a Unity log at
`artifacts/membrane-unity.log`. A failed validation is a failed run,
not a success. Avoid opening the same project in another Unity Editor
instance while running this script because Unity may lock it.

No local Unity execution has been performed by the assistant.

## GitHub Actions activation and trustworthy test reporting (2026-10-20)

The `Unity tests` workflow now triggers on pushes to
`feat/substance-ontology` as well as pull requests and manual dispatch.
Both EditMode and PlayMode jobs receive `UNITY_LICENSE`,
`UNITY_EMAIL`, `UNITY_PASSWORD`, and `UNITY_SERIAL` from GitHub
Actions secrets, as applicable to the GameCI activation method in use.
These are secret **names**, not credentials; do not put secret values
in workflow files, issues, chat, or commit history.

To configure, open
`https://github.com/dylandlr/cells/settings/secrets/actions`
with repository admin privileges, follow the current GameCI Unity
activation guide appropriate to your subscription, and create the
supported secrets there. Then manually dispatch **Unity tests** on
`feat/substance-ontology` and confirm **both** modes advance past
the activation preflight to the Unity test runner itself. A skipped
Unity test step is **not** a test failure in C# and **not** a pass.
Only actual Unity test reports can establish whether the source
compiles and its tests pass. No installed secret values can be read
back through the repository's source API.

Once tests run, separately dispatch **Membrane physics assay** and
confirm its output JSONs, CSVs and summary artifacts exist. This
workflow also has unverified GameCI custom-build-method compatibility
that must be corrected if the runner reaches the Editor but does not
produce assay reports.
