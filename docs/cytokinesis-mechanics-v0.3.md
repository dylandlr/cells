# Cytokinesis mechanics vertical slice v0.3

Authored and committed 2026-10-19. Unity compilation, Edit Mode, and
Play Mode tests are NOT yet executed.

## Implemented
- `SoftCellBody2D.divisionConstriction` (0..1) adds a localized equatorial
  inward correction on actual simulated boundary positions at local y=0.
- The membrane's effective rest shape now narrows near that equator.
  The solver's edge, radial and area constraints determine the resulting
  shape; the renderer follows those same actual nodes.
- `ExperimentalConstrictionCycle2D` gradually drives actuation when
  living cells pass configured biomass and energy thresholds; it relaxes
  if eligibility is lost. The opt-in microscope bootstrap attaches it.
- An Editor test asserts a smaller equatorial membrane span after
  actuation and a nondegenerate remaining membrane area.

## Limitations and validation concerns
This is an **effective numerical actuation**, not calibrated actomyosin or
FtsZ ring mechanics. `ReadyForScission` is a visual/progress milestone
only; no cleavage or topological split of a mesh occurs. The current
`ExperimentalDivisionDemo` may still produce its single daughter before
constriction completes. It has not been wired to wait for scission
readiness because that would falsely suggest complete cytokinesis.
Neither internal organelles nor genetic material are partitioned by the
mechanical solver; organelle movement remains independent.

The constriction model uses a single connected star-shaped 2D boundary
with one center node. At sufficiently large constriction, this
representation cannot accurately produce two lobes and neck scission.
For faithful simulated separation we will need two independently
tracked closed boundaries and an explicitly conservative topology
transition, as well as an organelle segregation phase.

### Next validation gates
1. Run Editor test for equatorial span, area and finite geometry; inspect
   progression across several fixed timesteps and check convergence.
2. Render slowed microscopy video, monitor stress and node velocity
   during activation; verify neither explosive forces nor nonphysical
   scale changes.
3. Introduce genetic compartment duplication/segregation with resource
   accounts before allowing mechanical separation to commit a birth.
4. Stage creation with rollback-safe resource transfer and preserve
   molecular/particle counts, then hand off two closed soft bodies.
5. Only after those tests pass, link birth registration and mutation
   scheduling to successful physical scission.

Reference discipline: Substrate: Emergence user-supplied binary + README
are visual/gameplay references, not accessible editable C# source.
The actuation is an explicitly gamified approximation, not evidence of
real organism viability.

## Safety/sequence correction — 2026-10-19

The opt-in `ExperimentalDivisionDemo` now rejects births until
`ExperimentalConstrictionCycle2D.ReadyForScission` is true, rather than
creating an independent daughter while the membrane is still unconstricted.
The name `ReadyForScission` indicates a *progress threshold* only. True
membrane topology change has NOT been implemented. The experimental scene
remains opt-in and supports at most one birth.

The Play Mode reproduction regression now asserts that an early birth fails
and explicitly advances the constriction cycle before attempting the birth.
Because that test enables the experiment after the scene has initialized,
it attaches the cycle component during setup. These tests are not verified
until Unity runs with CI activation.

The daughter factory now rejects secretor organisms because the current
resource budget does not include toxin storage. Permitting such a birth
would create unaccounted toxin material in the daughter. Generalized
division must eventually include the full organism inventory, stage objects
inactive before configuring *any* component, and implement rollback after
any failed activation or parenting step.

**Next:** complete topology separation, internal organelle partitioning,
and atomic resource transfer before enabling continuous generations.
