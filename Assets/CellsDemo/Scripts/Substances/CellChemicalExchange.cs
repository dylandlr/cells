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
        public Transform secretionPore;
        public Transform feedingPore;
        [Min(0)] public float toxinReserve = 2f;
        [Min(0)] public float nutrientReserve;
        [Min(0)] public float toxinRate = .12f;
        [Min(0)] public float feedingRate = .2f;
        [Min(0)] public float secretionEnergyCost = .04f;
        public bool secretionEnabled = true;
        public bool feedingEnabled = true;

        void Awake() { if (!metabolism) metabolism = GetComponent<CellMetabolism>(); }

        void FixedUpdate()
        {
            if (!field || !metabolism || !metabolism.alive) return;
            float dt = Time.fixedDeltaTime;
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
            if (feedingEnabled)
            {
                Vector2 p = feedingPore ? (Vector2)feedingPore.position : (Vector2)transform.position;
                float consumed = field.Withdraw(nutrientId, p, feedingRate * dt);
                nutrientReserve += consumed;
                if (consumed > 0f && events) events.Record(Time.fixedTime, organismId, nutrientId,
                    ChemicalEventKind.Absorbed, consumed, p, "membrane.feeding_site");
            }
        }
    }
}
