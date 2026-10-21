# Final checks: early acquisition and independent training restarts

**Executed:** 2026-10-20, GitHub Actions
https://github.com/dylandlr/cells/actions/runs/39520247143

## Design

This is a diagnostic follow-up to the matched immediate-vs-discounted
commons experiment. Same finite-substrate world, scripted sender,
discount factor 0.95, fixed policy design and 30 common held-out seeds.
Frozen snapshots are evaluated after training episodes
1, 5, 10, 25, 50, 100, 200 and 400. Training is independently
repeated over five non-overlapping 400-seed ranges with identical
configuration, and evaluated on the same 30 held-out seeds. The
initialization values are deterministic zeros; the independent variable
is the training-world history, not random weights or randomized
hyperparameters.

## Frozen-policy checkpoint results

| Completed episodes | Mean held-out return | Repairs per episode | Survivors |
|---|---:|---:|---:|
| 1 | 66.390 | 24.000 | 4.000 |
| 5 | 66.390 | 24.000 | 4.000 |
| 10 | 66.390 | 24.000 | 4.000 |
| 25 | 77.850 | 22.267 | 3.267 |
| 50 | 70.383 | 22.800 | 4.000 |
| 100 | 70.383 | 22.800 | 4.000 |
| 200 | 66.390 | 24.000 | 4.000 |
| 400 | 70.383 | 22.800 | 4.000 |

## Independent training-world histories

| Restart | Mean held-out return | Repairs | Survivors |
|---|---:|---:|---:|
| 0 | 70.383 | 22.800 | 4.000 |
| 1 | 72.503 | 23.967 | 4.000 |
| 2 | 66.390 | 24.000 | 4.000 |
| 3 | 72.503 | 23.967 | 4.000 |
| 4 | 73.163 | 23.900 | 4.000 |

The five restarts use the **same test seeds**, so the 150 episode
records are *not* 150 independent evaluation worlds. Compare
within-seed variation by training history rather than treating these
as an independent confirmatory sample.

## Interpretation and unresolved validity questions

The first completed episode already yields high held-out performance.
This is consistent with a very small tabular policy moving away from
zero-initialized action ties into a favorable restoration-response
decision, not necessarily the gradual emergence of rich cooperation.
The nearly discrete performance plateaus warrant inspection of Q-values,
the very first reward updates, and action choices before treating this
as a demonstration of complex emergent intelligence.

The sender's invitation policy remains hard-coded. No multi-agent
communication code or vocabulary emerges through learning. There
is no genome evolution, no Unity biochemical integration, and no
out-of-distribution environment test.

**Next gate:** audit the first episode's exact state/action/return/Q
transitions, including whether a learned repair response works when
signaling timing or partners change. Then explicitly test independent
learned sender choices under controlled cooperative and conflicting
incentives, without tuning to force positive emergence.

## Reproducibility

```bash
dotnet run --project Tests/Headless/Cells.HeadlessChecks.csproj --configuration Release -- --commons-matched artifacts/commons-matched
dotnet run --project Tests/Headless/Cells.HeadlessChecks.csproj --configuration Release -- --commons-restarts artifacts/commons-restarts
```

CI archives `delayed-checkpoints.csv` and `restarts.csv` as
`commons-final-validation`. Both were generated and validated
in the successful workflow above.
