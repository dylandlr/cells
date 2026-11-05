#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using Cells.Experiments;

namespace Cells.Tests
{
    public sealed class PhysicalSignalAssayV1Tests
    {
        [Test]
        public void DisabledArmEmitsNoMolecules()
        {
            var off = PhysicalSignalAssayV1.Run(631, false);
            Assert.AreEqual(0f, off.SignalMassEmitted, 1e-7f);
            Assert.AreEqual(0f, off.SignalEnergySpent, 1e-7f);
            Assert.AreEqual(0f, off.SignalMassRemaining, 1e-7f);
            for (int i = 1; i < PhysicalSignalAssayV1.Agents; i++)
                Assert.AreEqual(0, off.Receptions[i]);
        }

        [Test]
        public void EnabledArmProducesPhysicalSignalAndPaysForIt()
        {
            var on = PhysicalSignalAssayV1.Run(631, true);
            Assert.Greater(on.SignalMassEmitted, 0f);
            Assert.That(on.SignalEnergySpent,
                Is.EqualTo(on.SignalMassEmitted * 2f).Within(.0001f));
            Assert.That(on.SignalMassRemaining + on.SignalMassDecayed,
                Is.EqualTo(on.SignalMassEmitted).Within(.002f));
        }

        [Test]
        public void MatchedSeedInterventionIsRepeatable()
        {
            var one = PhysicalSignalAssayV1.Run(777, true);
            var two = PhysicalSignalAssayV1.Run(777, true);
            Assert.AreEqual(one.TotalHarvestMass, two.TotalHarvestMass, 1e-6f);
            Assert.AreEqual(one.SignalMassEmitted, two.SignalMassEmitted, 1e-6f);
            CollectionAssert.AreEqual(one.Receptions, two.Receptions);
            CollectionAssert.AreEqual(one.NetEnergy, two.NetEnergy);
        }

        [Test]
        public void AllOutcomesHaveFiniteValues()
        {
            foreach (bool enabled in new[] { false, true })
            {
                var outcome = PhysicalSignalAssayV1.Run(97, enabled);
                Assert.IsFalse(float.IsNaN(outcome.TotalHarvestMass));
                Assert.IsFalse(float.IsInfinity(outcome.TotalHarvestMass));
                for (int i = 0; i < PhysicalSignalAssayV1.Agents; ++i)
                {
                    Assert.IsFalse(float.IsNaN(outcome.NetEnergy[i]));
                    Assert.IsFalse(float.IsInfinity(outcome.NetEnergy[i]));
                    Assert.GreaterOrEqual(outcome.ConsumedMass[i], 0f);
                }
            }
        }
    }
}
#endif
