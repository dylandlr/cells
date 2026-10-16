using UnityEngine;
using Cells.Fluid;

namespace Cells.Physiology
{
    /// <summary>
    /// Phenomenological rear-flagellum motor. Oscillation is an explicit actuator state,
    /// sampled by motility and by visualization. This is not a resolved Stokes-fluid solver.
    /// </summary>
    [RequireComponent(typeof(CellMetabolism))]
    [DefaultExecutionOrder(-60)]
    public sealed class CellFlagellarActuator : MonoBehaviour
    {
        [Min(.05f)] public float length = 1.05f;
        [Min(0f)] public float waveAmplitude = .24f;
        [Min(0f)] public float beatFrequencyHz = 2.2f;
        [Range(0f,1f)] public float motorActivation = .75f;
        [Range(0f,1f)] public float structuralIntegrity = 1f;
        [Min(0f)] public float motorCostPerSecond = .012f;
        public CellMetabolism metabolism;
        public FluidGrid2D fluid;
        [Min(0f)] public float fluidCoupling = 1.6f;

        public float PhaseRadians { get; private set; }
        public float EffectiveThrust { get; private set; }
        public float ActualActivation { get; private set; }

        void Awake() { if (!metabolism) metabolism = GetComponent<CellMetabolism>(); }
        void FixedUpdate() => Tick(Time.fixedDeltaTime);

        public void Tick(float dt)
        {
            if (dt <= 0f || float.IsNaN(dt) || float.IsInfinity(dt)) return;
            if (!metabolism) metabolism = GetComponent<CellMetabolism>();
            if (!metabolism || !metabolism.alive || structuralIntegrity <= 0f)
            {
                EffectiveThrust = 0f;
                ActualActivation = 0f;
                return;
            }

            float requested = motorCostPerSecond * motorActivation * dt;
            float paid = metabolism.Spend(requested);
            ActualActivation = requested > 0f ? motorActivation * Mathf.Clamp01(paid / requested) : motorActivation;
            if (!metabolism.alive) ActualActivation = 0f;

            // Force magnitude is a bounded proxy for geometry/beat efficiency, not an
            // exact flagellar hydrodynamic calculation.
            EffectiveThrust = Mathf.Clamp01(ActualActivation * structuralIntegrity
                * (beatFrequencyHz / 2.2f) * (length / 1.05f)
                * (waveAmplitude / .24f));
            PhaseRadians = Mathf.Repeat(PhaseRadians + 2f * Mathf.PI * beatFrequencyHz * dt
                * ActualActivation, 2f * Mathf.PI);
            if (fluid && EffectiveThrust > 0f && !GetComponent<FlagellarHydrodynamics2D>())
            {
                // A low-Reynolds-number swimmer is force-free. Represent its
                // coarse-grained near-field flow by a front/rear force dipole,
                // not a net momentum source. This is not resolved hydrodynamics.
                Vector2 front = transform.TransformPoint(new Vector3(.48f, 0f));
                Vector2 aft = transform.TransformPoint(SampleCenterline(.85f));
                fluid.AddForceDipole(front, aft,
                    (Vector2)transform.right * (EffectiveThrust * fluidCoupling));
            }
        }

        /// <summary>Local-space centerline, anchored on the posterior (-X) membrane.</summary>
        public Vector2 SampleCenterline(float normalizedDistance)
        {
            float u = Mathf.Clamp01(normalizedDistance);
            float envelope = Mathf.Sin(u * Mathf.PI * .5f);
            float lateral = Mathf.Sin(PhaseRadians - 2f * Mathf.PI * u * 1.35f)
                * waveAmplitude * envelope * structuralIntegrity;
            return new Vector2(-.78f - length * u, lateral);
        }
    }
}
