# Renewable Commons — Delayed Return Learner V1 (Exploratory)

## Verified result

GitHub Actions: https://github.com/dylandlr/cells/actions/runs/39518726426

The headless job successfully built and passed **4,662 assertions**,
then completed 400 seeded training episodes and 30 disjoint held-out
evaluation seeds (100 decisions per episode) with updates disabled.

| Held-out condition | Mean collective return | Mean successful repairs | Mean survivors (of 4) | Total accepted intentions |
|---|---:|---:|---:|---:|
| Frozen trained, messages ON | +70.383 | 22.8 | 4.000 | 799 |
| Frozen untrained, messages ON | -20.490 | 0.733 | 0.133 | 26 |
| Frozen trained, messages OFF | -20.587 | 0.000 | 0.067 | 0 |

Mean paired difference trained ON minus untrained ON:
**+90.8733**; seed-level paired bootstrap 95% interval
**[+83.06575, +98.87683]**.

This is a promising **within-distribution held-out improvement**:
learned followers use incoming scripted repair invitations to
coordinate restoration and preserve resource access.

## Study design and replication

Implementation:
`Assets/CellsDemo/Scripts/Experiments/CommonsDelayedLearningV1.cs`.

- World model: finite-substrate `RenewableCommonsV0` (not Unity biochemical simulator).
- Training worlds: seeds 910000–910399; evaluation: 1010000–1010029.
- Sender at seat 0 is scripted. Three adaptive followers choose between
  local harvest and restoration of a signaled patch.
- Learner: discounted episode-return (gamma 0.95), tabular incremental
  updates, learning rate 0.08, epsilon 0.12. State encodes local
  scarcity, availability of a message, and personal low energy.
- Frozen copies of each learned Q-table evaluated with **no test-time updates**.
- Controls: identically coded untrained Q-tables with delivery ON, and
  trained tables with message delivery OFF. Paired world seeds.
- Outputs: `episodes.csv`, `summary.csv`, `manifest.json` archived
  in `commons-delayed-heldout-v1` CI artifact.

Reproduce:

```bash
dotnet run --project Tests/Headless/Cells.HeadlessChecks.csproj --configuration Release -- --commons-delayed artifacts/commons-delayed-v1
```

## Limitations requiring further controls

1. **No learned signaling:** the same scripted sender provides invitations.
   This is learned *response* to messages, not emergent language or
   learned sender incentives.
2. **Credit assignment not isolated:** this learner differs from the
   preceding immediate-reward study in training horizon, evaluation
   split, state representation and learning schedule. A matched
   immediate-reward control is essential before attributing improvement
   specifically to delayed credit assignment.
3. **Potential reward shaping through scenario architecture:** requiring
   simultaneous restoration and sending invitations makes cooperation
   discoverable, but does not guarantee it. The result is specific to
   these incentives and policy composition.
4. **Shared distribution:** train and test seeds are disjoint but
   generated from the same game and resource parameterization.
5. **No genome evolution:** there is no inheritance, mutation,
   biomass conservation or reproduction. Results do not yet transfer
   to Unity's biological model.
6. **Experimental status:** exploratory results after several rounds
   of post-hoc design modifications. The observed confidence interval
   does not substitute for a preregistered confirmatory evaluation.

## Next gates

Implement a matched immediate-reward control using identical
observations, exploration and training/test seeds, and then
study whether a sender can learn when to invite partners instead of
relying on fixed periodic messaging. Preserve this configuration as
v1 for reproducibility. Only then build a Unity adapter and a
read-only replay from event traces.
