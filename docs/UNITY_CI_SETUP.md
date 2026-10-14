# Unity CI setup (unverified)

The repository now has minimal Unity project metadata, an EditMode test assembly definition, and a GameCI workflow pinned to Unity 6000.0.23f1.

## Licensing

The hosted runner checks activation configuration and fails explicitly if none is configured. Do not commit activation credentials. Unity Personal licensing may require Unity Hub and may not work in unattended hosted CI. Prefer a suitably licensed runner or a self-hosted Windows runner with a supported Unity installation if the hosted workflow cannot activate.

## Before interpreting any result

- The legacy v0.3 Causal Microscope scripts have **not** been committed. The current branch is only the new chemical-system source.
- Unity Editor has **not** run this project.
- GameCI versions and package resolution must be checked in the first actual workflow run.
- Tests in Assets/CellsDemo/Tests/Editor must be discovered and compile before claiming a pass.
- A test pass does not verify visuals or physically calibrated biology.

## Local steps

Open repo in Unity Hub using Editor 6000.0.23f1. Inspect compiler output and run Window > General > Test Runner > EditMode. Create an empty 2D scene, add ChemistryDemoBootstrap and ChemistryDemoView to an empty GameObject, and press Play.

## Video capture

A game view MP4 requires a render-capable runner/editor, scripted camera capture, and a frame encoder. This workflow **only runs EditMode tests**; it does not capture footage.
