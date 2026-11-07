using NUnit.Framework;
using UnityEngine;
using Cells.Physiology;

namespace Cells.Tests
{
    public sealed class GrowthDependentDragTests
    {
        GameObject cell;
        [TearDown] public void TearDown() { if (cell) Object.DestroyImmediate(cell); }

        [Test]
        public void GrowingBodyIncreasesTranslationAndRotationResistance()
        {
            cell = new GameObject("grown swimmer");
            var metabolism = cell.AddComponent<CellMetabolism>();
            var growth = cell.AddComponent<CellBiomassGrowth>();
            growth.metabolism = metabolism;
            var tail = cell.AddComponent<SoftFlagellum2D>();
            var hydro = cell.AddComponent<FlagellarHydrodynamics2D>();
            hydro.tail = tail;
            hydro.growth = growth;
            growth.structuralBiomass = growth.referenceBiomass;
            float baseLinear = hydro.EffectiveBodyDrag;
            float baseAngular = hydro.EffectiveRotationalBodyDrag;
            growth.structuralBiomass = growth.referenceBiomass * 2f;
            Assert.Greater(hydro.EffectiveBodyDrag, baseLinear);
            Assert.Greater(hydro.EffectiveRotationalBodyDrag, baseAngular);
            float radiusRatio = Mathf.Sqrt(2f);
            Assert.AreEqual(baseLinear * radiusRatio, hydro.EffectiveBodyDrag, 1e-4f);
            Assert.AreEqual(baseAngular * Mathf.Pow(radiusRatio, 3f), hydro.EffectiveRotationalBodyDrag, 1e-4f);
        }
    }
}
