# Experiment 001 v3 — held-out policy transfer

**Status:** executable experiment, subject to CI validation. This is a
separate follow-up to the exploratory v2 learning ablation; not an approved
replacement for the course proposal.

## Question

Do reward-updated action values transfer to world seeds that were never
used for training, rather than improving only because Q-values update
within the measured episode?

## Protocol

- Same Unity-independent `SignalingGameV1` resource game and immutable
  learning traits as v2.
- Three follower agents trained with online reward updates over
  **200 seeded episodes (310000–310199)**. One identical scripted signal
  sender occupies seat 0. Training uses message delivery ON.
- Freeze each follower's Q table after training.
- Compare on **30 unseen world seeds (410000–410029)**, 80 decisions each:
  (1) untrained Q=0, messages ON; (2) pretrained Q, messages ON;
  (3) pretrained Q, messages OFF.
- No policy weight updates on evaluation seeds. A fresh seeded policy RNG
  is used for each episode. Identical world seeds pair each condition.
- Primary v3 contrast: `trained_on - untrained_on` on the held-out seeds.
  Secondary: `trained_on - trained_off`. Bootstrap resampling uses world
  seeds as the unit of analysis.

## Reproduce

```bash
dotnet run --project Tests/Headless/Cells.HeadlessChecks.csproj --configuration Release -- --heldout001 artifacts/experiment001-heldout-v3
```

Expected artifacts: `episodes.csv`, `paired.csv`, `summary.csv`,
`manifest.json`.

## Interpretation limits

- The world uses symbolic patches, not living cells, biology or genetics.
- The genome is an immutable parameter proxy. It is not an evolving genome.
- The *signal sender and signal-weight coefficient remain scripted*. This
  study does not test the emergence of new communication policies.
- Policy RNG seeding restarts each episode; training and testing worlds are
  disjoint but drawn from the same generator distribution. This tests
  within-distribution transfer, not robust out-of-distribution generalization.
- A difference on held-out worlds supports transfer for this policy class;
  it does not imply emergent cooperation or strategic equilibrium.
- Training curves, fixed-code unbiased action-selection controls, and
  learnable communication choices remain future work.

## Integration into latest cells

`AdaptiveHeldOut001V3.cs` shares the C# namespace and experiment build
with `AdaptiveExperiment001V2.cs`. The original Unity physics and
microscope project remain untouched and are **not** validated by this
headless study. A shared scientific observation/action adapter can be
designed only after this experimental boundary is clear.
