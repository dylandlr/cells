# Playable cells demo milestone

Goal: a Windows Unity player demonstrating two free-swimming cells, finite nutrient
uptake, energy spending, local toxin injury, paid repair and a causal microscope.
This is a scripted physiology baseline, not trained ML-Agents or validated evolution.
The stochastic metabolism research remains separate.

## Build on Windows

Use Unity 6000.6.5f1 with Windows build support and a valid local activation.
Close this project in the Editor first (batch tests need its project lock).
From the repository root:

```powershell
powershell -ExecutionPolicy Bypass -File tools/Build-PlayableDemo.ps1 -Launch
```

For a nonstandard Editor location, add `-UnityExe "C:\path\to\Unity.exe"`.
The script runs EditMode and PlayMode tests, requires nonempty passing NUnit
reports with no failed/skipped tests, then builds a new player directory beneath
`Build/PlayableDemo/`. It stops at the first failure and retains logs there.
A build manifest records the executable hash and tests. Runtime and visual review
remain false until separately reviewed. The script has not been executed in
PowerShell or Unity in this cloud environment.

`BuildPlayableDemo` generates `PlayableDemo.unity` with seed 631, cultureView=false,
initialToxin=0 and experimental division off. It uses the existing simulation and
microscope; its food still comes from the bootstrap's explicit starting budget.
The generated scene path is reserved for this build command; it is regenerated.
Other scenes and the existing culture build method remain available.

## Acceptance walkthrough — pending execution

1. Launch the new player and confirm simulation time advances, both focal cells
   are visible and center positions and flagella change over time.
2. Select cells; observe actual nutrient intake/digestion and energy spending.
   Check values against the event log, not just appearance.
3. Pause, inspect and resume. Confirm zoom/follow and 0.25x playback work.
4. Apply a small toxin dose to a selected membrane region; confirm damage and its
   logged location. Apply repair before rupture; confirm energy decreases and
   integrity increases. Ruptured cells must not revive.
5. Confirm readable controls at 1280x720 and 1920x1080, no exception spam in the
   player log, and capture actual gameplay. Record whether frame pacing is usable.

A passing build alone does not meet these acceptance criteria. Do not report a
working demo until they have been exercised. The next phase is an ML-Agents smoke
test and a trained-vs-scripted comparison in the same validated environment.

## Current execution blocker

At published commit 072f4f1, GitHub Actions Unity test run 39572975826 failed
at activation preflight; EditMode and PlayMode were skipped. The successful
standalone Windows run builds the separate .NET project, not this Unity demo.
The cloud host has Unity CLI but no installed Editor or reachable licensing client.
Use the existing licensed Windows installation for the next verification pass.
