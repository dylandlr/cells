using System;
using System.Globalization;
using System.Text;

namespace Cells.Experiments
{
    /// <summary>
    /// Full 4^4 restricted normal-form empirical payoff table of frozen policies.
    /// Each payoff is the mean of the seat's 80-decision return on a common seed set.
    /// All 256 profiles are evaluated, without assuming symmetric seats or invoking
    /// an unobserved counterfactual inside the learner. Analysis can inspect all
    /// payoffs afterwards; learning agents are never given this tensor.
    /// </summary>
    public sealed class EmpiricalPayoffTensorV1
    {
        public const int Strategies = 4;
        public const int Seats = SignalingGameV1.Players;
        public const int Profiles = 256;
        public const string Schema = "cells.payoff-tensor.v1";

        private readonly double[,] payoffs = new double[Profiles, Seats];
        public int FirstSeed { get; private set; }
        public int Seeds { get; private set; }
        public int EpisodeCount { get { return Profiles * Seeds; } }

        private EmpiricalPayoffTensorV1(int firstSeed, int seeds)
        {
            FirstSeed = firstSeed;
            Seeds = seeds;
        }

        public static int Encode(int[] strategies)
        {
            if (strategies == null || strategies.Length != Seats)
                throw new ArgumentException("Exactly four strategy indices required.");
            int id = 0;
            for (int i = 0; i < Seats; i++)
            {
                if (strategies[i] < 0 || strategies[i] >= Strategies)
                    throw new ArgumentOutOfRangeException("strategies");
                id = id * Strategies + strategies[i];
            }
            return id;
        }

        public static int[] Decode(int profile)
        {
            if (profile < 0 || profile >= Profiles)
                throw new ArgumentOutOfRangeException("profile");
            var strategies = new int[Seats];
            for (int i = Seats - 1; i >= 0; i--)
            {
                strategies[i] = profile % Strategies;
                profile /= Strategies;
            }
            return strategies;
        }

        public double Payoff(int profile, int seat)
        {
            if (profile < 0 || profile >= Profiles || seat < 0 || seat >= Seats)
                throw new ArgumentOutOfRangeException("profile/seat");
            return payoffs[profile, seat];
        }

        public static EmpiricalPayoffTensorV1 Build(int firstSeed = 300000, int seeds = 30)
        {
            if (seeds < 2 || seeds > 10000 || (long)firstSeed + seeds > int.MaxValue)
                throw new ArgumentOutOfRangeException("seeds");
            var tensor = new EmpiricalPayoffTensorV1(firstSeed, seeds);
            for (int profile = 0; profile < Profiles; profile++)
            {
                int[] indices = Decode(profile);
                var policies = new SignalingGameV1.IPolicy[Seats];
                for (int seat = 0; seat < Seats; seat++)
                    policies[seat] = SignalingExperimentRunnerV1.NewPolicy(
                        Exp3StrategyTrainingV1.StrategyNames[indices[seat]]);
                for (int repetition = 0; repetition < seeds; repetition++)
                {
                    var result = SignalingGameV1.Evaluate(firstSeed + repetition, policies);
                    for (int seat = 0; seat < Seats; seat++)
                        tensor.payoffs[profile, seat] += result.Returns[seat] / seeds;
                }
            }
            return tensor;
        }

        /// <summary>
        /// Expected seat payoffs under independent mixed strategy distributions.
        /// Each row must sum to one. This exact contraction uses estimated table
        /// means, so it does NOT account for table sampling uncertainty.
        /// </summary>
        public double[] ExpectedPayoffs(double[][] mixtures)
        {
            ValidateMixtures(mixtures);
            var value = new double[Seats];
            for (int profile = 0; profile < Profiles; profile++)
            {
                var choices = Decode(profile);
                double probability = 1;
                for (int seat = 0; seat < Seats; seat++)
                    probability *= mixtures[seat][choices[seat]];
                for (int seat = 0; seat < Seats; seat++)
                    value[seat] += probability * payoffs[profile, seat];
            }
            return value;
        }

