using UnityEngine;
using Cells.Physiology;

namespace Cells.Substances
{
    /// <summary>Physically local particulate ingestion. Material removal precedes energy gain.</summary>
    public sealed class ParticleFeedingSite : MonoBehaviour
    {
        public int organismId;
        public TrackedParticleWorld world;
        public CellMetabolism metabolism;
        public ChemicalEventLog events;
        public string edibleSubstanceId = "nutrient-A";
        [Min(0f)] public float captureRadius = .35f;
        [Min(0f)] public float usableEnergyPerMass = 2f;
        [Min(0)] public int maximumParticlesPerTick = 1;

        void Awake() { if (!metabolism) metabolism = GetComponentInParent<CellMetabolism>(); }

        void FixedUpdate()
        {
            if (!world || !metabolism || !metabolism.alive) return;
            for (int i = 0; i < maximumParticlesPerTick; i++)
            {
                if (!world.TryConsume(edibleSubstanceId, transform.position, captureRadius, out var particle))
                    break;
                metabolism.Feed(particle.mass * usableEnergyPerMass);
                if (events) events.Record(Time.fixedTime, organismId, particle.substanceId,
                    ChemicalEventKind.Absorbed, particle.mass, transform.position,
                    "membrane.particulate_feeding_site");
            }
        }
    }
}
