# Experiment 001 controller transport

This small adapter exposes the existing `SignalingGameV1` to an external controller.
It does not introduce new dynamics, train a policy, or establish an LLM result.
Experiment 001 remains the only required deliverable; see `EXPERIMENT_001_SCOPE_FREEZE.md`.

Build `Tests/Headless/Cells.HeadlessChecks.csproj` in Release, then run:

```sh
dotnet Tests/Headless/bin/Release/net8.0/Cells.HeadlessChecks.dll --strategic-session 1200000 on
```

Use `off` to suppress message delivery while preserving signaling costs.
The session always lasts 80 rounds. Standard input/output are newline-delimited JSON.
The initial `session` event records protocol version, seed, delivery condition and horizon.
A `decision` event supplies a step number and four seat observations.

**The controller is trusted infrastructure. Never send the whole event stream to an agent.**
Give each agent only its own observation and its own permitted history, together with
frozen rules/prompt. Keep world seed, other seats' observations/actions/rewards and the
combined audit log out of agent context. This adapter does not enforce isolation between
external model calls; the future controller must do that. Collect all four responses before
submitting a batch; agents must not see another agent's current-round choice.

Submit exactly one action per seat, in any order:

```json
{"step":0,"actions":[{"agent":0,"code":6},{"agent":1,"code":6},{"agent":2,"code":6},{"agent":3,"code":6}]}
```

Codes 0–2 forage at the selected patch, 3–5 signal that patch, and 6 rests.
Only the submitted fields are accepted. Invalid JSON, missing/extra/duplicate fields,
stale rounds, duplicate seats and out-of-range actions produce `error` without advancing.
The controller may retry that same round. Input closure emits `aborted`, never a completed
summary. Choose and freeze a bounded retry/timeout policy before an actual model study;
this transport itself has no timeout and does not silently substitute actions.

Each accepted batch emits a `transition` with the pre-action local observations, canonical
seat-ordered actions and realized rewards. The next `decision` follows. After round 79,
`summary` reports the existing game's returns and counters. Capture stdout as an audit
JSONL file. Replay by starting the same seed/condition and resubmitting each transition's
step and actions. Pin the source revision and .NET runtime alongside the log; seeded RNG
reproducibility is not promised across runtime changes. These logs do not yet capture
model identifiers, prompts, raw outputs or inference costs, which a model controller must add.

Validation:

```sh
dotnet build Tests/Headless/Cells.HeadlessChecks.csproj -c Release
python -m unittest discover -s Tests/Headless -p 'test_strategic_session.py'
```

Set `CELLS_DOTNET` if dotnet is not on PATH. Checks cover complete episodes, atomic rejection,
partial-observation fields, replay, seat-order invariance and costly delivery ablation.
The transport is for the symbolic resource game; it is not a biomechanical or chemotaxis result.
