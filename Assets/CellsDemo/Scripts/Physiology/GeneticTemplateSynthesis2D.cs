using UnityEngine;
using Cells.Substances;

namespace Cells.Physiology
{
    /// <summary>
    /// Effective pseudo-template synthesis. Converts a finite nutrient reserve
    /// into genetic template material at an energetic cost over simulated time.
    /// No chromosome duplication or molecular sequence copying is claimed.
    /// </summary>
    [DefaultExecutionOrder(-25)]
    [RequireComponent(typeof(CellHeritableInventory))]
    public sealed class GeneticTemplateSynthesis2D : MonoBehaviour
    {
        public CellHeritableInventory inventory;
        public CellChemicalExchange exchange;
        public CellMetabolism metabolism;
        [Min(0f)] public float precursorPerTemplate = 1f;
        [Min(0f)] public float energyPerTemplate = 2f;
        [Min(0f)] public float templatePerSecond = .004f;
        [Min(.001f)] public float targetTemplateMass = .12f;
        // Each new organism starts with one effective genome equivalent.
        // A second equivalent must be synthesized using tracked precursors.
        [Min(.001f)] public float singleCopyMass = .06f;
        public int CompletedCopyCount => inventory && inventory.IsValid &&
            inventory.geneticTemplateMass >= 2f*singleCopyMass - 1e-6f &&
            CumulativeSynthesis >= singleCopyMass - 1e-6f ? 2 : 1;
        public float CumulativeSynthesis { get; private set; }
        public bool ReplicationReady => inventory && inventory.IsValid &&
            inventory.geneticTemplateMass >= targetTemplateMass &&
            CompletedCopyCount >= 2;

        void Awake()
        {
            if (!inventory) inventory=GetComponent<CellHeritableInventory>();
            if (!exchange) exchange=GetComponent<CellChemicalExchange>();
            if (!metabolism) metabolism=GetComponent<CellMetabolism>();
        }
        void FixedUpdate() => Tick(Time.fixedDeltaTime);
        public float Tick(float dt)
        {
            if (!inventory || !exchange || !metabolism || !metabolism.alive ||
                !inventory.IsValid || !float.IsFinite(dt) || dt<=0f ||
                !float.IsFinite(templatePerSecond) || templatePerSecond<=0f ||
                !float.IsFinite(precursorPerTemplate) || precursorPerTemplate<=0f ||
                !float.IsFinite(energyPerTemplate) || energyPerTemplate<=0f ||
                !float.IsFinite(targetTemplateMass) || targetTemplateMass<=0f ||
                !float.IsFinite(exchange.nutrientReserve) || exchange.nutrientReserve<0f ||
                !float.IsFinite(metabolism.energy) || metabolism.energy<=0f) return 0f;

            float missing=Mathf.Max(0f,targetTemplateMass-inventory.geneticTemplateMass);
            float quantity=Mathf.Min(missing,templatePerSecond*dt);
            quantity=Mathf.Min(quantity,exchange.nutrientReserve/precursorPerTemplate);
            // Keep metabolism alive; basal costs are accounted separately.
            quantity=Mathf.Min(quantity,metabolism.energy/energyPerTemplate);
            if (!(quantity>0f)) return 0f;
            inventory.geneticTemplateMass+=quantity;
            exchange.nutrientReserve-=quantity*precursorPerTemplate;
            metabolism.energy-=quantity*energyPerTemplate;
            if (metabolism.energy<=0f) metabolism.alive=false;
            CumulativeSynthesis+=quantity;
            return quantity;
        }
    }
}
