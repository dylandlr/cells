using UnityEngine;

namespace Cells.Substances
{
    /// <summary>
    /// Physical exchange bridge: storage -> localized pore -> environmental field,
    /// or environmental field -> membrane intake -> storage. Icons have no role here.
    /// Attach to a cell transform and configure its membrane-local secretion/intake sites.
    /// </summary>
    public sealed class CellChemicalExchange : MonoBehaviour
    {
        public int organismId;
        public MultiChemicalField2D field;
        public ChemicalEventLog events;
        public string toxinId = "toxin-B";
        public string nutrientId = "nutrient-A";
        public Transform secretionPore;
        public Transform feedingPore;
        [Min(0)] public float toxinReserve = 2f;
        [Min(0)] public float nutrientReserve;
        [Min(0)] public float toxinRate = .12f;
        [Min(0)] public float feedingRate = .2f;
        [Min(0)] public float synthesisCost = .04f;
        [Min(0)] public float metabolicEnergy = 8f;
        public bool secretionEnabled = true;
        public bool feedingEnabled = true;

        void FixedUpdate()
        {
            if (!field) return;
            float dt = Time.fixedDeltaTime;
            if (secretionEnabled && toxinReserve > 0f && metabolicEnergy > 0f)
            {
                float energyLimited = synthesisCost > 0f ? metabolicEnergy / synthesisCost : float.MaxValue;
                float amount = Mathf.Min(toxinReserve, toxinRate * dt, energyLimited);
                if (amount > 0f)
                {
                    Vector2 p = secretionPore ? (Vector2)secretionPore.position : (Vector2)transform.position;
                    float deposited = field.Deposit(toxinId, p, amount);
                    toxinReserve -= deposited;
                    metabolicEnergy -= deposited * synthesisCost;
                    if (deposited > 0f && events)
                        events.Record(Time.fixedTime, organismId, toxinId,
                            ChemicalEventKind.Secreted, deposited, p, "membrane.secretion_pore");
                }
            }
            if (feedingEnabled)
            {
                Vector2 p = feedingPore ? (Vector2)feedingPore.position : (Vector2)transform.position;
                float consumed = field.Withdraw(nutrientId, p, feedingRate * dt);
                nutrientReserve += consumed;
                if (consumed > 0f && events)
                    events.Record(Time.fixedTime, organismId, nutrientId,
                        ChemicalEventKind.Absorbed, consumed, p, "membrane.feeding_site");
            }
        }
    }
}
