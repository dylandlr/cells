# v0.11 — Tracked suspended particles

Implemented initial individually tracked material packets with stable IDs, mass, location, radius, velocity, deterministic seeded stochastic kicks, imposed flow drift, bounded reflective domain, ingestion and world-coordinate rendering. Particulate ingestion removes mass before crediting metabolic energy.

**Critical physics distinction:** this is a lightweight overdamped/Langevin-like approximation with imposed uniform drift; no resolved Stokes flow, particle-particle collisions, buoyancy/sedimentation, deformation, adhesive membrane binding, or reaction kinetics. It should not be described as calibrated single-cell biophysics.

**Critical integration distinction:** TrackedParticleWorld represents a separate particulate reservoir from MultiChemicalField2D; particles are not counted in dissolved concentrations and must not be duplicated by depositing the same mass into both representations. ChemicalFieldRenderer is for continuous concentrations, TrackedParticleRenderer for discrete packets.

**Current tests:** two editor test cases committed but not run. The older runtime demo does not yet auto-create the particle world. Add it to scene bootstrap with a seeded particle patch, particle feeding site, and tracked renderer in the next integration pass.

**Next scientific gates:** mass accounting across phase transfer (dissolution/precipitation), spatial uptake flux, target-dependent particulate toxicity, particle-state inspector/causal provenance, actual local fluid velocity grid, and measurements at reproducible seeds.
