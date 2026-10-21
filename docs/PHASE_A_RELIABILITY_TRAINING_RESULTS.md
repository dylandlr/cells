# Phase A — Learning under unreliable communication

## Status

**Completed exploratory train-versus-test reliability sweep.** Successful headless CI:
https://github.com/dylandlr/cells/actions/runs/39537028039

Research suite passed **4,677 assertions**. The previous CI attempt failed compilation due to a result buffer declaration and was corrected; the run linked above succeeded.

Implementation: `CommonsDelayedLearningV1.cs`; machine-readable `reliability-matrix.csv` in the CI artifact `commons-trained-reliability-matrix`.

## Study design

Each cohort trains on the same 400 world seeds and same action/observation space, scripted sender, discounted return (gamma 0.95), learning rate and epsilon. Each cohort differs only by its message reception probability: 100%, 90%, 75%, 60%, 50%. Frozen policies are evaluated against 30 common held-out world seeds at 25%, 40%, 50%, 60%, 75%, 90%, 100% reception. Environment and communication streams are separately seeded. The algorithm is an eight-state/two-choice follower; no learned sender, no self-organized communication.

### Mean collective return on 30 held-out seeds

| Train reception | Test 25% | 40% | 50% | 60% | 75% | 90% | 100% |
|---:|---:|---:|---:|---:|---:|---:|---:|
| 100% | -19.700 | -19.197 | -17.493 | -10.530 | +9.633 | +41.100 | +70.383 |
| 90% | -20.143 | -19.867 | -18.703 | -11.037 | +12.183 | +46.577 | +72.503 |
| 75% | -20.073 | -19.550 | -18.513 | -11.673 | +9.333 | +41.490 | +66.390 |
| 60% | -20.143 | -19.867 | -18.703 | -11.037 | +12.183 | +46.577 | +72.503 |
| 50% | -20.143 | -19.867 | -18.703 | -11.037 | +12.183 | +46.577 | +72.503 |

At 50–60% test reception, all policies remain below zero mean collective return. At 90–100%, policies perform relatively well regardless of training delivery. Training under message loss provides **no convincing broad robustness improvement in this exploratory sweep**. Identical rows for the 90%, 60%, and 50% trained policies suggest convergence to the same effective tabular choices; inspect actual Q tables before asserting exact equality of policies.

## Interpretation

The current follower can only harvest its present location or restore a signaled location. Without a received invitation, its abstract 'respond' action falls back to local harvesting. It has no independent way to initiate repair, recruit partners, infer signaling loss, or plan around uncertain partner behavior. Thus training on lost messages may be insufficient even when return learning works correctly.

The intervention changes message receipt while keeping the sender's decision *rule* and direct signal cost unchanged, but altered follower actions change future resource stocks and can indirectly alter future sender choices. All conditions share a single family of scripted sender policy and in-distribution worlds.

## Acceptance result and next decision

**Phase A1: training under imperfect reception, completed. Robustness goal NOT MET.**

Do not prematurely move to large neural networks or claim agents evolved resilient cooperation.

### Phase A2: action and coordination redesign (recommended)

1. Add explicit action choices for *independent restoration attempts*, including an intentional no-invitation repair choice, not just the two-choice respond/harvest fallback.
2. Give agents bounded observations of neighboring intention cues, partner presence and past repair outcomes; avoid oracle information.
3. Define a restoration-attempt metric to distinguish autonomous cooperation attempts from coordinated success. Log sender broadcasts, delivered invitations and communication costs separately.
4. Compare the current two-action learner against an enhanced policy **under an identical fixed-sender environment** across the full train/test reliability matrix.
5. Add negative controls: fake messages, shuffled sender identity, no sender, and repaired-resource budgets, and include multiple independent training histories and confidence intervals.
6. Gate any learnable sender experiment on an observed, reproducible improvement at intermediate reception rates (50–75%) without major full-delivery regression.

### Subsequent work

If robust cooperation emerges, introduce strategic sender learning with explicit signal cost and then integrate the research observations/action/event traces with the Unity physics and biophysical simulation. The latter remains a separate validation effort.

**Scientific claim now supported:** delayed reward learning produces an effective but brittle response to scripted invitations, and simply training the same limited follower architecture with stochastic invitation loss does not substantially improve out-of-distribution reception robustness.
