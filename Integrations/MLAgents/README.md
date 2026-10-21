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
3. After compilation, run Cells > ML Agents > 3 Create or open training scene.
   This creates a dedicated scene, camera, persistent agent and Decision Requester.
   Existing training scenes are opened without overwriting their settings; unsaved
   scene edits trigger Unity's normal save prompt.
4. Generated settings: Behavior Name `CellsMotorV1`, Default behavior, vector size 5,
   four stacked vectors, one continuous action, no discrete actions; decision period
   5 with actions repeated between decisions. The project timestep must be .02 seconds.
   The adapter checks these settings at startup and reports mismatches explicitly.
5. Do not add ChemistryDemoBootstrap or a batch runner: the Agent owns the assay world.
   Use a single arena for this initial integration.
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

## Episode diagnostics and baseline checks
Each completed episode emits one `CELL_ML_EPISODE` JSON line with schema
`cells.ml-episode.v2`: ending (death or time_limit), environment seed, recordedSteps,
elapsedSeconds, nutrientAbsorbed, nutrientDigested, motorEnergy, pathLength, reward,
and alive. An interrupted editor session does not emit a completed episode row.
For fixed motorPenalty w, expected return is digestion - w * motorEnergy - death,
where death is 1 for a death ending and 0 otherwise (allow float accumulation error).
A completed horizon alone does not establish that a trainer was connected.

Use Heuristic Only with heuristicActivation=0 for a motor-off control or .75 for
constant propulsion. Repeat the same firstSeed and number of episodes for comparisons.
For trained evaluation, use Inference Only with the trained model and held-out seeds.
Do not change reward weights during an episode. The logs are diagnostics, not yet an
automated evaluation report. Reset/resource tests and the PPO handshake remain pending.

## Compare completed evaluation runs
Set evaluationLabel to a meaningful policy/model identifier. Use separate logs for
Inference Only and Heuristic Only runs, with identical seeds, horizon, fixed timestep
and motor penalty. Do not mix training episodes into the evaluation files.
After updating adapter source through menu 2, collect v2 logs and run from repo root:

```sh
python tools/evaluate_motor_logs.py learned-eval.log constant-eval.log --candidate-model results/cells-motor-v1/CellsMotorV1.onnx --output constant-comparison.json
python tools/evaluate_motor_logs.py learned-eval.log motor-off-eval.log --candidate-model results/cells-motor-v1/CellsMotorV1.onnx --output motor-off-comparison.json
```

The standard-library tool validates each record before reporting means and paired
candidate-minus-baseline differences. It refuses missing/duplicate seeds, timing
inconsistency, invalid rewards, mixed policy labels, nonfinite data, empty logs and
older schemas. Output is descriptive JSON, not a significance claim. Motor penalty
and horizon are captured at episode start. Keep configuration fixed during runs.
Model identity labels are user-supplied, not cryptographic provenance; the tool does
not prove that inference used the intended model or that evaluation seeds were held
out from training. Preserve model files and training/evaluation seed ranges separately.

Validation performed here: seven Python tests passed using synthetic fixtures.
No Unity episodes or trained-policy performance measurements have been collected.

## Real-environment smoke test before PPO
With the documented Python dependencies installed, run:

```sh
python tools/smoke_motor_environment.py --episodes 3 --activation 0.75
```

Open MotorTraining in Unity with Behavior Type Default and press Play when prompted.
Do not run mlagents-learn at the same time: only one Python process should own the
connection. The smoke script verifies one 20-float observation (5 values x 4 stacks),
one continuous action, finite observations/rewards and three completed episodes.
It supplies constant motor commands, closes the connection on success/failure, and
returns a nonzero exit code on failure. `--build` accepts a standalone training
executable if one has been built; no executable is included. `--seed` is the ML-Agents
environment seed; the assay's firstSeed remains configured on the Unity agent.

The shutdown path now notifies ML-Agents before destroying the organism, preserving
terminal sensor access during Agent.OnDisable. Normal episode completion already
notifies ML-Agents before OnEpisodeBegin replaces the world.

