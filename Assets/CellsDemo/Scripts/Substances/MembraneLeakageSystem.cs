using UnityEngine;
using Cells.Physiology;

namespace Cells.Substances
{
    /// <summary>Outward passive nutrient transfer from a well-mixed internal reserve.
    /// Each membrane/field contact uses an exact two-compartment exchange, in sector order.
    /// Leakage continues after death and never supplies energy or creates material.</summary>
    [RequireComponent(typeof(CellChemicalExchange), typeof(MembraneIntegrity))]
    [DefaultExecutionOrder(20)]
    public sealed class MembraneLeakageSystem : MonoBehaviour
    {
        public CellChemicalExchange exchange;
        public MembraneIntegrity membrane;
        [Min(.01f)] public float membraneRadius = .9f;
        [Min(0f)] public float permeability = .8f;
        public float TotalLeaked { get; private set; }

        void Awake()
        {
            if (!exchange) exchange = GetComponent<CellChemicalExchange>();
            if (!membrane) membrane = GetComponent<MembraneIntegrity>();
        }

        void FixedUpdate() => Tick(Time.fixedDeltaTime);

        public void Tick(float dt)
        {
            if (!exchange || !exchange.field || !membrane || !MultiChemicalField2D.Finite(dt)
                || dt <= 0f || !MultiChemicalField2D.Finite(membraneRadius) || membraneRadius <= 0f
                || !MultiChemicalField2D.Finite(permeability) || permeability <= 0f) return;
            float internalArea = Mathf.PI * membraneRadius * membraneRadius;
            float externalArea = exchange.field.CellArea;
            float inverseAreas = 1f / internalArea + 1f / externalArea;
            float contactLength = 2f * Mathf.PI * membraneRadius / MembraneIntegrity.SegmentCount;
            for (int i = 0; i < MembraneIntegrity.SegmentCount; i++)
            {
                if (exchange.nutrientReserve <= 0f) break;
                float damage = 1f - membrane.GetIntegrity(i);
                if (damage <= 0f) continue;
                Vector2 position = transform.TransformPoint(MembraneIntegrity.DirectionOf(i) * membraneRadius);
                float gradient = exchange.nutrientReserve / internalArea - exchange.field.Sample(exchange.nutrientId, position);
                if (gradient <= 0f) continue;
                float conductance = permeability * damage * damage * contactLength;
                float equilibrationMass = gradient / inverseAreas;
                float requested = Mathf.Min(exchange.nutrientReserve,
                    equilibrationMass * (1f - Mathf.Exp(-conductance * inverseAreas * dt)));
                float emitted = exchange.field.Deposit(exchange.nutrientId, position, requested);
                if (emitted <= 0f) continue;
                exchange.nutrientReserve -= emitted;
                TotalLeaked += emitted;
                if (exchange.events) exchange.events.Record(Time.fixedTime, exchange.organismId,
                    exchange.nutrientId, ChemicalEventKind.Leaked, emitted, position, "membrane.sector[" + i + "]");
            }
        }
    }
}
