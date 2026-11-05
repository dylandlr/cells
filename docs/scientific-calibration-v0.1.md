# Scientific calibration protocol v0.1 — microscopic motility

Status: **reference design + authored tests; Unity tests and experimental matching not yet run**.

## Sources and scope

- E. M. Purcell (1977), *Life at Low Reynolds Number*, American Journal of Physics 45, 3–11. https://doi.org/10.1119/1.10903
- Eric Lauga & Thomas R. Powers (2009), *The Hydrodynamics of Swimming Microorganisms*, Reports on Progress in Physics 72, 096601. https://doi.org/10.1088/0034-4885/72/9/096601
- Howard C. Berg & Douglas A. Brown (1972), *Chemotaxis in Escherichia coli Analysed by Three-dimensional Tracking*, Nature 239, 500–504. https://doi.org/10.1038/239500a0
- User-provided Substrate: Emergence readme and compiled Unity build are **visual/gameplay references only**; they do not provide editable original scientific source.

## Organism distinction

The existing `SoftFlagellum2D` is a *deforming flagellar waveform* model.
Do **not** label it a calibrated E. coli helical rotary flagellum. The
first validation organism should initially be called a generic beating-tail
microswimmer. An E. coli model will need a distinct rotary/motor and tumble
architecture while sharing the appropriate fluid mechanics abstractions.

## Dimensional specification to resolve

Unity distance, timescale and viscosity/drag coefficients remain effective
simulation units. Use `MicrohydrodynamicsReference` only for calculations
made with actual SI input parameters; its outputs **must not be assigned
directly to the current solver's arbitrary drag coefficients**.

Reference case (assumed values, not a unique organism calibration):
density 1000 kg/m^3; dynamic viscosity 0.001 Pa s; characteristic length
2e-6 m; speed 20e-6 m/s. Expected Re=4e-5.

Ideal unbounded rigid sphere theory:
translation drag = 6 pi mu R; rotation drag = 8 pi mu R^3.
The current effective `FlagellarHydrodynamics2D` radius-dependent scaling
was inspired by these powers, but **its coefficients are not calibrated
physical SI resistances**, and neither formulas nor coefficients
describe nearby walls or non-spherical cell geometries accurately.

## Mandatory validation gates

1. **Zero motor:** in still fluid, output active swimming velocity
   and net displacement from propulsion approach zero immediately, with
   no inertial coasting. Separately test advection in imposed flow.
2. **No duplicate drag:** in hydrodynamic `CellMotility` mode, treat
   `hydrodynamics.LocalSwimmingVelocity` as the terminal velocity
   returned by the resistance solve. Do not apply another arbitrary
   acceleration easing or exponential velocity drag afterward.
3. **Time-step convergence:** compare trajectories at 0.02, 0.01 and
   0.005 simulated seconds under the *same* motor stroke; quantify speed,
   displacement and heading error.
4. **Flagellar stroke:** demonstrate displacement over a full nonreciprocal
   beat; a reciprocal control stroke should not artificially swim in the
   low-Re limit (subject to numerical tolerance and geometry).
5. **Chemotaxis:** separately compare run-length distributions and
   gradient drift against a temporally sensing baseline. Controllers must
   not secretly read the true global nutrient gradient.
6. **Growth:** sweep structural biomass and compare expected trends in
   swimming speed, rotational response and membrane geometry.
7. **Conservation:** nutrient mass in extracellular field, internal
   reserve, biomass and recorded byproduct should be reconciled,
   allowing explicit sinks and energetics. Energy itself is not mass.
8. **Reporting:** export raw per-seed trajectories, wall-clock vs
   simulation seconds, fixed step, unit mapping and confidence intervals;
   include a stationary / no-gradient negative control.

## Implementation state

- Authoritative body growth and fluid resistance scaling: prototype.
- Existing zero-motor enforcement: present in hydrodynamic component,
  **not yet validated end-to-end after this change**.
- Double smoothing removed for hydrodynamically solved swimmer translation.
- SI reference formulas + NUnit tests: authored, not executed in Unity.
- Angular chemotactic steering, chemical sampling, near-wall effects, fluid
  feedback, compartment geometry and division are **not calibrated**.
- Existing direct-gradient legacy movement should be treated as a
  convenience sandbox baseline, not a biologically accurate E. coli model.

Scientific upgrade policy: choose an organism and experimental assay before
claiming that a numerical parameter is measured or that a model is validated.
