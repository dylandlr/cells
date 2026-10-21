# cells — Research findings and next-stage plan (October 2026)

## Executive finding

The headless renewable-commons experiments now demonstrate **learned, delayed-reward response to scripted coordination invitations**. They do **not** yet demonstrate spontaneous communication, evolutionary emergence, or calibrated single-cell biology. The learned response policy is reliable in its original signaling environment but fragile under message loss. The next high-value milestone is **training under unreliable communication and testing generalization**, not moving to a larger model.

## Verified empirical findings

All numbers below are exploratory and specific to the finite-substrate `RenewableCommonsV0` headless environment, with four agents, three patches and 100-step episodes unless otherwise noted.

### Ecological accounting

Restoration previously increased patch stock without deducting any material, inflating scripted collective return from +230.147. Adding a finite 120-unit environmental substrate reserve reduced scripted restoration's mean return to +46.587 in the original 30-seed comparison. Harvesting removes resources from the external ecosystem; biomass, waste and thermodynamic bookkeeping are still absent. [Finite-substrate report](COMMONS_SUBSTRATE_CONSERVATION_V01.md).

### Delayed versus immediate reward: matched control

Both learner variants used the same 400 training worlds, 30 disjoint held-out test seeds, observation encoding, exploration rate, update schedule, and frozen evaluation. The only intended update target difference was `gamma=0` versus `gamma=0.95`.

| Frozen policy (signals delivered) | Collective return | Successful restorations per episode | Survivors / 4 |
|---|---:|---:|---:|
| Immediate reward | -20.490 | 0.733 | 0.133 |
| Discounted return | +70.383 | 22.800 | 4.000 |

Paired held-out return difference: +90.873 over 30 common seeds. **This supports delayed credit assignment for the tested task**, not a general claim about all cooperative learning algorithms. [Methods and matched protocol](COMMONS_MATCHED_CREDIT_ASSIGNMENT.md), [CI](https://github.com/dylandlr/cells/actions/runs/39519293574).

### Rapid policy acquisition and training robustness

Frozen policies evaluated after 1, 5, 10, 25, 50, 100, 200 and 400 training episodes showed **+66.390 mean held-out return after just one training episode**. Five distinct 400-episode training-world histories yielded mean returns from +66.390 to +73.163 and four survivors. The learner likely discovers a compact response rule very quickly; this is not gradual emergence of complex social cognition. [Checkpoint/restart report](COMMONS_FINAL_CHECKS.md), [CI](https://github.com/dylandlr/cells/actions/runs/39520247143).

### Sensitivity to communication reliability

Holding the sender decision rule and the frozen follower policy fixed, random independent broadcast loss yielded:

| Probability of broadcast delivery | Mean restorations | Collective return | Survivors / 4 |
|---:|---:|---:|---:|
| 25% | 1.233 | -19.700 | 0.233 |
| 40% | 2.333 | -19.197 | 0.300 |
| 50% | 3.067 | -17.493 | 0.500 |
| 60% | 5.733 | -10.530 | 1.033 |
| 75% | 10.333 | +9.633 | 1.933 |
| 90% | 17.367 | +41.100 | 3.133 |
| 100% | 22.800 | +70.383 | 4.000 |

These are descriptive means over 30 matched held-out seeds for **one frozen trained policy**. Signaling is scripted; stochastic delivery uses its own seeded RNG. Collective return crosses zero between the tested 60% and 75% conditions, but a critical threshold has **not** been established. [Randomized reception report](COMMONS_RANDOM_DELIVERY_RESULTS.md), [CI](https://github.com/dylandlr/cells/actions/runs/39533979278). Per-step-window time series: CI artifact `commons-random-delivery-curve`.

## What remains unknown

- **Out-of-distribution generalization:** all test worlds share the scenario's underlying parameters and learning dynamics.
- **Learning under uncertainty:** existing followers were trained with 100% delivery and evaluated under loss. Performance of followers trained on randomized reception is not yet known.
- **Communication causality:** delivery intervention holds the *sender rule* and costs fixed, but downstream actions and resource states can diverge; it is not an identical physical action replay.
- **Role asymmetry:** one scripted sender organizes three responding learners. Peers do not yet learn whom, when or what to message.
- **Ecological validity:** simple finite nutrient accounting does not model complete biomass/detritus recycling, calibrated biophysics, reproduction or heritable traits.
- **Unity parity:** research code remains a headless, simplified scenario. It is not yet the microscope simulation. Local Play Mode and integration verification remain separate gates.

## Decision: the next milestone

### Phase A — robust response under message loss (implement next)

Keep the eight-state/two-action tabular architecture and sender fixed. Train multiple independently seeded policies with message reception probabilities **100%, 90%, 75%, 60%, 50%**, including a curriculum across probabilities as a separately labeled condition. Evaluate every frozen training condition at **25%, 40%, 50%, 60%, 75%, 90%, 100%** receipt on separate world seeds and independent communication RNG seeds. Use matched test worlds and no held-out updates.

**Primary metrics:** collective return, successful/attempted repairs, survival, responded invitations and message attempts/deliveries. Plot time-window curves as well as a train-reliability × test-reliability heatmap. Report paired uncertainty and variation across training histories; predefine success as improved mean held-out return at 50–75% receipt **without substantial regression** at 90–100%, rather than tuning to a single favorable seed.

**Controls:** a frozen perfect-delivery-trained policy, untrained response policy, and immediate-reward variant under matched random reception. Separate ecological RNG, exploration RNG, training seed and reception seed provenance.

### Phase B — strategic sender learning

Only after Phase A, allow seat 0 to choose *whether to signal* and *which patch to invite*, while preserving signal cost and restoration threshold. Compare learned versus fixed sender on exactly matched follower policies and training budgets. Add a no-message condition, a random-sender condition, and informative-vs-uninformative signals. Measure net welfare against signaling cost and counterfactual follower actions. Avoid claiming emergent language from predetermined message symbols.

### Phase C — ecological extension and Unity adapter

Model nutrient transfer through explicit biomass/waste pools, verify conservation and energetics, then define versioned observation/action/event contracts between headless experiments and Unity. Add deterministic replay/parity checks before visualization. After that, study reproduction and genetic–strategic coevolution with empirically grounded parameters.

## Engineering and acceptance gates

1. CI compiles the new experiment and checks seeded reproducibility, disjoint train/test world seeds, unchanged sender costs, and frozen held-out Q tables.
2. Machine-readable artifacts include manifests, all seed-level episodes, time windows, condition definitions and confidence intervals; raw results remain discoverable through GitHub Actions.
3. Each change is accompanied by a negative control and explicit discussion of possible confounding.
4. Unity Play Mode/Windows build issues are tracked independently from green headless research checks; a passing research workflow is **not** evidence of a passing Unity workflow.
5. No merge to the default branch without separate Unity validation and review.

## Scientific framing for the master's project

The defensible claim today: **In a resource-constrained multi-agent commons with fixed scripted signaling, delayed reward assignment supports a simple coordinated restoration response that immediate-reward learning does not acquire under matched conditions. The resulting strategy is fragile to communication loss.**

This directly motivates a research question: *Can agents trained under unreliable information develop robust collective resource management, and can signaling decisions themselves subsequently be learned under real communication costs?*

Earlier research audit files and CI artifacts remain authoritative records of each experimental revision. This roadmap consolidates their interpretation; it is not a new independent experiment.
