# Chemistry model and limits

## Quantities

`MultiChemicalField2D` stores mass per voxel. `Deposit`, `Withdraw`, and `Total` use
mass; `Sample` and `SampleCell` return mass / cellSize squared. World lookup uses the
field's local plane; units are local simulation units (the demo uses unit scale).
Outside-domain interactions return zero rather than depositing material at a distant edge.
Do not change grid dimensions/species at runtime without calling Initialize, which clears it.

Diffusion solves a 5-point Laplacian with reflective boundaries and automatic explicit
substeps (alpha <= 0.24). Exponential first-order decay removes mass and increases
`DecayedMass`. Diffusion and reactions are operator split. Diffusion conserves mass to
floating-point tolerance; decay, feeding and experimental pipetting are explicit sinks/sources.

## Reaction

Toxin B + neutralizer C -> waste D consumes equal mass of B and C and produces twice
the reaction extent as D. Each voxel integrates the bimolecular law dc/dt = -k*a*b
analytically, including equal initial concentrations. Different voxels cannot react until
transport brings species together. Missing or repeated species IDs reject the reaction.
This is an illustrative equal-equivalent-mass neutralization, not an identified chemical
reaction or elemental stoichiometry engine. Rate units are area / (mass * time).

## Cells and material accounting

Dissolved food enters a reserve at a bounded intake rate, then digestion consumes that
reserve and supplies energy with a configured yield. Digestion emits equal mass of a
distinct metabolic byproduct (`metabolic-waste-E`) at a membrane outlet. It does not
consume food or supply energy if that product/outlet cannot accept the material.
This equal-mass conversion is an abstract catabolic pathway, not elemental/oxygen balance.
Particulate food uses a different external species ID and transfers packet mass into the
same internal nutrient reserve; it no longer creates energy immediately. The exchange's
usableEnergyPerMass governs digestion for both feeding routes. A missing reserve component
prevents ingestion rather than discarding the food. Each demo cell starts with 0.5 mass
of stored nutrient, explicitly part of its initial condition. Toxin secretion transfers a finite internal
reserve to the field and charges energy. Exposure samples 32 regions at the membrane surface. Each receives 1/32 of the
whole-cell uptake coefficient (area / time). Uptake removes actual local toxin mass,
then applies resistance-adjusted injury to the corresponding region. An optional
explicit intake site concentrates the whole coefficient at its nearest region.
Absorbed toxin is no longer represented externally; intracellular clearance is not modeled.

Membrane integrity is the mean of 32 bounded [0,1] material regions. Local damage
is scaled by 32 so a uniform exposure has the same mean effect as the original
whole-cell model. Injury event quantities are actual mean integrity losses; locations
and sourceStructure identify the affected region. A region reaching ruptureThreshold
kills the cell even if the mean remains high. This weakest-region rule is illustrative,
not a resolved pore mechanics model. Feeding does not revive a dead cell.

Damaged regions increase their uptake coefficient linearly from 1x to the configurable
maximum (default 3x). This is a phenomenological permeability feedback. It does not yet
simulate intracellular toxin concentration or osmosis. The dissolved grid
does not treat the cell interior as an impermeable obstacle. All contacts snapshot field
concentrations before withdrawals; severe depletion of a shared grid voxel is resolved
in region order, so normal small fixed time steps should be retained.

Repair spends energy proportional to the mean integrity restored, distributing it
across local deficits without changing healthy regions. Rupture is not repairable.
Explicitly assigning the legacy integrity scalar resets all regions uniformly for
scenario compatibility; it is not a resurrection mechanism.
Events identify absorption, secretion, digestion, leakage, injury, toxin-associated death, and pipette additions.
Basal starvation is visible in physiological state but does not yet create a causal log event.
Reaction extent is exposed as aggregate reacted mass rather than per-voxel event spam.

## Passive nutrient leakage

