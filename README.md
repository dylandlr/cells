# cells

A Unity microscopic chemistry and physiology sandbox with a human-facing causal microscope.

## Run

Open this folder in **Unity 6000.6.5f1 (Unity 6.6)**, open
`Assets/CellsDemo/Scenes/MicroscopeCulture.unity`, and press Play. If the scene is absent,
use **Cells > Create or open microscope culture**. This denser, adherent culture uses
phase-contrast-inspired grayscale rendering, irregular contours and varied static
intracellular texture. Every visible cell has metabolism, exchange and leakage.
Local boundaries deform under contact and injury and relax when released. The centers
remain anchored; there is no synchronized beating or scripted pulsing.

The smaller two-organism experiment remains available in `ChemistryDemo.unity` and
**Cells > Create or open chemistry demo**. Windows builds start in the culture view.

- Click a cell or its named button to inspect energy, membrane integrity, and recent events.
- Scroll over the world to zoom; F follows selection; Escape clears it.
- Space or Pause stops simulation while inspection remains usable.
- Food, Toxin, and Reagent buttons deposit measured quantities at the selected membrane region (or origin).
- The region slider moves the pipette around the boundary; inspection reports local concentration, mean integrity and weakest-region integrity.
- Repair spends the selected living cell's energy. A ruptured membrane cannot be repaired.
- Toggle **Show false-color chemical concentrations** to inspect the underlying field.
  It is hidden initially in the culture view; chemistry continues running.

## What is simulated

Independent dissolved species diffuse with no-flux boundaries. Field storage is mass per
voxel; microscope queries report mass per local area. First-order decay is explicitly
accounted for. An illustrative local reaction, toxin + reagent -> neutralization product,
conserves equal-mass reactants and creates their combined product mass. Suspended food is
tracked separately as seeded Brownian particles; ingestion removes actual packets.
Feeding moves material into an internal reserve; digestion releases a separate metabolic
byproduct before supplying energy. Damaged membranes leak nutrient mass back into the field,
including after death. Secretion costs, concentration-dependent exposure, local injury,
rupture and energy depletion change authoritative simulation state.

These are phenomenological models in arbitrary units, not calibrated molecular chemistry
or validated biological toxicology. Cell centers remain stationary in this demo. Membrane injury is
spatially sampled across 32 regions with state-driven visible openings; fluid flow through
pores, full mechanical tearing, osmosis, evolution, and learned behavior remain future work.
Nutrient leakage uses a well-mixed internal compartment and conservative surface transfers. Human substance names and colors are not organism sensor inputs.

## Live motility diagnostics (experimental)

The two swimming focal organisms have a measured-state overlay:
green = simulated swimmer velocity, blue = local fluid advection,
amber = signed appendage-induced angular velocity (tangential marker).
These arrows are drawn in `LateUpdate` from physics state and never
actuate the organism. They are initially enabled and can be disabled
individually using the `SwimmerTelemetryOverlay` component checkboxes.

The microscope's whole-field flow overlay still samples the fluid grid.
The displayed swimmer vector is the model's current velocity before
fluid advection; it is not a measured trajectory derivative. Chemistry
steering may also turn the organism and is deliberately **not** shown by
the amber marker.

Visual appearance and performance have not yet been verified in Unity
Play Mode; run the local scene and Unity tests before using any footage
as research evidence.

## Interrupted-assay cleanup (unverified)

The Play Mode batch runner now uses iterator-finally cleanup and a
component-disable safeguard to deactivate and destroy an in-progress
world. If there are completed treatments but the batch is interrupted,
it saves only those completed outcomes to a separate
`chemotaxis-outcomes-v1-partial.csv` (or corresponding configured
basename). Only a fully completed schedule produces the normal output
CSV and `CELL_ASSAY_COMPLETE` log marker. Errors writing partial
results are surfaced in the Unity Console; disabled-runner cleanup may
still be subject to platform file I/O errors.

No Unity Editor executable was available in the development container,
so these lifecycle paths are not yet Play Mode tested. Test interruption
after several completed trials, confirm no active prior-world objects
remain, and verify partial rows are not mistaken for a complete batch.

## Fixed-step assay synchronization hardening

The trial recorder now increments an authoritative `RecordedSteps` counter in
`FixedUpdate` and resets it at `BeginTrial`. The batch runner waits until the
requested number of **recorded** steps, verifies an exact count at capture,
and deactivates the completed root before deferred destruction so no
additional physics ticks can leak into the next treatment.