Ten Python unit tests pass locally, including smoke-contract checks using fake specs.
The live smoke test has NOT run: these unit tests do not verify a Unity connection,
compiled adapter, real terminal observations, resource cleanup or successful training.

## Standalone Windows training
After updating templates, rerun menu 2 to enable the latest source, then use
**Cells > ML Agents > 4 Build Windows training player**. The command requires
Windows Build Support, validates the scene and agent configuration, and builds only
the training scene to `Build/MLTraining/CellsTraining.exe`. It rejects an assigned
inference model, non-Default behavior, extra training agents and demo/batch runners.
It does not modify the project's global build-scene list. Unsaved-scene cancellation
aborts the build. Share the entire build directory, not the executable alone.

Activate your Python training environment in PowerShell, then from the project root:

```powershell
.\tools\Start-MotorTraining.ps1 -SmokeOnly
.\tools\Start-MotorTraining.ps1 -RunId cells-motor-v1
```

The launcher locates the built executable and training commands, runs three real
smoke-test episodes, and starts PPO only after the smoke process exits successfully.
It preserves your working directory. Existing run IDs are handled by ML-Agents;
this script does not force overwrite or automatically resume prior experiments.
Do not press Play in the Editor for standalone training. The Python process launches
and closes its own player. A smoke pass is an interface gate, not evidence of useful
learning. PowerShell execution and the Windows build have not been tested here.

## Python preflight
The PowerShell launcher now runs `python tools/check_training_environment.py` before
opening the player. It checks the pinned Python range and ML-Agents package versions,
then imports the real environment and trainer entry points to detect dependency errors.
It reports the Python executable and package versions as JSON and returns exit code 2
on incompatibility. It neither installs packages nor modifies your environment.

Smoke testing and training use that same interpreter (`python -m mlagents.trainers.learn`),
so a different `mlagents-learn` command elsewhere on PATH cannot silently select another
virtual environment. This does not check Unity licensing, GPU usability, or model quality.

The Editor build helper checks the serialized m_Model field instead of directly
referencing the inference package's model type. This avoids an unnecessary direct
assembly dependency; compilation in Unity still needs verification.

Validation: 13 Python unit tests passed. Running preflight in the development container
correctly returned not-ready (Python 3.12.14, ML-Agents and PyTorch absent). No live
training result is implied by either the unit tests or this dependency check.


## Comparison file provenance
Comparison report v2 records SHA-256, filename and byte size for each input log and
supplied model. Inference runs require --candidate-model or --baseline-model as
appropriate; heuristic runs reject model arguments. --output creates a new JSON file
and refuses to overwrite an existing report. Without it, JSON prints to stdout.
File identities are supplied-file provenance, not proof that the recorded Unity run
loaded that model. The tool does not parse or validate ONNX. Keep run artifacts intact
while hashing them; concurrent modification is unsupported. Tests use synthetic byte
fixtures and are explicitly not model inference tests. Seventeen Python tests pass.

## Disabled actuator handling
Motor disable now clears activation/thrust immediately. Hydrodynamics disable clears
its velocity/yaw/history; a missing or disabled motor/tail also resets history on the
next hydrodynamic tick. Policy motility requires enabled motor and hydrodynamics
components before applying active translation or yaw. Ambient fluid advection remains
independent: an unpowered organism can still drift with the medium.
Three Unity regression cases cover component disable after powered tail motion.
These tests are written but not executed here. The 17 Python tooling tests still pass;
they do not validate these C# physics changes. Global update-order determinism remains
an open validation question; this change addresses cached-output reuse only.

## Policy ownership and handoff
CellMotorPolicyPort now disallows duplicate components on the same organism. Each
enable cycle captures the current motor activation, behavioral multiplier and control
mode; disabling restores all three. Commands sent to a disabled port remain ignored.
This prevents policy removal from silently changing a scripted controller's motor
attenuation. An added Unity test covers repeated handoffs with different baseline
values; it is not executed here. This does not provide arbitration against unrelated
components that write the same public motor fields while the policy is enabled.
