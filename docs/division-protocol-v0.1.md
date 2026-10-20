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

## Opt-in Unity daughter-spawn assay (2026-10-19)

`ChemistryDemoBootstrap.enableExperimentalDivision` defaults to false.
When explicitly enabled before Build, `ExperimentalDivisionDemo` watches
the named Grazer for sufficient structural biomass and energy, then attempts
exactly **one** reproduction. It uses the existing cell constructor to
instantiate a daughter, applies copied quantitative genes and previewed
regulatory genes, initializes the daughter with the daughter-side energy,
nutrient and structural biomass shares, and assigns the remaining shares
to the parent. Ancestor registration and mutation commitment happen
after the prepared factory returns success. A distinct chemical-event
organism ID is used rather than reusing the lineage record ID.

A Play Mode regression test is authored to force a resource-eligible
parent, invoke the opt-in path, confirm a daughter object exists and
assert the combined intracellular biomass, nutrient reserve and energy
are unchanged at birth. **Test execution has not been verified.**

Limitations are material: this is a prototype **daughter spawning**
mechanic, not simulated cytokinesis. It creates the daughter using the
existing component builder, not a progressive pinching/splitting membrane;
the parent and daughter do not go through a replicated DNA/material
segregation animation; collision-free placement is only a heuristic;
toxin/secretory reserves are not partitioned, and creating a
secretor-class daughter would provision its default toxin inventory
without accounting for the parent's reserve. The assay currently
uses the nonsecretory Grazer only. Internal organelle inventory is not
conserved. A failure during/after activation needs full explicit
resource rollback and object cleanup, which this simple factory does
not yet guarantee. A sustained population/evolution assay is **not**
enabled: only one birth is permitted. A production version needs a
proper serializable transaction object and shared authoritative module
inventory before reproduction becomes general gameplay.

Substrate: Emergence's supplied documents and build artifacts remain
visual/gameplay references, not a source-code basis for this factory.

## Heritable material pool accounting (2026-10-19)

`CellHeritableInventory` adds explicit nonnegative abstract material pools
for pseudo-genetic templates, vesicle contents/material and structural
functional modules. The opt-in demo assigns each newly built cell a baseline
inventory. Division now computes a 50/50 allocation for all three pools,
rejects insufficient inherited genetic material, transfers the parent's share
only after a daughter has been prepared, and restores the original parent
inventory on activation exceptions. NUnit conservation tests are authored.

These material budgets are **separate from `CellBiomassGrowth.structuralBiomass`**.
They are tracked as additional illustrative resources rather than proven
subcomponents of a closed total mass budget; this risks double counting
if combined naively. A subsequent systems migration must define one
authoritative accounting hierarchy for cellular dry mass.

A pseudo-genetic material share is **not** a duplicated chromosome. Actual
replication must acquire precursor molecules, spend energy, and produce
sufficient copies so both viable daughters inherit the genetic program.
Compartments remain fixed-count physical visualization objects and are
not physically segregated or generated from the new material pools.
This step advances explicit conservation contracts; it does NOT complete
the physical cell cycle. Unity tests and runtime visualization still await
a licensed runner.

## Finite-rate pseudo-template synthesis (2026-10-19)

`GeneticTemplateSynthesis2D` converts intracellular nutrient reserve to
an abstract genetic-template mass pool, spending metabolic energy at a
separate declared per-template cost and respecting a fixed synthesis rate.
Reproduction is now gated on reaching `ReplicationReady`. Finite
precursor and energy availability cap each synthesis step, and an
authored regression test checks nutrient+template material conservation.

**Important:** This models effective template *material accumulation*, not
literal DNA duplication, base-by-base fidelity, chromosome segregation,
or complete synthesis of organelles. The initial experimental founder
already starts with enough genetic material for 50/50 partition; the
readiness gate can therefore initially be satisfied without additional
synthesis. A future replication state machine must track two distinct
heritable copies and require completion of replication before division.

Energy is a separate budget and not a physical mass account. Synthesis
does not model thermodynamic conversion efficiency. This code has not
yet passed the Unity test runner because repository Unity activation
remains unconfigured.

## Copy-completion gate (2026-10-19)

The founder's pre-existing 0.06-unit genetic-template inventory now
represents **one effective genome equivalent**. `GeneticTemplateSynthesis2D`
requires an additional 0.06 equivalent synthesized from finite nutrient
reserve at explicit energy cost, before the experimental birth gate is
satisfied. The synthesis target defaults to 0.12 and
`CompletedCopyCount` reports two only after the required new synthesis,
not merely because the starting resource pool exceeds the minimum.
After a 50/50 division, the parent and daughter have one equivalent
each; each new cycle would need replication anew.

The opt-in reproduction Play Mode test explicitly synthesizes the second
equivalent and verifies readiness before proceeding. Unity test execution
remains blocked on the configured CI licensing.

**Limitations:** these are numerical genome equivalents, not independent
DNA sequences, strand-specific replication, or physically segregated
chromosomes. The mutation scheduler currently creates a variant at birth,
not at individual pseudo-base synthesis events. Actual two-copy genetic
objects and segregation dynamics are still future work.

## Spatial two-copy checkpoint (2026-10-19)

`GenomeCopySegregation2D` introduces two separately positioned **effective
genome-equivalent markers**. The second becomes present only when
`GeneticTemplateSynthesis2D.ReplicationReady` is reached; their
opposing local-frame positions separate progressively within the membrane.
The experimental division gate now requires this segregation stage to
finish before birth, and a Play Mode test checks copy separation.

