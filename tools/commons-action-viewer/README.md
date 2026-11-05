# Commons action microscope — real simulation replay

This small viewer displays symbols and text **from the existing C# renewable-commons simulation**, not from a separate browser game.

## Produce the trace

From the repository root (requires .NET 8):

```bash
dotnet run --project Tests/Headless/Cells.HeadlessChecks.csproj -- --commons-trace commons-action-trace.csv
```

For a **frozen learned follower replay**, use:

```bash
dotnet run --project Tests/Headless/Cells.HeadlessChecks.csproj -- --commons-learned-trace commons-learned-action-trace.csv
```

The learned trace is generated with the same 400-episode discounted-return follower learning schedule as the matched evaluation, and a scripted sender. Each follower observation (`◉`) includes its actual heard invitation, location and local stock before the action. The action rows identify the frozen learned followers; realized rewards and resource events come from `RenewableCommonsV0.Advance`. The viewer has not been checked in a real graphical browser during this run.

Alternatively, download `commons-action-trace.csv` from the GitHub Actions artifact named `commons-symbolic-action-trace` in a successful **Headless research checks** run.

Open `tools/commons-action-viewer/index.html` in a browser and select the exported CSV. Step through or play the recorded events.

The symbols and evidence constraints are defined in [cells Scientific Iconography Standard v0.1](../../docs/CELLS_ICONOGRAPHY_STANDARD_V01.md). Controller role badges are inferred from the trace exporter: [S] scripted sender, [L] frozen learned follower in the learned trace only. The viewer displays a received invitation only when its observation row says it was heard.

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

- The `--commons-trace` command uses fully scripted agents; `--commons-learned-trace` uses frozen trained followers with a scripted sender. We show the observations and resulting action choices but not yet exact Q-table values for each choice.
- This commons environment has discrete patches, not simulated continuous 2D motion. Patch cards are scientifically more faithful than arbitrary moving dots.
- The stock change is *net* post-step difference, not isolated nutrient uptake; stochastic replenishment also occurs.
- The renderer does not calculate or invent world transitions. It only replays input rows, and has no ability to evolve or modify world state.
- This trace is a basic readable CSV format for a proof-of-integration. Next, version the event schema and capture observations, selected Q values, actual message deliveries and the seed/configuration manifest.
- Real browser interaction has not yet been verified in this environment.

Do not use the older disconnected `tools/evolution-microscope/` toy to demonstrate game-theoretic research results.
