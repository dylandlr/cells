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

## Next controlled evaluation

Rerun the same four policy controls and matched seeds. Compare utility,
survival, effective repairs, final patch stocks **and remaining reserve**.
Evaluate sensitivity to the reserve and repair parameters before making
claims about social emergence.
