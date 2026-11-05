# Experiment 001: progress-report scope freeze

Decision date: November 5, 2026. Instructor feedback supplied directly by the student:

> Before the progress report, freeze Experiment 001 as the only required deliverable, use the smallest existing environment and logging surface that can support it, and treat the genome compiler, genetics, and spectator interface as out of scope unless core results already exist.

This supersedes the Unity-first implementation sequence in COURSE_ALIGNED_AGENT_ARCHITECTURE.md. No new physics, neural receptor model, developmental engine or spectator work is required for this milestone.

## Smallest executable baseline

Use existing SignalingGameV1 and Experiment001V1 with four scripted agents, three patches, 80 simultaneous decisions, one-step public patch messages and unchanged action costs. Freeze configuration in experiments/experiment001/baseline-v1.json. This is a baseline validation, not a replacement for the proposed full LLM/EXP3 study. No modeled genome changes occur; there is no actual genome implementation in this environment.

The primary baseline contrast is delivery-on minus delivery-off for the identical periodic-sender/three-followers composition. Sending remains charged when delivery is disabled. Policy actions may diverge in response to messages. World drift is independent of actions. There is no requirement that communication improve payoff.

Utility is accumulated harvest minus forage/movement/signaling expenditure plus the declared rest credit. There is no cooperation bonus. This abstraction has no mortality, integrity dynamics or finite conserved biomass; those proposal metrics cannot be claimed as measured here.

## Reproduce

Prerequisites: .NET 8 SDK, Python 3.11 or later and Git. From the repository root:

```sh
python tools/run_experiment001.py artifacts/my-experiment001-baseline
```

The destination must not already exist. The runner builds the existing headless project, executes the focused Experiment 001 validity checks, evaluates the frozen 30-world baseline, audits all episode/agent/action coordinates and reward aggregation, checks delivery-off isolation and paid signal actions, then repeats the run and requires byte-identical outputs. It stores source/output SHA-256 hashes and source-worktree status in verification.json. A missing verification.json means packaging did not complete.

The broad default headless check command also exercises unrelated cell-field development. On November 5 its CellFieldV01.Trace(12) nutrient-conservation check failed. This remains unfixed; broad-suite success is not claimed. The dedicated --experiment001-checks path and baseline CLI no longer execute unrelated automata prerequisites. Default broad checks still run unchanged.

## Results obtained November 5

Run: seeds 1200000–1200029, 60 episodes, 240 individual outcomes, 19,200 action records. Repeated output files were byte-identical. C# compiled with zero warnings/errors; 18 focused validity assertions and five Python evidence-validator tests passed.

Mean collective utility difference, delivery ON minus OFF: **-240.3533**. Seed-paired percentile bootstrap 95% interval: **[-285.4762, -192.9972]**, 4,000 bootstrap resamples, bootstrap seed 631. These exploratory baseline findings apply to the fixed scripted policy composition and symbolic environment only. They do not establish failure of learned communication or LLM agents, and do not establish a mechanism for the negative effect.

The run used a modified source checkout, explicitly recorded in verification.json with per-file source hashes. The SDK installation reported version 8.0.427; its CLI version query intermittently failed under the execution environment, so the evidence file records that limitation rather than inventing a successful query.

## Remaining gaps to the proposal

| Item | Status / next action |
| --- | --- |
| Paired environment, explicit costs, communication ablation, scripted baseline | Executed baseline above |
| Direct LLM decisions | Not run; requires bounded observation/action adapter, frozen prompt/model/budget, and real response logs |
| Four LLM-authored frozen policies | Existing library is scripted; establish actual generation provenance before labeling LLM-authored |
| EXP3 and random selection | Existing implementations available; align them to the same frozen policy library and communication arms |
| Empirical payoff/deviation analysis | Existing utilities available; run on the final frozen controller profiles, not mixed study variants |
| Composite-action ablation | Not present in baseline; define matched primitive/composite opportunities before pilot |
| Survival/integrity utility | Not modeled by this smallest environment; disclose reduced endpoint and reconcile with proposal rather than silently claim equivalence |

Next work is the strategic controller adapter and provenance, followed by a small development pilot. Freeze the full study before drawing untouched evaluation seeds. Prior baseline seeds are now development evidence and must not become an allegedly unseen test set for controllers tuned after inspecting these results.
