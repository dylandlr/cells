# Division protocol v0.1 — conservative preparation

This is a **preparatory API, not live mitosis**. Authored in Unity C#;
Unity compilation, Edit Mode tests and Play Mode have not been run.

## Implemented
- `CellDivisionBudget.TryPartition`: validates positive finite pools,
  daughter fraction, and minimum structural biomass for both halves.
  Splits intracellular nutrient reserves, structural biomass and metabolic
  energy into paired shares with no arithmetic duplication.
- `DivisionLineageCoordinator.TryRecordDivision`: validates a division
  budget and existing parent record, invokes a caller-provided birth
  confirmation callback, then records one descendant and calls the
  existing seeded `GeneticMutationScheduler`. It disables the separate
  legacy quantitative-genome mutation for that operation to avoid two
  independent mutation chances.
- NUnit tests cover conservation, invalid inputs, failed confirmation
  without lineage entry, and successful lineage record with mutation.

## Important unresolved issue
The callback only confirms birth; it does NOT yet receive the prepared
daughter genotype or enact the parent's resource partition. It cannot
guarantee atomic rollback if a Unity GameObject factory partially spawns
and then fails. Therefore the coordinator is NOT sufficient for a
scientifically valid live reproduction implementation. Do not hook it
to automatic mitosis before designing a prepare/commit/rollback flow
that allocates the child genome before spawning but commits mutation
statistics only after successful adoption.

Still required:
1. Replication readiness and time costs, including energetic cost of copying
   pseudo-genetic information.
2. A **single owner** of Unity spawn and split commit, complete rollback
   semantics, and no double accounting for metabolic reserves.
3. Correct reinitialization of physics, organs, membrane patches, rotation,
   cytoplasm, motility and internal transport for both daughters.
4. Parent and daughter geometries that do not initially overlap; interaction
   with growing-cell contact solver.
5. Genotype copied or mutated before phenotype assembly and applied to
   both metabolic and regulatory modules.
6. Division-related birth/death telemetry, event log and save/replay.
7. Play Mode checks and mass balance under repeated division.

## Scientific and game design boundaries
In real cell division, DNA replication, segregation, envelope synthesis,
mechanical division, and growth-cycle control interact. This code is
only a macroscopic partition, not a mechanistic reproduction claim.
See Taheri-Araghi et al., 2015, Cell-Size Control and Homeostasis in
Bacteria, https://pubmed.ncbi.nlm.nih.gov/25544609/

The user-provided Substrate: Emergence README discusses nutrient use and
mitosis; it is a gameplay and visual design reference, not source code
for the implementation in this repository.

## Amendment: prepared inheritance transaction (2026-10-19)

New API `DivisionLineageCoordinator.TryPrepareAndRecordDivision`
calculates a conservative split, computes the daughter's **copied**
quantitative genome and proposed mutated regulatory genome, passes both to
the daughter factory for initialization, then adds the descendant and
commits the genetic mutation event *only after factory confirmation*.

`GeneticMutationScheduler.Preview` uses a seed derived from committed birth
count; rejected preparations don't consume future mutation opportunities.
`LineageRegistry.PeekNextIndividualId` supports pre-spawn identity assignment.
The prior `TryRecordDivision` is retained for compatibility but does not
provide these stronger genotype-before-spawn semantics. Prefer the new API.

**Still not a complete atomic transaction:** a Unity factory returning true
must have fully initialized a daughter and committed the parent resource
partition, while a factory returning false must roll back any partial
GameObject/resource changes. The coordinator does not undo Unity side effects.
A factory exception could also leave effects outside the registry. Real
replication costs, independent daughter cellular assemblies, collision-free
placement, and evidence from running tests remain outstanding. This new
interface is not wired to live Play Mode births yet.

This amendment adds NUnit coverage for a rejected prepared birth and retry
with identical inherited genes; the tests have not run in Unity.
