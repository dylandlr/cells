using UnityEngine;
using Cells.Physiology;

namespace Cells.Substances
{
    /// <summary>Transfers material at anatomical sites using the cell's authoritative energy reserve.</summary>
    [RequireComponent(typeof(CellMetabolism))]
    public sealed class CellChemicalExchange : MonoBehaviour
    {
        public int organismId;
        public MultiChemicalField2D field;
        public ChemicalEventLog events;
        public CellMetabolism metabolism;
        public string toxinId = "toxin-B";
        public string nutrientId = "nutrient-A";
        public string digestionProductId = "metabolic-waste-E";
        public Transform secretionPore;
        public Transform feedingPore;
        public Transform metabolicOutlet;
        [Min(0)] public float toxinReserve = 2f;
        [Min(0)] public float nutrientReserve;
        [Min(0)] public float toxinRate = .12f;
        [Min(0)] public float feedingRate = .2f;
        [Min(0)] public float usableEnergyPerMass = 2f;
        [Min(0)] public float digestionRate = .2f;
        [Min(0)] public float secretionEnergyCost = .04f;
        public bool secretionEnabled = true;
        public bool feedingEnabled = true;
        public float CumulativeNutrientAbsorbed { get; private set; }
        public float CumulativeNutrientDigested { get; private set; }

        void Awake() { if (!metabolism) metabolism = GetComponent<CellMetabolism>(); }

        void FixedUpdate() => Tick(Time.fixedDeltaTime);

        public void Tick(float dt)
        {
            if (!field || !metabolism || !metabolism.alive || !MultiChemicalField2D.Finite(dt) || dt <= 0f) return;
            if (secretionEnabled && toxinReserve > 0f)
            {
                float allowed = secretionEnergyCost > 0f
                    ? metabolism.energy / secretionEnergyCost : float.MaxValue;
                float amount = Mathf.Min(toxinReserve, toxinRate * dt, allowed);
                if (amount > 0f)
                {
                    Vector2 p = secretionPore ? (Vector2)secretionPore.position : (Vector2)transform.position;
                    float deposited = field.Deposit(toxinId, p, amount);
                    if (deposited > 0f)
                    {
                        toxinReserve -= deposited;
                        metabolism.Spend(deposited * secretionEnergyCost);
                        if (events) events.Record(Time.fixedTime, organismId, toxinId,
                            ChemicalEventKind.Secreted, deposited, p, "membrane.secretion_pore");
                    }
                }
            }
            if (!metabolism.alive) return;
            if (feedingEnabled)
            {
                Vector2 p = feedingPore ? (Vector2)feedingPore.position : (Vector2)transform.position;
                float consumed = field.Withdraw(nutrientId, p, feedingRate * dt);
                nutrientReserve += consumed;
                CumulativeNutrientAbsorbed += consumed;
                if (consumed > 0f && events) events.Record(Time.fixedTime, organismId, nutrientId,
                    ChemicalEventKind.Absorbed, consumed, p, "membrane.feeding_site");
            }
            // Material is converted only when the byproduct can enter the represented world.
            Vector2 outlet = metabolicOutlet ? (Vector2)metabolicOutlet.position : (Vector2)transform.position;
            float requestedDigestion = Mathf.Min(nutrientReserve, Mathf.Max(0f, digestionRate) * dt);
            float digested = digestionProductId == nutrientId ? 0f : field.Deposit(digestionProductId, outlet, requestedDigestion);
            nutrientReserve -= digested;
            CumulativeNutrientDigested += digested;
            metabolism.Feed(digested * Mathf.Max(0f, usableEnergyPerMass));
            if (digested > 0f && events)
            {
                events.Record(Time.fixedTime, organismId, nutrientId, ChemicalEventKind.Digested,
                    digested, transform.position, "metabolism.nutrient_reserve");
                events.Record(Time.fixedTime, organismId, digestionProductId, ChemicalEventKind.Secreted,
                    digested, outlet, "membrane.metabolic_outlet");
            }
        }
    }
}
