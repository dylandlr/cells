# Randomized communication reliability — exploratory experiment

Research run: https://github.com/dylandlr/cells/actions/runs/39533979278

Finite-substrate Renewable Commons, frozen discounted-return follower policies, fixed scripted sender decision rule, 30 matched held-out seeds, 100 steps. Messages are independently accepted on each broadcast using a seeded reception RNG distinct from environmental RNG. Probabilities 25, 40, 50, 60, 75, 90, 100 percent.

| Delivery percent | Mean cooperative restorations | Mean return | Mean surviving agents |
|---:|---:|---:|---:|
| 25 | 1.233 | -19.700 | 0.233 |
| 40 | 2.333 | -19.197 | 0.300 |
| 50 | 3.067 | -17.493 | 0.500 |
| 60 | 5.733 | -10.530 | 1.033 |
| 75 | 10.333 | +9.633 | 1.933 |
| 90 | 17.367 | +41.100 | 3.133 |
| 100 | 22.800 | +70.383 | 4.000 |

The CI run passed **4,676 headless assertions** and published
`probabilistic-exchange.csv` (seed/rate/10-step-window records)
and `probabilistic-exchange-curve.csv` (10-window mean trajectories)
in the artifact `commons-random-delivery-curve`.

Interpretation: Cooperation is strongly sensitive to reliable message receipt. Positive mean welfare begins between tested 60 and 75 percent conditions, but this is not a statistically established phase transition or a calibrated real-world biological threshold. The single learned policy and scripted sender remain important constraints.

Note: the present experiment tests fixed independent reception probabilities; senders do not learn when to communicate. Unity biological simulator integration is not included. Future research should evaluate multiple independently trained follower policies, paired uncertainty intervals, independent holdout environments, and a learning sender.
