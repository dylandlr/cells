# v0.7 — Energy unification and membrane integrity

- CellChemicalExchange now uses CellMetabolism rather than independent metabolicEnergy.
- CellExposureSystem now damages MembraneIntegrity; membrane resistance reduces injury.
- Rupture triggers death through the authoritative metabolism component.
- Membrane repair can use energy to restore integrity.
- New edit-mode test checks resistance, integrity damage, repair, and energy cost.

## Important unresolved behavior
- NutrientReserve is still a storage pool, not yet converted via digestion into energy.
- Membrane integrity is a scalar approximation, not spatially resolved membrane mechanics.
- Death and damage logs should evolve toward explicit parent-event references and causality DAGs.
- Diffusion still lacks active fluid advection and chemistry reaction networks.
- Legacy Unity v0.3 scene has not been imported into GitHub; no Unity Editor test/compile or genuine video capture has been performed.

## Next acceptance gate
Create a playable integration scene binding a cell, membrane, metabolism, anatomy sites, and multiple chemical species. Then verify in Unity the chain secretion -> diffusion -> ingestion -> injury -> rupture/death and replay from event history.
