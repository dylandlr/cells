using NUnit.Framework;
using Cells.Experiments;

namespace Cells.Tests.Editor
{
    public sealed class SignalingExperimentRunnerV1Tests
    {
        [Test]
        public void ProducesTenProfilesForEachSeedWithFourAgentRows()
        {
            var output = SignalingExperimentRunnerV1.Run(2, 631, "test-sha");
            Assert.AreEqual(20, output.Episodes);
            Assert.AreEqual(21, output.EpisodeCsv.TrimEnd().Split('\n').Length);
            Assert.AreEqual(81, output.AgentCsv.TrimEnd().Split('\n').Length);
            StringAssert.Contains("\"replicates\": 2", output.ManifestJson);
            StringAssert.Contains("\"source_revision\": \"test-sha\"", output.ManifestJson);
        }

        [Test]
        public void RerunWithIdenticalSeedsIsByteForByteRepeatable()
        {
            var first = SignalingExperimentRunnerV1.Run(3, 130, "same");
            var second = SignalingExperimentRunnerV1.Run(3, 130, "same");
            Assert.AreEqual(first.AgentCsv, second.AgentCsv);
            Assert.AreEqual(first.EpisodeCsv, second.EpisodeCsv);
            Assert.AreEqual(first.ManifestJson, second.ManifestJson);
        }

        [Test]
        public void EverySignalingRunHasMatchedNoSignalSeatRotation()
        {
            var output = SignalingExperimentRunnerV1.Run(1, 740);
            var lines = output.EpisodeCsv.TrimEnd().Split('\n');
            for (int r = 0; r < 4; r++)
            {
                bool hasSignal = false;
                bool hasControl = false;
                foreach (string line in lines)
                {
                    if (line.Contains(",mixed_signaling_r" + r + ",740,")) hasSignal = true;
                    if (line.Contains(",mixed_no_signal_r" + r + ",740,")) hasControl = true;
                }
                Assert.IsTrue(hasSignal);
                Assert.IsTrue(hasControl);
            }
        }

        [Test]
        public void CsvContainsNoLocaleDependentDecimalCommas()
        {
            var original = System.Threading.Thread.CurrentThread.CurrentCulture;
            try
            {
                System.Threading.Thread.CurrentThread.CurrentCulture =
                    System.Globalization.CultureInfo.GetCultureInfo("fr-FR");
                var output = SignalingExperimentRunnerV1.Run(1);
                Assert.AreEqual(9, output.EpisodeCsv.Split('\n')[1].Split(',').Length);
                Assert.AreEqual(7, output.AgentCsv.Split('\n')[1].Split(',').Length);
            }
            finally
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = original;
            }
        }

        [Test]
        public void InvalidExperimentSizesAreRejected()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(
                () => SignalingExperimentRunnerV1.Run(0));
            Assert.Throws<System.ArgumentOutOfRangeException>(
                () => SignalingExperimentRunnerV1.Run(2, int.MaxValue));
        }
    }
}