This addresses identified scheduling risks, but does not establish a passing
Play Mode run. The batch runner still requires Unity execution to confirm
coroutine ordering and evaluate scene lifecycle. The runner deliberately
produces no empirical outcomes in this documentation.

## Assay nutrient deposition audit (validation in progress)

The isolated assay bootstrap now accumulates the **actual** nutrient
mass returned by each `MultiChemicalField2D.Deposit` call into
`AssayNutrientDeposited`. Its Edit Mode test asserts positive
deposition that does not exceed the requested total budget.
This detects basic failed/oversized deposits, but does not yet establish
cross-seed equality or mass conservation after diffusion and uptake.
The field-level comparisons and Play Mode execution remain pending.

## Seed-paired assay improvements (experimental)

In assay mode, nutrient locations now derive from the environment seed using
five fixed-mass patches inside a bounded assay region. Trials with the same
seed and different controllers see the same patch positions and total
requested deposited nutrient mass; different seeds change the positions.
The seed does not currently randomize toxin locations, physical coefficients,
or initial organism phenotype.

The planned controller execution order rotates `spatial -> temporal ->
stochastic` by seed-block index; each controller still occurs exactly
once per seed. This reduces simple order bias but does not substitute
for rigorous randomized blocking. Nutrient deposits may be quantized by
field discretization, so actual world mass should be audited before
assuming exact mass equality.

New Edit Mode tests inspect the patch generator and revised schedule.
They are committed, **not yet run in Unity**. Batch-runner Play Mode
execution and CSV inspection remain required.

## Executable chemistry assay batch (Unity Play Mode; unverified)

Create an empty Unity scene with one GameObject containing
`ChemotaxisBatchRunner`. Leave `runOnStart=true` and configure
`firstSeed`, `replicates`, and `horizonSeconds`. Play Mode
constructs one fresh `ChemistryDemoBootstrap` per planned trial with
`assayMode=true` and one focal swimmer. Each trial is run for a fixed
number of physics steps, then its root is destroyed and a new world is
built. When all trials complete, the runner writes
`chemotaxis-outcomes-v1.csv` (or the requested basename) to
`Application.persistentDataPath`. Look for `CELL_ASSAY_COMPLETE` in
the Unity Console for the file location. Avoid placing a separate
auto-building chemistry demo in the same scene.

**Experimental limitations:** only stochastic tumble streams vary by
seed at present; chemical deposition and starting position remain fixed.
The three modes use the same prototype phenotype, but their turning and
propulsion rules differ by design. Treatment order is fixed, not
counterbalanced. This runner is meant to collect initial descriptive
data; seed pairing does **not yet** imply diverse matched environmental
replicates or calibrated causal estimates. The scene and tests have not
been executed in Unity. Compare output only after validating that
reset isolation, physics order, and mass/energy accounting hold in
Play Mode.

## Matched chemotaxis assay protocol v1

`ChemotaxisAssayPlanV1.Build(firstSeed,replicates)` deterministically emits
three treatments per seed: `spatial`, `temporal`, `stochastic`.
The protocol CSV explicitly marks **fresh world reset required** on every
trial. The separate outcome CSV schema captures elapsed time, path length,
absorbed/digested nutrient mass, motor ATP, final energy, tumble count,
and survival. Unexecuted plans produce **no fabricated outcome rows**.

This is planning/export infrastructure, not an operational batch runner:
Unity scene resets, identical genotype and location, treatments applied to
one experimental focal organism, fixed observation horizon, treatment-order
counterbalancing, per-run telemetry capture, and actual file writes are
still to be integrated. Editor tests have been authored but not executed.
No population claims or empirical win rates should be inferred.

## Nutrient mass accounting for chemotaxis trials

The cell chemical-exchange system now records cumulative mass withdrawn from
the nutrient field and cumulative mass digested into the represented waste
field. The trial recorder subtracts start-of-trial baselines and emits
`nutrientAbsorbed` and `nutrientDigested` alongside nutrient exposure,
path length, and energy use. Absorption and digestion are distinct quantities;
neither should be inferred from local concentration readings.

The current chemistry microscope logs two different organisms in a shared
environment, so those logs are observational only—not a matched-control,
multi-seed causal comparison. Nutrient mass-accounting tests have been
authored but not executed in Unity. A separate experiment runner with
environment resets and equal initial conditions remains necessary.

## Chemotaxis trial telemetry (experimental)

