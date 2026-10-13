# Unity migration and verification checklist

Existing generated prototype: **Causal Microscope v0.3** (standalone ZIP; not yet committed). Do not treat the repository's initial docs as an Editor-verified Unity game.

## Integration order

1. Import baseline Unity project (Assets, Packages, ProjectSettings) while preserving full file history.
2. Compile/test in a pinned Unity 6 version; replace placeholder editor revision metadata.
3. Fix runtime/compile issues before extending biological fidelity.
4. Introduce substance registry and target-dependent interaction rules.
5. Attach rendering presets to simulation substance IDs, never let appearance define physics.
6. Create inspectable causal provenance for production, transport, exposure, metabolism and damage.
7. Add microscope zoom LOD, highlight selection and UI decluttering.
8. Run calibrated low-Re locomotion tests and field conservation/mass-balance checks.
9. Record **actual Unity Game View** footage with time scale, build SHA and reproducible seed.

## Verification gates

- Agent sensors cannot access global registry metadata unless translated through sensor models.
- Two distinct compounds from one class are distinguishable without relying on hue alone.
- Semantic icons do not obscure organisms or appear per micro-particle at wide zoom.
- Mutation/production/death chain reconstructible from timestamped events.
- Editor compilation, Play Mode and visual contrast tests recorded separately from conceptual mockups.
