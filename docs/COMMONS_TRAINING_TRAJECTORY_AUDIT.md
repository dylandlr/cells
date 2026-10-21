# Commons learning trajectory audit

## Aim
Inspect how coordinated restoration develops during 400 training
episodes for the matched gamma=0 and gamma=0.95 credit-assignment study.
This is a diagnostic extension, not a new independent sample or
preregistered confirmation.

## Implementation
`CommonsDelayedLearningV1` now captures one training row per episode:
`episode,seed,collective_return,successful_repairs,survivors,accepted_intentions`.
The `--commons-matched` command exports
`immediate-training.csv` and `delayed-training.csv` alongside the
existing held-out records. GitHub CI validates episode numbering,
seed distinctness and record counts, printing successive 100-episode
means to distinguish gradual acquisition, sudden transitions and
instability.

The experiment still uses the same finite-substrate commons and a
scripted sender. Delayed-return learning is not equivalent to
spontaneously learning when and what to communicate.

## Next research gates
- Verify learning trajectories and compare early/late held-out
  snapshots, not solely final-policy scores.
- Inspect per-seed causal traces to distinguish response to messages
  from incidental restoration.
- Introduce a learnable sender only as a separate preregistered
  control after proving that delivered messages create distinct
  restoration opportunities.
- Keep Unity chemistry and microscope state separate until a
  versioned observation/action adapter passes parity tests.

No claim of emergent communication or genetic coevolution follows
from successful CI execution alone.
