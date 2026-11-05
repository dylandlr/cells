# cells: CSCE 631 progress report

**Author:** Dylan De La Rosa  
**Date:** November 5, 2026  
**Status:** Working draft; not submitted. Report length, deadline and required format remain to be confirmed.

## Research objective and revised scope

cells investigates strategic decision-making and communication among agents competing for shared resources under partial observability. The proposed study asks whether costly communication and episode-level strategy selection can improve collective outcomes relative to fixed and random controls. The broader project envisions microscopic agents with biomechanics, chemotaxis and evolution.

Following instructor feedback, I have narrowed the immediate deliverable to Experiment 001 in the smallest existing environment. Unity visualization, genome compilation, genetics and additional biological infrastructure are deferred. This makes it possible to test strategic interactions before attempting a substantially more complex cellular simulation.

The completed work is a scripted baseline and an experimental interface. It is not yet the proposed LLM-agent or learned-strategy study.

## How the current system works

Experiment 001 uses an existing C# simulator, SignalingGameV1, that runs without opening Unity. Each episode contains four agents, three resource patches and 80 simultaneous decision rounds. Agents begin with an energy balance of five. Patch yields vary through seeded stochastic drift that is independent of agent actions.

Each round, an agent observes its own patch, energy balance, a binary cue about its current patch's yield and public signals delivered from the preceding round. Other patch cues are marked unknown. Agents do not observe the hidden yields or other agents' current choices.

There are seven primitive actions: forage at one of three patches, signal one of three patches, or rest. Foragers at the same patch split its yield. Foraging costs 0.15 utility units, changing patch costs an additional 0.20, signaling costs 0.40, and resting credits 0.10. Signaling selects a patch index; it does not certify that the patch is rich. Movement is an abstract patch change, not simulated locomotion.

All four actions are collected before the world advances. Individual utility is the sum of realized rewards; collective utility is the sum across the four agents. There is no explicit cooperation bonus. The energy balance is an accounting variable: this version does not model death, integrity or conserved biomass.

A new controller interface accepts JSON action batches and exposes local observations. It rejects malformed, stale and duplicate submissions without advancing the world, and records transitions for replay. The future model controller must pass only each agent's own observations and permitted history to that agent; the combined audit stream is not agent context.

## Completed baseline experiment

The baseline consists of one fixed periodic signaling policy and three fixed signal-following policies. I compared message delivery enabled against message delivery disabled, using the same 30 world seeds in both conditions. Signaling attempts remain charged when delivery is disabled. This isolates delivery availability while allowing actions to differ in response to received information.

The experiment produced 60 episodes, 240 individual outcomes and 19,200 action records. The statistical unit is the paired world seed, not an individual action or agent. The primary endpoint is the difference in collective episode utility: delivery enabled minus disabled.

| Measure | Result |
| --- | ---: |
| Paired worlds | 30 |
| Mean collective utility difference | −240.3533 |
| Seed-paired bootstrap 95% interval | [−285.4762, −192.9972] |
| Bootstrap resamples | 4,000 |

For this fixed scripted composition, enabling communication reduced collective utility. This is a useful negative baseline: communication does not automatically improve outcomes in the implemented game. It does not show that learned communication is ineffective, that LLM agents perform poorly, or why the loss occurred. Congestion and costly signaling are candidate explanations requiring additional controlled analysis.

## Verification and reproducibility

The archived evidence includes episode, agent, action and paired-result tables, configuration, source/output hashes and verification metadata. Repeated baseline runs produced byte-identical output files. Eighteen focused validity assertions and five evidence-validator tests passed. Four additional controller integration tests passed, covering complete episodes, atomic rejection, replay/order invariance and message-delivery costs.

The evidence honestly records that the baseline ran from a modified checkout with per-file source hashes. A runtime-version query failed in the execution environment. The focused experiment works, but an unrelated default headless nutrient-conservation check remains failing; the whole repository and Unity demo are not claimed to be validated.

## Limitations and relation to the proposal

The current agents are scripted. There have been no model calls, no learned signaling and no demonstrated emergent specialization in this baseline. EXP3 strategy-selection and payoff-analysis code exists, but its presence is not evidence that the planned matched LLM/EXP3 study has been completed.

The proposal's frozen-genome condition is currently approximated by fixed agents without genetic mechanisms; there is no implemented genome in this environment. Survival and integrity endpoints from the original proposal are absent. The report therefore uses accumulated resource-game utility and explicitly discloses this reduced scope rather than claiming biological equivalence.

Earlier renewable-commons experiments used a different environment, learning method and utility. Their results are prior exploratory work and are not pooled with Experiment 001. The current 30 worlds have also been inspected and must be treated as development evidence for subsequent controller tuning.

## Next milestones

1. Connect isolated model calls to the tested controller interface. Record model version, frozen prompts, raw responses, actions, token usage and failures. Set a bounded retry policy and inference budget before the pilot.
2. Run a small development pilot to check action validity, observation isolation and cost. Establish genuine generation provenance for any policies described as LLM-authored.
3. Freeze the policy library, controller settings, utility and communication conditions. Compare fixed rules, direct LLM decisions, EXP3 policy selection and random selection on fresh matched evaluation worlds, within the agreed course scope.
4. Analyze paired utility differences and restricted unilateral deviation incentives. Report null or negative findings as outcomes, and qualify equilibrium claims by the policy set actually evaluated.

The immediate progress-report contribution is a reproducible baseline with a negative communication effect and a tested path to the proposed agent comparison. New biology and spectator features remain outside the critical path.

## Evidence and source pointers

- Repository: https://github.com/dylandlr/cells/tree/feat/substance-ontology
- Baseline evidence: `experiments/experiment001/evidence-20261105/`
- Frozen baseline settings: `experiments/experiment001/baseline-v1.json`
- Scope and known gaps: `docs/EXPERIMENT_001_SCOPE_FREEZE.md`
- Controller protocol: `docs/EXPERIMENT_001_CONTROLLER.md`
- Baseline/evidence commit: `c1197d56d73d02863875f63148e6529b8d6dd7d6`
- Controller commit: `39243c3dfd08ff6859d59de726a938f67f5b70fa`

Reproduce the baseline from the repository root with .NET 8 and Python 3.11 or later:

```sh
python tools/run_experiment001.py artifacts/progress-report-baseline
```

The output directory must be new. Inspect verification.json before treating a run as packaged evidence.
