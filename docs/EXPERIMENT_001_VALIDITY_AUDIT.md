# Experiment 001 — Scientific validity audit (2026-10-20)

**Disposition: NOT CLEARED for claims of emergent communication or
biological intelligence.** The existing v1–v4 records are retained as
exploratory software experiments; passing deterministic tests is
necessary but insufficient for scientific validity.

## Code inspection: findings and severity

| Finding | Severity | Evidence in current implementation | Required corrective action |
|---|---|---|---|
| v4 trust/ignore actions may be identical | HIGH | `MessageUseExperiment001V4.TrustPolicy.Decide` chooses local highest cue, and the trust path chooses the advertised patch. When they coincide, the two abstract actions yield exactly the same game action and reward. Exposure and trust counts alone cannot identify *behaviorally effective* communication. | Record action disagreement at exposures and compute policy sensitivity conditional on disagreements. |
| v4 state collapses all meaningful information | HIGH | v4 trust Q-table indexes only the binary presence/absence of any signal, not cue quality, sender identity, conflicts or patch congestion. | Introduce an observation contract that represents local-vs-advertised evidence and incentives. |
| v4 sender is scripted and nearly all learned control is on the recipient | HIGH | `PeriodicSignaler` remains seat 0, fixed, in all treatments. A binary response policy is not emergent message generation. | Add optional strategic send/withhold actions and sender controls in a separate future experiment. |
| v2/v3 Q-state merges distinct resource situations | HIGH | `AdaptivePolicy.Decide` indexes Q by whether *any* local cue is positive, not location, resource signal pattern, occupancy or previous reward. | Establish state-aliasing diagnostics and train/test learning curves before extending the model. |
| v2 fixed signal weight is not learned | HIGH | `AdaptivePolicy` adds `genome.SignalWeight * ReceivedSignals[p]` without reward updates to that coefficient. | Do not describe v2/v3 results as evidence of learned communication. |
| v3 held-out shift limited | MEDIUM | Training and test seeds are disjoint but use the same stochastic world generator and fixed composition. | Evaluate systematically altered resource distributions / incentives, once independently designed. |
| Online reward improvement may be narrow reward fitting | MEDIUM | v2 `Learn` uses immediate reward with Q-table indexed by cue class and chosen patch; not a long-horizon credit-assignment or strategic-learning model. | Compare within-episode learning curves; repeat with placebo updates; avoid claims of sophisticated RL. |
| Environment ring-distance comment misstates sensing | LOW | `Observe` comments 'Ring distance', but the code gives only own patch a cue and all other patches -1. This is not ring adjacency. | Correct model description, then redesign spatial visibility if required. |
| Energy does not constrain actions or survival | MEDIUM | Energy accumulates each reward, but `Advance` never uses it to restrict actions; there is no death/fission pressure. | Distinguish 'payoff accounting' from physiological survival. |
| Audit callbacks are mutable | MEDIUM | `SignalingGameV1.OnDecisionStep` is a public field and receives mutable observation arrays. An external subscriber can modify messages in its local copied observations, and arbitrary callback side effects can affect controls. | Provide read-only immutable trace records or defensively verified logging hooks. |
| Previous significance intervals exploratory | MEDIUM | Hyperparameters and successive mechanisms were added after inspecting v1–v4 results. | Label prior results exploratory; freeze a research question and split before confirmatory tests. |
| Inherited traits are not genomes | HIGH | v2 `Genome` holds three immutable numeric parameters; there is no genetic transmission or mutation. | Name as fixed trait configuration; implement genetics only if course deliverables require it. |

## Concrete deterministic regression gates introduced

`Tests/Headless/Experiment001ValidityChecks.cs` exercises:
- ON vs OFF world equivalence under identical submitted actions.
- Resource sharing and reward conservation including movement/foraging costs.
- Signal timing (message at the *next* decision step), blocked delivery, fixed sender cost.
- Learned reward update, independent Q snapshots, and defense against mutating a reloaded snapshot.
- A simple signal/local agreement scenario where 'trust' is not an observable behavioral change.

These are **mechanical sanity tests**, not proof of learning quality or emergent intelligence.

## Interpretation of unusual prior outcomes

- The negative effect of communication in v1 is expected to be sensitive
  to crowding and the cost of signal-aligned convergence. The design
  allows this result but does not prove that signal exchange is
  generically harmful.
- The positive learning ablation in v2 and held-out return in v3 show
  performance gains of a narrow bandit-like policy on the same world
  generator. They do not show learned social or communicative competence.
- The exact-zero v4 result is **not yet explained by training data**.
  Since `TrustPolicy`'s choice is only a binary trust/ignore state and
  two choices often map to the same patch, we must measure distinct
  action opportunities and Q-values before calling it a negative
  empirical finding.

## Experimental release gate

Do not declare E001 a valid demonstration of emergent intelligence until:
1. Environment accounting and message semantics pass unit tests.
2. Belief/action opportunities are externally visible and nontrivial.
3. Learning updates demonstrably modify actions in a handcrafted test,
   and learned strategy survives matched, disjoint-seed evaluation.
4. Appropriate learning-disabled, communication-disabled and scripted
   baselines isolate the claim.
5. Professor-facing proposal/rubric terms are checked against the
   evaluation protocol before the progress report.

The existing Unity biochemical model is untouched by this audit.
No browser-only microscope screenshot is a substitute for logged
behavioral evidence.
