# cells — Scientific Minimalism Visual Contract v0.1

Status: design direction for Experiment 001, not a requirement to build a UI before results.

## Intent
A minimalist scientific instrument: each mark has a stable meaning that can be explained from logged state. Pleasant abstract glyphs, not decorative representations of unmeasured biology.

## Experiment 001 scope
The minimal viewer is an optional, read-only **replay of existing logged episodes**. Headless runs, policy evaluation, CSV output, and paired analysis must work without graphics, Unity, or a running viewer. Build only after the first validated Experiment 001 run, unless it is demonstrably trivial.

## Visual grammar
- Background: subdued charcoal / slate; subtle spatial guides only if they encode patch positions.
- Agents: four persistent identifiers (shape + short label A-D). A small ring encodes normalized energy with textual value on selection. Position means *resource patch assignment*, not physical microscopic coordinates.
- Patches: three fixed nodes with resource cue shown only at the same information level the policy receives; hidden yields must not leak through the live view in an agent-observation mode.
- Actions: a brief, discrete motion or glyph transition for forage / signal / rest. Do not synthesize continuous locomotion for a turn-based game.
- Communication: a dashed directed trail from sender to public channel; message receipt shown one step later. Distinguish send attempts from delivered observations.
- Utility: compact per-agent numeric returns and total return, not decorative health bars.
- Comparisons: condition, seed, timestep and replay status always visible. Pair on/off runs by seed and present differences numerically.
- Palette is redundant with labels/shapes: use muted teal for agent state, amber for signaling, neutral gray for world and a limited warning red for invalid states. Never rely on color alone.
- Every graphical mark must map to a field in the frozen observation/action/outcome schema; if no field exists, omit the mark.

## Interaction minimum
Pause / step / play logged states, select an agent for observables, toggle received messages, view the cumulative utility trace. Disable nonessential particle effects and cinematics.

## Scientific guardrails
Log and plot truthfully, with explicit simulation steps. Never reveal hidden yield in a policy-view without an explicit omniscient/debug view label. Avoid implying continuous geometry, biological molecules, genes or mechanics not present in SignalingGameV1.

## Acceptance criteria
1. The visualization reads exported events without modifying the simulation or its random streams.
2. The same input trace produces the same visual state at every step.
3. Labels remain legible at reduced resolutions and no quantity relies solely on color.
4. Every displayed quantity can be traced to its source event/measurement.
5. Removing the visualization does not change experimental outcomes.
6. No UI work blocks Experiment 001 results or the progress report.

## Deferred
Unity microscope presentation, membrane/shell rendering, symbolic genome illustrations, full spectator console, live multiplayer, shader-based biological effects, and rich animation.
