using NUnit.Framework;
using Cells.Physiology;

namespace Cells.Tests
{
    public sealed class CellDivisionBudgetTests
    {
        [Test]
        public void ConstrictionActuatesRealMembraneNodes()
        {
            var go = new UnityEngine.GameObject("constriction-test");
            try
            {
                go.AddComponent<MembraneIntegrity>();
                var body = go.AddComponent<SoftCellBody2D>();
                body.Initialize();
                for (int i=0; i<25; i++) body.Tick(.02f);
                float baseline = body.ConstrictionSpan;
                body.divisionConstriction = 1f;
                for (int i=0; i<250; i++) body.Tick(.02f);
                Assert.Less(body.ConstrictionSpan,baseline,
                    "Localized constriction should change the actual simulated membrane shape.");
                Assert.Greater(System.Math.Abs(body.SignedArea()),.01f,
                    "A pre-scission boundary should remain nondegenerate.");
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        [Test]
        public void ValidPartitionPreservesAllThreeResourceAccounts()
        {
            Assert.IsTrue(CellDivisionBudget.TryPartition(2.4f,.8f,6f,.5f,.5f,out var split));
            Assert.AreEqual(2.4f,split.parentBiomass+split.daughterBiomass,1e-6f);
            Assert.AreEqual(.8f,split.parentNutrient+split.daughterNutrient,1e-6f);
            Assert.AreEqual(6f,split.parentEnergy+split.daughterEnergy,1e-6f);
            Assert.AreEqual(1.2f,split.daughterBiomass,1e-6f);
        }

        [Test]
        public void InsufficientBiomassPreventsDivision()
        {
            Assert.IsFalse(CellDivisionBudget.TryPartition(.8f,1f,4f,.5f,.5f,out _));
        }

        [Test]
        public void InvalidAmountsAndFractionsAreRejected()
        {
            Assert.IsFalse(CellDivisionBudget.TryPartition(2f,-1f,5f,.5f,.5f,out _));
            Assert.IsFalse(CellDivisionBudget.TryPartition(2f,1f,5f,.5f,1f,out _));
            Assert.IsFalse(CellDivisionBudget.TryPartition(float.NaN,1f,5f,.5f,.5f,out _));
        }
    }
}
