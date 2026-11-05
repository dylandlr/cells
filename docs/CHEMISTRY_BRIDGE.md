# Chemical exchange bridge — implementation note

The chemical field and exchange bridge are independent from augmented microscopy. A configured organism stores material in reserves; the bridge withdraws nutrients at a feeding site and deposits toxins at a secretion pore. Each successful transfer emits a timestamped source event, including organism ID, material ID, amount and anatomy site.

**This is an initial functional bridge**, not a calibrated intracellular synthesis pathway. In particular, the current toxin reserve is supplied at setup; there is no reaction-network-based toxin synthesis. The energy cost is an abstract maintenance/secretion expenditure. It should not be interpreted as chemically accurate.

## Wiring in Unity

1. Place `MultiChemicalField2D` and `ChemicalEventLog` on scene GameObjects.
2. Create `SubstanceDefinition` assets with stable IDs `nutrient-A` and `toxin-B`; assign both to the field's species list.
3. Attach `CellChemicalExchange` to a cell, assign field and event log, and optionally assign child Transforms for `feedingPore` and `secretionPore`.
4. Seed the nutrient field via `Deposit` (e.g. from a world generator), enable Play Mode, and query the log to inspect transfers.
5. Rendering/hover/camera components should consume field samples and logged evidence but never write organism observations.

## Next

- Couple metabolic reserves to existing cell energy via a single authoritative metabolism service; do not maintain competing independent energy counters in production.
- Add receptor response to chemical gradients sampled by ID internally, but expose only limited measured activations to agent policies.
- Implement reaction/uptake/damage models with dose and phenotype-specific susceptibility.
- Port v0.3 Unity project files and run Unity Edit Mode/Play Mode tests; no runtime validation has been performed yet.
