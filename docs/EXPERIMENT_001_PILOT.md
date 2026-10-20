# Experiment 001 — communication delivery pilot v0.1

This is a **candidate research protocol**, not the formally approved
frozen-genome protocol. Obtain confirmation of the scientific contrast and
sample size against the submitted proposal before final data collection.

## Minimal execution

Requires .NET 8, no Unity Editor or visualization:

```bash
dotnet run --project Tests/Headless/Cells.HeadlessChecks.csproj --configuration Release -- --experiment001 artifacts/experiment001
```

Optional pilot with five matched world seeds:

```bash
dotnet run --project Tests/Headless/Cells.HeadlessChecks.csproj --configuration Release -- --experiment001 artifacts/experiment001-pilot 100001 5
```

Outputs: `episodes.csv` (two conditions per seed), `paired.csv`
(seed-matched collective return), `summary.csv` (seed-level paired
bootstrap interval) and `manifest.json`. The current manifest does **not**
embed an independently resolved source commit hash; record the Git SHA
externally before treating results as archival.

## Operational study

- Four fixed scripted policies: one periodic signaler and three
  signal-followers. No within-episode learning, no genomic change.
- Three resource patches, 80 simultaneous decision steps per episode.
- Communication ON: previous-step public signal counts are visible to each
  policy. Communication OFF: all policies observe zero delivered signals.
- Signaling actions and their utility cost **still occur** when delivery
  is disabled. This isolates the effect of *receiving* information while
  keeping the signaling rule and cost structure in place.
- Both treatments use the same environment seed; stochastic world
  transitions are driven by a separate environment PRNG. Policy composition
  and the policy seed mapping are unchanged.
- Primary outcome: total realized per-agent return, summed over the four
  agents, compared ON minus OFF per seed.
- Analyze differences at the **world seed** level using paired bootstrap
  percentile intervals. Results do not imply Nash equilibrium or that
  cooperation has occurred.

## Known methodological limits

- This is a symbolic game. The game does not model microbes, physical
  chemistry, genetics, genome freeze or evolutionary change.
- The communication-OFF agents still spend signal costs; it is a
  delivery-blocked channel rather than a strategy adapted to silence.
  Additional interventions would answer a different causal question.
- Environment PRNG draws can diverge if policies eventually affect the
  sequence of random calls. In this version exogenous patch drift draws
  are independent of player actions, preserving matched world randomness.
- A single signaling seat and fixed starting locations can bias
  population estimates. A seat-rotation robustness experiment is
  advisable *after* the core protocol is approved, not silently added
  to the primary estimand.
- Do not interpret tests or generated CSV as scientific conclusions
  until a run has actually completed and the output has been inspected.

## Visual policy

A viewer, if developed later, is an optional read-only renderer of
recorded states. It cannot alter random streams, turn logic or outcomes.
See `SCIENTIFIC_MINIMALISM_VISUAL_CONTRACT.md`. No viewer is needed to
run or analyze Experiment 001.
