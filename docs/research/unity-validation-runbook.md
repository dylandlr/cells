# cells — Unity verification runbook (v0.1)

## Current evidence and blocker

The research-only .NET 8 GitHub Actions workflow has **passed** on previous commits, including `9d587a06` (headless CI run 38850810511). This verifies the non-Unity research algorithms only. Unity CI run 38850810529 failed in **Verify Unity activation configuration exists**; both EditMode and PlayMode test runner steps were skipped. It does **not** imply that Unity compilation has passed or failed.

Unity is not installed in the assistant execution container. We cannot directly launch or certify the Windows Unity game here.

## Option A: run locally on Windows (recommended for first verification)

1. Open this repository in Unity Hub using a Unity 6 editor compatible with its `ProjectSettings/ProjectVersion.txt`. Confirm that Unity imports the project successfully and the editor is licensed.
2. Close the Unity Editor before using batch mode. Avoid opening the same project in two Unity processes at once.
3. In Windows PowerShell from the repository root run:

   ```powershell
   powershell -ExecutionPolicy Bypass -File .\Tools\Run-UnityTests.ps1 -Mode Both
   ```

   If auto-detection picks a different Unity Hub installation, specify `-UnityEditor "C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor\Unity.exe"` with the **actual** installed path. You may choose `-Mode EditMode` first to diagnose compile problems without running PlayMode.
4. Inspect `UnityTestResults/EditMode.xml`, `UnityTestResults/PlayMode.xml`, and corresponding `.log` files. The script checks Unity process return code and validates that an NUnit XML report exists, contains nonzero tests and has no failures or inconclusive tests.
5. Preserve the XML and editor logs as evidence. Before changing experiment coefficients, record the source revision and exact Unity editor version used.
6. In the Unity Editor, use **Cells → Research → Run Physical Signaling 2x2 Factorial (30 Seeds)**; inspect `ExperimentRuns/physical-factorial-v1/`. Also run **Cells → Research → Probe Signal Detection Sensitivity (10 Seeds)** to inspect raw receptor/movement telemetry.

## Option B: GitHub Actions

The existing workflow `.github/workflows/unity-tests.yml` calls GameCI's Unity test runner using editor `6000.6.5f1`. It requires a supported Unity activation configuration via GitHub Actions repository secrets (`UNITY_LICENSE` or `UNITY_EMAIL` + `UNITY_PASSWORD`) or a licensed self-hosted runner. **Do not commit credentials or license material to Git.** Check Unity's current licensing rules and GameCI's activation documentation; Unity Personal activation may require a different local approach than the CI secret path.

If the license configuration is still absent, the workflow intentionally fails before running tests. Do not report its failure as a code defect. After configuring activation, rerun the workflow and inspect the uploaded `unity-EditMode-results` and `unity-PlayMode-results` artifacts.

## Scientific pass conditions

- **Compilation:** no missing namespace, assembly definition, or Unity API version errors.
- **Regression:** NUnit reports enumerate nonzero tests with zero failures and inconclusive results.
- **Molecular conservation:** emitted = remaining + decayed signal, within a documented numerical tolerance. Initial nutrient = remaining + harvested nutrient, when no additional sources/sinks are configured.
- **No-transmission controls:** no production or zero diffusion must yield no long-range receptor detection; silent receptor-only controls must not change payoffs.
- **Mechanistic telemetry:** distinguish reception, periodically activated policy, actual motor execution and changed steering direction.
- **Treatment identification:** estimate receiver-energy production-by-response interaction on matched seeds. If no receivers detect signals or no policy decisions activate, diagnose transport/timescales rather than interpret effect estimates as communication failures.
- **Calibration boundaries:** all coefficients are simulation units; current motor controller is discrete and not the continuous fluid/flagellar physiology. Passing tests is not evidence of biological realism, adaptation, or game-theoretic equilibrium.

## Remaining engineering work

Once real Unity test reports are available: fix compile/runtime failures first; record a known-good Unity scene and short real gameplay capture; then replace the simplified motor controller with explicit `CellMotility` / `CellFlagellarActuator` integration under seed-matched calibration. Do not tune on held-out evaluation seeds.

## Build a playable Windows development executable

A local wrapper around the repository's existing `Cells.Editor.DemoProjectSetup.BuildWindows` method is now available:

```powershell
powershell -ExecutionPolicy Bypass -File .\Tools\Build-Windows.ps1
```

This uses a locally installed, **licensed** Unity editor with Windows Build Support. The command invokes Unity batch mode, creates/opens both existing demonstration scenes, builds `Build/Cells.exe`, and checks that both the executable and `Build/Cells_Data` exist. Build diagnostics are saved to `UnityTestResults/WindowsBuild.log`. An explicit editor path can be supplied with `-UnityEditor`.

**Do not distribute only the EXE**. Unity's Windows player requires the complete `Build/` directory, including data, DLLs and supporting files. This build uses `BuildOptions.Development`; it is not signed. Windows Smart App Control may refuse to launch an unsigned executable. Do not attempt to disable the security controls as an implementation shortcut; use a trusted local Unity PlayMode test while signing and distribution are planned.

The demonstration now reserves 15% of its existing initial nutrient budget in the secretor's local grid voxel, with the remainder at the original center position. This is a scripted initial challenge intended to make the nutrient-triggered signaling mechanism visible and does not demonstrate autonomous discovery, cooperation or genuine emergent evolution. Total initial nutrient mass is unchanged for nonnegative `initialFood` settings.

At commit time the Windows wrapper was **not executed** in the assistant environment: no licensed Unity editor or Windows player verification available. Use the local test script before interpreting gameplay footage or distributing a build.
