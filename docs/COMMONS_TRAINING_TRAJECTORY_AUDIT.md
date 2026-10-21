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

## Verified corrected results

GitHub Actions: https://github.com/dylandlr/cells/actions/runs/39519851440

Build and **4,666 headless assertions** passed. A first trajectory
run exposed a measurement bug: invitation responses were accumulated
over all episodes. Code now subtracts the counter at each episode start
and reports per-episode responses. The corrected 400-episode run produced:

| Episodes | Delayed mean return | Delayed restorations | Delayed accepted invitations | Immediate mean return | Immediate accepted invitations |
|---|---:|---:|---:|---:|---:|
| 0–99 | +67.591 | 23.02 | 46.31 | -17.989 | 1.42 |
| 100–199 | +65.068 | 22.99 | 49.84 | -19.801 | 0.68 |
| 200–299 | +68.593 | 23.51 | 48.47 | -19.876 | 0.74 |
| 300–399 | +67.329 | 23.23 | 46.41 | -19.565 | 0.68 |

The delayed learner already shows strong cooperation in the first
100-episode block. This block-averaged evidence alone cannot tell
whether learning was gradual or occurred very early. All statistics are
descriptive of this one continuous training trajectory, and the reward
target was designed after earlier exploratory experiments.

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
