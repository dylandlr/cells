# Course-aligned cellular agents

Design decision: 2026-11-05. Proposed architecture, not an executed experiment.

## Source authority

The retrieved project proposal is [cells: Strategic Learning and Genetic Adaptation in a Microscopic Ecology](https://docs.google.com/document/d/1gTzRC8SaXYoxKCAXrku1Az5tpshZ6sXlQzuZ3VCeJSE/edit), modified October 12, 2026. Its revised deliverables version agrees on the central experiment. These are student proposal documents, not independent proof of instructor approval or the final grading rubric. The uploaded Project-Class.txt emphasizes strategic LLM agents, multi-agent interactions, equilibria and learning dynamics. Repository summaries of professor feedback are secondary evidence; obtain the original feedback/rubric before claiming full compliance.

The proposal asks whether costly communication with regret-based selection improves cooperation and payoff-relevant specialization under scarcity and partial observability. It specifies four agents, 80 simultaneous decision rounds, frozen genomes, and terminal utilities. Genetic evolution is an extension.

## Architecture decision

Use a slow strategic controller above a shared physical execution layer. Direct LLM decisions, fixed rules, EXP3 selection and random selection must all receive the same allowed observations and action capabilities. Each organism has separate local history. The controller never gets source coordinates, hidden resource inventories, another agent's private state, or direct transform/velocity access.

The fast physical controller converts bounded commands to paid actuator work. A recurrent chemotaxis controller is an optional learned executor. Train and validate it separately, then freeze and share it across strategic comparisons. Introducing different motor competence in different strategic arms would confound the comparison. The existing one-action CellsMotorV1 adapter is an engineering baseline, not the proposed full strategic agent.

The novel hypothesis to investigate is whether physically costly sensing, movement and signaling change which strategies pay off and whether complementary behaviors appear. Novelty is provisional; learned chemotaxis and learned morphogenesis already exist in related work.

## Experiment 001 contracts

| Proposal commitment | Implementation contract |
| --- | --- |
| Fixed forage/reciprocate rules | Frozen scripted policy through the common adapter |
| Direct LLM decisions | Structured local observation and bounded action request; pin model, prompt and inference budget; log actual calls and returned concise explanations |
| EXP3 over four frozen LLM-authored policies | Generate and review four policies before evaluation; archive provenance; select a policy at episode start; update only from realized terminal utility during training |
| Random policy selection | Sample from exactly the same frozen four-policy library |
| Communication ablation | Compare enabled/disabled channels with all other capabilities matched; distinguish retrained no-channel arms from intervention on an already trained policy |
| Composite-action ablation | Compare equivalent primitives with/without composition, matching simulated time and physical opportunities; report planning-budget differences |
| Frozen genomes | No mutation, inheritance or evolutionary selection in the primary study |
| Four agents, 80 decisions | Snapshot observations for all agents, collect actions, resolve them jointly, then advance physics for a fixed interval |
| Explicit utility | Freeze coefficients after the pilot: terminal survival, energy and integrity; physical expenditure debited once, with no cooperation bonus |
| At least 30 paired held-out worlds | Disjoint pilot/training/test sets; pair environment randomness; separate RNG streams for policies, receptors and fields |
| Empirical game analysis | Policy-profile payoff estimates, restricted unilateral deviation gains and uncertainty; no unrestricted Nash-equilibrium claim |
| Specialization | Measure individual action/resource contributions; compare shuffled-action and randomized-role controls |
| Reproducibility | Observation/action records, actual cost and delivery ledgers, state hashes, seed streams, frozen configuration and model/policy provenance |

The reward must not subtract energy expenditures again if their effect is already included in terminal energy, unless that extra preference is explicitly defined and justified. Physical costs, scientific utility and API inference cost are different quantities and must be reported separately.

During held-out evaluation freeze policy parameters and EXP3 distributions; recurrent state may update. Evaluate online adaptation separately if needed. EXP3 regret guarantees do not establish equilibrium or general cooperation in this changing multi-agent environment.

## Biological interface

Start with fixed morphology and material-limited physiology. Observe noisy receptor occupancy at actual membrane sites, internal reserves, integrity, and local mechanical load. Preserve sampling history within the agent. Implement physical action requests for motor effort, feeding allocation, repair and secretion; validate budgets before execution. No action is a shortcut to an outcome called "cooperate" or "specialize."

Structured communication in the proposal and chemical sensing are distinct channels. First declare message cost, delay, locality and delivery semantics for the course experiment. Chemical signals can carry a limited, predefined encoding in a later physical-channel condition. Do not silently grant text messages arbitrary bandwidth through a scalar chemical field, or describe a designed encoding as emergent language.

Growth, receptor redistribution and morphogenesis remain follow-up interventions. Freeze development rules and initial morphology in the first comparison; do not add adaptive anatomy simultaneously with a new strategic controller. Later compare fixed and plastic bodies under matched material/energy budgets. Mutation-by-learning factorial experiments remain separate from developmental plasticity.

## Immediate implementation sequence

1. Preserve the repaired Unity demo and run its EditMode/PlayMode validation. A passing structure check is insufficient.
2. Define a versioned shared strategic observation/action schema, simultaneous-round scheduler and auditable cost ledger. Test locality, action-order invariance, no resource creation and actual delivered messages.
3. Connect four agents with frozen morphology to one resource-scarcity scenario. Reuse prior headless studies as baselines; do not transfer their numerical findings to Unity without new runs.
4. Implement and compare the four promised strategic controller families. LLM inference requires explicit configuration and real outputs; handwritten policies cannot be labeled LLM-authored.
5. Pilot reward scales and message semantics; freeze protocol, then run held-out paired evaluations and restricted payoff analysis.
6. Add recurrent motor learning and plastic receptor/body allocation as controlled extensions, with baseline checkpoints retained.

The single-cell swimming task is a diagnostic prerequisite, not the course's final research result. A convincing demo must expose actions, paid costs, received messages and consequences for four interacting agents, alongside reproducible payoff results.

## Remaining external requirements

Confirm the submitted/approved proposal version, original professor feedback, final rubric, exact deadlines and presentation constraints. Do not block implementation on these unknowns; do not present student-proposed deliverables as confirmed instructor mandates.
