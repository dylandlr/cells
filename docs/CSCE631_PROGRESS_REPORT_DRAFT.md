# CSCE 631 — Project Progress Report (Draft for Instructor Review)

**Project:** cells — Resource-Constrained Microscopic Multi-Agent Learning  
**Course:** CSCE 631 Intelligent Agents, Fall 2026  
**Reporting date:** October 20, 2026  
**Status:** Working experimental research prototype; final deliverables incomplete  
**Submission status:** Draft, not submitted. Student name, instructor, team and due date to confirm.

## 1. Research question and motivation

Our project investigates how simple agents operating under resource scarcity learn to communicate, coordinate and maintain a shared environment. The broader scientific goal is to connect **game-theoretic learning and interaction** to a microscopic, visually inspectable simulation and, in later stages, **genetic–strategic coevolution**.

For the course, the tractable research question is:

> Can delayed-reward learning produce better cooperative resource restoration than immediate-reward learning, and how does the reliability of communication affect collective outcomes?

This report distinguishes the current **headless renewable-commons experiment** from the still separate **Unity biological sandbox**. Findings in one should not be presented as experimentally validated results in the other.

## 2. Implemented system

### Experimental world

The headless C# `RenewableCommonsV0` simulator models **four agents and three resource patches**. Agents pay action-specific costs to harvest, restore, signal or rest. Successful restoration requires **at least two simultaneous restoration attempts** at one patch. A **finite substrate reserve** supplies restoration transfers; messages are one-step delayed and carry a patch index. The environment contains stock, hazard, energy, mortality and a seeded stochastic replenishment process.

The current communication experiments use **one scripted signaling agent** and **three trained followers**. The follower is an eight-state, two-choice tabular learner conditioned on local scarcity, whether a message was heard and low energy. A message-response choice restores a signaled patch only if a signal is available; otherwise it falls back to harvesting.

### Training and evaluation

We implemented separate world seeds for training and frozen held-out evaluation, matched immediate-reward and discounted-return updates, baseline conditions, message-on/off controls, independent reception randomness, seed-level CSV outputs, reproducibility checks and GitHub Actions automation.

The current default comparison uses **400 training episodes**, **30 held-out worlds**, **100 steps per world**, learning rate 0.08 and action exploration probability 0.12. The delayed-return update uses discount factor 0.95; its matched immediate-return control uses discount factor zero. Policy parameters are frozen at evaluation, although stochastic exploratory action selection remains active.

## 3. Results to date

### 3.1 Matched reward-credit assignment

| Frozen trained follower | Collective return | Successful restorations / episode | Mean survivors / four |
|---|---:|---:|---:|
| Immediate reward | -20.490 | 0.733 | 0.133 |
| Discounted return | +70.383 | 22.800 | 4.000 |

The paired held-out collective-return advantage was **+90.873** across 30 common worlds. This supports an important role for **future-return credit assignment in this particular simulated task**; it does not establish general cooperation or equilibrium convergence.

