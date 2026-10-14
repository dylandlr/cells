using UnityEngine;
using Cells.Physiology;

namespace Cells.Substances
{
    /// <summary>Physically local ingestion transfers packet mass into the nutrient reserve.
    /// Energy is supplied later by the same digestion pathway as dissolved food.</summary>
    public sealed class ParticleFeedingSite : MonoBehaviour
    {
        public int organismId;
        public TrackedParticleWorld world;
        public CellMetabolism metabolism;
        public CellChemicalExchange exchange;
        public ChemicalEventLog events;
        public string edibleSubstanceId = "nutrient-A";
        [Min(0f)] public float captureRadius = .35f;
        [Min(0)] public int maximumParticlesPerTick = 1;

        void Awake()
        {
            if (!metabolism) metabolism = GetComponentInParent<CellMetabolism>();
            if (!exchange) exchange = GetComponentInParent<CellChemicalExchange>();
        }

        void FixedUpdate() => Tick();

        public void Tick()
        {
            if (!world || !metabolism || !exchange || !metabolism.alive) return;
            for (int i = 0; i < maximumParticlesPerTick; i++)
            {
                if (!world.TryConsume(edibleSubstanceId, transform.position, captureRadius, out var particle))
                    break;
                exchange.nutrientReserve += particle.mass;
                if (events) events.Record(Time.fixedTime, organismId, particle.substanceId,
                    ChemicalEventKind.Absorbed, particle.mass, transform.position,
                    "membrane.particulate_feeding_site");
            }
        }
    }
}
