# Evolution and taxonomy foundation v0.1

## Status
The Unity runtime now has a quantitative OrganismGenome, OrganismPhenotype
component, in-memory LineageRegistry, and authored NUnit tests. The microscope
bootstrap initializes founder-like phenotypes. **No reproduction, mutation
events during play, live population registry integration, persistence, or
automatic taxonomic discovery is implemented yet.** Unity compilation and
tests have not been executed in this environment.

## Scientific design rules
- Evolution is a change in heritable characteristics of populations across
  generations, not an individual cell leveling up.
- An existing cell is already a microorganism. Complexity is not a mandatory
  progression ladder; unicellular lineages can remain successful.
- Each specimen has a distinct individual ID; each recorded descendant stores
  its parent, generation, original lineage ID and an immutable *copy* of its
  genome at registration.
- Mutation is stochastic, seeded and bounded by parameter domains. The current
  log-normal perturbation is a **toy quantitative-genetics abstraction**,
  not a biochemical gene regulatory network or nucleotide-level genome.
- Mutation does **not** automatically cause a new species. Taxonomic labels
  remain provisional annotations, explicitly assignable to existing lineages.
  Automated speciation would require a justified divergence criterion,
  which differs for asexual and sexual organisms.
- Label examples such as *Cellaria prima* are fictional simulator names,
  not scientifically described taxa under a nomenclatural code.

## Trait contract
1. assimilationRate -> CellBiomassGrowth.assimilationRate
2. feedingRate -> CellChemicalExchange.feedingRate
3. growthEnergyPerMass -> CellBiomassGrowth.growthEnergyPerMass
4. maximumBiomass -> CellBiomassGrowth.maximumBiomass
5. sensorGain -> CellMotility.attractantGain (effective gain, uncalibrated)
6. motorInvestment -> CellFlagellarActuator.motorActivation (bounded effective activation)

## Roadmap and acceptance criteria
1. Reproduction: gated by biomass, replication progress, geometry and nutrient
   balance; zero extra mass or energy introduced by parent/daughter split.
2. Integrate LineageRegistry with actual organism spawning and lifecycle,
   and save/replay of parent-child event records.
3. Evidence-based classification: separate morphological/ecological tags,
   simulation clade labels, and species hypotheses; log all naming decisions.
4. Implement explicit population branching and optional inherited change of
   developmental/structural modules rather than a fixed complexity tier.
5. Challenge evolving phenotypes in identical seeded environments; export
   survival, lineage frequencies, population diversity and selection trends.
6. Build a phylogeny UI from actual recorded parent links, with extinct
   branches retained for analysis.

## References and provenance
Source architecture follows the scientific focus in:
- Taheri-Araghi et al. 2015, *Cell-Size Control and Homeostasis in Bacteria*,
  https://doi.org/10.1016/j.cub.2014.12.009
- Harris & Theriot 2016, *Relative Rates of Surface and Volume Synthesis
  Set Bacterial Cell Size*, https://doi.org/10.1016/j.cell.2016.01.025

The user-supplied *Substrate: Emergence* README and Unity compiled artifacts
are gameplay and visual comparisons, **not** the original Unity source or a
basis for claiming that our taxonomy/genetics algorithm came from Substrate.
