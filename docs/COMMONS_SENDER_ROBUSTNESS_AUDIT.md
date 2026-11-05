# Commons mechanism audit: first-episode Q values and sender perturbation

## Verified run

GitHub Actions: https://github.com/dylandlr/cells/actions/runs/39520766218

Headless .NET checks passed **4,669 assertions**. The audited configuration
keeps 400 training episodes, 30 frozen held-out test seeds, the finite-substrate
commons and unchanged discounted-return learner.

Data artifacts: `first-episode-q.csv` and `sender-challenges.csv`
(artifact `commons-mechanism-audit`).

## First-episode learner inspection

Immediately after **one** training episode, the preferred response actions
(`q[state,respond] > q[state,harvest]`) are:

| Agent seat | States preferring response |
|---|---|
| 1 | 1, 3, 5 |
| 2 | 1, 3, 5 |
| 3 | 0, 1, 3, 5 |

State bits are `local_stock<=2` (bit 0), signal-present (bit 1),
and `energy<2.5` (bit 2). **Important:** an abstract preference
for the response action does *not* necessarily cause restoration:
the policy only restores when there is an incoming signal.
The result suggests a rapid, low-dimensional threshold-policy selection
rather than gradual discovery of complex social behavior.

## Held-out frozen sender challenge

| Sender variant | Mean collective return | Successful repairs | Survivors |
|---|---:|---:|---:|
| Original condition, periodic signal every 4 steps | +70.383 | 22.800 | 4.000 |
| Scripted signal every 6 steps | -15.140 | 4.500 | 0.833 |
| Scripted sender never signals | -20.777 | 0.000 | 0.067 |

**Critical causal caveat:** the sender's schedule is implemented as an
action choice. Changing periodic signaling changes *both* when a message
is broadcast and what seat 0 does instead (restore or harvest).
These contrasts test **sender-policy perturbations**, not pure message
frequency in isolation. Do not attribute the full effect solely to
communication frequency. A clean communication-frequency experiment
needs to hold the sender's non-message actions fixed or independently
vary signal delivery over a matched action trajectory.

## Scientific conclusion

The learned cooperative response is reproducible on matched held-out
worlds under its original scripted sender, but is **not robust** to
a different sender policy. This supports a narrow coordination
mechanism and contradicts any strong claim of generally emergent
social intelligence or communication.

## Next required acceptance gates

1. Separate sender *message delivery timing* from sender's restorative
   action budget, preserving physical resource and effort accounting.
2. Examine state/action traces for counterfactual opportunities:
   an invitation must change an actual action and potentially future
   rewards, not merely switch a label.
3. Challenge learned followers with altered partner capabilities and
   scarcity levels, without modifying frozen policy parameters.
4. Only then test an independently learning sender, with a matched
   scripted sender control and explicit communication costs.

The experiment remains Unity-independent and is committed only on
`feat/substance-ontology`.
