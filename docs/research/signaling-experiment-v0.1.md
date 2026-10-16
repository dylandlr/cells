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

## Held-out EXP3 evaluation (source implemented; Unity run unverified)

The Unity menu command **Cells → Research → Train EXP3 Strategies (300 Episodes)** now trains independent episode-level selectors, freezes their **per-seat probability vectors**, and then performs **100 held-out environment-seed evaluations** using seeds 100000–100099. The training environment seeds are 631–930. The evaluator verifies that seed intervals do not overlap and makes no updates on test seeds.

For each test seed, the same underlying environment and common seat-specific random quantiles are reused for:
- `learned_frozen`: each seat samples from its frozen learned policy probabilities;
- `uniform_frozen`: each seat samples uniformly from the same four scripted strategies;
- `all_silent`: each seat plays the fixed SilentGreedy strategy.

The outputs appear in `ExperimentRuns/exp3-strategies-v1/heldout/`: `episodes.csv` (one record per seed × condition), `agents.csv` (one record per agent), `paired_by_seed.csv` (matched raw collective returns), `paired_summary.csv` (learned-minus-uniform and learned-minus-silent seed-level mean effects, uncertainty estimates), and `manifest.json`. The primary comparison is **frozen EXP3 minus uniform**. Holdout data is not used for model selection.

**Methodological limits:** evaluation is for the *symbolic* signaling game, not spatial chemical motility. Training involves adapting opponents, so this does not prove EXP3 regret bounds or equilibrium convergence. Frozen policy distributions are sampled once per test episode. The common random number design may reduce variance but is not a causal intervention isolating signaling. Per-seat policies may exploit initial seat asymmetry; seat-rotation and opponent deviation tests are still future work. Source revision defaults to `unverified` on export. Automated EditMode tests must pass before any result is interpreted.

## Empirical game-theoretic restricted deviations v0.1

A new **restricted unilateral deviation study** complements EXP3 training and held-out evaluation.

- Source: `Assets/CellsDemo/Scripts/Experiments/RestrictedDeviationStudyV1.cs`
- Tests: `Assets/CellsDemo/Tests/Editor/RestrictedDeviationStudyV1Tests.cs`
- Menu: **Cells → Research → Evaluate Restricted Strategy Deviations (30 Seeds)**
- Export: `ExperimentRuns/restricted-deviations-v1/paired_samples.csv`, `deviation_estimates.csv`, `manifest.json`

There are four frozen policies (`silent`, `follower`, `signaler`, `random`). For each **homogeneous** resident profile, the evaluator substitutes one deviating agent at one seat with each of the three remaining policies and replays the same 30 environment seeds. This gives 4 resident policies × 3 deviation policies × 4 seats = **48 conditional comparisons**, 30 seed-paired observations each (**1,440 per-seed deviation samples**). It computes the changed agent's own payoff difference and a 95% percentile-bootstrap interval using seed-level paired differences. Raw rows also record collective payoffs before and after deviation.

For resident strategy `r`, deviation `d`, seat `i`, and random seed `s`, the estimated unilateral advantage is
`mean_s [ u_i(d, r_-i; s) - u_i(r, r_-i; s) ]`.

A positive estimated advantage suggests that the **homogeneous resident profile is exploitable within this restricted library** at that seat, subject to uncertainty. A nonpositive estimate does **not** establish Nash equilibrium: the action space is larger than four fixed scripts, environments have sampling variability, and only homogeneous resident profiles are evaluated. The full 4^4=256 pure profiles, learned-mixed-profile incentives, confidence-aware restricted best responses, spatial signaling chemistry, and approximate-equilibrium analysis remain follow-on work.

**Verification gate:** run EditMode tests and Unity menu export; inspect paired CSVs, verify 48 comparisons and 1,440 raw observations, and avoid selecting a "best" policy from the same test seeds used for statistical inference. No empirical study results have been executed or confirmed here.

## Full restricted empirical game (v0.1 source implementation)

The new `EmpiricalPayoffTensorV1` enumerates all **4^4 = 256** ordered joint strategy profiles of four agents using the frozen policy library `silent`, `follower`, `signaler`, `random`. It preserves individual seat asymmetries rather than averaging them away. Each profile is evaluated on the same 30 exogenous environment seeds (300000–300029), producing **7,680 intended episodes** and an empirical table of four mean individual payoffs per joint profile.

