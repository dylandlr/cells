using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Cells.Experiments
{
    /// <summary>
    /// Seed-level paired differences, with deterministic percentile bootstrap intervals.
    /// No inference of Nash equilibrium or emergent cooperation. Individual cell
    /// observations are NOT independent replicates; the unit is the environment seed.
    /// </summary>
    public static class PairedAssayAnalysisV1
    {
        public struct Record
        {
            public int Seed;
            public double Control;
            public double Treatment;
            public Record(int seed, double control, double treatment)
            {
                Seed = seed;
                Control = control;
                Treatment = treatment;
            }
        }

        public struct Estimate
        {
            public int Count;
            public double MeanDifference;
            public double StandardDeviation;
            public double StandardError;
            public double Lower95;
            public double Upper95;
        }

        public static Estimate EstimatePaired(IList<Record> records, int bootstrapSamples = 4000,
            int bootstrapSeed = 631)
        {
            if (records == null || records.Count < 2)
                throw new ArgumentException("At least two paired seeds required.");
            if (bootstrapSamples < 100)
                throw new ArgumentOutOfRangeException("bootstrapSamples");
            var seen = new HashSet<int>();
            var differences = new double[records.Count];
            double total = 0;
            for (int i = 0; i < records.Count; i++)
            {
                Record r = records[i];
                if (!seen.Add(r.Seed))
                    throw new ArgumentException("Duplicate environment seed.");
                if (double.IsNaN(r.Control) || double.IsInfinity(r.Control) ||
                    double.IsNaN(r.Treatment) || double.IsInfinity(r.Treatment))
                    throw new ArgumentException("Non-finite outcome.");
                differences[i] = r.Treatment - r.Control;
                total += differences[i];
            }

            double mean = total / differences.Length;
            double variance = 0;
            for (int i = 0; i < differences.Length; i++)
                variance += (differences[i] - mean) * (differences[i] - mean);
            double standardDeviation = Math.Sqrt(variance / (differences.Length - 1));
            var bootstrapMeans = new double[bootstrapSamples];
            var rng = new Random(bootstrapSeed);
            for (int b = 0; b < bootstrapSamples; b++)
            {
                double sum = 0;
                for (int i = 0; i < differences.Length; i++)
                    sum += differences[rng.Next(differences.Length)];
                bootstrapMeans[b] = sum / differences.Length;
            }
            Array.Sort(bootstrapMeans);
            return new Estimate
            {
                Count = differences.Length,
                MeanDifference = mean,
                StandardDeviation = standardDeviation,
                StandardError = standardDeviation / Math.Sqrt(differences.Length),
                Lower95 = Percentile(bootstrapMeans, .025),
                Upper95 = Percentile(bootstrapMeans, .975)
            };
        }

        private static double Percentile(double[] values, double p)
        {
            double index = p * (values.Length - 1);
            int low = (int)Math.Floor(index);
            int high = Math.Min(low + 1, values.Length - 1);
            return values[low] + (values[high] - values[low]) * (index - low);
        }

        public static string ToCsv(string metric, Estimate estimate)
        {
            return metric + "," + estimate.Count + "," + F(estimate.MeanDifference) + "," +
                F(estimate.StandardDeviation) + "," + F(estimate.StandardError) + "," +
                F(estimate.Lower95) + "," + F(estimate.Upper95) + "\n";
        }

        public const string Header =
            "metric,paired_seeds,mean_on_minus_off,sample_sd,standard_error,bootstrap95_low,bootstrap95_high\n";

        private static string F(double x) { return x.ToString("R", CultureInfo.InvariantCulture); }
    }
}
