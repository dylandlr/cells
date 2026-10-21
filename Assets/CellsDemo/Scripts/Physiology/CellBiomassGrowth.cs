using UnityEngine;

namespace Cells.Physiology
{
    /// <summary>
    /// Explicit material bookkeeping for an illustrative single-cell biomass pool.
    /// Assimilation transfers existing intracellular nutrient mass into structural mass.
    /// No arbitrary energy-to-mass conversion and no automatic mitosis.
    /// </summary>
    [RequireComponent(typeof(CellMetabolism))]
    public sealed class CellBiomassGrowth : MonoBehaviour
    {
        [Min(0f)] public float structuralBiomass = 1f;
        [Min(0f)] public float maximumBiomass = 2f;
        [Min(0f)] public float assimilationRate = .03f;
        [Min(0f)] public float growthEnergyPerMass = .5f;
        [Min(.001f)] public float referenceBiomass = 1f;
        [Range(.5f, 3f)] public float maximumVisualScale = 1.5f;

        public CellMetabolism metabolism;
        public float CumulativeAssimilated { get; private set; }
        public float VisualRadiusScale => Mathf.Clamp(
            Mathf.Sqrt(Mathf.Max(0f, structuralBiomass) / Mathf.Max(.001f, referenceBiomass)),
            .1f, maximumVisualScale);

        void Awake()
        {
            if (!metabolism) metabolism = GetComponent<CellMetabolism>();
        }

        /// <returns>Amount of existing nutrient mass transferred to structural biomass.</returns>
        public float AssimilateFrom(ref float nutrientReserve, float dt)
        {
            if (!metabolism) metabolism = GetComponent<CellMetabolism>();
            if (!metabolism || !metabolism.alive || !float.IsFinite(dt) || dt <= 0f ||
                !float.IsFinite(nutrientReserve) || nutrientReserve <= 0f ||
                !float.IsFinite(structuralBiomass)) return 0f;

            float request = Mathf.Min(
                Mathf.Max(0f, nutrientReserve),
                Mathf.Max(0f, maximumBiomass - structuralBiomass),
                Mathf.Max(0f, assimilationRate) * dt);
            if (growthEnergyPerMass > 0f)
                request = Mathf.Min(request, Mathf.Max(0f, metabolism.energy - .0001f) / growthEnergyPerMass);
            if (request <= 0f) return 0f;

            if (growthEnergyPerMass > 0f)
            {
                float paid = metabolism.Spend(request * growthEnergyPerMass);
                request = Mathf.Min(request, paid / growthEnergyPerMass);
            }
            nutrientReserve -= request;
            structuralBiomass += request;
            CumulativeAssimilated += request;
            return request;
        }
    }
}
