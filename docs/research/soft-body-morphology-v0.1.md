# cells — physically skinned organism prototype v0.1

## Implemented

- Unity `SoftCellBody2D` stores 32 independently integrated membrane node positions and velocities, radial/edge constraints, a shared enclosed area target, and a dynamic interior mass node.
- Every boundary sector samples `MembraneIntegrity.GetIntegrity`; chemical damage changes local compliance and equilibrium contour.
- Boundary node motion can sample `FluidGrid2D` locally (drag / entrainment) and responds to posterior motor activity as a local deformation.
- `SoftCellMeshRenderer2D` triangulates the live membrane and places its interior organelle visual on the core node. It does *not* invent physical movement.
- `ChemistryDemoBootstrap` attaches this body and mesh to the **two motile focal cells**, disabling their legacy sprite/membrane rendering. The stationary culture still uses the previous sector shape solver.
- The flagellum now has a separately integrated 18-node chain with segment-length constraints, drag and motor-preferred curvature. The LineRenderer uses the simulated chain instead of directly sampling the motor curve.\n- Editor tests cover finite state, area retention, injury stability, dynamic mesh topology and the segmented flagellar chain.

- `InternalCompartments2D` resolves three finite-radius interior nodes (nucleoid, storage vesicle and signal vesicle) with drag, tether forces, pairwise repulsion, and membrane-edge contact. The live mesh renderer places separate organelle art on those nodes. This is visual-mechanical compartmentalization; there is **no modeled biochemical exchange between the compartments yet**.
- The compartment update loop reuses buffers, avoiding per-step heap allocations. Tests cover boundedness, non-overlap, and membrane damage stability.

## Membrane / organelle contact coupling v0.2

When an interior compartment violates a membrane edge clearance, the compartment is projected inward. A bounded opposing displacement is now queued on the adjacent soft-body boundary nodes and applied once on the next body integration step. The diagnostic `LastContactMagnitude` records the applied reaction correction, while `LastContactPenetration` accumulates contact depth. This is reciprocal **geometric feedback**, not a momentum-conserving force or a verified constitutive tissue model. Displacements are capped to avoid numerical instability; constraint projection can remove energy. Added a regression test for queue bounds and single-step consumption.

## Stress visualization

The membrane records per-sector edge strain and a decaying, bounded memory of compartment contact reactions. `SoftCellMeshRenderer2D` maps this diagnostic stress to a high-contrast amber/red vertex gradient on the physical skin. `MaximumStress` and `StressAt(i)` are available for inspection. These are dimensionless surrogate indicators and **do not automatically inflict biological injury**; a validated stress-to-damage response must be developed separately.

## Segment-derived propulsion prototype

`FlagellarHydrodynamics2D` estimates local resistance from changes in each of the 18 flagellar nodes. Each segment contributes a tangent/perpendicular drag term; the solver balances summed segment resistance against an effective body drag, yielding a bounded force-free **local swimming velocity**. `CellMotility` now consumes that velocity on the two focal organisms; chemical gradients still steer orientation and flow advection is applied separately. The legacy scalar motor-thrust fallback remains for organisms without the hydro component. The original `CellFlagellarActuator` flow dipole is suppressed when the new component is present, avoiding duplicate fluid forcing. The hydro component deposits its own balanced front/rear dipole and reports stroke magnitude and effective dissipation.

This is a **proxy resistive-force calculation**, not calibrated fluid mechanics. Axial/lateral body drag is diagonalized, no full mobility tensor or wall correction is solved, the tail's preferred waveform is still motor-prescribed, and the segment reaction to fluid is approximated by a single dipole. Further validation must compare direction, speed, beat-frequency scaling, energy, and grid refinement against low-Reynolds reference problems. A battery of Unity tests was added for zero-power motion, bounded outputs, and zero net queued fluid force.

## Controlled flagellar sweep (experimental protocol)

Unity EditMode parameterized tests now sample `(frequency, amplitude, structural integrity)` under a fixed 0.02-second solver step for 500 steps with 100 warm-up steps. Each condition reports **signed X/Y travel**, **absolute path traveled**, **segment drag/dissipation proxy**, and **motor ATP expended**. The zero-integrity control must produce no active travel. All conditions must remain finite and below the declared velocity cap.

Signed displacement is reported separately from path length because back-and-forth waving can generate visual motion but **zero net propulsion**. No assumption is made that faster beating necessarily increases *net* swimming speed; that must emerge from the model and be compared with a low-Re reference. Outputs are written to NUnit test logs only until licensed Unity runners become available. Passing the standalone .NET workflow does not execute this assay.

## Reproducible local Unity experiment export

Open this repository in Unity 6000.6.5f1 (or a compatible Unity 6 editor), wait for script compilation, and use **Cells → Experiments → Export flagellar propulsion CSV**. The Editor writes `Build/Experiments/flagellar-propulsion.csv` relative to the project root.

Nine isolated conditions include five baseline/injury comparisons and four single-factor controls. All conditions use a fixed timestep of 0.02 simulation seconds, 500 steps, and 100 warm-up steps; no GUI rendering is necessary. Expected CSV columns: `frequency_hz, amplitude, integrity, net_x, net_y, net_distance, absolute_path, peak_speed, stroke_dissipation, motor_atp, steps, warmup`. Missing license activation in hosted CI does not stop this from running in an already activated local Unity Editor.

Batch mode (only on a machine with a licensed Unity editor):
```powershell
& "C:\\Program Files\\Unity\\Hub\\Editor\\6000.6.5f1\\Editor\\Unity.exe" -batchmode -nographics -projectPath "<repository-root>" -executeMethod Cells.Editor.FlagellarExperimentRunner.RunBatch -quit -logFile "Build/flagellar-assay.log"
```
Adjust the Unity.exe path for your installed version. The CSV results remain local until exported or uploaded; the current GitHub workflow does not produce experimental measurements.

## Science and fidelity boundary

This is a coarse **position-based membrane proxy** with arbitrary effective parameters, not molecular mechanics, a finite-element membrane, or a calibrated Stokes solver. The motor still prescribes a **preferred** flagellar waveform, but the actual visible chain is constrained and dynamically relaxed. The new optional hydrodynamics component computes a **coarse drag balance from node motions**, but is not a calibrated hydrodynamic force solver. Internal compartments now have independent positions, velocities, finite radii and collisions, but their interiors do not have deformable meshes, modeled reactions, or independent biochemical mass balances. Fluid node sampling does not imply two-way momentum-conserving fluid–membrane coupling.

## Next physical milestones

1. Validate segment-derived propulsion against low-Re reference cases, add complete anisotropic resistance tensor, bending energy, and spatially distributed reciprocal fluid coupling.
2. Local membrane permeability/leakage as a function of node-adjacent sector integrity and collision/contact.
3. Expand internal compartments to deformable meshes, explicit biochemical budgets, and contact-aware membrane tension (current compartments are finite-radius particles).
4. Deterministic test scene for mesh stability, collisions, mass/energy accounting, and recorded comparisons with analytical overdamped limits.
5. Zoomable 2.5D microscope rendering with shader-based membrane thickness and consistent depth sorting.

## Validation

GitHub Unity EditMode/PlayMode jobs need the project Unity activation credentials. A successful standalone .NET CI build does **not** verify Unity soft-body compilation or Play Mode behavior. Run `SoftCellBody2DTests` in the Unity Test Runner and inspect the `ChemistryDemo` microscope scene.
