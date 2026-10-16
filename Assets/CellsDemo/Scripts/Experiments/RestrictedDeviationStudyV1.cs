using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Cells.Experiments
{
    /// <summary>
    /// Empirical unilateral deviation analysis in a RESTRICTED four-policy library.
    /// For each homogeneous resident profile and each seat, hold three residents
    /// fixed and switch one deviator to every other frozen strategy.
    /// Comparisons are paired by environment seed. These conditional gains are
    /// NOT unrestricted Nash regret or proof of game-theoretic equilibrium.
    /// </summary>
    public static class RestrictedDeviationStudyV1
    {
        public const string Schema = "cells.restricted-deviation.v1";
        public sealed class Output
        {
            public string SamplesCsv;
            public string EstimatesCsv;
            public string ManifestJson;
            public int Seeds;
            public int Comparisons;
        }

        public static Output Run(int firstSeed = 200000, int seeds = 30)
        {
            if (seeds < 2 || seeds > 10000 || (long)firstSeed + seeds > int.MaxValue)
                throw new ArgumentOutOfRangeException("seeds");

            string[] names = Exp3StrategyTrainingV1.StrategyNames;
            var raw = new StringBuilder(
                "schema,seed,resident,deviator,seat,resident_seat_return,deviation_seat_return,delta_seat_return,resident_collective_return,deviation_collective_return\n");
            var estimated = new StringBuilder(
                "schema,resident,deviator,seat,paired_seeds,mean_deviation_gain,bootstrap95_low,bootstrap95_high\n");
            int comparisons = 0;

            for (int resident = 0; resident < names.Length; resident++)
            {
                var baselineBySeed = new SignalingGameV1.RunSummary[seeds];
                for (int n = 0; n < seeds; n++)
                    baselineBySeed[n] = Play(firstSeed + n, names, resident, -1, resident);

                for (int deviator = 0; deviator < names.Length; deviator++)
                {
                    if (deviator == resident) continue;
                    for (int seat = 0; seat < SignalingGameV1.Players; seat++)
                    {
                        var pairs = new List<PairedAssayAnalysisV1.Record>();
                        for (int n = 0; n < seeds; n++)
                        {
                            int seed = firstSeed + n;
                            var baseline = baselineBySeed[n];
                            var changed = Play(seed, names, resident, seat, deviator);
                            double baseCollective = Sum(baseline.Returns);
                            double changedCollective = Sum(changed.Returns);
                            double before = baseline.Returns[seat];
                            double after = changed.Returns[seat];
                            pairs.Add(new PairedAssayAnalysisV1.Record(seed, before, after));
                            raw.Append(Schema).Append(',').Append(seed).Append(',')
                                .Append(names[resident]).Append(',').Append(names[deviator])
                                .Append(',').Append(seat).Append(',')
                                .Append(F(before)).Append(',').Append(F(after)).Append(',')
                                .Append(F(after - before)).Append(',')
                                .Append(F(baseCollective)).Append(',')
                                .Append(F(changedCollective)).Append('\n');
                        }
                        var effect = PairedAssayAnalysisV1.EstimatePaired(pairs);
                        estimated.Append(Schema).Append(',').Append(names[resident]).Append(',')
                            .Append(names[deviator]).Append(',').Append(seat).Append(',')
                            .Append(effect.Count).Append(',')
                            .Append(F(effect.MeanDifference)).Append(',')
                            .Append(F(effect.Lower95)).Append(',')
                            .Append(F(effect.Upper95)).Append('\n');
                        comparisons++;
                    }
                }
            }

            return new Output
            {
                Seeds = seeds,
                Comparisons = comparisons,
                SamplesCsv = raw.ToString(),
                EstimatesCsv = estimated.ToString(),
                ManifestJson = "{\n"
                    + "  \"schema\": \"" + Schema + "\",\n"
                    + "  \"first_seed\": " + firstSeed + ",\n"
                    + "  \"seeds\": " + seeds + ",\n"
                    + "  \"resident_profiles\": \"homogeneous frozen strategies\",\n"
                    + "  \"deviation_scope\": \"one agent switches to another strategy from frozen library\",\n"
                    + "  \"comparison_unit\": \"paired environment seed per resident, deviation, and seat\",\n"
                    + "  \"comparisons\": " + comparisons + ",\n"
                    + "  \"source_revision\": \"unverified\",\n"
                    + "  \"limitations\": \"Only homogeneous residents and restricted unilateral deviations; not full empirical normal form, mixed equilibria, or unrestricted Nash regret.\"\n"
                    + "}\n"
            };
        }

        private static SignalingGameV1.RunSummary Play(
            int seed, string[] names, int resident, int seat, int deviator)
        {
            var policies = new SignalingGameV1.IPolicy[SignalingGameV1.Players];
            for (int p = 0; p < policies.Length; p++)
                policies[p] = SignalingExperimentRunnerV1.NewPolicy(
                    names[p == seat ? deviator : resident]);
            return SignalingGameV1.Evaluate(seed, policies);
        }

        private static double Sum(double[] values)
        {
            double total = 0;
            foreach (double value in values) total += value;
            return total;
        }

        private static string F(double value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }
    }
}
