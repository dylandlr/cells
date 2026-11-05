# Renewable Commons v1 — Exploratory learning control results

GitHub Actions: https://github.com/dylandlr/cells/actions/runs/39515123843

## Protocol

30 matched seeds (710000–710029), 100 decision steps, four conditions:
online immediate-reward tabular learner; same policy with updates disabled;
greedy harvesting; simple scripted restoration. Each episode has four
organisms and three renewable nutrient patches. The learner's state
encodes only local stock (empty/low/high) and selects among ten actions.
This is online adaptation on measured episodes, **not held-out policy
transfer or an experiment in learned communication**.

## Verified results

| Controller | Mean collective return | Repairs per seed | Survivors (0–4) | Final stock |
|---|---:|---:|---:|---:|
| Learner | +4.810 | 1.867 | 3.500 | 1.267 |
| Learning disabled | -20.568 | 0.067 | 0.033 | 11.500 |
| Greedy harvester | -20.043 | 0.000 | 0.200 | 4.333 |
| Scripted restoration | +230.147 | 49.000 | 4.000 | 12.267 |

Paired learner minus learning-disabled mean **+25.378**;
95% paired bootstrap interval **[+22.185, +28.623]**.

C# build succeeded, headless checks **4,353** passed, all 120 episode
rows and one seed-paired summary were validated and uploaded to CI.

## Critical interpretation and known issues

- Scripted restoration is a *programmed group behavior*, not emergent
  cooperation. Its very large advantage may signal unbalanced payoff
  or restoration dynamics; restoration costs 0.6 energy per participant
  and can create four resource units repeatedly without additional
  substrate constraints.
- Learner's reward update only uses immediate personal rewards,
  not discounted future stock, teammates' welfare, or a prediction of
  how many others will restore. It does not solve the coordination game.
- Restoring on the same patch requires two agents acting simultaneously;
  the learner has no explicit teammate model, so most lone restorative
  acts pay a cost without providing any stock.
- A major baseline flaw: the `scheduled_repair` heuristic chooses
  patch 0 when its current patch's stock is low, *not* when patch 0
  stock is known low. It is a narrow hard-coded schedule, and its
  superiority cannot be generalized.
- Mean stock in the learning condition is nearly exhausted, despite
  better short-term individual survival. We must not equate survival
  with sustainability.
- The current artificial ecosystem does not yet implement biological
  mass/energy conservation or ecological reproduction.
- Different policy compositions change the reward trajectory, so these
  seed-matched comparisons do not isolate specific social mechanisms.

## Correct next experiment

Before training larger networks or connecting Unity, add a
**viability audit**: make restoration require a finite substrate
reserve, enforce resource conservation, and test an oracle condition
to ensure restoration can genuinely outperform selfish behavior for
selected parameter ranges, without guaranteeing it. Then expose
coordination-relevant information (incoming intentions and uncertainty)
to the learner and compare with a learning-disabled twin and competent
scripted cooperative control.

Preserve existing Unity components. This C# headless scenario is
research-only; no engine adaptation or default-branch merge has occurred.
