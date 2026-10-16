# cells — Game-Theoretic Experiment Protocol v0.1

Status: **implemented headless game and editor tests; Unity execution not yet verified**.
Branch: `feat/substance-ontology`.
This is a deliberately simplified **experimental game**, not a validated single-cell model, not a substitute for the physical chemical field, and not evidence of emergent evolution.

## Scientific question

Does allowing costly, imperfect, public chemical-like signaling change collective harvest, individual payoff, and distribution of rewards in a four-player resource-competition game with partial observation?

## Versioned specification

Environment ID: `cells.signaling-game.v1`.
4 agents; 3 nutrient patches; 80 **simultaneous** decisions per episode; random seed controls initial resource quality and exogenous resource-quality drift. Hidden yield per patch is initially uniformly sampled from {1,2,3,4}; each patch refreshes with probability .08 after a step. Each cell observes its own energy, current patch, a coarse cue of quality at its own patch only (1 if yield >= 3, else 0), and counts of **unverified public signals** from the previous step. Remote cues are -1 (unknown).

Discrete actions: 0..2 forage at patch, 3..5 publicly signal a patch (without harvesting), 6 rest. Action abstraction is intentional: the Unity physical layer does not yet implement the signaling mechanism. Sending a signal costs .4 energy. Harvesting costs .15, changing patch adds .2; harvest at a patch is divided by the number of foragers selecting that patch on that decision. Rest grants .1. Invalid action is penalized -1. All actions apply simultaneously; agents never inspect each other's current choice before acting. Payoff is cumulative change in energy, not a biological fitness estimate.

## Baselines now available

- `RandomPolicy`: uniform random decisions across seven actions.
- `SilentGreedy`: use local coarse cue; default to current patch.
- `SignalFollower`: value previous-step signals alongside local cues; does not broadcast.
- `PeriodicSignaler`: occasionally broadcast local high cue, otherwise forage.

These are **engineering baselines**, not trained agents. In particular a team of SignalFollowers sends no signals; evaluate mixed profiles and report exact roles and positions. The signal content is a nominated patch, and may be truthful, mistaken, or strategically dishonest. No signaling truthfulness guarantee is implemented.

## Pre-registered evaluation design

1. Run paired seeds, initially at least 100 independent seeds, with **agent-seat rotations** and identical seed lists for every profile. Vary sender frequency and number of senders: 0, 1, 2, 3, 4. Report mean, median, sample standard deviation, bootstrap 95% interval, and per-seed paired differences in collective payoff and harvest.
2. Record **individual returns** and inequality (e.g., Gini), signal budget, foraging efficiency, concentration at shared patches, and fraction of agents with negative accumulated payoff. Report resource quality distribution as an exogenous covariate.
3. Main ablations: no signaling; zero signal cost; increased signaling cost; noise/dropout; unverifiable or dishonest signals; longer latency; no drift; increased scarcity; and removed shared-resource competition. These are proposed additions, not features currently available.
4. Explicit policy learning and equilibrium analysis are **future stages**. Freeze a strategy set before measuring empirical payoff matrices. Obtain payoff tensors via seeded repeated profiles, compute uncertainty, test approximate best responses and epsilon-Nash regret (or correlated deviation incentives under a correlation device), and avoid claiming equilibrium from raw wins.
5. **Paired within-seed testing**, effect sizes, and bootstrap confidence intervals precede p-values. Use a preregistered primary endpoint to prevent metric fishing. Multi-run strategies require separately seeded policy randomness.
6. Never equate higher collective payoff with evolved cooperation. A communication effect only supports a causal statement under the experiment's controlled assumptions. Test exploitation, deception, and inequality explicitly.

## Next implementation gates

- G1: Run editor tests in Unity 6.6 on Windows, diagnose compile/assembly-definition compatibility.
- G2: Create a headless runner that exports raw per-episode/agent CSV and a manifest with source commit, parameters, RNG seeds, policy identities, and schema version.
- G3: Add a per-step event trace, observability masks, assertions against privileged global observations, and conservation/payoff accounting.
- G4: Add explicit cheap-talk/deception and chemical signaling through `MultiChemicalField2D`, tracking spatial diffusion, decay, detection threshold, secretion cost, and lag. Compare the abstract game against the physically coupled game.
- G5: Build empirical game-theoretic payoff tensor evaluation and approximate best-response / deviation-regret estimators.
- G6: Implement tabular bandit/EXP3, independent MLP and multi-agent RL baselines before attempting evolutionary genotype-policy coadaptation.

## Literature provenance

This design is **our proposed experimental extrapolation** drawing on classical concepts in Nash/correlated equilibrium, repeated stochastic games, regret minimization, empirical game-theoretic analysis, and action abstraction. Do not attribute its exact numbers, outcomes or implementation decisions to any Google Drive paper; the long-form Deep Research paper-by-paper report has not yet been incorporated/verified here.

## Implemented evaluation runner (research tooling, not completed results)

