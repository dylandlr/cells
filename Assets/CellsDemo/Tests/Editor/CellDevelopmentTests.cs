using NUnit.Framework;
using UnityEngine;
using Cells.Physiology;

namespace Cells.Tests
{
    public sealed class CellDevelopmentTests
    {
        GameObject cell;
        CellMetabolism metabolism;
        CellBiomassGrowth growth;
        CellDevelopment development;
        OrganismPhenotype phenotype;
        [SetUp] public void Setup()
        {
            cell = new GameObject("development test");
            metabolism = cell.AddComponent<CellMetabolism>();
            metabolism.energy = 5f;
            growth = cell.AddComponent<CellBiomassGrowth>();
            growth.assimilationRate = .5f;
            phenotype = cell.AddComponent<OrganismPhenotype>();
            development = cell.AddComponent<CellDevelopment>();
        }
        [TearDown] public void Teardown() => Object.DestroyImmediate(cell);

        [Test] public void MaterialAllocationMatchesPaidAssimilation()
        {
            phenotype.genome.developmentBias = .9f;
            float reserve = .5f;
            growth.AssimilateFrom(ref reserve, 1f);
            float sum = 0f;
            for (int i=0;i<CellDevelopment.Regions;i++) sum += development.DepositAt(i);
            Assert.AreEqual(.5f,sum,1e-6f);
            Assert.AreEqual(1.5f,growth.structuralBiomass,1e-6f);
            Assert.AreEqual(4.75f,metabolism.energy,1e-6f);
            Assert.AreEqual(0f,reserve,1e-6f);
            Assert.Greater(development.RadiusFactor(0,1.5f),development.RadiusFactor(8,1.5f));
        }
        [Test] public void StarvationAndDeathPreventDevelopment()
        {
            float reserve = 0f;
            growth.AssimilateFrom(ref reserve,1f);
            Assert.AreEqual(0f,development.AllocatedMass);
            reserve = 1f;
            metabolism.alive = false;
            growth.AssimilateFrom(ref reserve,1f);
            Assert.AreEqual(0f,development.AllocatedMass);
            Assert.AreEqual(1f,reserve);
        }
        [Test] public void IsotropicControlStaysUniformAndTraitsCopy()
        {
            float reserve = .5f;
            growth.AssimilateFrom(ref reserve,1f);
            for (int i=0;i<CellDevelopment.Regions;i++)
                Assert.AreEqual(1f,development.RadiusFactor(i,1.5f),1e-6f);
            phenotype.genome.developmentBias = .8f;
            phenotype.genome.developmentAxis = .7f;
            var child = phenotype.genome.Inherit(new System.Random(1),0,0);
            Assert.AreEqual(.8f,child.developmentBias);
            Assert.AreEqual(.7f,child.developmentAxis);
        }
        [Test] public void PaidGrowthChangesPhysicalBoundary()
        {
            phenotype.genome.developmentBias = .9f;
            var body = cell.AddComponent<SoftCellBody2D>();
            float reserve = .5f;
            growth.AssimilateFrom(ref reserve,1f);
            for (int t=0;t<100;t++) body.Tick(.02f);
            Assert.Greater(body.Node(0).magnitude,body.Node(8).magnitude * 1.05f);
            float expected = 16f * Mathf.Sin(2f*Mathf.PI/32f) * .9f*.9f * 1.5f;
            Assert.AreEqual(expected,Mathf.Abs(body.SignedArea()),expected*.05f);
        }
    }
}
