# Matched Commons Credit Assignment Study — 2026-10-20

## Question

Does discounted future-return credit assignment outperform immediate
reward feedback *when everything else about the learned follower
controller and evaluation is held constant*?

## Exact controlled comparison

Both variants run the same `CommonsDelayedLearningV1` implementation
with training seeds 910000–910399, held-out seeds 1010000–1010029,
100 decisions per episode, scripted sender, four agents, finite-substrate
`RenewableCommonsV0` mechanics, tabular eight-state/two-action follower,
epsilon 0.12, update step size 0.08 and frozen held-out policies.

The single toggled factor is the value of the return target during
training, inside `Policy.Finish()`:

- Immediate: `target[t] = reward[t]` (gamma = 0).
- Discounted: `target[t] = reward[t] + 0.95 * target[t+1]`
  (gamma = 0.95).

Both variants apply updates at the same point in the episode, in reverse
temporal order, using the same state/action trajectory bookkeeping.
World trajectories may subsequently diverge when training changes the
learned policies, which is expected in a causal policy intervention.

## Reproducibility and run results

Verified GitHub Actions run:
https://github.com/dylandlr/cells/actions/runs/39519293574

The full comparison is executed with:

```bash
dotnet run --project Tests/Headless/Cells.HeadlessChecks.csproj --configuration Release -- --commons-matched artifacts/commons-matched
```

The CI artifact `commons-matched-discount-control` includes separate
episode records, seed-paired summaries and manifests for both variants.

| Held-out outcome | Immediate gamma=0 | Discounted gamma=0.95 |
|---|---:|---:|
| Mean collective return | -20.4900 | +70.3833 |
| Mean successful restorations / episode | 0.7333 | 22.8000 |
| Mean surviving agents / episode | 0.1333 | 4.0000 |
| Trained with messages OFF (mean return) | -20.5867 | -20.5867 |

Paired delayed minus immediate held-out collective utility:
**+90.8733** over 30 matched test seeds.

## Interpretation

This is direct evidence that changing the credit-assignment horizon
makes a substantial difference to resource-restoration competence **in
this specific fixed-sender environment and tabular policy**. The
immediate-reward trained controller did not outperform the untrained
control on the means above.

This does not establish spontaneous signaling, emergent language,
genetic coevolution or biological realism. The social role of the
sender remains scripted, and the delayed-return design itself was
developed iteratively after inspecting previous experiments. This
is an exploratory mechanistic comparison, not a preregistered
confirmatory result.

**Next gate:** inspect per-seed behavioral traces and measure learning
trajectories, then add a learned sender with a controlled fixed-sender
comparator. Do not enlarge to Unity before establishing the
observation/action adapter contract.
