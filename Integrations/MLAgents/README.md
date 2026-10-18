# ML-Agents motor experiment

Source-ready integration, NOT compiled, trained or runtime-verified in Unity yet.
The adapter template stays outside Assets until ML-Agents resolves, keeping existing
projects compilable without an optional dependency. The setup menu uses UPM's Client
API; it does not manually rewrite the manifest. Pin: Unity package 4.0.0 / Python
mlagents 1.1.0 (official release 23 pairing).

## Setup on Windows
1. Open cells in Unity 6. Run Cells > ML Agents > 1 Install ML Agents package.
   Requested package: com.unity.ml-agents@4.0.0; UPM resolves inference dependencies.
2. After package resolution and compilation, run menu 2 Enable adapter source.
3. In a new empty scene, add an empty GameObject with CellForagingAgent.
4. Configure its Behavior Parameters: Behavior Name `CellsMotorV1`, Behavior Type
   Default, Vector Observation Space Size 5, Stacked Vectors 4, Continuous Actions 1,
   no discrete branches. Add Decision Requester, Decision Period 5, Take Actions
   Between Decisions enabled. Use a .02-second fixed timestep.
5. Add a camera for viewing if desired. Do not add ChemistryDemoBootstrap or a batch
   runner to this scene: the Agent creates an isolated world itself. One training
   arena per scene for this initial integration.
6. In a Python 3.10.12 virtual environment, install `mlagents==1.1.0`, following the
   release 23 installation instructions for supported PyTorch dependencies.
7. From the repository root run:
   `mlagents-learn config/cells-motor-ppo.yaml --run-id=cells-motor-v1 --seed=631`
   Then press Play in Unity when prompted.

Official release pairing: https://github.com/Unity-Technologies/ml-agents/releases/tag/release_23

## Experiment contract
The persistent Agent rebuilds a fresh assay hierarchy each episode. Old worlds are
inactive before deferred destruction; bootstrap owns and releases generated assets.
Environment seeds increment from 631. For held-out evaluation choose a disjoint seed
range and repeat it for each baseline. ML-Agents' trainer seed is separate from this
world seed. No parallel arenas or reproducibility claims until runtime verification.

Reward per completed simulation step: increase in digested nutrient mass minus 0.1
 times increase in motor energy spent. Death adds -1 and terminates. The 30-second
horizon truncates via EpisodeInterrupted, allowing bootstrap value estimation.
Reward units/weights are exploratory; report raw uptake, digestion, energy and
survival alongside return. Low throttle may win; this is not learned steering.

Observations are five v1 port values with four stacked frames (20 inputs). One
continuous action [-1,1] maps to activation [0,1]. Heuristic mode supplies 75%.
No Behavior tree or separate inference worker drives the motor. No trained model
is included. After training, assign the exported compatible model to Behavior
Parameters and select Inference Only for evaluation.

## Required validation before training claims
- Run existing Edit Mode tests including CellMotorPolicyPortTests.
- Verify the 5/4/1 observation/stack/action configuration matches the trainer.
- In Heuristic Only, confirm powered swimming and motor expenditure.
- Verify death and time-limit resets replace the world, reset counters, and remove
  old objects/assets across 100 episodes without growing object counts.
- Verify reward deltas equal recorded digestion and motor expenditure; no reset reward.
- Confirm trainer handshake, finite observations/actions/rewards, and a short PPO run.
- Evaluate against motor-off and constant-throttle baselines on held-out seeds.
