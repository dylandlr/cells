using System;
using UnityEngine;

namespace Cells.Physiology
{
    /// <summary>Explicit, effective inventories for genetic template material and
    /// functional compartments. These are abstract mass-equivalent budgets,
    /// not chemical DNA or accurate organelle counts.</summary>
    public sealed class CellHeritableInventory : MonoBehaviour
    {
        [Min(0f)] public float geneticTemplateMass = .06f;
        [Min(0f)] public float vesicleMaterialMass = .08f;
        [Min(0f)] public float structuralModuleMass = .1f;

        public float Total => geneticTemplateMass + vesicleMaterialMass + structuralModuleMass;

        public bool IsValid => float.IsFinite(geneticTemplateMass) &&
            float.IsFinite(vesicleMaterialMass) && float.IsFinite(structuralModuleMass) &&
            geneticTemplateMass >= 0f && vesicleMaterialMass >= 0f &&
            structuralModuleMass >= 0f;

        public struct Allocation
        {
            public float parentGenetic, daughterGenetic;
            public float parentVesicles, daughterVesicles;
            public float parentModules, daughterModules;
        }

        public bool TryAllocate(float daughterFraction, float minimumGeneticMaterial,
            out Allocation a)
        {
            a = default(Allocation);
            if (!IsValid || !float.IsFinite(daughterFraction) ||
                !float.IsFinite(minimumGeneticMaterial) || minimumGeneticMaterial <= 0f ||
                daughterFraction <= 0f || daughterFraction >= 1f) return false;
            float dg = geneticTemplateMass*daughterFraction;
            float pg = geneticTemplateMass-dg;
            if (dg < minimumGeneticMaterial || pg < minimumGeneticMaterial) return false;
            float dv = vesicleMaterialMass*daughterFraction;
            float dm = structuralModuleMass*daughterFraction;
            a = new Allocation {
                daughterGenetic=dg, parentGenetic=pg,
                daughterVesicles=dv, parentVesicles=vesicleMaterialMass-dv,
                daughterModules=dm, parentModules=structuralModuleMass-dm
            };
            return true;
        }

        public void Assign(float genetic, float vesicles, float modules)
        {
            if (!float.IsFinite(genetic) || !float.IsFinite(vesicles) ||
                !float.IsFinite(modules) || genetic < 0f || vesicles < 0f || modules < 0f)
                throw new ArgumentOutOfRangeException(nameof(genetic));
            geneticTemplateMass=genetic;
            vesicleMaterialMass=vesicles;
            structuralModuleMass=modules;
        }
    }
}