- Runtime: `Assets/CellsDemo/Scripts/Experiments/SignalingExperimentRunnerV1.cs`
- Unity Editor menu: **Cells → Research → Run Signaling Experiment (30 Seeds)**
- Tests: `Assets/CellsDemo/Tests/Editor/SignalingExperimentRunnerV1Tests.cs`
- Local export directory: `ExperimentRuns/signaling-v1/` (outside `Assets`); files: `agents.csv`, `episodes.csv`, `manifest.json`.
- Profiles: all silent, all random, and four seat rotations each of one periodic signaler + three followers and one silent control + three followers. The current runner uses **30 seeds and 10 profiles per seed = 300 episodes**, plus 1,200 per-agent records. Four seat rotations only apply to mixed profiles. `source_revision` defaults to `unverified` until the execution machine independently captures its Git HEAD.
- The runner records totals and per-agent returns; it does **not** yet export individual decision traces, raw hidden resource paths, variance/CIs, or a full empirical payoff tensor. The control profile differs from signaling only in one policy, so compare each matched `seed × rotation` pair; this is a policy intervention, not solely an isolated communication-channel ablation.
- **Before running:** open the Unity project, allow script import, run Unity Test Runner → EditMode and verify the `SignalingGameV1Tests` and `SignalingExperimentRunnerV1Tests` suites. The menu executes C# without Play Mode. These steps have not been verified here; generated results do not exist yet.
- The local `ExperimentRuns` directory contains generated research artifacts and should remain uncommitted unless explicitly curated as a dataset. Remove the folder or add it to `.gitignore` locally if needed.

### Interpretation guardrails

The present toy model neither conserves a depleting nutrient stock nor models spatial pheromone diffusion. Per-step patch yields regenerate as a payoff opportunity, so this **does not yet implement genuine cumulative scarcity**. Episode return equals net energy change, and initial energy is identical across agents. Therefore, payoff improvements do not establish biological survival, evolved cooperation, or Nash equilibrium. The paper-motivated next step is resource-stock accounting and physically costly signaling.

## Physical chemistry adapter milestone (added 2026-10-15)

The Unity microscope now contains a **separate preliminary physical signal mechanism**, not yet coupled to the above abstract game's action stream:

- `CellChemicalSignaling.Emit(mass)` debits available metabolic energy for the actual deposited dissolved mass. Rejected deposits cost no energy, and finite positive rates are required.
- `CellChemicalSignaling.ReadLocal()` and `ReadGradient()` read only local concentrations/gradients, avoiding omniscient signal broadcasts. Sensor thresholds and emission efficiencies are arbitrary test values, not measured cell biology.
- The demo registers `signal-S` with diffusion coefficient `.22` and decay rate `.13` in arbitrary simulation units, and displays it using a microscope false-color overlay.
- The demo Secretor emits periodic pulses; the Grazer has a passive detector. **No agent policy consumes those observations yet; there is no demonstrated learned communication.**
- The existing `MultiChemicalField2D` drives diffusion, advection, and decay. One must run Unity to validate ordering, transport mass budgets, signal availability at distance, and inspector visualization.
- `CellChemicalSignalingTests` cover mass-energy accounting, out-of-bounds rejection, unknown signals, affordable emission, diffusion detection, and emission bounds.

**Next scientific gate:** encode a policy-controllable action to emit a pulse (or stay silent), a receiver observation derived strictly from sensed spatial concentration (with receptor noise and thresholds), then compare explicit signaling enable/disable interventions under fixed matched seeds. A signaling molecule is not itself proof that meaningful information or cooperation has emerged.

## First matched-seed spatial chemistry assay (v1)

Source: `Assets/CellsDemo/Scripts/Experiments/PhysicalSignalAssayV1.cs`.
Unity Editor command: **Cells → Research → Run Physical Signal Assay (30 Paired Seeds)**.
Tests: `PhysicalSignalAssayV1Tests.cs`; generated dataset under `ExperimentRuns/physical-signal-v1/`.

This assay advances `MultiChemicalField2D` manually at 0.1 simulation units for 80 transport steps, with four cells and two arms (signaling disabled vs enabled) using identical patch placements for each seed. Only the source cell's policy changes between arms. Three receiver cells move based on locally sampled nutrient and (if present) signal gradients, harvest via `Withdraw` and gain energy via `Feed`. A locally fed source cell emits signal at an energy cost through `LocalSignalPolicyV1` and `CellChemicalSignaling`. Signal molecules diffuse and decay through the genuine field solver; emitted/remaining/decayed mass is reported for accounting. The Unity export generates 30 seeds × 2 arms × 4 agents = 240 agent records.

### Important scientific boundaries

- This assay uses **simplified explicit discrete movement**, not the `CellMotility` / `FluidGrid2D` motion model. It is **physics-coupled chemical transport** only in the qualified sense of simulated diffusion and decay; hydrodynamic coupling is switched off.
- The source is initially placed at the nutrient site to isolate signal transmission rather than discovery. Receivers do not see hidden patch locations, but their field-gradient controller is engineered, not trained.
- Source emission can reduce source utility; receivers may gain or lose individual utility. **No assumptions of improvement or cooperation are justified before observing results**.
- This assay does not model starvation mortality or reproduction meaningfully, enforce depletion in a well-mixed bulk environment, support LLMs/EXP3, or estimate an empirical Nash equilibrium.
- Signal diffusion, threshold, emission rate, nutrient uptake, energy conversion, discrete step length, and observation cadence are in **arbitrary units**. Their sensitivity must be assessed before interpreting findings.
- Testing status: committed source/tests; Unity compilation, execution, and exported CSVs are not yet independently verified.

