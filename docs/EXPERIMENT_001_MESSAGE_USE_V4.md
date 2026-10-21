# Experiment 001 v4 — Learning whether to trust messages

Status: exploratory; implementation and evaluation completed in headless CI.
The null outcome is preserved without tuning for a positive result.

## Design

This experiment advances v3's fixed signal-weight assumption by giving
three followers a **learned binary trust/ignore decision** when a public
patch signal is received. A common periodic sender remains scripted.
The candidate trust/ignore rule is a two-action contextual bandit:
reward-updated Q-values with learning rate 0.18 and exploration 0.12.
The follower's local-cue-only choice is the ignore action; the signaled
patch is the trust action.

200 seeded training episodes (510000 through 510199) update the
follower trust values. Values are snapshotted and frozen; 30 disjoint
held-out seeds (610000 through 610029) evaluate trained trust/ignore
versus identically coded untrained trust/ignore, both receiving signals.
A third condition turns delivery off with the trained values.

Primary seed-paired metric: frozen trained ON collective utility minus
frozen untrained ON collective utility. Diagnostic counts: number of
exposed follower decisions and trusted decisions. The two alternatives
use the same policy and seeded environment; no updates occur on test
seeds.

```bash
dotnet run --project Tests/Headless/Cells.HeadlessChecks.csproj --configuration Release -- --messageuse001 artifacts/experiment001-messageuse-v4
```

## Verified result (2026-10-20)

Execution: https://github.com/dylandlr/cells/actions/runs/39512377900

- **4,318 headless assertions passed**, including exact deterministic replay.
- Trained minus untrained held-out collective utility: **0.00**.
- Paired bootstrap 95% interval: **[0.00, 0.00]** (all 30 paired
  outcomes identical; this is not an estimate of broad robustness).
- With delivered signals, trained and untrained agents each had **960
  signal-exposed decisions** and **64 trust decisions** on held-out seeds.
- On this test distribution, learned trust behavior **did not
  differ** from the untrained policy. The training scheme has not
  demonstrated useful learned message use.

Do not substitute the positive resource-learning result from v3 as
evidence for learned communication. A sensible next experiment is
to analyze whether messages offer predictive value beyond the recipient's
local cues, then evaluate sender incentives/communication choices.
Those would be new explorations, not hidden modifications of v4.

## Research interpretation

A greater held-out return would indicate transfer of a learned
message-response rule *in the toy game*. A null or negative difference
is equally valid. Since the signal source remains scripted, this is
**not learned signal generation, emergent language, reciprocity,
evolution, or biological behavior**. All agents share the same
simple resource game. Learning progress over time and alternative
communication incentives remain to be evaluated.

The next integration boundary is a versioned agent observation/action
contract shared with Unity—not a claim that the chemistry solver already
supports this policy.
