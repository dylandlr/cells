# cells — physically skinned organism prototype v0.1

## Implemented

- Unity `SoftCellBody2D` stores 32 independently integrated membrane node positions and velocities, radial/edge constraints, a shared enclosed area target, and a dynamic interior mass node.
- Every boundary sector samples `MembraneIntegrity.GetIntegrity`; chemical damage changes local compliance and equilibrium contour.
- Boundary node motion can sample `FluidGrid2D` locally (drag / entrainment) and responds to posterior motor activity as a local deformation.
- `SoftCellMeshRenderer2D` triangulates the live membrane and places its interior organelle visual on the core node. It does *not* invent physical movement.
- `ChemistryDemoBootstrap` attaches this body and mesh to the **two motile focal cells**, disabling their legacy sprite/membrane rendering. The stationary culture still uses the previous sector shape solver.
- Editor tests cover finite state, area retention, injury stability, and dynamic mesh topology.

## Science and fidelity boundary

This is a coarse **position-based membrane proxy** with arbitrary effective parameters, not molecular mechanics, a finite-element membrane, or a calibrated Stokes solver. The motor's flagellar curve is still kinematic; thrust remains approximated by the existing actuator and motility module. The organelle currently follows a simulated interior core position but is not a resolved, deformable compartment. Fluid node sampling does not imply two-way momentum-conserving fluid–membrane coupling.

## Next physical milestones

1. Explicit segment-node flagella with bending constraints, active curvature, and reciprocal drag coupling. Replace the old kinematic LineRenderer.
2. Local membrane permeability/leakage as a function of node-adjacent sector integrity and collision/contact.
3. Dynamic internal compartments with mass, exclusion radii, drag, and envelope pressure (rather than visual-only inclusions).
4. Deterministic test scene for mesh stability, collisions, mass/energy accounting, and recorded comparisons with analytical overdamped limits.
5. Zoomable 2.5D microscope rendering with shader-based membrane thickness and consistent depth sorting.

## Validation

GitHub Unity EditMode/PlayMode jobs need the project Unity activation credentials. A successful standalone .NET CI build does **not** verify Unity soft-body compilation or Play Mode behavior. Run `SoftCellBody2DTests` in the Unity Test Runner and inspect the `ChemistryDemo` microscope scene.