        /// <summary>
        /// Best pure-strategy unilateral deviation gain against a fixed independent
        /// mixed profile, restricted to the four existing scripted strategies.
        /// Nonnegative by construction; 0 is NOT unrestricted Nash certification.
        /// </summary>
        public double[] RestrictedDeviationGains(double[][] mixtures)
        {
            ValidateMixtures(mixtures);
            double[] baseline = ExpectedPayoffs(mixtures);
            var gains = new double[Seats];
            for (int seat = 0; seat < Seats; seat++)
            {
                double best = baseline[seat];
                for (int strategy = 0; strategy < Strategies; strategy++)
                {
                    var changed = CloneMixtures(mixtures);
                    Array.Clear(changed[seat], 0, Strategies);
                    changed[seat][strategy] = 1;
                    best = Math.Max(best, ExpectedPayoffs(changed)[seat]);
                }
                gains[seat] = Math.Max(0, best - baseline[seat]);
            }
            return gains;
        }

        private static double[][] CloneMixtures(double[][] mixtures)
        {
            var copy = new double[Seats][];
            for (int i = 0; i < Seats; i++)
                copy[i] = (double[])mixtures[i].Clone();
            return copy;
        }

        private static void ValidateMixtures(double[][] mixtures)
        {
            if (mixtures == null || mixtures.Length != Seats)
                throw new ArgumentException("One mixture required per agent seat.");
            for (int seat = 0; seat < Seats; seat++)
            {
                if (mixtures[seat] == null || mixtures[seat].Length != Strategies)
                    throw new ArgumentException("Each seat needs four probabilities.");
                double sum = 0;
                foreach (double p in mixtures[seat])
                {
                    if (double.IsNaN(p) || double.IsInfinity(p) || p < 0 || p > 1)
                        throw new ArgumentException("Invalid mixed strategy.");
                    sum += p;
                }
                if (Math.Abs(sum - 1) > 1e-9)
                    throw new ArgumentException("Mixture probabilities must sum to one.");
            }
        }

        public static double[][] Homogeneous(int strategy)
        {
            if (strategy < 0 || strategy >= Strategies)
                throw new ArgumentOutOfRangeException("strategy");
            var result = new double[Seats][];
            for (int seat = 0; seat < Seats; seat++)
            {
                result[seat] = new double[Strategies];
                result[seat][strategy] = 1;
            }
            return result;
        }

        public static double[][] Uniform()
        {
            var result = new double[Seats][];
            for (int seat = 0; seat < Seats; seat++)
                result[seat] = new[] { .25, .25, .25, .25 };
            return result;
        }

        public string Csv()
        {
            var csv = new StringBuilder(
                "schema,profile_id,seat0,seat1,seat2,seat3,payoff0,payoff1,payoff2,payoff3,seeds\n");
            for (int profile = 0; profile < Profiles; profile++)
            {
                int[] s = Decode(profile);
                csv.Append(Schema).Append(',').Append(profile);
                for (int i = 0; i < Seats; i++) csv.Append(',').Append(s[i]);
                for (int i = 0; i < Seats; i++) csv.Append(',').Append(F(payoffs[profile, i]));
                csv.Append(',').Append(Seeds).Append('\n');
            }
            return csv.ToString();
        }

        public string DeviationCsv(double[][] mixture, string label)
        {
            double[] payoffsBySeat = ExpectedPayoffs(mixture);
            double[] gains = RestrictedDeviationGains(mixture);
            var csv = new StringBuilder("profile,seat,expected_payoff,restricted_deviation_gain\n");
            for (int seat = 0; seat < Seats; seat++)
                csv.Append(label).Append(',').Append(seat).Append(',')
                    .Append(F(payoffsBySeat[seat])).Append(',').Append(F(gains[seat]))
                    .Append('\n');
            return csv.ToString();
        }

        private static string F(double d) =>
            d.ToString("R", CultureInfo.InvariantCulture);
    }
}
