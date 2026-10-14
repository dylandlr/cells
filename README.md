# cells

A Unity microscopic chemistry and physiology sandbox with a human-facing causal microscope.

## Run

Open this folder in **Unity 6000.6.5f1 (Unity 6.6)**, open
`Assets/CellsDemo/Scenes/ChemistryDemo.unity`, and press Play. If the scene is absent,
use **Cells > Create or open chemistry demo**. The scene creates its substrate,
two organisms, suspended food, camera, renderers, and microscope overlay at runtime.

- Click a cell or its named button to inspect energy, membrane integrity, and recent events.
- Scroll over the world to zoom; F follows selection; Escape clears it.
- Space or Pause stops simulation while inspection remains usable.
- Food, Toxin, and Reagent buttons deposit measured quantities at the selected cell (or origin).
- Repair spends the selected living cell's energy. A ruptured membrane cannot be repaired.

## What is simulated

Independent dissolved species diffuse with no-flux boundaries. Field storage is mass per
voxel; microscope queries report mass per local area. First-order decay is explicitly
accounted for. An illustrative local reaction, toxin + reagent -> neutralization product,
conserves equal-mass reactants and creates their combined product mass. Suspended food is
tracked separately as seeded Brownian particles; ingestion removes actual packets.
Feeding, secretion costs, concentration-dependent exposure, membrane injury, rupture,
and energy depletion change authoritative simulation state.

These are phenomenological models in arbitrary units, not calibrated molecular chemistry
or validated biological toxicology. Cells are stationary in this demo. Membrane injury is
a scalar; membrane pores, spatial tearing, fluid mechanics, evolution, and learned behavior
remain future work. Human substance names and colors are not organism sensor inputs.

## Provenance and validation

Continues `feat/substance-ontology`, PR #1. The separately described **Causal Microscope
v0.3 ZIP was not available in this workspace or the inspected local project/download
locations**; this project develops the repository's existing code, and does not claim
that archive was migrated.

See [integration and test results](docs/INTEGRATION_STATUS.md),
[model units](docs/CHEMISTRY_MODEL.md), and [test instructions](docs/UNITY_CI_SETUP.md).
Actual Unity test reports and runtime imagery, if produced, are distinguished from mockups.
