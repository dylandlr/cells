using NUnit.Framework;
using UnityEngine;
using Cells.Physiology;

namespace Cells.Tests
{
    public sealed class CellBiomassGrowthTests
    {
        GameObject organism;
        CellMetabolism metabolism;
        CellBiomassGrowth growth;

        [SetUp]
        public void Setup()
        {
            organism = new GameObject("growth test");
            metabolism = organism.AddComponent<CellMetabolism>();
            growth = organism.AddComponent<CellBiomassGrowth>();
            growth.metabolism = metabolism;
            growth.structuralBiomass = 1f;
            growth.maximumBiomass = 2f;
            growth.assimilationRate = .5f;
            metabolism.energy = 5f;
        }

        [TearDown]
        public void Teardown() => Object.DestroyImmediate(organism);

        [Test]
        public void GrowthTransfersExistingReserveWithoutDuplicatingMass()
        {
            float reserve = .8f;
            float before = reserve + growth.structuralBiomass;
            float used = growth.AssimilateFrom(ref reserve, 1f);
            Assert.AreEqual(.5f, used, 1e-5f);
            Assert.AreEqual(before, reserve + growth.structuralBiomass, 1e-5f);
            Assert.AreEqual(used, growth.CumulativeAssimilated, 1e-5f);
            Assert.AreEqual(4.75f, metabolism.energy, 1e-5f);
            Assert.Greater(growth.VisualRadiusScale, 1f);
        }

        [Test]
        public void GrowthRespectsMassEnergyAndBiomassLimits()
        {
            growth.maximumBiomass = 1.2f;
            metabolism.energy = .051f;
            float reserve = 5f;
            float used = growth.AssimilateFrom(ref reserve, 3f);
            Assert.LessOrEqual(used, .101f + 1e-5f);
            Assert.LessOrEqual(growth.structuralBiomass, 1.2f);
            Assert.GreaterOrEqual(reserve, 0f);
            Assert.Greater(metabolism.energy, 0f);
        }

        [Test]
        public void DeadCellCannotGrow()
        {
            metabolism.alive = false;
            float reserve = 2f;
            Assert.AreEqual(0f, growth.AssimilateFrom(ref reserve, 1f));
            Assert.AreEqual(2f, reserve);
        }
    }
}
