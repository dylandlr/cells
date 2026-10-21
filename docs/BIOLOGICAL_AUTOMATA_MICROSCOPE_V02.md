# cells: Biological Automata & Microscope Visual System v0.2

Status: **architecture specification**, not an implementation claim. Existing commons viewer remains a categorical, replay-only environment. Future Unity machinery requires validated state and conservation tests.

## Thesis

A cell is an assembly of semi-autonomous functional mechanisms, not a humanoid agent with biological-themed high-level actions. Modules sense, transduce, metabolize, transport, repair, move and replicate subject to an explicit energy/material budget. Learning tunes the controller within a lifetime; evolution modifies heritable assembly/configuration across generations. Game-theoretic interaction emerges when several physically constrained cells compete, signal or collaborate.

## 1. Machine composition

| Module | Inputs | Outputs / local state | Cost and conservation | Microscopic mark |
|---|---|---|---|---|
| Membrane | pressure, stress, solute gradients | integrity, permeability, boundary shape | repair uses mass and energy | 2px translucent double contour with a small discontinuity when damaged |
| Receptor | local ligand samples | bounded receptor activation, saturation | finite sensing/noise budget | dots embedded in contour |
| Transporter | local material concentration | inward/outward flux | flux balances bath/cytoplasm pools | direction arrows at membrane |
| Metabolic engine | substrate, catalysts | energy carrier, waste, heat proxy | stoichiometric update, capped conversion | internal moving 2–4 particle motif |
| Signaling machinery | controller command, signaling material | emitted molecular messenger | material/energy cost; diffusion before receipt | outgoing concentric rings, color + glyph ⚑ |
| Controller | receptors, memory, energy | low-level activation vector | explicit control latency and switching cost | tiny network/node marks, not brain icon |
| Effectors | energy, control signal, local mechanical context | force, secretion, adhesion, uptake, repair | action costs and bounded actuators | membrane notch/appendage and event glyph |
| Genome / assembly template | parent blueprint, mutational variation | module inventory and parameters | reproduction requires component material | lineage node and inheritance badge |

No biological claim follows merely from naming a module; calibration evidence must be documented separately.

## 2. Machine runtime contract

Fixed-step scheduling:
1. read bath and neighbor states at time `t` (snapshot)
2. sample receptors and concentrations
3. update controller internal state
4. propose machine activations, subject to hardware/configuration constraints
5. allocate energy/material; resolve competing fluxes and conflicts
6. integrate motion, contact, membrane integrity, and chemical field
7. apply damage, division and death transitions when physically enabled
8. capture immutable `StateFrame(t+1)` and causal `EventBatch(t,t+1)`

**Crucial rule:** no action occurs without an installed effector and affordable resources. Failed intentions remain separately logged from realized transport, secretion and repair.

The current discrete-patch commons is an **experimental strategic abstraction**, not a faithful instance of this new spatial runtime. Keep both experimental engines, linked by stable observation/trace concepts—not misleadingly by shared physical assumptions.

## 3. Rendering target: minimalist machine-like Petri dish

Reference direction: dark, nearly black-blue lab instrument, dominant circular dish, clean two-layer membranes, small distinct internal functional nodes, mint/amber/violet semantic accents, soft lighting without photorealism. Do not degrade the screen to network diagrams or cards; the environment occupies **roughly 65–75%** of the central viewport.

### Spatial layers in painter's order

0. background: grid/noise suppressed to near invisibility
1. dish: dark radial fill + thick outer rim + thin glass inner rim
2. substrate: faint continuous gradients, only when supported by a field model
3. nutrients/waste/toxin particles: precise positions when present; deterministic decorative dots only in *explicit schematic mode*
4. cell shadows and body: membrane outer line, inner line, interior fill; restrained wobble only when simulated deformation exists
5. machine organs: receptor patches on edge, enzyme clusters, internal controller, transport channels; use category-specific minimalist geometry
6. effects: field arrows, bounded secretion, signal pulses and uptake lines only when event backed
7. state labels: agent ID, role, energy/integrity sparklines on select/hover; scale-dependent label reduction
8. microscope overlay: crosshair, scale (only when units calibrated), focus selection, comparison controls
9. peripheral panels: agent inspector, causal timeline, legend, experiment metadata

### Cell appearance specification

- Membrane: two concentric irregular but clean strokes, outer thickness 2–3 px at 1080p; mint neutral, warm amber when selected; alpha fill 8–16%; internal bilayer separated by 4–6 screen pixels at normal zoom.
- Shape: nonuniform closed contour with low-order Fourier variation *only as a visual skin for a real boundary*; never show invented deformation as physical evidence.
- Material exchange: tiny crossing particles and thin arrows, always from recorded flux. No free-floating signal effects merely to make the world lively.
- Machine identity: membrane transporters = paired bars, receptors = edge pins, metabolic cluster = three dots + link, controller = small circuit triangle, genome = compact coiled-segment mark (future).
- Differentiation: module topology, membrane size, silhouette, and iconography matter more than unique hues; color is secondary.
- Multi-scale: at zoomed-out scale a two-ring disk; at mid-scale modules appear; at close scale internal arrows/energy fluxes appear.
- Death: loss of membrane integrity only if physics supports it; otherwise distinct × death annotation and dimmed entity.

### UI structure

