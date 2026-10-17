#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using Cells.Experiments;

namespace Cells.Tests
{
    public sealed class PhysicalSignalCalibrationTestsV1
    {
        [Test]
        public void ZeroProductionHasNoDetectionsAcrossThresholds()
        {
            foreach (float threshold in new[] { .0001f, .001f, .01f })
            {
                var r = PhysicalSignalAssayV1.RunCalibrated(631, false, true, threshold, .18f, .13f);
                Assert.AreEqual(0f, r.SignalMassEmitted);
                for (int i = 1; i < PhysicalSignalAssayV1.Agents; i++)
                {
                    Assert.AreEqual(-1, r.FirstDetectionStep[i]);
                    Assert.AreEqual(0, r.Receptions[i]);
                    Assert.AreEqual(0f, r.PeakReceiverConcentration[i]);
                }
            }
        }

        [Test]
        public void EmissionAndNutrientLedgersAreAccountedFor()
        {
            foreach (bool respond in new[] { false, true })
            {
                var r = PhysicalSignalAssayV1.RunCalibrated(631, true, respond, .001f, .18f, .13f);
                float harvested = 0f;
                foreach (float value in r.ConsumedMass) harvested += value;
                Assert.That(r.TotalHarvestMass, Is.EqualTo(harvested).Within(.0001f));
                Assert.That(r.NutrientMassRemaining + harvested,
                    Is.EqualTo(r.NutrientMassInitial).Within(.002f));
                Assert.That(r.SignalMassRemaining + r.SignalMassDecayed,
                    Is.EqualTo(r.SignalMassEmitted).Within(.002f));
            }
        }

        [Test]
        public void NoDiffusionPreventsLongRangeTransmission()
        {
            var isolated = PhysicalSignalAssayV1.RunCalibrated(
                631, true, true, .0001f, 0f, 0f);
            Assert.Greater(isolated.SignalMassEmitted, 0f);
            for (int seat = 1; seat < PhysicalSignalAssayV1.Agents; seat++)
            {
                Assert.AreEqual(0, isolated.Receptions[seat]);
                Assert.AreEqual(0, isolated.ResponseEnabledSteps[seat]);
                Assert.AreEqual(0, isolated.SignalInfluencedMoves[seat]);
            }
            Assert.That(isolated.SignalMassRemaining,
                Is.EqualTo(isolated.SignalMassEmitted).Within(.002f));
        }

        [Test]
        public void ThresholdAndDiffusionMustBePhysicalFiniteParameters()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                PhysicalSignalAssayV1.RunCalibrated(631, true, true, 0f, .18f, .13f));
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                PhysicalSignalAssayV1.RunCalibrated(631, true, true, .001f, float.NaN, .13f));
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                PhysicalSignalAssayV1.RunCalibrated(631, true, true, .001f, .18f, -.1f));
        }


        [Test]
        public void DetectionIsNotAnAutomaticBehavioralDecision()
        {
            var passive = PhysicalSignalAssayV1.RunCalibrated(
                631, true, false, .0001f, .18f, .13f);
            for (int seat = 1; seat < PhysicalSignalAssayV1.Agents; seat++)
            {
                Assert.AreEqual(0, passive.ResponseEnabledSteps[seat]);
                Assert.AreEqual(0, passive.GradientUsedSteps[seat]);
                Assert.AreEqual(0, passive.SignalInfluencedMoves[seat]);
                Assert.AreEqual(0f, passive.CumulativeSignalDirectionalAlignment[seat]);
                Assert.AreEqual(-1, passive.FirstResponseDecisionStep[seat]);
            }
        }

        [Test]
        public void ActiveChemotaxisRequiresAReceptorBasedPolicyDecision()
        {
            var active = PhysicalSignalAssayV1.RunCalibrated(
                631, true, true, .0001f, .18f, .13f);
            for (int seat = 1; seat < PhysicalSignalAssayV1.Agents; seat++)
            {
                Assert.GreaterOrEqual(active.ResponseEnabledSteps[seat],
                    active.GradientUsedSteps[seat]);
                Assert.LessOrEqual(active.SignalInfluencedMoves[seat],
                    active.GradientUsedSteps[seat]);
                Assert.GreaterOrEqual(active.CumulativeMovementDistance[seat], 0f);
                if (active.ResponseEnabledSteps[seat] == 0)
                    Assert.AreEqual(-1, active.FirstResponseDecisionStep[seat]);
                else
                    Assert.GreaterOrEqual(active.FirstResponseDecisionStep[seat], 0);
                // Raw exposure may occur between policy decision ticks.
                Assert.GreaterOrEqual(active.Receptions[seat], 0);
            }
        }

        [Test]
        public void DetectionTelemetryIsDeterministicOnSeed()
        {
            var a = PhysicalSignalAssayV1.RunCalibrated(631, true, true, .001f, .18f, .13f);
            var b = PhysicalSignalAssayV1.RunCalibrated(631, true, true, .001f, .18f, .13f);
            CollectionAssert.AreEqual(a.FirstDetectionStep, b.FirstDetectionStep);
            CollectionAssert.AreEqual(a.PeakReceiverConcentration, b.PeakReceiverConcentration);
            CollectionAssert.AreEqual(a.FinalSourceDistance, b.FinalSourceDistance);
            CollectionAssert.AreEqual(a.SignalInfluencedMoves, b.SignalInfluencedMoves);
            CollectionAssert.AreEqual(a.CumulativeMovementDistance, b.CumulativeMovementDistance);
        }
    }
}
#endif
