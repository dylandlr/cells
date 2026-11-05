# Propulsion correction and validation — 2026-10-16

Continues branch `feat/substance-ontology` at `d2c9d8b`.

## Corrections

The segment drag tensor now retains its off-diagonal XY term. The translation-only
force balance solves `D u = -f` using the full symmetric 2x2 tensor. Previously,
dividing each force component by a diagonal drag coefficient omitted the coupling
from inclined segments and was not invariant under a rotation of coordinates.
`PlanarDragBalance` is shared production C# code without a Unity dependency.

The CSV runner and parameter-sweep test now measure motor ATP over the same 400
post-warm-up samples as displacement and stroke dissipation (8 seconds at 0.02 s).
Previously ATP included all 500 samples (10 seconds). CSV appends `measured_steps`,
`measured_seconds` and `warmup_motor_atp`; `motor_atp` now excludes warm-up.
Earlier CSV energy-per-distance comparisons must be recomputed.

## Executed here

- Compiled the complete `Tests/Headless` source list with the .NET 8.0.426 Roslyn
  C# compiler, warnings treated as errors, against the net8 reference assemblies.
- Executed the resulting assembly: **4,303 assertions passed**, including **4,010**
  drag checks: analytic solutions, zero stroke, 1,000 seeded rotated tensors with
  force residual and coordinate covariance checks, and invalid tensor rejection.
- `git diff --check` passed.

The standard dotnet CLI and MSBuild failed reading process metadata in this
container before compiling. Direct Roslyn compilation and execution succeeded;
this is not a claim that MSBuild or GitHub CI ran successfully here.

## Not verified here

No Unity Editor or Unity CLI was available in this workspace. Unity compilation,
EditMode/PlayMode tests, the nine-condition CSV run, interactive rendering, and a
Windows Unity build were not executed. The added ATP-window assertion awaits Unity.
Uploaded Substrate player/runtime files are not the cells source or a Unity Editor.

This fixes the mathematical translation solve, not the complete physical model.
Existing coupling gain and speed cap modify the solved velocity afterward, so
rendered swimming is not an exact force-free solution. Rotational force balance,
resolved fluid traction, calibrated units and ATP-to-dissipation coupling remain
absent. Motor ATP is still a fixed activation cost; frequency-dependent energy
claims are not supported. The current assay integrates estimated velocity while
holding the organism transform fixed, not a freely moving scene trajectory.
