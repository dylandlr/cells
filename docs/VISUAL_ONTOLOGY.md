# cells — visual language and augmented microscope

## Non-negotiable separation

**World data**: simulation-owned concentrations, particles, states, anatomy, mutations, interactions and events.

**Agent perception**: local, noisy receptor measurements accessible only if genetically/strategically enabled. Never expose the human labels/icons/semantic tags directly to agent controllers.

**Human interpretation**: optional overlays, annotations, iconography, taxonomy, material readouts, predictions and event replay. Predictions must carry a confidence/uncertainty marker and target-cell specificity.

## Substance ontology

Each substance has an immutable ID, category, phase/form, physical transport properties, biological interaction rules, sensory signatures, render signature, and inspector metadata.

The UI must never infer toxicity solely from color. Toxicity is **relative to target cell phenotype and exposure**. A skull appears only when there is a known lethal interaction for the current target, or as a categorized general hazard marker with an explicit scope.

Color communicates broad classes, while shape, motion, boundary, texture, stippling and optional icon distinguish individual compounds, including color-blind modes.

| Class | Starting hue | Secondary cue |
|---|---|---|
| Nutrient | yellow-green | uptake streaks, soft granules |
| Toxin | violet-magenta | jagged micro-motes, contained skull glyph where warranted |
| Signal | blue-cyan | sparse ring wavefronts |
| Metabolic waste | ochre | amorphous settling flecks |
| Corrosive / reactive | vermilion | sharp edge flicker |
| Inert / mineral | silvery neutral | rigid angular grains |

**States/forms** (not all map to a pure thermodynamic phase): dissolved, suspension, colloid, droplet, vesicle-bound, gel, crystal, gas bubble, membrane-bound. Inspectors distinguish material phase from transport morphology.

## Zoom tiers

- **Ecology**: aggregates and fluid/cloud shapes; sparse hazard badges attached to cloud centroids, never repeated for every particle.
- **Organism**: particles, feeding structures, secretion pores, cilia and sensor patches. Hover/selection exposes classification.
- **Microscope**: organelles, vesicle content, local membrane machinery, interactions and substructure.
- **Inspector**: chemical identifier, phase/form, transport parameters, target-specific susceptibility, producer, physiological impact, sensor readings and causal history.

Each tier has stable on-screen minimum legibility, declutter budget and user-selectable density. Preserve the substrate and motion underneath UI.

## Three-way representation requirement

Every material interaction should be queryable as (1) **physical process**, (2) **human interpretive annotation**, and (3) **causal event evidence**.

For poison secretion: inherited variant -> localized anatomical synthesis/storage -> secretion pore event -> world field/particles -> exposure via contact/ingestion -> measured physiological stress -> outcome.

Predictions are evaluated from simulated effects and exposure, not invented by captions. The viewer may know more than an organism but should not get certainty where simulation data cannot support it.

## Acceptance examples

1. Distinguish two poisons of the same hue by shapes/velocity profiles, icons, hover and inspector.
2. Indicate toxin as lethal to one phenotype but tolerated/metabolized by another.
3. Show material form and functional role separately.
4. Trace a cell death backward to dose, exposed organ, substance source and inherited resistance.
5. In an agent observation dump, no UI-only compound name, risk icon or global chemistry metadata is present.
6. Overlay off: membrane, intake, plume and deformation remain materially observable.
