# v0.8 — chemistry integration demo wiring

Unity integration scaffold added for a standalone 2D demonstration:

1. Create a fresh 2D scene.
2. Create an empty GameObject and attach `ChemistryDemoBootstrap` and `ChemistryDemoView`.
3. Press Play. The bootstrap creates two cells, nutrient and toxin chemical fields, and a causal log.
4. Select either cell and scroll to inspect energy/membrane condition.
5. Use the Console/inspector to verify accumulation of source events.

**Important limitations**:
- The scene is assembled at runtime, not checked in as a Unity serialized scene asset.
- The baseline organisms are initially stationary; this is a chemistry fixture, not a locomotion demonstration.
- Field evolution and injury are not yet rendered as animated clouds or localized membrane segments. HUD is a temporary OnGUI debugging layer.
- Visualized cells are intentionally placeholders. The 75%-realism biological art target is not yet achieved.
- A poisoned grazer may not die deterministically with these parameters; performance and outcomes require Unity tests.
- The project is not yet Unity Editor validated, and the legacy v0.3 source still has not been migrated.
- SubstanceDefinition instances are runtime-created objects and require lifecycle cleanup when this prototype is finalized.

## Required next gates

Pin Unity Editor, migrate v0.3 source and settings, add chemical cloud rendering + membrane visualization, perform Unity Edit/Play Mode validation and capture real gameplay footage.
