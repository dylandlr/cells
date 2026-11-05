using UnityEngine;
using Cells.Substances;

namespace Cells.Physiology
{
    /// <summary>Overdamped radial mechanics for an adherent 2D cell. No oscillator or beating.
    /// Rest-shape elasticity, local contact repulsion, contour smoothing and area recovery
    /// compete; injury retracts and softens the affected region. Arbitrary mechanical units.</summary>
    [RequireComponent(typeof(MembraneIntegrity))]
    [DefaultExecutionOrder(-30)]
    public sealed class CellShapeDynamics : MonoBehaviour
    {
        public MembraneIntegrity membrane;
        public CellShapeDynamics[] neighbors;
        public float radius = .9f;
        [Min(0)] public float contactStiffness = 3f;
        [Min(0)] public float elasticRecovery = 1.5f;
        [Min(0)] public float areaRecovery = 2f;
        readonly float[] next = new float[MembraneIntegrity.SegmentCount];
        float restArea;
        CellChemicalExchange exchange;

        void Start()
        {
            Initialize();
            if (neighbors == null) neighbors = transform.root.GetComponentsInChildren<CellShapeDynamics>();
        }

        public void Initialize()
        {
            if (!membrane) membrane = GetComponent<MembraneIntegrity>();
            exchange = GetComponent<CellChemicalExchange>();
            restArea = membrane.EnclosedArea(radius);
        }

        void FixedUpdate() => Tick(Time.fixedDeltaTime);

        public void Tick(float dt)
        {
            if (!MultiChemicalField2D.Finite(dt) || dt <= 0f) return;
            if (!membrane || restArea <= 0f) Initialize();
            // A fixed upper step keeps the explicit overdamped solve stable for test callers too.
            int steps = Mathf.CeilToInt(Mathf.Min(dt, 1f) / .01f);
            float step = Mathf.Min(dt, 1f) / steps;
            for (int substep = 0; substep < steps; substep++)
            {
                float area = membrane.EnclosedArea(radius);
                float pressure = Mathf.Clamp((restArea / Mathf.Max(.001f, area) - 1f) * areaRecovery, -.3f, .3f);
                for (int i = 0; i < next.Length; i++)
                {
                    float current = membrane.GetStretch(i);
                    float injury = 1f - membrane.GetIntegrity(i);
                    float desired = 1f - .18f * injury;
                    float force = elasticRecovery * (desired - current) + pressure * (1f - injury);
                    force += .6f * (membrane.GetStretch((i + 31) % 32) + membrane.GetStretch((i + 1) % 32) - 2f * current);
                    Vector2 point = transform.TransformPoint(membrane.SurfacePoint(i, radius));
                    if (neighbors != null)
                    foreach (var other in neighbors)
                    {
                        if (!other || other == this || !other.membrane) continue;
                        if (((Vector2)other.transform.position - (Vector2)transform.position).sqrMagnitude > 5f) continue;
                        Vector2 local = other.transform.InverseTransformPoint(point);
                        float boundary = other.radius * other.membrane.ContourScale(local) * other.membrane.StretchAt(local);
                        float penetration = Mathf.Max(0f, boundary - local.magnitude);
                        force -= contactStiffness * penetration / radius;
                    }
                    next[i] = Mathf.Clamp(current + force * step, .72f, 1.08f);
                }
                for (int i = 0; i < next.Length; i++) membrane.SetStretch(i, next[i]);
            }
            // Named exchange sites move with the same authoritative boundary.
            if (exchange)
            {
                if (exchange.feedingPore) exchange.feedingPore.localPosition = membrane.SurfacePoint(16, radius);
                if (exchange.secretionPore) exchange.secretionPore.localPosition = membrane.SurfacePoint(0, radius);
                if (exchange.metabolicOutlet) exchange.metabolicOutlet.localPosition = membrane.SurfacePoint(24, radius);
            }
        }
    }
}