Top toolbar: run identifier, seed, model, learning regime, clock, pause/step/record, speed (simulation tick/time not frame rate).
Center: one large Petri dish; comparison switches to two synchronized dishes on request.
Right inspector (collapsible): membrane, metabolism, sensors, action/control evidence, neighbors, lineage. Missing variables show **not tracked**, never fabricated zeros.
Bottom compact causal log: icons and verb + actor/target + intention/result distinction; filtering by agent, event type, machine subsystem.
Optional left rail: layers toggle (nutrients, gradients, membranes, messages, lineage, energy, forces), colorblind alternative symbols, labels.
Tools: hover, select, pin, follow, compare, scrub, frame-step, screenshot/video export.
Accessibility: text alternative for every icon and measured color state; keyboard-select and reduced-motion mode.

## 4. Event-first visualization contract

All visual animation is derived from `VisualizationFrameV02`:
```json
{
  "schema": "cells.visualization.frame.v0.2",
  "run": {"id":"example","seed":1,"engine":"commons|spatial","dt":null},
  "tick": 0,
  "space": {"kind":"categorical|2d_metric","bounds":null,"units":null},
  "cells": [{
    "id":"A0","role":"scripted|learned","patch":0,"position":null,
    "energy":null,"membraneIntegrity":null,
    "modules":null,"alive":true
  }],
  "patches": [{"id":"P0","stock":null,"position":null}],
  "events": [{
    "id":"evt-0","kind":"signal_sent","actor":"A0",
    "target":"P0","intent":true,"outcome":null,
    "evidence":"authoritative_trace_row"
  }]
}
```
Here `null` is an unknown/not recorded reading, not zero. `space.kind=categorical` prohibits presenting arbitrary agent positions as measured coordinates; patch placement is visibly marked **SCHEMATIC**. `space.kind=2d_metric` requires real simulation coordinates and units before any scale bar or physical trajectory is shown.

### Event renderer states

`signal_sent` -> violet outbound ⚑ wave at known sender only; `signal_heard` -> ⌁ at measured receiver; `harvest_action` -> ↘ intention marker, then resource delta separately; `repair_attempt` -> ♻, success only with `repair_success`; `death` -> × fade; `metabolize` -> flux arrows only with recorded transfer; `division` -> daughter animation only with recorded parent-child transition.

Aggregate restoration success in old commons CSV has **no patch ID**. Show ✓ WORLD in the log/header, not on a particular patch.

### Architecture modules

- `SimulatorAdapter`: reads native export; never computes unrecorded world transitions
- `TraceValidator`: checks schema, order, unique IDs, finite values, physical vs categorical positions
- `MicroscopeScene`: camera/dish, layers, LOD and scene graph
- `CellGlyphRenderer`: procedural membrane and fitted modules; schematic fallback when modules unknown
- `EffectRenderer`: event-owned animation, timing, provenance
- `Inspector`: reads selected cell's actual recorded state, observation and learned policy evidence
- `ReplayClock`: deterministic tick mapping with speed and scrubbing
- `CaptureHarness`: fixed viewport, stable fonts and deterministic MP4/screenshot production

## 5. Research experiments

- identical starting budgets: fixed, scripted, learning controllers
- ablations: receptor sensitivity, membrane permeability, ATP conversion efficiency, transport limits, controller memory
- communication reliability gradient, receiver-side observation evidence, coalition formation
- inherited module variation and reproduction only after stable conservation audits
- record per-run configuration/seed, train/test partition, paired control, survival, fitness, resource throughput, cooperation, and lineage diversity
- compare game outcomes using payoff matrices, empirical best responses/regrets and strategy-conditioned ecology when experiment supports them

## 6. Delivery gates

**Gate A — concept fidelity:** 1600×900 screen with circular dish dominating; membranes bilayer readable at 100%; particles visible but not visually noisy; inspector/log compact; consistent color and typography; approved reference comparison screenshots.

**Gate B — trace fidelity:** event animation has source event ID; sent vs heard, attempt vs success; no phantom motion, causality, or flux; aggregate repair never placed on a patch.

**Gate C — spatial physics:** quantitative positions, explicit units and conservation checks before enabling true microscopic motion and scale bars.

**Gate D — biological automata:** first executable module set (membrane, sensor, transporter, metabolizer, controller, effector), validated mass/energy ledgers, deterministic seed replay and behavior variation from machine configuration.

**Gate E — evolution:** heritable machine variation, resource-coupled division, selectable trait distribution and multi-generation lineage export.

## 7. Immediate prioritized work

1. Correct remaining commons viewer semantics (especially aggregate repairs and observed-heard patch meaning), pass browser screenshot test and record a reference-comparable demo.
2. Add `VisualizationFrameV02` adapter and validator so UI does not infer physical state from action-only CSV.
3. Prototype authentic 2D automata with six installed modules, explicit resource pools, and per-step conservation checks.
4. Upgrade visual layer to render **real** module locations and field observables, plus a clickable internal machinery inspector.
5. Once 1–4 pass, build reproduction, lineage, and evolutionary/game-theoretic comparisons.

**Non-goals:** fabricated cell motion to make sparse commons logs exciting, photorealistic cell textures, pretending all six modules exist in current strategic-only commons, or calling a cell's fixed controller 'emergent intelligence' without measured evidence.