Source: [matched comparison](COMMONS_MATCHED_CREDIT_ASSIGNMENT.md); [CI experimental run](https://github.com/dylandlr/cells/actions/runs/39519293574).

### 3.2 Learned rule and robustness

The discounted learner already achieved a mean held-out return of **+66.390 after one training episode**. Five distinct training-world histories subsequently achieved mean held-out returns between +66.390 and +73.163, with mean survivors four of four. This rapid acquisition suggests a **small effective decision rule**, not evidence of advanced emergent social cognition.

A sender-period perturbation caused coordination to collapse, motivating a more controlled message-reception test. We replaced deterministic step-index masking (which produced a phase artifact) with event-based and then randomized message delivery.

Source: [checkpoints](COMMONS_FINAL_CHECKS.md); [sender robustness audit](COMMONS_SENDER_ROBUSTNESS_AUDIT.md).

### 3.3 Communication delivery reliability

Under independently randomized message receipt, holding the trained follower policy and scripted sender rule fixed:

| Broadcast delivery probability | Mean restorations | Mean return | Mean survivors |
|---:|---:|---:|---:|
| 25% | 1.233 | -19.700 | 0.233 |
| 50% | 3.067 | -17.493 | 0.500 |
| 60% | 5.733 | -10.530 | 1.033 |
| 75% | 10.333 | +9.633 | 1.933 |
| 90% | 17.367 | +41.100 | 3.133 |
| 100% | 22.800 | +70.383 | 4.000 |

Collective welfare changes sign between **tested** 60% and 75% conditions. This is **not** proof of a general critical threshold. The comparison involves one trained policy, a fixed sender rule and a single environmental family.

Source: [randomized reception](COMMONS_RANDOM_DELIVERY_RESULTS.md); [CI run](https://github.com/dylandlr/cells/actions/runs/39533979278).

### 3.4 Negative result: training with unreliable messages

Five independently configured training reception conditions (50%, 60%, 75%, 90%, 100%) were evaluated against seven held-out reception levels, using the same compact architecture. **Simply exposing this agent architecture to dropped messages did not yield robust collective performance.** Mean welfare at test delivery 50% and 60% remained negative for every training condition.

This motivates changing the agents' available coordination actions and partial observations rather than merely increasing training iterations.

Source: [Phase A robustness study](PHASE_A_RELIABILITY_TRAINING_RESULTS.md); [CI run](https://github.com/dylandlr/cells/actions/runs/39537028039).

## 4. Limitations and threats to validity

- **Scripted organizer.** The communication sender does not learn its signaling decisions. No spontaneous communication protocol has emerged.
- **Restricted action abstraction.** The follower cannot independently repair without a delivered message; this strongly limits robustness by construction.
- **Incomplete empirical game-theoretic analysis.** Full empirical payoff tables, unilateral deviation analysis, approximate equilibrium and regret measurements for the current commons task remain incomplete.
- **Not yet an LLM comparison.** Current commons policies are tabular. An LLM-agent baseline must be designed and measured if required by the course's final evaluation.
- **Exploratory design.** Experiments and modeling decisions evolved in response to earlier findings. Results are not preregistered; fresh test worlds and independently varied parameters are needed for confirmatory conclusions.
- **Simplified physics and ecology.** Although restoration draws from a finite substrate reserve, complete biomass/waste conservation, organismal inheritance and calibrated biological kinetics are not implemented in the experimental commons.
- **Visualization separation.** Unity microscope scenes and the headless experiment are separate systems, and Unity Play Mode validation remains incomplete.

## 5. Plan for the remaining course work

**Milestone 1 — Game-theoretic evaluation.** Formalize the game as an empirical stochastic/multi-agent setting; compare candidate strategy profiles using fixed-policy rollouts; estimate payoff tables with confidence intervals; quantify deviation incentives, best responses and learning/regret measurements.

**Milestone 2 — Action-space robustness.** Give followers an explicit independent restoration attempt and limited partner cues. Repeat reliability-by-training matrices against the existing architecture, with multiple independent training histories and untouched held-out worlds.

**Milestone 3 — Agent baseline and presentation.** Add a constrained LLM-driven or other course-approved strategic baseline, ensuring matched observation/action budgets and reproducible prompts/outputs. Produce recorded traces and an instructor-facing visual demonstration.

**Milestone 4 — Final deliverables.** Prepare the project demonstration, documented code/experiment commands, reproducibility archive, final paper and slides. Confirm the professor's precise rubric, report due date and submission requirements before marking any deliverable complete.

## 6. Compact scientific visualization track

In parallel with course experiments, develop a **small, event-first evolutionary microscope**, not a visually elaborate game. An event stream should drive symbols for agents, finite nutrient units, signal packets, reproduction, mutation and death. Animated events must correspond to actual measured state transitions; renderers may interpolate motion but must never fabricate biological transitions.

Initial minimal reproduction model to implement separately: finite nutrients → uptake/metabolism → energy budget → heritable trait variation with seeded mutation → reproduction under explicit energy/material costs → mortality → population/tradeoff time series. Claims of evolution should require tracking inherited variation and changed trait frequencies across generations; a toy visual fixture is not itself evidence of evolutionary emergence.

Keep scientific model outputs, recorded trace schema and UI rendering separate. A headless deterministic model and replayable event log should be tested before connecting the visualization to Unity.

## 7. Current deliverable inventory

- Source: [`dylandlr/cells`](https://github.com/dylandlr/cells), branch `feat/substance-ontology`.
- Experimental results, scripts, audit reports and manifests: `Assets/CellsDemo/Scripts/Experiments/`, `Tests/Headless/`, `docs/` and CI workflow artifacts.
- Small scientific visual prototype: see `tools/evolution-microscope/` once implemented and verified. Distinguish its illustrative mechanics from the research experiment and from the Unity runtime.

**Bottom line:** This is a substantive progress report for a working experimental prototype, not a final-project completion declaration. The central empirical result is delayed-credit cooperative restoration under scripted information, plus a robustly documented failure mode when communication is unreliable.
