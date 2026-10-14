using UnityEngine;
using Cells.Physiology;

namespace Cells.Substances
{
    /// <summary>
    /// Target-relative contact exposure; configured susceptibilities are simulation data,
    /// not inferred from visual hue/glyph. This prototype withdraws ingested chemicals
    /// and applies damage without requiring access to the human inspector.
    /// </summary>
    public sealed class CellExposureSystem : MonoBehaviour
    {
        public MultiChemicalField2D field;
        public ChemicalEventLog events;
        public CellMetabolism metabolism;
        public int organismId;
        public string substanceId = "toxin-B";
        [Min(0)] public float uptakeRate = .1f;
        [Range(0, 1)] public float resistance = .25f;
        [Min(0)] public float damagePerUnit = 5f;
        [Min(0)] public float beneficialEnergyPerUnit;
        public Transform intakeSite;

        void Awake()
        {
            if (!metabolism) metabolism = GetComponent<CellMetabolism>();
        }

        void FixedUpdate()
        {
            if (!field || !metabolism || !metabolism.alive) return;
            float dt = Time.fixedDeltaTime;
            Vector2 position = intakeSite ? (Vector2)intakeSite.position : (Vector2)transform.position;
            float amount = field.Withdraw(substanceId, position, uptakeRate * dt);
            if (amount <= 0f) return;

            if (events)
                events.Record(Time.fixedTime, organismId, substanceId,
                    ChemicalEventKind.Absorbed, amount, position, "membrane.intake");

            float damage = amount * damagePerUnit * (1f - resistance);
            if (damage > 0f)
            {
                metabolism.Injure(damage);
                if (events) events.Record(Time.fixedTime, organismId, substanceId,
                    ChemicalEventKind.Damaged, damage, position, "internal.exposure");
            }
            float benefit = amount * beneficialEnergyPerUnit;
            if (benefit > 0f) metabolism.Feed(benefit);
            if (!metabolism.alive && events)
                events.Record(Time.fixedTime, organismId, substanceId,
                    ChemicalEventKind.Died, 0f, position, "metabolism");
        }
    }
}
