# v0.9 — Data-derived visualization

ChemicalFieldRenderer samples actual MultiChemicalField2D concentrations and displays their intensity as pixel-filtered alpha, without generating unrelated decorative particles.

MembraneStateRenderer tints/pulses cell sprites from the actual MembraneIntegrity scalar, rather than animating damage without a corresponding physiological state.

## Wiring

- Add ChemicalFieldRenderer to a GameObject with SpriteRenderer; assign a field, chemical ID, tint and opacity.
- Add MembraneStateRenderer to a cell's SpriteRenderer GameObject; assign MembraneIntegrity.
- Use separate sorted chemical renderers for each substance to distinguish classes.
- This is a renderer scaffold, not a finished microscopy particle/structural anatomy implementation.

## Limitations

Rendering uses a gridded heatmap, not individual advection-tracked particles or a cloud-bound skull annotation. The membrane remains a single scalar damage model, not mechanically segment-resolved. Realistic fluid flow, zoom-dependent detail and accessibility-preserving compound patterns are still required. No Unity Editor compilation or Play Mode footage is claimed.
