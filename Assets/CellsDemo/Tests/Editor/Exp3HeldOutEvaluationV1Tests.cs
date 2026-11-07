#if UNITY_INCLUDE_TESTS
using System;
using NUnit.Framework;
using Cells.Experiments;

namespace Cells.Tests
{
    public sealed class Exp3HeldOutEvaluationV1Tests
    {
        [Test]
        public void HeldOutReportIsDeterministicAndHasExpectedCounts()
        {
            var trained = Exp3StrategyTrainingV1.Run(631, 8, .12, 100);
            var a = Exp3HeldOutEvaluationV1.Evaluate(trained, 10000, 5, 444);
            var b = Exp3HeldOutEvaluationV1.Evaluate(trained, 10000, 5, 444);
            Assert.AreEqual(a.EpisodesCsv, b.EpisodesCsv);
            Assert.AreEqual(a.AgentsCsv, b.AgentsCsv);
            Assert.AreEqual(a.SummaryCsv, b.SummaryCsv);
            Assert.AreEqual(a.PairedBySeedCsv, b.PairedBySeedCsv);
            Assert.AreEqual(16, a.EpisodesCsv.TrimEnd().Split('\n').Length);
            Assert.AreEqual(61, a.AgentsCsv.TrimEnd().Split('\n').Length);
            Assert.AreEqual(6, a.PairedBySeedCsv.TrimEnd().Split('\n').Length);
            Assert.AreEqual(3, a.SummaryCsv.TrimEnd().Split('\n').Length);
        }

        [Test]
        public void HeldOutEvaluationDoesNotMutateLearnedDistributions()
        {
            var trained = Exp3StrategyTrainingV1.Run(631, 7);
            var original = (double[])trained.FrozenSeatProbabilities[0].Clone();
            Exp3HeldOutEvaluationV1.Evaluate(trained, 10000, 4);
            CollectionAssert.AreEqual(original, trained.FrozenSeatProbabilities[0]);
        }

        [Test]
        public void OverlappingTrainingAndTestSeedsAreRejected()
        {
            var trained = Exp3StrategyTrainingV1.Run(631, 10);
            Assert.Throws<ArgumentException>(() =>
                Exp3HeldOutEvaluationV1.Evaluate(trained, 638, 5));
        }

        [Test]
        public void ProbabilityContractRejectsBadDistributions()
        {
            Assert.Throws<ArgumentException>(() =>
                Exp3HeldOutEvaluationV1.Select(new double[] { 1, 1, 0, 0 }, .5));
            Assert.Throws<ArgumentException>(() =>
                Exp3HeldOutEvaluationV1.Select(new double[] { 0, -.1, .1, 1 }, .5));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                Exp3HeldOutEvaluationV1.Select(new double[] { .25, .25, .25, .25 }, 1));
            Assert.AreEqual(0,
                Exp3HeldOutEvaluationV1.Select(new double[] { 1, 0, 0, 0 }, .4));
        }

        [Test]
        public void FrozenUniformPolicySelectionIsIndependentOfTrainingWeights()
        {
            var uniform = new double[] { .25, .25, .25, .25 };
            Assert.AreEqual(0, Exp3HeldOutEvaluationV1.Select(uniform, 0));
            Assert.AreEqual(1, Exp3HeldOutEvaluationV1.Select(uniform, .25));
            Assert.AreEqual(2, Exp3HeldOutEvaluationV1.Select(uniform, .5));
            Assert.AreEqual(3, Exp3HeldOutEvaluationV1.Select(uniform, .75));
        }
    }
}
#endif