`MembraneLeakageSystem` transfers only nutrient-reserve material. Its inside concentration
is reserve mass / enclosed contour area, a well-mixed two-dimensional compartment.
Circular cells use pi * membraneRadius squared. Irregular contours use the analytic
polar area of their radial harmonics; contact lengths use chords between sector boundaries.
For each damaged region it samples outside concentration at the actual surface location.
Conductance is permeability * damage squared * arc length. Intact membrane has zero leak.
Only an outward concentration gradient transfers mass; reverse passive uptake is not modeled.

Each contact uses the exact two-compartment exchange toward equal concentrations:

```
S = 1 / internalArea + 1 / fieldCellArea
transfer = (insideConcentration - outsideConcentration) / S
           * (1 - exp(-conductance * S * dt))
```

The amount is bounded by the reserve and credited to the field before it is deducted
internally. Failed/outside-domain deposits preserve the reserve. Contacts run sequentially
in region order, an operator-split approximation of shared cytosol coupled to multiple
external voxels. Small fixed steps limit order dependence; this is not an exact simultaneous
multi-compartment solve. A single contact cannot overshoot its concentration equilibrium.
Leakage continues after death and neither spends nor creates energy. The microscope reports
remaining internal mass and cumulative emitted mass. Toxin reserve leakage, intracellular
spatial transport, pressure-driven flow, water balance and osmosis remain unimplemented.

Tests verify 500 steps of leak-plus-diffusion conservation, both living and dead leakage,
a concentrated exterior blocking outward flow, bounded equilibration over a very large step,
rejected-deposit accounting, digestion products, and particle-to-reserve transfer.

## Rendering and reproducibility

Bilinear concentration textures interpolate actual field values for display, with a
smooth saturating opacity response. This does not change the finite-volume chemistry.
Chemical false color can be hidden without pausing chemistry. Particle sprites use
simulated positions, masses and radii; visual caps do not remove simulation particles.
Runtime sprites/textures are disposed with their owner. A fixed particle seed supports
repeatable trajectories with the same time steps; whole-scene cross-platform bitwise
determinism is not claimed. A separate antialiased membrane texture draws each region
from its actual integrity and leaves transparent openings for failed regions. It does
not move or scale the organism: rendering cannot change contact sampling geometry.
The opening is a state-driven visualization, not a mesh or fluid-flow aperture.

The static contour is r(theta) = R * [1 + a * (0.65 cos(2u) + 0.25 sin(3u) + 0.1 cos(5u))],
where u = theta - phase and a is clamped to [0, 0.2]. Its area is pi * R^2 * (1 + 0.2475 a^2).
Exposure, leakage, pipetting, initial named pores and the boundary use the same contour.
Rest morphology is configured before bootstrap texture generation. A bounded radial stretch
is interpolated across the 32 sectors for dynamic deformation. Area is then integrated with
128 angular samples; physical contacts, leakage, pipetting, named pores and the optical
shader all follow this same contour. Non-unit transform scales remain unsupported.

The culture scene is an adherent arrangement with fixed centers. Local soft-contact
repulsion, rest-shape elasticity, neighboring-sector smoothing and area recovery drive
an overdamped deformation model. Injury softens/retracts its region. Deformation is bounded
to 0.72–1.08 times rest radius; it is not a full tissue stress solver and can leave projected
overlap under crowding. There is no oscillator, beating, locomotion, growth or cell division.
The mechanics use arbitrary units and do not claim calibrated elastic moduli, osmotic
pressure, water transport, or conservation of mechanical energy.

Intracellular granules, inclusions and
nuclei are illustrative static texture, not additional simulated chemicals or organelles.
Phase-contrast-inspired shading is a visual approximation, not an optical wave simulation.
The shader warps the texture with the physical boundary; no independent breathing animation
is applied. State-driven failed sectors remain optically open. Core tests verify contact
compression, release/relaxation, local injury response, bounded area and moving contact
coordinates; a PlayMode test reads actual rendered alpha at old/new boundary locations.

The mechanics are motivated by the separate roles of cortex stress and pressure described
in [Cellular Pressure and Volume Regulation and Implications for Cell Mechanics](https://pmc.ncbi.nlm.nih.gov/articles/PMC3736675/),
but do not implement or claim to reproduce that paper's quantitative model.
