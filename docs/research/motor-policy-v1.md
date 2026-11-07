# Motor policy boundary v1

Status: source implementation, not Unity-validated; no trained model or ML package installed.

Attach CellMotorPolicyPort only to experimental swimmers with a CellFlagellarActuator
and FlagellarHydrodynamics2D. Existing demo organisms are unchanged by default.
The port starts at zero activation. Disable it to restore the prior motor activation
and scripted-control selection. Do not attach multiple competing controllers.

Schema `cells.motor-policy.v1`: five float observations, in order:
1. Energy reserve, positive compression x/(1+x).
2. Nutrient concentration sampled at the cell center, same compression.
3. Toxin concentration sampled at the cell center, same compression.
4. Actual motor activation [0,1].
5. Flagellum structural integrity [0,1].
Nonfinite observations become zero. No position, global nutrient map, spatial
chemical gradient, or preferred direction is exposed. Temporal memory will require
observation stacking or a recurrent policy; v1 does not provide receptor history.

Action: one continuous motor activation [0,1]; NaN/infinity or dead metabolism
commands zero. Actions persist until replaced. Adapter must submit before the
actuator FixedUpdate (execution order -60); model scheduling is not yet implemented.

External control bypasses scripted heading changes, chemical speed modulation and
velocity smoothing. Physical yaw, energy charging, flow advection and boundary
constraints remain. Both actuator and hydrodynamics are required for active motion.
This is throttle-only: it does not implement learned tumble selection or steering.
Existing physics remains an uncalibrated approximation, including movement cost.

Next gate: run CellMotorPolicyPortTests and existing motility tests in Unity, then
inspect a powered and unpowered swimmer in Play Mode. Add an ML-Agents adapter
with CollectObservations/OnActionReceived only after selecting a compatible package
through Unity Package Manager. Keep the core independent of package dependencies.
Reference: https://unity-technologies.github.io/ml-agents/Learning-Environment-Design/

Episode reset, rewards, trainer configuration, inference model import and behavior
trees are not part of this increment. Reuse isolated matched-seed worlds when these
are added; never report exposure as uptake or claim successful training from source.
