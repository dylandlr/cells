#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using Cells.Experiments;

namespace Cells.Tests
{
    public sealed class RestrictedDeviationStudyV1Tests
    {
        [Test]
        public void EveryResidentGetsThreeDeviationOptionsAtEachSeat()
        {
            var output = RestrictedDeviationStudyV1.Run(1000, 2);
            Assert.AreEqual(48, output.Comparisons);
            Assert.AreEqual(49, output.EstimatesCsv.TrimEnd().Split('\n').Length);
            Assert.AreEqual(97, output.SamplesCsv.TrimEnd().Split('\n').Length);
        }

        [Test]
        public void RunsAreByteReproducibleWithSameSeeds()
        {
            var a = RestrictedDeviationStudyV1.Run(1000, 2);
            var b = RestrictedDeviationStudyV1.Run(1000, 2);
            Assert.AreEqual(a.SamplesCsv, b.SamplesCsv);
            Assert.AreEqual(a.EstimatesCsv, b.EstimatesCsv);
        }

        [Test]
        public void DeviationStudyRejectsInsufficientReplication()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(
                () => RestrictedDeviationStudyV1.Run(1000, 1));
            Assert.Throws<System.ArgumentOutOfRangeException>(
                () => RestrictedDeviationStudyV1.Run(int.MaxValue, 30));
        }

        [Test]
        public void PairedRowsMatchReportedIndividualGain()
        {
            var output = RestrictedDeviationStudyV1.Run(1000, 2);
            var rows = output.SamplesCsv.TrimEnd().Split('\n');
            for (int row = 1; row < rows.Length; row++)
            {
                string[] cells = rows[row].TrimEnd('\r').Split(',');
                double before = double.Parse(cells[5],
                    System.Globalization.CultureInfo.InvariantCulture);
                double after = double.Parse(cells[6],
                    System.Globalization.CultureInfo.InvariantCulture);
                double difference = double.Parse(cells[7],
                    System.Globalization.CultureInfo.InvariantCulture);
                Assert.That(difference, Is.EqualTo(after - before).Within(1e-10));
            }
        }
    }
}
#endif
