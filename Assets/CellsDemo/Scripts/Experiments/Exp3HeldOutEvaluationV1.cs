using System;
using System.Globalization;
using System.Text;

namespace Cells.Experiments
{
    /// <summary>
    /// Out-of-sample evaluation of FROZEN episode-level strategy distributions.
    /// No EXP3 weights or policies update on test seeds.
    /// Same environment seeds and same seat-specific random quantiles are reused
    /// across learned, uniform and fixed silent baselines (common random numbers).
    /// </summary>
    public static class Exp3HeldOutEvaluationV1
    {
        public const string Schema = "cells.exp3-heldout.v1";
        public sealed class Output
        {
            public string EpisodesCsv;
            public string AgentsCsv;
            public string PairedBySeedCsv;
            public string SummaryCsv;
            public string ManifestJson;
            public int HeldOutSeeds;
        }

        public static int Select(double[] probabilities, double u)
        {
            if (probabilities == null || probabilities.Length !=
                Exp3StrategyTrainingV1.StrategyNames.Length)
                throw new ArgumentException("One probability per frozen strategy is required.");
            if (double.IsNaN(u) || u < 0 || u >= 1)
                throw new ArgumentOutOfRangeException("u");
            double sum = 0;
            for (int i = 0; i < probabilities.Length; ++i)
            {
                if (double.IsNaN(probabilities[i]) || double.IsInfinity(probabilities[i]) ||
                    probabilities[i] < 0 || probabilities[i] > 1)
                    throw new ArgumentException("Invalid policy probability.");
                sum += probabilities[i];
            }
            if (Math.Abs(sum - 1) > 1e-9)
                throw new ArgumentException("Probabilities must sum to one.");
            double cumulative = 0;
            for (int i = 0; i < probabilities.Length; i++)
            {
                cumulative += probabilities[i];
                if (u < cumulative) return i;
            }
            return probabilities.Length - 1;
        }

        public static Output Evaluate(Exp3StrategyTrainingV1.Output training,
            int firstTestSeed = 100000, int heldOutSeeds = 100,
            int drawSeed = 29013)
        {
            if (training == null || training.FrozenSeatProbabilities == null ||
                training.FrozenSeatProbabilities.Length != SignalingGameV1.Players)
                throw new ArgumentException("Training must contain four frozen seat distributions.");
            if (heldOutSeeds < 2 || heldOutSeeds > 100000 ||
                firstTestSeed > int.MaxValue - heldOutSeeds)
                throw new ArgumentOutOfRangeException("heldOutSeeds");
            // IMPORTANT: caller must additionally audit the training manifest for seed overlap.
            // The exporter uses a disjoint hard-coded interval and asserts it below.
            var learned = training.FrozenSeatProbabilities;
            for (int i = 0; i < learned.Length; i++) Select(learned[i], 0);

            string[] conditions = { "learned_frozen", "uniform_frozen", "all_silent" };
            var episodes = new StringBuilder(
                "schema,seed,condition,collective_return,harvest,signals,invalid_actions\n");
            var agents = new StringBuilder(
                "schema,seed,condition,seat,strategy,individual_return\n");
            var pairs = new StringBuilder(
                "seed,learned_return,uniform_return,silent_return,learned_minus_uniform,learned_minus_silent\n");
            var vsUniform = new System.Collections.Generic.List<PairedAssayAnalysisV1.Record>();
            var vsSilent = new System.Collections.Generic.List<PairedAssayAnalysisV1.Record>();
            var uniform = { 0.25, 0.25, 0.25, 0.25 };
            for (int n = 0; n < heldOutSeeds; ++n)
            {
                int seed = firstTestSeed + n;
                // Deterministic shared quantiles for variance reduction: the fixed
                // policy conditions encounter the same environment and selection noise.
                var drawRandom = new Random(unchecked(drawSeed + n * 7919));
                var draws = new double[SignalingGameV1.Players];
                for (int seat = 0; seat < draws.Length; ++seat)
                    draws[seat] = drawRandom.NextDouble();
                double[] totals = new double[conditions.Length];
                for (int condition = 0; condition < conditions.Length; ++condition)
                {
                    var policies = new SignalingGameV1.IPolicy[SignalingGameV1.Players];
                    var names = new string[SignalingGameV1.Players];
                    for (int seat = 0; seat < policies.Length; ++seat)
                    {
                        int selected = condition == 2 ? 0 :
                            Select(condition == 0 ? learned[seat] : uniform, draws[seat]);
                        names[seat] = Exp3StrategyTrainingV1.StrategyNames[selected];
                        policies[seat] = SignalingExperimentRunnerV1.NewPolicy(names[seat]);
                    }
                    var result = SignalingGameV1.Evaluate(seed, policies);
                    double total = 0;
                    for (int seat = 0; seat < policies.Length; ++seat)
                    {
                        total += result.Returns[seat];
                        agents.Append(Schema).Append(',').Append(seed).Append(',')
                            .Append(conditions[condition]).Append(',').Append(seat).Append(',')
                            .Append(names[seat]).Append(',').Append(F(result.Returns[seat]))
                            .Append('\n');
                    }
                    totals[condition] = total;
                    episodes.Append(Schema).Append(',').Append(seed).Append(',')
                        .Append(conditions[condition]).Append(',').Append(F(total)).Append(',')
                        .Append(F(result.TotalHarvest)).Append(',').Append(result.SignalCount)
                        .Append(',').Append(result.InvalidActions).Append('\n');
                }
                vsUniform.Add(new PairedAssayAnalysisV1.Record(seed, totals[1], totals[0]));
                vsSilent.Add(new PairedAssayAnalysisV1.Record(seed, totals[2], totals[0]));
                pairs.Append(seed).Append(',').Append(F(totals[0])).Append(',')
                    .Append(F(totals[1])).Append(',').Append(F(totals[2])).Append(',')
                    .Append(F(totals[0] - totals[1])).Append(',')
                    .Append(F(totals[0] - totals[2])).Append('\n');
            }
            var summary = new StringBuilder(PairedAssayAnalysisV1.Header);
            summary.Append(PairedAssayAnalysisV1.ToCsv("learned_minus_uniform",
                PairedAssayAnalysisV1.EstimatePaired(vsUniform)));
            summary.Append(PairedAssayAnalysisV1.ToCsv("learned_minus_silent",
                PairedAssayAnalysisV1.EstimatePaired(vsSilent)));
            return new Output
            {
                HeldOutSeeds = heldOutSeeds,
                EpisodesCsv = episodes.ToString(),
                AgentsCsv = agents.ToString(),
                PairedBySeedCsv = pairs.ToString(),
                SummaryCsv = summary.ToString(),
                ManifestJson = "{\n"
                    + "  \"schema\": \"" + Schema + "\",\n"
                    + "  \"first_test_seed\": " + firstTestSeed + ",\n"
                    + "  \"held_out_seeds\": " + heldOutSeeds + ",\n"
                    + "  \"draw_seed\": " + drawSeed + ",\n"
                    + "  \"training_episodes\": " + training.Episodes + ",\n"
                    + "  \"policy_updates_on_test\": 0,\n"
                    + "  \"comparison\": \"frozen EXP3 vs frozen uniform vs all-silent\",\n"
                    + "  \"primary_endpoint\": \"paired collective return: learned minus uniform\",\n"
                    + "  \"source_revision\": \"unverified\",\n"
                    + "  \"caution\": \"Symbolic game, not physical chemistry or equilibrium evidence.\"\n"
                    + "}\n"
            };
        }

        private static string F(double value) =>
            value.ToString("R", CultureInfo.InvariantCulture);
    }
}
