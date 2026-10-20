# Experiment 001 v4 — Learning whether to trust messages

Status: exploratory, implemented on `feat/substance-ontology`. Numeric
results must be copied from a successful CI run; do not treat this
file as proof that strategic communication emerged.

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