### Acceptance gates

1. Execute EditMode tests, require the disabled arm to emit exactly zero signal and enabled arm to debit actual chemical emission.
2. Check per-run mass ledger `emitted ~= remaining + decayed`, assuming no external signal sinks and no nutrient/other chemical decay.
3. Run 30 seed pairs, publish raw data and paired `on - off` outcomes by seed. Report uncertainty; if signal does not reach receivers, diagnose geometry, sensor threshold, diffusion and timescale rather than tune to desired outcome.
4. Extend assay to controlled fluid advection and physical cell motility, then separate effects of signal production vs response via factorial interventions.

## Paired inference implementation (v0.1)

`PairedAssayAnalysisV1` computes **treatment-minus-control** differences from one independent observation per environment seed, not from individual agent rows. It reports paired mean difference, sample standard deviation, standard error, and 2.5th/97.5th percentile bootstrap bounds (4,000 resamples, deterministic bootstrap RNG seed). The percentile interval is exploratory and **not a proof of treatment effectiveness**; bias-corrected, studentized, and sensitivity analyses remain future work.

The Unity Editor physical assay export now includes:
- `agents.csv`: per-agent net energy, consumption, reception counts and episode-wide mass ledger.
- `paired_by_seed.csv`: source and receiver aggregate outcomes matched by seed, including treatment-control deltas.
- `paired_summary.csv`: seed-level effect estimates for collective energy, receivers' energy, source energy, harvest mass, and receiver detected steps.
- `manifest.json`: current fixed configuration and limitations.

### Critical interpretation note

Receiver detection count is an **intermediate mechanism check**, not an independent primary reward. In particular, an all-zero signal-detection count indicates a transport/sensing scale problem; it is not evidence that communication has no strategic value. The experiment compares a source that emits molecule pulses versus one that remains silent. This changes emission costs AND environmental concentration simultaneously. Subsequent factorial ablations should disable receptors, clamp source costs independently, and vary production/transport separately to identify mechanisms.

No Unity test results or statistically measured effect sizes have been recorded yet. Any claims about chemical communication improving utility must await compilation, real execution, diagnosis of signal reach, and predeclared control checks.

## EXP3 episode-level strategy learning v0.1 (implemented, unverified)

The abstract `SignalingGameV1` now has a separate **four-agent independent EXP3 training harness**:
- `Assets/CellsDemo/Scripts/Experiments/Exp3EpisodeSelectorV1.cs`: numerically stable log-weights, smoothed exploration probabilities `(1-gamma)*softmax(logWeights)+gamma/K`, single pending chosen strategy, and importance-weighted observed-reward update `logWeight[arm] += gamma * reward01 / (K * selectionProbability)`. Only the selected arm receives feedback.
- `Assets/CellsDemo/Scripts/Experiments/Exp3StrategyTrainingV1.cs`: four controllers select strategies at episode boundaries from `silent`, `follower`, `signaler`, and `random`; play full 80-step episodes, then update independently using their own realized episode return. Exogenous seeds and selection seeds are recorded separately.
- `Assets/CellsDemo/Editor/Exp3TrainingExportV1.cs`: Unity Editor menu **Cells → Research → Train EXP3 Strategies (300 Episodes)**; CSV exports under `ExperimentRuns/exp3-strategies-v1/`: `training_episodes.csv`, `final_policy.csv`, `manifest.json`. The source SHA is intentionally marked `unverified` unless supplied by the execution environment.
- `Assets/CellsDemo/Tests/Editor/Exp3StrategyTestsV1.cs`: 7 tests for initial probabilities, bounded feedback, exploration floors, update ordering, deterministic replay and validation.

**Reward contract.** In this version of the abstract game, per-step individual return is bounded by [-1,4]. Each 80-step realized episode utility is normalized by `(utility / horizon + 1) / 5` and must satisfy this documented bound; out-of-range inputs raise exceptions. This is a designed simulation-specific scaling, **not** a universal EXP3 reward transform. No per-step reward or unplayed-arm payoff is supplied to the selector.

**Scientific limits.** Strategies are frozen, and only their episode-selection probabilities learn. Four agents learn simultaneously, meaning each faces nonstationary opponents. Consequently, standard adversarial-bandit regret bounds cannot be interpreted as convergence to Nash equilibrium, and training CSVs are **not held-out evaluation**. `Exp3EpisodeSelectorV1` is an algorithmic baseline; it is not fitted to the physical-signaling assay and does not validate strategic emergence in physical chemistry. Run Unity EditMode tests before publishing results.

**Next gate**: add checkpoint/evaluation splits, frozen-policy rollouts under held-out seeds, paired fixed-strategy baselines and payoff tensors with uncertainty, followed by deliberate physical-environment adapter work.