Unity Editor command: **Cells → Research → Evaluate Full Strategy Payoff Tensor (30 Seeds)**. Expected local outputs in `ExperimentRuns/payoff-tensor-v1/`:
- `tensor.csv`: 256 indexed profiles, their four policy indices and four mean payoff estimates.
- `learned_mixture.csv`: four seat-specific EXP3 frozen selection distributions, trained on 631–930.
- `learned_deviation.csv`, `uniform_deviation.csv`, `silent_deviation.csv`: expected utilities and best restricted unilateral deviation gains against the indicated independent mixed profiles.
- `manifest.json`: environment version, seeds, profile count, and analysis caveats.

**Analysis equation**: for independently mixed selection probabilities `pi_i(s_i)` and empirical payoff table `U_i(s_0,s_1,s_2,s_3)`, compute
`V_i(pi) = sum_s [prod_j pi_j(s_j)] U_i(s)`.
The restricted incentive to deviate to any single frozen strategy is
`epsilon_i^restricted(pi) = max(0, max_a V_i(a,pi_-i)-V_i(pi))`.
A small restricted epsilon on this four-strategy table does **not** establish full-game Nash equilibrium or convergence: off-library policy deviations are absent and payoff table means have statistical uncertainty. No payoff confidence bounds have yet been propagated through this contraction; these estimates must be labeled preliminary even after Unity executes. The EXP3 frozen mixture is *not* selected using tensor evaluation seeds.

**Test gates:** `EmpiricalPayoffTensorV1Tests` covers 256-profile bijection, table population, pure-profile consistency, nonnegative restricted gain, repeatability, and invalid mixture rejection. Run EditMode tests and inspect CSV output before interpreting any payoff or claiming observed emergence. The Unity runtime and experiments have not been run in this development environment.

## Independent verification infrastructure (v0.1)

We added `Tests/Headless/Cells.HeadlessChecks.csproj` and `Tests/Headless/Program.cs` linking the **actual research C# sources** into a .NET 8 console executable without Unity assembly dependencies. It checks deterministic seeded replays, EXP3 reward contracts, non-overlapping train/test seeds, bootstrap calculations, 256-profile encoding/decoding, restricted deviations and mixed-policy payoff consistency. This is *not* a mocked implementation or a claim of physical-model validity.

Run locally with .NET SDK 8:

```sh
dotnet build Tests/Headless/Cells.HeadlessChecks.csproj --configuration Release
dotnet run --project Tests/Headless/Cells.HeadlessChecks.csproj --configuration Release --no-build
```

GitHub Actions `.github/workflows/headless-research.yml` executes both commands on pull requests and changes to the headless research layer. This is independent of `.github/workflows/unity-tests.yml`, which requires an actual Unity installation and functioning license setup.

The presence of the workflow does **not** establish a passing status. The exact check-run status and logs must be inspected before claims of successful compilation. Even passing headless checks would **not** prove correct fluid projection, biological calibration, spatial transport, Unity importer behavior, or empirical cooperation.

### First CI verification evidence

