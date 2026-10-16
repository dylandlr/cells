# cells

A Unity microscopic chemistry and physiology sandbox with a human-facing causal microscope.

## Run

Open this folder in **Unity 6000.6.5f1 (Unity 6.6)**, open
`Assets/CellsDemo/Scenes/MicroscopeCulture.unity`, and press Play. If the scene is absent,
use **Cells > Create or open microscope culture**. This denser, adherent culture uses
phase-contrast-inspired grayscale rendering, irregular contours and varied static
intracellular texture. Every visible cell has metabolism, exchange and leakage.
Local boundaries deform under contact and injury and relax when released. The centers
remain anchored; there is no synchronized beating or scripted pulsing.

The smaller two-organism experiment remains available in `ChemistryDemo.unity` and
**Cells > Create or open chemistry demo**. Windows builds start in the culture view.

- Click a cell or its named button to inspect energy, membrane integrity, and recent events.
- Scroll over the world to zoom; F follows selection; Escape clears it.
- Space or Pause stops simulation while inspection remains usable.
- Food, Toxin, and Reagent buttons deposit measured quantities at the selected membrane region (or origin).
- The region slider moves the pipette around the boundary; inspection reports local concentration, mean integrity and weakest-region integrity.
- Repair spends the selected living cell's energy. A ruptured membrane cannot be repaired.
- Toggle **Show false-color chemical concentrations** to inspect the underlying field.
  It is hidden initially in the culture view; chemistry continues running.

## What is simulated

Independent dissolved species diffuse with no-flux boundaries. Field storage is mass per
voxel; microscope queries report mass per local area. First-order decay is explicitly
accounted for. An illustrative local reaction, toxin + reagent -> neutralization product,
conserves equal-mass reactants and creates their combined product mass. Suspended food is
tracked separately as seeded Brownian particles; ingestion removes actual packets.
Feeding moves material into an internal reserve; digestion releases a separate metabolic
byproduct before supplying energy. Damaged membranes leak nutrient mass back into the field,
including after death. Secretion costs, concentration-dependent exposure, local injury,
rupture and energy depletion change authoritative simulation state.

These are phenomenological models in arbitrary units, not calibrated molecular chemistry
or validated biological toxicology. Cell centers remain stationary in this demo. Membrane injury is
spatially sampled across 32 regions with state-driven visible openings; fluid flow through
pores, full mechanical tearing, osmosis, evolution, and learned behavior remain future work.
Nutrient leakage uses a well-mixed internal compartment and conservative surface transfers. Human substance names and colors are not organism sensor inputs.

## Coupled flagellar mobility (experimental)

Free-swimming organisms use a symmetric planar 3-DOF resistance matrix,
derived from the current local segment geometry, to jointly solve
translation (x/y) and rotation about z. A Cholesky-based solver rejects
ill-conditioned or non-positive-definite matrices. Velocity and yaw remain
bounded, with force/torque diagnostics tested independently from Unity.

This model is a local resistive-force approximation, not a validated
low-Reynolds-number Navier–Stokes / Stokes boundary solver. It does not
currently account for near-wall hydrodynamic corrections, full
body/appendage hydrodynamic interactions, or exact reciprocal coupling
to the rendered fluid field. The same simulated organism may be steered
by a separate chemical policy; its policy yaw is not a hydrodynamic torque.

Unity Editor tests for the new solver have been authored but have not
been executed in this environment. Run the Unity Test Runner before
accepting the physics increment.

## Provenance and validation

Continues `feat/substance-ontology`, PR #1. The separately described **Causal Microscope
v0.3 ZIP was not available in this workspace or the inspected local project/download
locations**; this project develops the repository's existing code, and does not claim
that archive was migrated.

See [integration and test results](docs/INTEGRATION_STATUS.md),
[model units](docs/CHEMISTRY_MODEL.md), and [test instructions](docs/UNITY_CI_SETUP.md).
Actual Unity test reports and runtime imagery, if produced, are distinguished from mockups.
The optional [camera recording workflow](tools/capture/README.md) produces real Unity
frames with measured-state captions. It excludes the IMGUI panel and adds no beating.
