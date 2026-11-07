#if UNITY_INCLUDE_TESTS
using System;
using NUnit.Framework;
using Cells.Experiments;

namespace Cells.Tests
{
    public sealed class Exp3StrategyTestsV1
    {
        [Test]
        public void InitiallyAllStrategiesHaveEqualProbability()
        {
            var selector = new Exp3EpisodeSelectorV1(4, 631, .2);
            foreach (double p in selector.Probabilities())
                Assert.That(p, Is.EqualTo(.25).Within(1e-12));
        }

        [Test]
        public void PositiveSelectedArmRewardRaisesItsProbability()
        {
            var selector = new Exp3EpisodeSelectorV1(4, 631, .2);
            int chosen = selector.Choose();
            selector.Observe(1);
            Assert.Greater(selector.Probabilities()[chosen], .25);
            Assert.AreEqual(1, selector.Updates);
        }

        [Test]
        public void FeedbackCannotLeakCounterfactualOrBeRepeated()
        {
            var selector = new Exp3EpisodeSelectorV1(4, 631);
            Assert.Throws<InvalidOperationException>(() => selector.Observe(.5));
            selector.Choose();
            Assert.Throws<InvalidOperationException>(() => selector.Choose());
            Assert.Throws<ArgumentOutOfRangeException>(() => selector.Observe(-.1));
            Assert.IsTrue(selector.AwaitingFeedback);
            selector.Observe(.75);
            Assert.IsFalse(selector.AwaitingFeedback);
            Assert.Throws<InvalidOperationException>(() => selector.Observe(.75));
        }

        [Test]
        public void ExplorationFloorPersistsForEveryStrategy()
        {
            var selector = new Exp3EpisodeSelectorV1(4, 631, .2);
            for (int t = 0; t < 1000; t++)
            {
                selector.Choose();
                selector.Observe(1);
            }
            double total = 0;
            foreach (double probability in selector.Probabilities())
            {
                Assert.GreaterOrEqual(probability, .05 - 1e-12);
                Assert.IsFalse(double.IsNaN(probability));
                total += probability;
            }
            Assert.That(total, Is.EqualTo(1).Within(1e-10));
        }

        [Test]
        public void EpisodeReturnNormalizationIsBoundedWithoutSilentClipping()
        {
            Assert.AreEqual(0, Exp3StrategyTrainingV1.NormalizeEpisodeReturn(-80, 80), 1e-12);
            Assert.AreEqual(1, Exp3StrategyTrainingV1.NormalizeEpisodeReturn(320, 80), 1e-12);
            Assert.AreEqual(.2, Exp3StrategyTrainingV1.NormalizeEpisodeReturn(0, 80), 1e-12);
            Assert.Throws<ArgumentOutOfRangeException>(
                () => Exp3StrategyTrainingV1.NormalizeEpisodeReturn(400, 80));
        }

        [Test]
        public void TrainingIsByteReproducibleWithIndependentAgentControllers()
        {
            var first = Exp3StrategyTrainingV1.Run(631, 10, .12, 1234);
            var second = Exp3StrategyTrainingV1.Run(631, 10, .12, 1234);
            Assert.AreEqual(first.EpisodesCsv, second.EpisodesCsv);
            Assert.AreEqual(first.FinalPolicyCsv, second.FinalPolicyCsv);
            Assert.AreEqual(first.ManifestJson, second.ManifestJson);
            Assert.AreEqual(41, first.EpisodesCsv.TrimEnd().Split('\n').Length);
            Assert.AreEqual(17, first.FinalPolicyCsv.TrimEnd().Split('\n').Length);
        }

        [Test]
        public void InvalidTrainingInputsAreRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => Exp3StrategyTrainingV1.Run(631, 0));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => Exp3StrategyTrainingV1.Run(int.MaxValue, 2));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new Exp3EpisodeSelectorV1(4, 1, 0));
        }
    }
}
#endif
