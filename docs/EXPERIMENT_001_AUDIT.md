# Experiment 001 — repository audit and scope freeze candidate

Date: 2026-10-20. Branch inspected: `feat/substance-ontology`.

## Scope decision

The progress-report critical path is **Experiment 001 only**. The smallest existing
candidate is the plain C# `SignalingGameV1` resource game, not the Unity microscope.
Do not make the membrane solver, Unity activation, genome compiler, division,
genetics, or spectator interface a dependency of Experiment 001.

**This is an audit and proposed freeze, not a claim that the experiment was run
or that the course proposal's complete protocol has been verified.**

## Already present

- `Assets/CellsDemo/Scripts/Experiments/SignalingGameV1.cs`:
  seed-reproducible four-seat, three-patch symbolic resource game, 80 decisions by
  default. Local cues, one-step public signals, competitive shared harvesting,
  explicit action costs and per-agent realized return.
- `SignalingExperimentRunnerV1.cs`: all-silent, all-random, and four seat rotations
  each of mixed-signaling/mixed-no-signal; fixed policies and paired environment seeds;
  agent CSV, episode CSV and JSON manifest produced in memory.
- `Exp3StrategyTrainingV1.cs`: episode-level bandit learning across frozen
  scripted strategies, with separate environment and selector RNG streams.
- `Exp3HeldOutEvaluationV1.cs`: out-of-training-seed evaluation of frozen EXP3
  distributions versus frozen-uniform and all-silent controls, with seed-paired
  comparisons and output strings.
- `PairedAssayAnalysisV1.cs`: seed-level paired mean effects and deterministic
  bootstrap percentile intervals.
- `Tests/Headless/Cells.HeadlessChecks.csproj` + `Program.cs`: compile and
  exercise these plain C# algorithms under .NET 8 without Unity.
- `.github/workflows/headless-research.yml`: runs those checks. Latest inspected
  workflow run on 2026-10-20 passed; this is **not** a completed research trial.

## Key mismatches and risks

1. **No modeled genome:** the current environment evaluates fixed scripted
   policies or frozen learned *strategy distributions*, not actual genetically
   encoded traits. Describe the operational freeze honestly, or align the
   research terminology with the proposal before freezing the protocol.
2. **Different experimental contrasts:** `SignalingExperimentRunnerV1`
   compares a periodic signaler with a silent agent among signal-followers;
   `Exp3HeldOutEvaluationV1` compares learned, uniform, and all-silent
   populations. Neither directly implements a single agent-matched
   communication-on/off toggle holding every policy and decision budget
   constant. Replacing the signaler with a silent forager also changes actions
   and opportunity costs.
3. **Frozen learned policy is episode-level:** EXP3 is trained before held-out
   evaluation. It is not within-episode adaptive policy learning. Treat training
   and evaluation as distinct phases and make the freeze point explicit.
4. **Analysis unit:** seeds are independent replication units; rotated seats
   within a seed are not additional independent samples. Use a seed-level
   aggregate before a paired bootstrap.
5. **Execution deliverable missing verification:** in-memory CSV and manifest
   exist, but this audit did not verify a clean-checkout one-command *study*
   runner that persists final raw results, analysis, schema, and source revision.
   The existing headless CI checks invariants, not a frozen experiment report.
6. **Physics relationship:** `SignalingGameV1` is expressly a toy symbolic
   resource game, not Unity chemical transport, cells' physical physiology,
   microbial evolution, or organism-scale communication.
7. **Research parameters not frozen:** primary contrast, communication semantics,
   fixed or learned conditions, seed set, training/test separation, sample count,
   utility definition, and stop rules need a signed-off protocol. Avoid post-hoc
   changing conditions after inspecting pilot results.

## Minimum credible Experiment 001 plan

1. Freeze a precise study protocol and hypothesis using the approved course
   proposal as authority. Default candidate: communication benefit of a
   predeclared fixed population in the symbolic four-player resource game;
   hold world seeds/initial conditions fixed across contrasts.
2. Use an explicit communication-channel switch or otherwise matched policy
   decisions with message delivery on/off (not just change a signaler into
   a different policy). Document how message costs are handled.
3. Fix policy configuration, seed manifest, horizon, action utility, and
   population seats. If learned strategies are included, train only on disjoint
   training seeds and freeze weights before held-out evaluation.
4. Implement a CLI research exporter for raw seed-level outcomes, paired
   contrast table, bootstrap interval, manifest including source revision,
   and a machine-checkable integrity summary.
5. Run a small smoke test and verify determinism, ablation isolation,
   resource/reward accounting, output cardinality, and positive/negative
   reporting. Then run the prespecified full study, publish results and
   limitations. Do not assert conclusions before execution.

## Hard exclusions before progress report

Unity scene/physics refinements, membrane division, genome compiler, genome
mutation and selection, population evolution, spectator visualization, and
new gameplay content are out of scope unless the core experiment is already
run and analyzed.