The GitHub Actions run [38847159419](https://github.com/dylandlr/cells/actions/runs/38847159419) completed **successfully**: both `Build plain C# experiments` and `Run deterministic research invariants` steps passed. This confirms the headless research code compiles under .NET 8 and the executable checks ran. It does not constitute Unity PlayMode or EditMode coverage.

By contrast, Unity workflow run [38847163264](https://github.com/dylandlr/cells/actions/runs/38847163264) failed at `Verify Unity activation configuration exists`; the `Unity test suite` step was skipped in both EditMode and PlayMode. A supported GameCI Unity license or licensed runner must be configured before Unity C# compilation and biological model tests can be validated. No scientific results should be reported yet.

## Physical signaling factorial intervention (v0.1, source committed)

`PhysicalSignalAssayV1.Run(seed, produce, respond)` now supports a **2 × 2 factorial design**, keeping the original `Run(seed, produce)` overload equivalent to `respond=true`. Emission is controlled independently of whether receiver agents use the detected signal gradient in navigation. Critically, receivers can still **physically detect and record** the molecules when `respond=false`; this separates sensing from behavioral use.

Unity menu: **Cells → Research → Run Physical Signaling 2x2 Factorial (30 Seeds)**.
Export: `ExperimentRuns/physical-factorial-v1/agents.csv`, `paired_by_seed.csv`, `paired_summary.csv`, `manifest.json`. The run plan is 30 matched environment seeds × 4 interventions = **120 episodes**, **480 agent rows**.

Four experimental arms:
- `produce=0, respond=0`: neither molecule production nor behavioral following.
- `produce=0, respond=1`: placebo receptor-policy enabled, but no substance emitted.
- `produce=1, respond=0`: sender pays the secretion cost while receivers **cannot** act on the molecular information.
- `produce=1, respond=1`: production and active gradient-following jointly enabled.

Primary *mechanism-isolating* estimand is the receiver-energy difference-in-differences:
`[U_receiver(1,1)-U_receiver(1,0)] - [U_receiver(0,1)-U_receiver(0,0)]`.
The independent emission-only comparison `U_sender(1,0)-U_sender(0,0)` isolates the sender's metabolic cost when the receiving cells cannot respond. Each contrast is computed **within seed**, then summarized with percentile-bootstrap intervals. This is evidence about the toy assay under controlled assumptions, not biological cooperation or a learned equilibrium.

Added EditMode regression tests check no-production behavioral invariance, unchanged receiver returns under production-only treatment, source emission-energy accounting, reproducibility of all four arms, and backward-compatible API behavior. **Tests remain unexecuted until Unity licensing works.**

### Potential assay failure modes to investigate before reporting effects

1. A physically real but too-short-lived signal may never reach receivers; inspect nonzero `physical_detection_steps` and spatial separation before interpreting the interaction estimate.
2. Initial source placement directly at a nutrient patch is deliberately favorable. Follow-up seeds should vary nutrient geography and source discovery.
3. Receiver translation uses the simplified discrete gradient controller, not continuous hydrodynamic motility. Energy returns and food acquisition depend on the arbitrary grid/time discretization.
4. The assay has no molecular synthesis delays, reaction pathways, receptor desensitization, misinformation, or evolutionary fitness. These must not be inferred from a positive statistical estimate.
5. The Unity license-configuration check is blocking both EditMode and PlayMode CI; the independent .NET research checks passing cannot validate this physically coupled assay.

## Signal propagation observability and calibration probe

The physics-coupled `PhysicalSignalAssayV1` now captures additional mechanistic diagnostics: initial nutrient mass, remaining nutrient mass, per-receiver first detection step (or -1), peak local receptor concentration, and final source-to-receiver distance. The values come from the actual `MultiChemicalField2D` and locally queried `CellChemicalSignaling` samples. The original two-argument and three-argument `Run` APIs remain unchanged. `RunCalibrated` accepts explicit receptor threshold, signal diffusivity, and signal decay; it validates finite nonnegative transport coefficients and strictly positive thresholds.

**Diagnostic sensitivity sweep:** Unity Editor menu **Cells → Research → Probe Signal Detection Sensitivity (10 Seeds)** runs a preregistered grid of three thresholds `{0.0001, 0.001, 0.01}`, three diffusion coefficients `{0.06, 0.18, 0.36}`, 10 matched seeds `7100..7109`, and two response conditions (emission always enabled). That is **180 configured episodes**. Export location: `ExperimentRuns/signal-sensitivity-v1/observations.csv` with `manifest.json`. Avoid interpreting these exploratory seeds as an independent test of a setting selected from the same sweep.

**New checks:** `PhysicalSignalCalibrationTestsV1` checks that non-production results in no detected molecules, verifies both nutrient and emitted-signal mass ledgers, checks threshold/diffusivity parameter validation, and tests deterministic detection telemetry. These tests are committed, not executed; Unity licensing remains a prerequisite.

**Interpretation:** A detected molecule is not necessarily informative to a cell; positive or negative gradient steering gains must be separated from raw exposure. This sweep identifies if thresholds and transport scales prevent detection, not whether signaling improves evolution or cooperation. All coefficients remain arbitrary simulation units.
