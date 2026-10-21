# Controlled communication delivery audit — broadcast-event ablation

The earlier robustness assay changed sender actions while changing message frequency, making attribution ambiguous. This follow-up leaves the sender's **decision rule and signal costs unchanged**, and varies reception independently.

## Method

The frozen discounted-return follower policies are evaluated on 30 common held-out seeds with identical sender code and intervention levels: every broadcast delivered, every second broadcast event delivered, or every third broadcast event delivered. Delivery is filtered after actions and payoffs are applied; undelivered signals are still costly to send. This is **event-count** subsampling, not step-number gating. A preliminary step-number mask was rejected because a sender emitting every fourth step and a receiver mask on even steps can hide all broadcasts through deterministic phase alignment.

The environment and sender policy are otherwise identical at the beginning of each paired world. Later sender actions can differ indirectly because changing followers' responses changes patch depletion and hence sender observations. These are policy-level treatment effects, not an identical counterfactual sender-action trace.

## Research limitations

The sender is scripted; policy response is not the discovery of a communication protocol. Delivery perturbation tests reliability of established followers under partial reception, not learned choice by senders. Results should be measured for return, repairs, survival and actual responded invitations. A robust policy should not depend on an exact message cadence unless that dependence is a testable property of the experimental ecology.

See the headless CI step **Analyze matched sender-rule delivery cadence** and the `commons-delivery-cadence` artifact.
