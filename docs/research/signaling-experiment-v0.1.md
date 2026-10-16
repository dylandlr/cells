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