`GenomeCopiesRenderer2D` creates original procedural glowing markers
following these positions in the microscope. These are deliberately
kinematic markers, not actual chromosomes, independent heritable sequence
objects, or collision-constrained internal organelles. Genetic material
mass remains tracked by `CellHeritableInventory`. Membrane scission,
mechanistic chromosome partitioning, and fully validated mass accounting
remain unimplemented. Unity compilation and scene rendering remain untested.

## Ordered constriction checkpoint (2026-10-19)

The previous implementation allowed `ExperimentalConstrictionCycle2D` to
advance from biomass and energy alone, in parallel with genetic copying
and segregation. This permitted visible narrowing before the genetic
contents were prepared for separation.

The experimental constriction driver now also requires
`GenomeCopySegregation2D.Complete`. With insufficient genetic templates
or incomplete marker segregation it cannot advance. If readiness is
subsequently lost, constriction relaxes toward zero, as before.
The existing Play Mode reproduction assay now verifies that a fully
resourced parent cannot start constriction prior to segregation, and
then checks normal advancement after segregation is complete.

This ordering is a **simulation design contract**, not a universal
biological claim: real bacteria and eukaryotic cells have diverse,
partly overlapping cell-cycle mechanisms. The segregation stage is
still kinematic, and constriction remains an effective 2D actuator.
Neither topology scission nor mechanistic genome partition is resolved.
Unity compilation/test execution is pending an activated runner.

## Two-body geometry handoff (2026-10-19)

`DivisionGeometryHandoff2D` now positions independently initialized parent
and daughter `SoftCellBody2D` solvers according to their measured
biomass-dependent effective radii. It enforces nonoverlapping nominal
bounding circles and zeroes the predivision constriction variable on
both bodies. The parent position and constriction are restored if
activation fails. The Play Mode reproduction assay includes a
minimum-separation and reset assertion.

**This remains a spawn-based geometric handoff, not mechanical scission**:
the parent's original closed contour is not cut and reconnected into
two loops. The parent solver's rest geometry relaxes toward reduced
postdivision biomass on subsequent ticks. The daughter has its own
initialized solver. No surface-area or membrane-material conservation
law is claimed. There is no conservation-guaranteeing mechanism for
all cell modules, and transient overlap of irregular deformed contours
is possible despite bounding-radius separation. Simulation-driven
lineage registration still follows successful handoff only.
Unity validation is still blocked by license setup.

## Deformed-contour handoff safety (2026-10-19)

The geometry handoff now scans all live `SoftCellBody2D` nodes
and measures the largest finite local radial extent. For each cell,
the conservative bound is the greater of its current
biomass-dependent nominal radius and the measured extent.
Parent and daughter are separated using the sum of these
envelopes plus a configurable clearance fraction. Invalid
(nonfinite) positions reject the transaction before
transform positions are changed. A Play Mode regression
asserts the post-handoff envelope clearance.

This is a circle-envelope *safety approximation*: it does
not detect intersections of irregular mesh edges, model
adhesion, calculate local membrane collision impulses,
partition actual membrane material or enact topological
scission. Live Unity verification remains pending activation.

## Explicit membrane-material budget (2026-10-19)

`CellMembraneMaterial` now holds an effective finite material inventory
separate from membrane geometry and membrane integrity. Experimental
reproduction requires enough material for two viable shares, assigns
one share to the staged daughter, subtracts it from the parent on the
successful handoff path, and restores parent material if activation
throws. A Play Mode test checks the conserved summed inventory.

This is a **bookkeeping prerequisite**, not physical membrane scission:
there is no tracked lipid/protein composition, local surface density,
material transport to the furrow, area-to-mass calibration, or direct
conversion of one closed membrane into two. The effective inventory is
currently also separate from the authoritative structural biomass, so
whole-cell material accounting remains incomplete. No Unity test
execution has been verified due to CI licensing.

## Observed membrane neck validation (2026-10-19)

`MembraneNeckDiagnostic2D` measures the equatorial node-pair separation
relative to upper/lower membrane shoulder widths, rejects nonfinite
coordinates, near-zero area, and detected self-intersections. The
experimental birth gate now requires an observed narrow, valid neck
(default equator/shoulder ratio <= 0.70) in addition to completed
constriction progress. This prevents a completed timer from being
mistaken for a physically successful neck.

The existing Play Mode resource-transaction test disables only the
neck gate after explicitly checking that timer-only readiness fails;
a separate Editor test confirms that a normal unpinched membrane does
not qualify. Threshold calibration, positive constriction cases,
numerical robustness, and real scene throughput must be validated
in Unity before accepting the gate as a reliable reproduction
criterion. In particular the 32-node star-shaped contour may not
generate a viable narrow neck; the diagnostic intentionally permits
division to stall rather than invent membrane scission.

**No topological scission implemented.** Even a successful narrow-neck
measurement only indicates shape readiness for an eventual topology
operation. Unity tests have not been executed.

## Independent membrane-neck geometry fixtures (2026-10-19)

The live-body diagnostic now delegates to
`MembraneNeckDiagnostic2D.MeasureContour(Vector2[])`, allowing direct
tests of the geometric classifier without a Unity physics timestep.
Authored Edit Mode fixtures include a unit circular membrane (not
constricted), a synthetic equatorially pinched simple contour
(constricted), and invalid nonfinite node positions (rejected).

The classifier's shoulder/equator metric is deliberately a 32-sector
effective proxy. Its ability to identify a synthetic narrowed contour
**does not demonstrate** that the existing physical soft-body will
produce that contour. That is a separate calibration/solver validation
gate. No GitHub Actions Unity test execution is verified.
