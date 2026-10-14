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
reserve and supplies energy with a configured yield. Digestion removes represented food
mass; catabolic products are not yet modeled. Particulate food uses a different species ID
and is not double counted as dissolved food. Toxin secretion transfers a finite internal
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
simulate intracellular concentration, cytosolic leakage or osmosis. The dissolved grid
does not treat the cell interior as an impermeable obstacle. All contacts snapshot field
concentrations before withdrawals; severe depletion of a shared grid voxel is resolved
in region order, so normal small fixed time steps should be retained.

Repair spends energy proportional to the mean integrity restored, distributing it
across local deficits without changing healthy regions. Rupture is not repairable.
Explicitly assigning the legacy integrity scalar resets all regions uniformly for
scenario compatibility; it is not a resurrection mechanism.
Events identify absorption, secretion, injury, toxin-associated death, and pipette additions.
Basal starvation is visible in physiological state but does not yet create a causal log event.
Reaction extent is exposed as aggregate reacted mass rather than per-voxel event spam.

## Rendering and reproducibility

Point-filtered concentration textures reflect actual field values. Particle sprites use
simulated positions, masses and radii; visual caps do not remove simulation particles.
Runtime sprites/textures are disposed with their owner. A fixed particle seed supports
repeatable trajectories with the same time steps; whole-scene cross-platform bitwise
determinism is not claimed. A separate point-filtered membrane texture draws each region
from its actual integrity and leaves transparent openings for failed regions. It does
not move or scale the organism: rendering cannot change contact sampling geometry.
The opening is a state-driven visualization, not a mesh or fluid-flow aperture.
