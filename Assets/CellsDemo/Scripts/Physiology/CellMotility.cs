using UnityEngine;
using Cells.Substances;
using Cells.Fluid;

namespace Cells.Physiology
{
    /// <summary>
    /// Minimal low-Reynolds motility model for free-swimming cells.
    /// The controller may choose a heading, but translation is overdamped, speed-limited,
    /// energetically costly, and derived only from local chemical samples.
    /// </summary>
    [RequireComponent(typeof(CellMetabolism))]
    public sealed class CellMotility : MonoBehaviour
    {
        public MultiChemicalField2D field;
        public CellMetabolism metabolism;
        public CellFlagellarActuator flagellum;
        public FlagellarHydrodynamics2D hydrodynamics;
        public FluidGrid2D fluid;
        [Range(0f,1f)] public float flowEntrainment = .8f;
        public string attractantId = "nutrient-A";
        public string repellentId = "toxin-B";
        public bool useTemporalChemotaxis;
        public TemporalChemotaxisSensor temporalSensor = new TemporalChemotaxisSensor();
        public float TemporalResponse => temporalSensor.Response;
        // Optional policy-controlled signal chemotaxis; disabled unless a policy activates it.
        public CellChemicalSignaling signaling;
        [Min(0f)] public float signalAttractantGain;

        [Min(.01f)] public float sensorOffset = .65f;
        [Min(0f)] public float maxSpeed = .55f;
        [Min(0f)] public float acceleration = 1.4f;
        [Min(0f)] public float angularSpeedDegrees = 110f;
        [Min(0f)] public float drag = 3.2f;
        [Min(0f)] public float movementCostPerUnit = .05f;
        [Min(0f)] public float attractantGain = 1.0f;
        [Min(0f)] public float repellentGain = 1.6f;
        [Range(0f,1f)] public float exploratoryBias = .12f;

        public Vector2 Velocity { get; private set; }
        public Vector2 LastActiveVelocity { get; private set; }
        public Vector2 LastFluidVelocity { get; private set; }
        public Vector2 LastDisplacementVelocity { get; private set; }
        public Vector2 DesiredDirection { get; private set; } = Vector2.right;
        public float LastAttractantGradient { get; private set; }
        public float LastRepellentGradient { get; private set; }

        float phase;

        void Start()
        {
            if (!metabolism) metabolism = GetComponent<CellMetabolism>();
            if (!flagellum) flagellum = GetComponent<CellFlagellarActuator>();
            if (!hydrodynamics) hydrodynamics = GetComponent<FlagellarHydrodynamics2D>();
            phase = Mathf.Repeat(transform.position.x * 2.137f + transform.position.y * 5.173f, Mathf.PI * 2f);
        }

        void FixedUpdate() => Tick(Time.fixedDeltaTime);

