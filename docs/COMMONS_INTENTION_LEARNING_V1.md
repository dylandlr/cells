# Commons intention learning v1 — exploratory results and decision

## Run provenance

Executed in GitHub Actions:
https://github.com/dylandlr/cells/actions/runs/39516724477

The headless build and **4,659 assertions** passed. Thirty matched
seeds, 100 decision steps and four conditions produced 120 episode
records. Research outputs are in the run artifact `commons-intentions-study`.

## Protocol

In `CommonsIntentionStudyV1.cs`, a *scripted* sender advertises a
restoration target when locally scarce and periodically able to signal.
Each recipient chooses between local harvesting and restoration at an
advertised patch, using a small state-dependent immediate-reward
learning table. The comparator uses the same response policy with
updates disabled. A communication-disabled learning condition and a
separate deterministic scripted responder provide exploratory controls.

This is **online learning on the same measured episodes**; there are no
training/frozen held-out evaluations, and this is not emergent signal
production. Message delivery is delayed by one step.

## Outcomes (30 paired seeds)

| Condition | Mean total return | Successful repairs | Surviving agents | Followed messages |
|---|---:|---:|---:|---:|
| Learning ON | -19.1400 | 3.8333 | 0.5667 | 200 total |
| Frozen updates ON | -19.9567 | 1.0667 | 0.2667 | 32 total |
| Learning OFF | -20.2667 | 0.0000 | 0.1333 | 0 total |
| Scripted ON | +65.5000 | 24.0000 | 4.0000 | not tracked |

Paired learning ON minus frozen ON mean **+0.8167** collective return,
95% paired bootstrap interval **[-0.1333,+1.9967]**. The interval
includes zero. This is not evidence for reliably improved return.
Successful repairs do increase descriptively.

## Validity warnings

- Sender behavior is explicitly scripted. The agents do not negotiate
  intentions or discover a signaling vocabulary.
- The scripted responder is a policy upper-reference, **not a matched
  learning ablation**; it responds to a specific message channel,
  and the learner chooses between local harvest and any advertised
  patch. Its large return cannot be directly attributed to learning.
- Reward updating uses the immediate personal payoff. Restoration
  pays a *negative* immediate reward, while replenishment benefits
  future harvesters, so the feedback rule undervalues collective
  investment. The slight utility improvement is not robust at 30 seeds.
- The state representation still omits reputation, expected partner
  responses, delayed value, and depletion dynamics outside the local
  patch.
- The economy's substrate reserve is bounded but harvested material
  is not recycled into biomass or waste, and the strategic world is
  not the Unity biophysical world.

## Correct next milestone

Rather than enlarging the policy or searching for favorable seeds,
implement delayed-reward credit assignment with small episodes,
partner-intention features, and frozen policy evaluation against
disjoint worlds. First test a tiny deterministic restoration task
where joint investment should beat selfish depletion by a known
amount. Keep current data frozen as exploratory evidence.

This does not modify Unity physics or constitute a default-branch merge.
