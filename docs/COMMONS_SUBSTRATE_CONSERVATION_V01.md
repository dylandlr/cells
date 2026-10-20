# Renewable Commons v0.1 — Finite environmental substrate

## Purpose

The first commons learning trial found an extremely favorable scheduled-restoration
baseline (+230.147 mean collective utility) against a largely depleted ecosystem.
That was exploratory and **not evidence of emergence**. The model previously
created four resource units after each coordinated restoration and added random
regeneration without deducting material from any other store.

## Implemented conservation fix

The scenario now begins with a fixed pool of **120 substrate units** in addition
to the existing patch stocks. A successful coordinated restoration **transfers**
up to four units from the shared reserve into the chosen patch (cap 8).
Passive replenishment transfers one unit, also drawn from this reserve.
Harvesting removes units from the external ecosystem, representing their
consumption by agents. Both restorative and natural replenishment cease when
the reserve is empty; restoration still costs energy when ineffective.
No restoration action creates matter.

The model records `SubstrateReserve` and `TotalExternalNutrients()`.
The commons learning output now has `reserve_remaining` per episode.

## Validity contract

Under any action trajectory, external nutrient quantity (patch stocks
plus reserve) must never increase. It decreases only when nutrients are
harvested. Restoring and passive replenishment cannot increase that total,
including when resource or reserve caps are reached.

`RenewableCommonsChecks.cs` includes controlled and repeated actions
to verify accounting, restoration costs and matched seeded execution.

## Scientific caveats

- This is an **external resource accounting** constraint, not yet a
  thermodynamic mass balance: harvested nutrients leave the environment,
  rather than entering explicit organism biomass, waste or decomposers.
- Repair still only uses two simultaneous actors as a binary threshold.
  The ecological cost and causal incentives need further calibration.
- 120 units is an exploratory parameter, not a biological measurement.
  There is no claim of calibrated physiology, evolution, or emergent cooperation.
- Prior v1 commons results came from a different model and are **not
  directly comparable** to v0.1 without naming the changed dynamics.
- The Unity chemistry engine has not been swapped out or wired to this
  simplified research scenario.

## Verified rerun — 2026-10-20

GitHub Actions: https://github.com/dylandlr/cells/actions/runs/39515768949

C# headless build and **4,657 assertions passed**. Repeated
restoration/conservation stress checks passed, and 30-seed four-policy
evaluation outputs were archived.

| Policy | Previous return | Conserved-substrate return |
|---|---:|---:|
| Online learner | +4.810 | +4.810 |
| Updates disabled | −20.568 | −20.568 |
| Greedy harvester | −20.043 | −20.043 |
| Scheduled restoration | +230.147 | **+46.587** |

Scheduled-restoration mean successful repair count fell from 49.0
to 29.3; mean final patch stock fell from 12.267 to 7.8.
The controlled learner-vs-frozen contrast remains **+25.378**
(95% paired bootstrap interval [+22.185, +28.623]).

The three other means did not change because these policies either
did not effectively restore resources or restored so rarely that the
finite reserve did not materially affect their payoffs on these seeds.
That interpretation is provisional; the experiment additionally exports
per-episode remaining reserve for inspection.

**This is a new model version**, not a replication of unchanged v1
dynamics. Earlier results must retain their model version identifiers.

## Next controlled evaluation

Rerun the same four policy controls and matched seeds. Compare utility,
survival, effective repairs, final patch stocks **and remaining reserve**.
Evaluate sensitivity to the reserve and repair parameters before making
claims about social emergence.
