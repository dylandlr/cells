# Message exchange rate and cooperation

Headless research run: https://github.com/dylandlr/cells/actions/runs/39532831648

30 matched held-out seeds, 100 steps, frozen learned followers, fixed scripted sender decision rule. Broadcast delivery rates are 100%, 50%, 33%, 25%; all signal costs remain charged. Successful joint resource restoration is the measure of cooperation.

| Delivery | Mean repairs | Mean collective return | Mean survivors |
|---|---:|---:|---:|
| 100% | 22.800 | +70.383 | 4.000 |
| 50% | 2.300 | -18.967 | 0.367 |
| 33% | 0.967 | -19.953 | 0.200 |
| 25% | 0.367 | -20.363 | 0.100 |

Time-resolved measurements appear in the CI artifact commons-exchange-curve, including exchange-timeseries.csv and exchange-curve.csv. CI passed 4,673 headless assertions.

Full delivery sustains cooperation throughout the 100-step test; lower delivery rates mostly lose successful cooperative restorations during the first 40 steps. This suggests a fragile coordination regime, NOT a confirmed phase transition. Broadcast sub-sampling is deterministic and may interact with periodic sender timing. The sender policy is scripted, and outcomes remain exploratory.

Next: randomized receipt at intermediate delivery probabilities, separate reception RNG, repeated training histories, and uncertainty intervals.
