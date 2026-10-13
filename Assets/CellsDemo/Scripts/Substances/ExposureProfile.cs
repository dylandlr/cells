using System;
using UnityEngine;

namespace Cells.Substances
{
    // Target-dependent interaction, evaluated in simulation rather than inferred from a purple tint.
    [Serializable]
    public struct ExposureProfile
    {
        [Min(0)] public float dose;
        [Min(0)] public float uptakePerSecond;
        [Range(0, 1)] public float membraneResistance;
        [Min(0)] public float toxicityCoefficient;
        [Min(0)] public float metabolicBenefitCoefficient;

        public float NetEnergyRate()
        {
            float absorbed = dose * Mathf.Max(0, uptakePerSecond);
            float injury = absorbed * toxicityCoefficient * (1f - Mathf.Clamp01(membraneResistance));
            float benefit = absorbed * metabolicBenefitCoefficient;
            return benefit - injury;
        }

        public bool IsLethalRisk(float energyReserve, float exposureSeconds)
        {
            return energyReserve + NetEnergyRate() * Mathf.Max(0, exposureSeconds) <= 0f;
        }
    }
}
