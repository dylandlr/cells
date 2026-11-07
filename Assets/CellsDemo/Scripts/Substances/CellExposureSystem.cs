using UnityEngine;
using Cells.Physiology;

namespace Cells.Substances
{
    /// <summary>Samples chemical exposure on the membrane, with spatial provenance.
    /// UptakeRate is the whole-cell coefficient; each sector receives 1/N of that area.</summary>
    [RequireComponent(typeof(CellMetabolism), typeof(MembraneIntegrity))]
    public sealed class CellExposureSystem : MonoBehaviour
    {
        public MultiChemicalField2D field;
        public ChemicalEventLog events;
        public CellMetabolism metabolism;
        public MembraneIntegrity membrane;
        public SoftCellBody2D softBody;
        public int organismId;
        public string substanceId = "toxin-B";
        [Min(0)] public float uptakeRate = .1f;
        [Min(0)] public float damagePerUnit = 5f;
        [Min(0)] public float beneficialEnergyPerUnit;
        [Min(0)] public float internalDamageFraction = .25f;
        [Min(0)] public float membraneRadius = .9f;
        [Min(1)] public float damagedPermeabilityMultiplier = 3f;
        public Transform intakeSite;
        readonly Vector2[] locations = new Vector2[MembraneIntegrity.SegmentCount];
        readonly float[] requests = new float[MembraneIntegrity.SegmentCount];

        void Awake()
        {
            if (!metabolism) metabolism = GetComponent<CellMetabolism>();
            if (!membrane) membrane = GetComponent<MembraneIntegrity>();
            if (!softBody) softBody = GetComponent<SoftCellBody2D>();
        }

        public Vector2 SurfacePosition(int segment) => transform.TransformPoint(
            softBody && softBody.Initialized ? softBody.SurfacePoint(segment) :
            (membrane ? membrane.SurfacePoint(segment, membraneRadius)
              : MembraneIntegrity.DirectionOf(segment) * membraneRadius));
        void FixedUpdate() => Tick(Time.fixedDeltaTime);

        public void Tick(float dt)
        {
            if (!field || !metabolism || !membrane || !metabolism.alive || !MultiChemicalField2D.Finite(dt) || dt <= 0f) return;
            if (membrane.ruptured)
            {
                KillIfNeeded(transform.position);
                return;
            }
            int count = intakeSite ? 1 : MembraneIntegrity.SegmentCount;
            int siteSegment = intakeSite ? MembraneIntegrity.SegmentAt(transform.InverseTransformPoint(intakeSite.position)) : 0;
            // Snapshot all local concentrations before any withdrawal: sampling itself is read-only.
            for (int i = 0; i < count; i++)
            {
                int segment = intakeSite ? siteSegment : i;
                locations[i] = intakeSite ? (Vector2)intakeSite.position : SurfacePosition(segment);
                float permeability = Mathf.Lerp(1f, Mathf.Max(1f, damagedPermeabilityMultiplier), 1f - membrane.GetIntegrity(segment));
                requests[i] = Mathf.Max(0f, uptakeRate) * permeability * field.Sample(substanceId, locations[i]) * dt / count;
            }
            Vector2 lastInjury = transform.position;
            for (int i = 0; i < count; i++)
            {
                int segment = intakeSite ? siteSegment : i;
                float amount = field.Withdraw(substanceId, locations[i], requests[i]);
                if (amount <= 0f) continue;
                string structure = "membrane.sector[" + segment + "]";
                if (events) events.Record(Time.fixedTime, organismId, substanceId,
                    ChemicalEventKind.Absorbed, amount, locations[i], structure);
                float injury = membrane.ApplyExposureToSegment(segment, amount * Mathf.Max(0f, damagePerUnit) * MembraneIntegrity.SegmentCount);
                if (injury > 0f)
                {
                    lastInjury = locations[i];
                    metabolism.Injure(injury * Mathf.Max(0f, internalDamageFraction));
                    if (events) events.Record(Time.fixedTime, organismId, substanceId,
                        ChemicalEventKind.Damaged, injury, locations[i], structure);
                }
                if (metabolism.alive && beneficialEnergyPerUnit > 0f && !membrane.ruptured)
                    metabolism.Feed(amount * beneficialEnergyPerUnit);
            }
            KillIfNeeded(lastInjury);
        }

        void KillIfNeeded(Vector2 location)
        {
            if (membrane.ruptured && metabolism.alive) metabolism.Injure(metabolism.energy);
            if (!metabolism.alive && events)
                events.Record(Time.fixedTime, organismId, substanceId, ChemicalEventKind.Died, 0f,
                    location, membrane.ruptured ? "membrane.rupture" : "metabolism");
        }
    }
}
