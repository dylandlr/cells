#if UNITY_INCLUDE_TESTS
using System;
using NUnit.Framework;
using Cells.Experiments;

namespace Cells.Tests
{
    public sealed class PairedAssayAnalysisV1Tests
    {
        [Test]
        public void ConstantTreatmentEffectHasExactIntervalAndZeroVariance()
        {
            var records = new[] {
                new PairedAssayAnalysisV1.Record(10, 1, 3),
                new PairedAssayAnalysisV1.Record(11, -2, 0),
                new PairedAssayAnalysisV1.Record(12, 4, 6)
            };
            var estimate = PairedAssayAnalysisV1.EstimatePaired(records, 500, 12);
            Assert.AreEqual(3, estimate.Count);
            Assert.AreEqual(2.0, estimate.MeanDifference, 1e-12);
            Assert.AreEqual(0.0, estimate.StandardDeviation, 1e-12);
            Assert.AreEqual(2.0, estimate.Lower95, 1e-12);
            Assert.AreEqual(2.0, estimate.Upper95, 1e-12);
        }

        [Test]
        public void BootstrapIsRepeatableWithFixedSeed()
        {
            var records = new[] {
                new PairedAssayAnalysisV1.Record(10, 1, 2),
                new PairedAssayAnalysisV1.Record(11, 1, 4),
                new PairedAssayAnalysisV1.Record(12, 1, 8),
                new PairedAssayAnalysisV1.Record(13, 1, 5)
            };
            var first = PairedAssayAnalysisV1.EstimatePaired(records);
            var second = PairedAssayAnalysisV1.EstimatePaired(records);
            Assert.AreEqual(first.Lower95, second.Lower95);
            Assert.AreEqual(first.Upper95, second.Upper95);
            Assert.AreEqual(first.MeanDifference, second.MeanDifference);
            Assert.Less(first.Lower95, first.Upper95);
        }

        [Test]
        public void RejectsDuplicateSeedsAndNonFiniteData()
        {
            Assert.Throws<ArgumentException>(() => PairedAssayAnalysisV1.EstimatePaired(
                new[] { new PairedAssayAnalysisV1.Record(4, 0, 1),
                        new PairedAssayAnalysisV1.Record(4, 1, 2) }));
            Assert.Throws<ArgumentException>(() => PairedAssayAnalysisV1.EstimatePaired(
                new[] { new PairedAssayAnalysisV1.Record(4, 0, double.NaN),
                        new PairedAssayAnalysisV1.Record(5, 1, 2) }));
            Assert.Throws<ArgumentException>(() => PairedAssayAnalysisV1.EstimatePaired(
                new[] { new PairedAssayAnalysisV1.Record(4, 0, 1) }));
        }

        [Test]
        public void EstimateCsvUsesInvariantDecimalSeparator()
        {
            var current = System.Threading.Thread.CurrentThread.CurrentCulture;
            try
            {
                System.Threading.Thread.CurrentThread.CurrentCulture =
                    System.Globalization.CultureInfo.GetCultureInfo("fr-FR");
                var estimate = PairedAssayAnalysisV1.EstimatePaired(
                    new[] { new PairedAssayAnalysisV1.Record(1, 0, .25),
                            new PairedAssayAnalysisV1.Record(2, 0, .75) });
                var csv = PairedAssayAnalysisV1.ToCsv("net", estimate);
                Assert.AreEqual(7, csv.Trim().Split(',').Length);
                StringAssert.Contains("0.5", csv);
            }
            finally
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = current;
            }
        }
    }
}
#endif
