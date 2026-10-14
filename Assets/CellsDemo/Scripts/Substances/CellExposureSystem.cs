using UnityEngine;
using Cells.Physiology;

namespace Cells.Substances
{
    /// <summary>Phenotype-dependent chemical injury to membranes and internal energy.</summary>
    [RequireComponent(typeof(CellMetabolism), typeof(MembraneIntegrity))]
    public sealed class CellExposureSystem : MonoBehaviour
    {
        public MultiChemicalField2D field;
        public ChemicalEventLog events;
        public CellMetabolism metabolism;
        public MembraneIntegrity membrane;
        public int organismId;
        public string substanceId = "toxin-B";
        [Min(0)] public float uptakeRate = .1f;
        [Min(0)] public float damagePerUnit = 5f;
        [Min(0)] public float beneficialEnergyPerUnit;
        [Min(0)] public float internalDamageFraction = .25f;
        public Transform intakeSite;

        void Awake()
        {
            if (!metabolism) metabolism = GetComponent<CellMetabolism>();
            if (!membrane) membrane = GetComponent<MembraneIntegrity>();
        }

        void FixedUpdate() => Tick(Time.fixedDeltaTime);

        public void Tick(float dt)
        {
            if (!field || !metabolism || !membrane || !metabolism.alive || dt <= 0f) return;
            Vector2 p = intakeSite ? (Vector2)intakeSite.position : (Vector2)transform.position;
            // First-order uptake: exposure scales with the actual local concentration.
            float amount = field.Withdraw(substanceId, p,
                Mathf.Max(0f, uptakeRate) * field.Sample(substanceId, p) * dt);
            if (amount <= 0f) return;
            if (events) events.Record(Time.fixedTime, organismId, substanceId,
                ChemicalEventKind.Absorbed, amount, p, "membrane.intake");

            float injury = membrane.ApplyExposure(amount * damagePerUnit);
            if (injury > 0f)
            {
                metabolism.Injure(injury * internalDamageFraction);
                if (events) events.Record(Time.fixedTime, organismId, substanceId,
                    ChemicalEventKind.Damaged, injury, p, "membrane.integrity");
            }
            if (membrane.ruptured) metabolism.Injure(metabolism.energy);
            if (metabolism.alive && beneficialEnergyPerUnit > 0f)
                metabolism.Feed(amount * beneficialEnergyPerUnit);
            if (!metabolism.alive && events)
                events.Record(Time.fixedTime, organismId, substanceId,
                    ChemicalEventKind.Died, 0f, p, membrane.ruptured ? "membrane.rupture" : "metabolism");
        }
    }
}
