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
reserve to the field and charges energy. Exposure removes local toxin in proportion to
concentration and uptake rate (area / time), then applies resistance-adjusted injury.
Absorbed toxin is no longer represented externally; intracellular clearance is not modeled.

Membrane integrity is bounded in [0,1]. Injury records actual integrity loss, not damage
beyond complete destruction. Rupture kills the cell, energy depletion also kills it,
and feeding does not revive it. Repair consumes energy and cannot repair rupture.
Events identify absorption, secretion, injury, toxin-associated death, and pipette additions.
Basal starvation is visible in physiological state but does not yet create a causal log event.
Reaction extent is exposed as aggregate reacted mass rather than per-voxel event spam.

## Rendering and reproducibility

Point-filtered concentration textures reflect actual field values. Particle sprites use
simulated positions, masses and radii; visual caps do not remove simulation particles.
Runtime sprites/textures are disposed with their owner. A fixed particle seed supports
repeatable trajectories with the same time steps; whole-scene cross-platform bitwise
determinism is not claimed. Injury tint/pulsing represents scalar state, not physical holes.
