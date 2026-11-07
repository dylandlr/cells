# cells — research scenario reset: renewable commons v0

## Why the previous scenario was inadequate

Experiment 001 v1–v4 used a four-agent, three-patch resource game. An
agent could forage, send a patch-index message, or rest, but resource
availability was exogenous and harvesting had no durable effect on patch
stocks. Under that game, sending a single patch index mostly increased
competition for the same fixed reward. It was too narrow to support
claims of emergent cooperation, evolutionary adaptation or biological
survival. The previous results remain archived, not silently replaced.

## What we already have in cells and what is reused

The current branch retains a Unity microscope/game architecture, separate
organism sensing and human-augmented inspection, C# headless research
algorithms, experimental seeding, event traces and CI checks. The
experimental design preserves these boundaries: agents only see
organism-level `View`, while `StepLog` and stock snapshots are research
instrumentation. This patch reuses the C# headless test route and
explicit seed/ablation pattern. It does not claim that Unity's chemistry
solver has been connected to the new world.

## Minimal research scenario (implemented)

`Assets/CellsDemo/Scripts/Experiments/RenewableCommonsV0.cs`

- 4 agents with finite energy, 3 patches with finite renewable nutrient
  stocks and patch-specific hazards.
- Actions: harvest patch (0–2), restore patch (3–5), signal patch (6–8),
  and rest (9).
- Harvesting consumes real stock and supplies private energy/reward,
  with movement and hazard penalties.
- Restoration costs its participants energy immediately and requires at
  least 2 agents working on the *same* patch during the same step to
  replenish 4 nutrient units (capped at 8).
- Without coordinated intervention, rare exogenous regeneration occurs.
  Resource depletion therefore changes later action rewards; the
  benefit of cooperation can be delayed and is non-excludable.
- Signals cost energy and become available in the next decision step;
  delivery-only ablation blocks observation but does not refund cost.
- Observation is partial: each agent sees its location, own patch stock,
  local hazard, energy, and received patch-index messages, not the
  global stock vector. An omniscient diagnostic log records chosen
  actions, rewards and stocks before/after.
- No agents automatically die by reaching an episode step cap;
  energy <= 0 kills an agent and blocks further participation.

**No learning model is wired to this world yet.** It is a verified-
when-CI-passes substrate designed to provide strategic opportunities,
not evidence that cooperation *emerged*. A minimal next step is to
compare fixed selfish, scheduled restorative, randomized, and learned
policies on matched worlds using a finite decision horizon, and measure
survival, cooperation rates, stock sustainability and per-agent rewards.

## Acceptance criteria and limitations

- Stock cannot be harvested twice, and cooperative restoration must
  materially replenish it while costing the restorers immediately.
- A lone restorer cannot replenish stock.
- Identical seeds and actions reproduce the same world.
- Communication ON/OFF must not change the cost of sending.
- The partial observation boundary must not reveal other patch stocks.
- Tests in `RenewableCommonsChecks.cs` provide these mechanical checks.
- This is a *toy renewable resource commons*, not biological physics,
  chemical diffusion, reproduction or game-theoretic equilibrium.
- Coordination still needs a viable learned policy and controlled
  comparison. We must not tune dynamics to force positive cooperation.

## How to merge with most up-to-date Unity cells

1. Leave the Unity diffusion, membrane, physiology and microscope code
   unchanged; do not replace it with this abstract world.
2. Define a stable scientific observation/action contract with these
   pieces: local nutrient concentration, internal energy, hazard,
   secreted signal, movement/harvest/restoration choices, and step logs.
3. Implement a **small validated adapter** translating between a
   biological model and the headless contract. The adapter must not
   access researcher-only hidden state.
4. Add reward/observation parity tests; then port learner snapshots and
   a minimalist replay from logged actions. Do not claim validation in
   Unity until the actual engine CI passes.

Earlier Experiment 001 results are still scientifically exploratory.
