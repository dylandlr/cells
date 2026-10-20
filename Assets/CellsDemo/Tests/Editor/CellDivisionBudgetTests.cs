using NUnit.Framework;
using Cells.Physiology;

namespace Cells.Tests
{
    public sealed class CellDivisionBudgetTests
    {
        [Test]
        public void UnconstrictedClosedMembraneDoesNotPassNeckGate()
        {
            var go = new UnityEngine.GameObject("neck diagnostic");
            try
            {
                go.AddComponent<MembraneIntegrity>();
                var body=go.AddComponent<SoftCellBody2D>();
                body.Initialize();
                var observed=MembraneNeckDiagnostic2D.Measure(body);
                Assert.IsTrue(observed.finite);
                Assert.IsTrue(observed.simple);
                Assert.IsTrue(observed.valid);
                Assert.IsFalse(observed.narrowed,
                    "An ordinary round membrane cannot pass a constriction gate.");
                Assert.Greater(observed.area,0f);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

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
        public void TemplateSynthesisConsumesFinitePrecursorsAndEnergy()
        {
            var go=new UnityEngine.GameObject("synthesis budget");
            try
            {
                var inventory=go.AddComponent<CellHeritableInventory>();
                var metabolism=go.AddComponent<CellMetabolism>();
                var exchange=go.AddComponent<Cells.Substances.CellChemicalExchange>();
                var synth=go.AddComponent<GeneticTemplateSynthesis2D>();
                synth.inventory=inventory;
                synth.exchange=exchange;
                synth.metabolism=metabolism;
                inventory.geneticTemplateMass=.03f;
                exchange.nutrientReserve=.025f;
                metabolism.energy=1f;
                synth.templatePerSecond=.1f;
                synth.targetTemplateMass=.12f;
                float beforeMass=inventory.geneticTemplateMass+exchange.nutrientReserve;
                float beforeEnergy=metabolism.energy;
                float made=synth.Tick(1f);
                Assert.AreEqual(.025f,made,1e-5f);
                Assert.AreEqual(1,synth.CompletedCopyCount);
                Assert.IsFalse(synth.ReplicationReady);
                Assert.AreEqual(beforeMass,inventory.geneticTemplateMass+
                    exchange.nutrientReserve,1e-5f);
                Assert.AreEqual(beforeEnergy-made*synth.energyPerTemplate,
                    metabolism.energy,1e-5f);
                Assert.IsFalse(synth.ReplicationReady);
                Assert.AreEqual(0f,synth.Tick(1f),1e-6f);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        [Test]
        public void GeneticAndCompartmentInventoriesPartitionWithoutCreation()
        {
            var go = new UnityEngine.GameObject("inventory conservation");
            try
            {
                var pool=go.AddComponent<CellHeritableInventory>();
                float before=pool.Total;
                Assert.IsTrue(pool.TryAllocate(.5f,.01f,out var split));
                float parent=split.parentGenetic+split.parentVesicles+split.parentModules;
                float daughter=split.daughterGenetic+split.daughterVesicles+split.daughterModules;
                Assert.AreEqual(before,parent+daughter,1e-6f);
                pool.Assign(split.parentGenetic,split.parentVesicles,split.parentModules);
                Assert.AreEqual(parent,pool.Total,1e-6f);
                Assert.IsFalse(pool.TryAllocate(.5f,.04f,out _));
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
