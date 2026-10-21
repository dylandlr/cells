# Commons action microscope — real simulation replay

This small viewer displays symbols and text **from the existing C# renewable-commons simulation**, not from a separate browser game.

## Produce the trace

From the repository root (requires .NET 8):

```bash
dotnet run --project Tests/Headless/Cells.HeadlessChecks.csproj -- --commons-trace commons-action-trace.csv
```

Alternatively, download `commons-action-trace.csv` from the GitHub Actions artifact named `commons-symbolic-action-trace` in a successful **Headless research checks** run.

Open `tools/commons-action-viewer/index.html` in a browser and select the exported CSV. Step through or play the recorded events.

## Display vocabulary

| Symbol | Recorded event |
|---|---|
| ● | Agent and most recently recorded energy |
| ▤ | Patch and observed stock |
| ↘ | Harvest action and recorded payoff |
| ♻ | Restoration attempt; one attempt does not mean success |
| ✓ | World-reported successful joint restoration |
| ⚑ | Signal action; **sent does not imply delivered** |
| × | Death after energy depletion |
| ◇ | Rest |
| ▤ stock | Net patch stock change; may combine harvest, repair and natural replenish |

The trace is derived directly from `RenewableCommonsV0.Advance()` and `Observe()`. It contains chosen action codes, realized per-agent rewards, resulting energies, per-patch stock changes, joint restoration counts and transitions from alive to dead.

### Scientific boundaries

- The current exported demonstration uses a **transparent scripted action policy**. It does not display learned policy internals; the trained-policy capture adapter is a separate next step.
- This commons environment has discrete patches, not simulated continuous 2D motion. Patch cards are scientifically more faithful than arbitrary moving dots.
- The stock change is *net* post-step difference, not isolated nutrient uptake; stochastic replenishment also occurs.
- The renderer does not calculate or invent world transitions. It only replays input rows, and has no ability to evolve or modify world state.
- This trace is a basic readable CSV format for a proof-of-integration. Next, version the event schema and capture observations, selected Q values, actual message deliveries and the seed/configuration manifest.
- Real browser interaction has not yet been verified in this environment.

Do not use the older disconnected `tools/evolution-microscope/` toy to demonstrate game-theoretic research results.
