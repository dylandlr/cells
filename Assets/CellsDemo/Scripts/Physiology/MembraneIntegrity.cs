using UnityEngine;

namespace Cells.Physiology
{
    /// <summary>Membrane damage and permeability state; independent of human rendering.</summary>
    public sealed class MembraneIntegrity : MonoBehaviour
    {
        [Range(0f,1f)] public float integrity = 1f;
        [Range(0f,1f)] public float chemicalResistance = .25f;
        [Min(0f)] public float repairEnergyPerUnit = 3f;
        [Min(0f)] public float ruptureThreshold = .05f;
        public bool ruptured => integrity <= ruptureThreshold;

        public float ApplyExposure(float incomingDamage)
        {
            float effective = Mathf.Min(integrity, Mathf.Max(0f, incomingDamage) * (1f - Mathf.Clamp01(chemicalResistance)));
            integrity = Mathf.Clamp01(integrity - effective);
            return effective;
        }

        public float Repair(float desiredIntegrity, CellMetabolism metabolism)
        {
            if (!metabolism || !metabolism.alive || ruptured) return 0f;
            float missing = Mathf.Max(0f, Mathf.Min(desiredIntegrity, 1f) - integrity);
            if (missing <= 0f) return 0f;
            if (repairEnergyPerUnit <= 0f) { integrity += missing; return missing; }
            float paid = metabolism.Spend(missing * repairEnergyPerUnit);
            float restored = paid / repairEnergyPerUnit;
            integrity = Mathf.Clamp01(integrity + restored);
            return restored;
        }
    }
}
