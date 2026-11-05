# Exposure and metabolism integration — v0.6 prototype

New components:

- `CellMetabolism`: energy budget, upkeep, damage and terminal alive state.
- `CellExposureSystem`: configurable phenotype resistance and uptake rate for one substance.
- `MetabolismTests`: prototype accounting assertions.

For each organism, attach `CellMetabolism` and `CellExposureSystem`, and assign the same `MultiChemicalField2D` reference. One exposure component is presently required per monitored substance; later use a single multi-substance processor.

**Known integration limitation:** `CellChemicalExchange` v0.5 still contains its own `metabolicEnergy` and `nutrientReserve` fields. Do not run both as authoritative metabolism sources without refactoring. The new component is a step toward unification, not proof it has occurred.

**Known biological limitation:** contact dose is proportional to withdrawn material and predefined susceptibility. There are no receptors, reaction networks, organelle damage compartments or measured lethal doses yet. `HazardAnnotationPolicy` still uses a simplified energy-depletion model; do not claim an icon conveys experimentally validated biological lethality.

**Testing:** tests committed but not executed. Legacy v0.3 Unity project still requires import, compilation and Play Mode testing.
