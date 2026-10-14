# Integration status — 2026-10-13

**GitHub repository**: `dylandlr/cells`.
**Development branch**: `feat/substance-ontology`.

## Implemented and committed in this branch

- ScriptableObject substance metadata and associated display signatures.
- Preliminary target-relative exposure calculation.
- Human hazard badge policy (including unknown state to avoid unwarranted skulls).
- Agent chemical observation structure without labels/icons.
- Stable-ID substance lookup.
- Design specification for zoom-based overlays and scientific causal UI.

## Legacy Unity source status

The v0.3 Unity Causal Microscope ZIP was inspected locally and contains the gameplay C# scripts. **Those scripts have not been committed to this branch yet.** These new files are a standalone code scaffold and are **not presently a runnable Unity project**.

## Known caveats

- Simplified exposure calculation predicts *energy depletion*, not validated biological lethality.
- Renderer still needs actual cloud geometry, spatial aggregation, and accessibility testing.
- Sensor code is a receptor projection starter, not an evolved receptor-network model.
- The existing scalar-field simulation has only one generic toxin channel; multi-chemical transport remains pending.
- No Unity Editor compile or playtest has occurred through this environment.

## Next integration ticket

Migrate the archived Unity Assets, Packages and ProjectSettings using an authenticated Git-enabled work environment, then compile, bind each substance ID to a transport field and source/sink event, and run reproducible multi-species experiments.
