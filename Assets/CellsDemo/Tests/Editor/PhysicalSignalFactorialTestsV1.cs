#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using Cells.Experiments;

namespace Cells.Tests
{
    public sealed class PhysicalSignalFactorialTestsV1
    {
        [Test]
        public void WithoutProductionReceiverToggleDoesNothing()
        {
            var noResponse = PhysicalSignalAssayV1.Run(631, false, false);
            var response = PhysicalSignalAssayV1.Run(631, false, true);
            CollectionAssert.AreEqual(noResponse.NetEnergy, response.NetEnergy);
            CollectionAssert.AreEqual(noResponse.ConsumedMass, response.ConsumedMass);
            Assert.AreEqual(0, response.Receptions[1]);
            Assert.AreEqual(0, response.SignalMassEmitted, 1e-7f);
        }

        [Test]
        public void ProductionWithoutResponseIsPureSenderCost()
        {
            var silent = PhysicalSignalAssayV1.Run(631, false, false);
            var releaseOnly = PhysicalSignalAssayV1.Run(631, true, false);
            Assert.Greater(releaseOnly.SignalMassEmitted, 0f);
            for (int i = 1; i < PhysicalSignalAssayV1.Agents; ++i)
            {
                Assert.That(releaseOnly.NetEnergy[i],
                    Is.EqualTo(silent.NetEnergy[i]).Within(1e-5f));
                Assert.That(releaseOnly.ConsumedMass[i],
                    Is.EqualTo(silent.ConsumedMass[i]).Within(1e-6f));
            }
            Assert.That(releaseOnly.NetEnergy[0] - silent.NetEnergy[0],
                Is.EqualTo(-releaseOnly.SignalEnergySpent).Within(1e-4f));
        }

        [Test]
        public void FourFactorialArmsAreIndividuallyReproducible()
        {
            for (int emission = 0; emission < 2; ++emission)
            for (int response = 0; response < 2; ++response)
            {
                var a = PhysicalSignalAssayV1.Run(719, emission == 1, response == 1);
                var b = PhysicalSignalAssayV1.Run(719, emission == 1, response == 1);
                Assert.AreEqual(a.TotalHarvestMass, b.TotalHarvestMass, 1e-6f);
                Assert.AreEqual(a.SignalMassEmitted, b.SignalMassEmitted, 1e-6f);
                CollectionAssert.AreEqual(a.NetEnergy, b.NetEnergy);
            }
        }

        [Test]
        public void BackwardsCompatibleAssayUsesEnabledReceiver()
        {
            var legacy = PhysicalSignalAssayV1.Run(715, true);
            var explicitArm = PhysicalSignalAssayV1.Run(715, true, true);
            Assert.AreEqual(legacy.TotalHarvestMass, explicitArm.TotalHarvestMass);
            CollectionAssert.AreEqual(legacy.NetEnergy, explicitArm.NetEnergy);
        }
    }
}
#endif
