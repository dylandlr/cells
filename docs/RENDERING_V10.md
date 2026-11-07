# v0.10 — Material-specific chemical rendering

Renderer now changes its microtexture with `MaterialForm`, and at closer camera zoom reveals its underlying deterministic pattern. This texture is multiplied by the simulated concentration, so no visible material is generated where concentration is absent.

Dissolved chemicals produce smooth opacity. Suspensions, crystals, droplets and vesicle-bound forms use contrasting deterministic forms. These are **illustrative rendering masks**, not proof of discrete microscale particle simulation, advection or molecular state.

Set the renderer's `form` from the same SubstanceDefinition associated with its `substanceId`. Currently assignment is manual; it must be connected to the registry and verified. This remains a prototype.

**Important**: grid pixels are not physical individual particles. At extreme zoom, use a physically backed sub-grid representation or explicitly label the visualization as concentration sampling. The fixed visual seed should not be confused with deterministic underlying world state.

Next: registry-driven form assignments, distinct shape atlas / icons, world-anchored annotation aggregation, inspectors and accessible visual contrast. Unity compile/Play Mode validation is still outstanding.