`ChemotaxisTrialRecorder` is attached to both focal chemistry-demo swimmers.
It records actual post-clamp path length, time-integrated local nutrient
concentration (exposure only, **not uptake**), total cellular energy change,
authoritative cumulative flagellar motor ATP spend, tumble count, and life
status. Log lines start with `CELL_TRIAL ` followed by a JSON snapshot,
and are emitted every 0.2 simulated seconds by default.

These logs are **not yet a controlled multi-controller experiment**:
the two default swimmers have different phenotypes and start positions,
and the present camera scene is not a matched-seed comparison protocol.
A proper experiment needs matched environments and repeated randomized
replicates with environment resets between treatments, including a baseline.

The recorder tests are authored but have not been executed in Unity. CPU
and log-volume impact need Play Mode profiling before scaling populations.

## Motor ATP / thrust integration regression coverage

`FlagellarMotorGatingTests` exercises the real actuator and energy reserve:
normal running versus throttled tumble expenditure, return to running
thrust, completely disabled motor behavior, and exhaustion-induced shutdown.
These are authored Unity Editor tests, **not yet executed** in Unity 6.6.

Before using experimental results, run Edit Mode tests and observe the
chemistry microscope in Play Mode, checking for frame-order effects between
motility motor commands, flagellar actuation, appendage geometry, and flow.
Motor energy decreases on a tumble command applied by the next actuator tick;
there is currently no validated wall-clock biological calibration.

## Timed tumble motor gating (experimental)

A tumble now starts a timed motor-state interval (default 0.22 s) that
reduces behavioral motor activation (default multiplier 0.12), attenuates
the requested swimming target, and consequently reduces actuator ATP
consumption while the state is active. Each swimmer retains independently
seeded stochastic tumble decisions; the state prevents additional tumble
starts until the previous one completes.

This is an intentionally simplified coupling: the controller chooses a new
heading and the motor state throttles the flagellar beat; the simulation
does **not** resolve bacterial flagellar bundle switching or a hydrodynamic
tumble torque. Physics execution ordering means motor commands set during
motility are applied on the following fixed step, while requested velocity
attenuation begins immediately. The current controller also applies its
existing heading turn law during the timed tumble.

New editor tests cover timer/recovery semantics but are not verified in
the Unity runtime yet. Benchmarks and calibration are still required.

## Stochastic chemotaxis experiment (unvalidated)

The two focal swimmers now default to a seeded run-and-tumble heading controller.
It samples concentration only at the cell's current location, compares it against
an exponentially adapting receptor baseline, and increases the instantaneous
tumble hazard after unfavorable changes. Each decision uses
`P(tumble)=1-exp(-hazard*dt)` and a seeded pseudorandom turn angle.
Separate organism IDs derive distinct, reproducible seeds.

The heading controller is not a mechanistic motor-switching model:
tumble events do not yet modulate flagellar power, explicitly pause forward
swimming, or instantiate a measured biological reorientation duration.
Also, reproducibility is conditional on the same step sequence; cross-timestep
trajectory invariance has not been demonstrated. Rate constants are exploratory
and not calibrated to a named bacterial species. Unity Test Runner and actual
Play Mode inspection remain pending.

## Coupled flagellar mobility (experimental)

Free-swimming organisms use a symmetric planar 3-DOF resistance matrix,
derived from the current local segment geometry, to jointly solve
translation (x/y) and rotation about z. A Cholesky-based solver rejects
ill-conditioned or non-positive-definite matrices. Velocity and yaw remain
bounded, with force/torque diagnostics tested independently from Unity.

This model is a local resistive-force approximation, not a validated
low-Reynolds-number Navier–Stokes / Stokes boundary solver. It does not
currently account for near-wall hydrodynamic corrections, full
body/appendage hydrodynamic interactions, or exact reciprocal coupling
to the rendered fluid field. The same simulated organism may be steered
by a separate chemical policy; its policy yaw is not a hydrodynamic torque.

Unity Editor tests for the new solver have been authored but have not
been executed in this environment. Run the Unity Test Runner before
accepting the physics increment.

## Provenance and validation

Continues `feat/substance-ontology`, PR #1. The separately described **Causal Microscope
v0.3 ZIP was not available in this workspace or the inspected local project/download
locations**; this project develops the repository's existing code, and does not claim
that archive was migrated.

See [integration and test results](docs/INTEGRATION_STATUS.md),
[model units](docs/CHEMISTRY_MODEL.md), and [test instructions](docs/UNITY_CI_SETUP.md).
Actual Unity test reports and runtime imagery, if produced, are distinguished from mockups.
The optional [camera recording workflow](tools/capture/README.md) produces real Unity
frames with measured-state captions. It excludes the IMGUI panel and adds no beating.
