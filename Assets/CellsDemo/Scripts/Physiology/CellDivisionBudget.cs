using System;
using UnityEngine;

namespace Cells.Physiology
{
    /// <summary>Conservative numerical partition used by the division pipeline.
    /// No new biomass, intracellular nutrient, or metabolic energy is created.
    /// </summary>
    public static class CellDivisionBudget
    {
        public struct Partition
        {
            public float parentBiomass, daughterBiomass;
            public float parentNutrient, daughterNutrient;
            public float parentEnergy, daughterEnergy;
        }

        public static bool TryPartition(float biomass, float nutrients, float energy,
            float minimumDaughterBiomass, float fractionToDaughter, out Partition result)
        {
            result = default(Partition);
            if (!float.IsFinite(biomass) || !float.IsFinite(nutrients) ||
                !float.IsFinite(energy) || !float.IsFinite(minimumDaughterBiomass) ||
                !float.IsFinite(fractionToDaughter) || biomass < 0f ||
                nutrients < 0f || energy < 0f || minimumDaughterBiomass <= 0f ||
                fractionToDaughter <= 0f || fractionToDaughter >= 1f) return false;
            float daughterMass = biomass * fractionToDaughter;
            float parentMass = biomass - daughterMass;
            if (daughterMass < minimumDaughterBiomass || parentMass < minimumDaughterBiomass)
                return false;
            float daughterNutrient = nutrients * fractionToDaughter;
            float daughterEnergy = energy * fractionToDaughter;
            result = new Partition {
                parentBiomass=parentMass, daughterBiomass=daughterMass,
                parentNutrient=nutrients-daughterNutrient, daughterNutrient=daughterNutrient,
                parentEnergy=energy-daughterEnergy, daughterEnergy=daughterEnergy
            };
            return true;
        }
    }
}