        public void Tick(float dt)
        {
            if (dt <= 0f || !MultiChemicalField2D.Finite(dt)) return;
            if (!metabolism) metabolism = GetComponent<CellMetabolism>();
            if (!metabolism || !metabolism.alive)
            {
                Velocity = Vector2.zero;
                LastActiveVelocity = Vector2.zero;
                LastFluidVelocity = Vector2.zero;
                LastDisplacementVelocity = Vector2.zero;
                return;
            }

            Vector2 forward = transform.right;
            Vector2 side = new Vector2(-forward.y, forward.x);
            Vector2 p = transform.position;

            float aFront = Sample(attractantId, p + forward * sensorOffset);
            float aBack  = Sample(attractantId, p - forward * sensorOffset);
            float aLeft  = Sample(attractantId, p + side * sensorOffset);
            float aRight = Sample(attractantId, p - side * sensorOffset);
            float rFront = Sample(repellentId, p + forward * sensorOffset);
            float rBack  = Sample(repellentId, p - forward * sensorOffset);
            float rLeft  = Sample(repellentId, p + side * sensorOffset);
            float rRight = Sample(repellentId, p - side * sensorOffset);

            Vector2 attractantGradient = forward * (aFront - aBack) + side * (aLeft - aRight);
            Vector2 repellentGradient = forward * (rFront - rBack) + side * (rLeft - rRight);
            LastAttractantGradient = attractantGradient.magnitude;
            LastRepellentGradient = repellentGradient.magnitude;
            Vector2 signalGradient = signaling && signaling.ReadLocal() >= signaling.detectionThreshold
                ? signaling.ReadGradient() * (2f * sensorOffset) : Vector2.zero;

            phase += dt * .73f;
            Vector2 wander = new Vector2(Mathf.Cos(phase), Mathf.Sin(phase));
            Vector2 steering = attractantGradient * attractantGain
                             - repellentGradient * repellentGain
                             + signalGradient * signalAttractantGain
                             + wander * exploratoryBias;

            if (useTemporalChemotaxis)
            {
                // Only receptor samples at the organism's present location
                // influence this mode: no spatial gradient vector is read.
                float receptor = Sample(attractantId,p)*attractantGain
                    - Sample(repellentId,p)*repellentGain;
                if (signaling && signaling.ReadLocal() >= signaling.detectionThreshold)
                    receptor += signaling.ReadLocal()*signalAttractantGain;
                temporalSensor.Tick(receptor,dt);
                phase += dt * temporalSensor.TurnRateDegrees * Mathf.Deg2Rad;
                DesiredDirection = new Vector2(Mathf.Cos(phase),Mathf.Sin(phase));
            }
            else if (steering.sqrMagnitude > 1e-8f)
                DesiredDirection = steering.normalized;

            float currentAngle = Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg;
            float desiredAngle = Mathf.Atan2(DesiredDirection.y, DesiredDirection.x) * Mathf.Rad2Deg;
            float nextAngle = Mathf.MoveTowardsAngle(currentAngle, desiredAngle, angularSpeedDegrees * dt);
            // Flagellar deformation can also create a bounded reaction yaw. Keep the
            // chemotactic controller and the physical appendage contribution explicit.
            if (hydrodynamics)
                nextAngle += hydrodynamics.LocalAngularVelocityDegrees * dt;
            transform.rotation = Quaternion.Euler(0f, 0f, nextAngle);

            Vector2 thrustDirection = transform.right;
            float stimulus = Mathf.Clamp01(.18f + (useTemporalChemotaxis ? Mathf.Max(0f,temporalSensor.Response) : attractantGradient.magnitude * attractantGain)
                                                + (useTemporalChemotaxis ? 0f : repellentGradient.magnitude * repellentGain)
                                                + (useTemporalChemotaxis ? 0f : signalGradient.magnitude * signalAttractantGain));
            // No hidden propulsion: swimming requires actuator output when equipped.
            float motorThrust = flagellum ? flagellum.EffectiveThrust : 1f;
            Vector2 targetVelocity = hydrodynamics
                ? (Vector2)transform.TransformDirection(hydrodynamics.LocalSwimmingVelocity) * stimulus
                : thrustDirection * (maxSpeed * stimulus * motorThrust);

            Velocity = Vector2.MoveTowards(Velocity, targetVelocity, acceleration * dt);
            Velocity *= Mathf.Exp(-drag * dt * .12f);

            float requestedDistance = Velocity.magnitude * dt;
            float requestedCost = requestedDistance * movementCostPerUnit;
            float paid = metabolism.Spend(requestedCost);
            float fraction = requestedCost <= 1e-8f ? 1f : Mathf.Clamp01(paid / requestedCost);
            Vector2 ambientFlow = fluid ? fluid.Sample(p) * flowEntrainment : Vector2.zero;
            Vector2 activeVelocity = Velocity * fraction;
            Vector2 displacement = (activeVelocity + ambientFlow) * dt;

            Vector2 next = p + displacement;
            if (field)
            {
                float halfWidth = field.width * field.cellSize * .5f - .5f;
                float halfHeight = field.height * field.cellSize * .5f - .5f;
                Vector2 local = field.transform.InverseTransformPoint(next);
                local.x = Mathf.Clamp(local.x, -halfWidth, halfWidth);
                local.y = Mathf.Clamp(local.y, -halfHeight, halfHeight);
                next = field.transform.TransformPoint(local);
            }
            LastActiveVelocity = activeVelocity;
            LastFluidVelocity = ambientFlow;
            LastDisplacementVelocity = (next - p) / dt;
            transform.position = new Vector3(next.x, next.y, transform.position.z);
        }

        float Sample(string id, Vector2 point)
        {
            return field && !string.IsNullOrEmpty(id) ? field.Sample(id, point) : 0f;
        }
    }
}
