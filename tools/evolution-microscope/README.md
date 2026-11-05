# Evolution Microscope — Small Visual Scientific Track

## Purpose
A lightweight symbolic visualization of population changes, intentionally separate from both the Unity physical microscope and the headless game-theoretic research experiments.

## Current prototype
Open `tools/evolution-microscope/index.html` in a modern browser. No dependencies or server are required. It contains a seeded illustrative **toy simulation** that produces actual local simulation events (uptake, reproduction, mutation and death), step and play controls, agent circles with trait-coded hues, point nutrients, a population/trait trend plot, and an event log. Symbols and motions are intentionally low-detail.

This prototype has **not been tested in a real browser** in this development session. It is source-committed for subsequent manual testing. It is not a scientifically calibrated or resource-conserving organism model and is not linked to the Unity physics system.

### Mechanics (illustrative, not empirically calibrated)
- One local deterministic pseudo-random stream with fixed seed 67.
- Every tick adds a small number of nutrient symbols up to a world cap.
- Agent agents move toward the nearest visible nutrient, consume it within a fixed radius, pay basal energy costs, and gain energy scaled by an inherited uptake trait.
- Reproduction requires energy above a threshold; offspring inherit the parent's uptake trait plus bounded variation. Parent loses energy and offspring starts with a fixed energy endowment.
- Individuals die upon energy exhaustion or maximum age.
- The population and mean inherited trait are plotted as separate scaled traces; changes in trait frequency can thus be inspected symbolically.

### Scientific and engineering caveats
These are **pedagogical** dynamics, not verified biological dynamics. In particular, nutrients spontaneously enter the world, offspring receive energy without exact mass accounting, uptake has no calibrated units, mutation uses illustrative parameters, inheritance is one scalar and motion is idealized (nearest-nutrient sensing without occlusion). Neither energy nor matter is strictly conserved.

As a result, any population adaptation on screen is an output of the toy selection assumptions and cannot be cited as biological evolution evidence without a stronger, parameterized research model and statistical controls.

## Research-grade next gate
1. Define versioned event schema `cells.trace.v1` with tick, agent ID, parent, action, exact resource transfers, energy before/after, genotype, phenotype and RNG-stream provenance.
2. Implement a **headless** ecological evolution model with finite nutrient, biomass and waste pools and mass/energy conservation checks. Calibrate uptake/maintenance parameter ranges from sources, mark uncertain terms explicitly.
3. Record deterministic traces and verify exact replay and conservation with tests.
4. Feed the exact same logs into the minimal canvas renderer: graphical animation may interpolate only between observed states, never generate events.
5. Add genealogy overlays, trait-frequency charts, extinction/recolonization and lineage selection tests, then compare genetic and learned behaviors under controlled agent interactions.
6. Integrate the validated model through a Unity adapter only after headless parity checks; don't duplicate simulation logic inside the view.

## Division of work
- **Course now:** game-theoretic evaluation and final experimental deliverables, see [progress report](../../docs/CSCE631_PROGRESS_REPORT_DRAFT.md).
- **Long-term experimental science:** heritable variation, selection, calibrated physical accounting and genetic–strategic coevolution.
- **Presentation:** minimal symbols, movement and event traces driven by actual state transitions.

The visual prototype's local model does **not** claim to implement the course commons experiment.
