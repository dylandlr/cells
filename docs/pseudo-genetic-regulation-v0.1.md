# Pseudo-genetic regulation implementation v0.1

Status: authored and committed; Unity compilation, Edit Mode tests and Play Mode
inspection **not yet executed**.

## Architecture
- `RegulatoryGenome` stores six heritable bounded signed weights/biases
  and a transcription-like response timescale.
- `RegulatoryNetwork` applies a bounded logistic response with first-order
  relaxation. Inputs are local nutrient concentration (clamped 0..1) and
  available energy fraction. Outputs are fractional motor and uptake activity.
- `GeneticRegulationController` is nucleus-*inspired* regulatory control. It
  reads only present cell/environment state, then modulates the existing
  `CellFlagellarActuator.motorActivation` and
  `CellChemicalExchange.feedingRate`. It never teleports cells, creates
  ATP or nutrient mass, or bypasses the motility and transport systems.
  It runs before most physics components via Unity execution order.
- `GeneticMutationScheduler` takes one mutation opportunity **per explicit
  replication invocation**. Seeded PRNG, 3% default event probability,
  one bounded regulatory weight/bias change if triggered, and event
  provenance (parent ID, descendant ID, generation, target, before/after).
  At 0% probability the child's network is an exact copy.
- The demo now instantiates an active regulatory controller for each cell,
  influencing real feeding and flagellar activation.

## Critical caveats
This is **not** a real nucleus, DNA or RNA chemistry model. The first
regulatory network is equivalent to a tiny heritable signed-weight graph.
Prokaryote-like organisms can have an analogous distributed regulatory
circuit without a membrane-bound nucleus. Complex nuclear organization,
RNA pools, promoter logic, gene duplication, compartmentation, replication
machinery, stochastic transcription, and mutation-class-specific rates are
future modules.

The demo has **no cell division pipeline** yet. Thus its active population
will not spontaneously mutate. The mutation scheduler is available to
future division/inheritance callers, and its tests use synthetic replication
invocations. `OrganismGenome.Inherit` is an older independent quantitative
mutation mechanism; these will need a unified reproduction contract rather
than double application of mutation probabilities.

### Research vs simulation parameters
The default 3% probability is an intentionally rare *per birth gameplay
event*, not a measured biological per-base mutation rate. The user-facing
'nucleus as brain' is an abstraction: real cellular decisions are distributed
among regulatory networks and biochemical motors. Mutation is *not* proof
that an organism or lineage could exist in real life.

## Required next gates
1. Edit Mode compile and tests: `GeneticRegulationTests`.
2. Controlled Play Mode A/B assay: regulator on/off, same environmental
   seed; compare uptake, motor duty, speed, energy and survival.
3. Time-step convergence and step-order check for regulatory output.
4. Integrate a material-conserving physical cell division system;
   connect lineage records and scheduler **only at valid division events**.
5. Make module graph extensible and permit duplication/deletion/reconnection
   with costs and mechanochemical constraints; persist and replay its changes.
6. Measure mutation counts per 1000 births and across various population
   sizes, then tune game-paced novelty without accidental hypermutation.

## Source references
- Purcell, 1977, *Life at Low Reynolds Number*,
  https://doi.org/10.1119/1.10903
- Berg & Brown, 1972, *Chemotaxis in Escherichia coli Analysed by
  Three-dimensional Tracking*, https://doi.org/10.1038/239500a0
- Tu, Shimizu & Berg, 2008, bacterial chemotactic adaptation,
  https://pmc.ncbi.nlm.nih.gov/articles/PMC2551628/

User-provided *Substrate: Emergence* README and compiled Unity files remain
visual/gameplay reference material, **not original editable Unity C# sources**.
These pseudo-genetic rules are our own experimental abstractions.
