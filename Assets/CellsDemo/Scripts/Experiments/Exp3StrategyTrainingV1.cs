using System;
using System.Globalization;
using System.Text;

namespace Cells.Experiments
{
    /// <summary>
    /// Independent episode-level EXP3 learners in SignalingGameV1. Each controls
    /// one of four players and commits to one frozen scripted policy per episode.
    /// Environment RNG is separate from selection RNG and policy-decision RNG.
    /// All updates happen after the complete 80-decision episode.
    /// </summary>
    public static class Exp3StrategyTrainingV1
    {
        public const string Schema = "cells.exp3-episode.v1";
        public static readonly string[] StrategyNames =
            { "silent", "follower", "signaler", "random" };

        // The game bounds individual per-decision reward in [-1,4]:
        // invalid = -1, signal = -.4, rest = .1, or forage <= 4 - .15.
        // Mapping the average per-step realized return onto [0,1]
        // does NOT expose counterfactual rewards or hidden state to the agent.
        public static double NormalizeEpisodeReturn(double realizedReturn, int horizon)
        {
            if (horizon < 1 || double.IsNaN(realizedReturn) || double.IsInfinity(realizedReturn))
                throw new ArgumentOutOfRangeException("realizedReturn");
            double average = realizedReturn / horizon;
            if (average < -1 - 1e-9 || average > 4 + 1e-9)
                throw new ArgumentOutOfRangeException("realizedReturn",
                    "Reward bound violated; do not silently clip bandit feedback.");
            return Math.Max(0, Math.Min(1, (average + 1) / 5));
        }

        public sealed class Output
        {
            public string EpisodesCsv;
            public string FinalPolicyCsv;
            public string ManifestJson;
            public int Episodes;
        }

        public static Output Run(int firstSeed = 631, int episodes = 300,
            double exploration = .12, int selectorSeed = 7341)
        {
            if (episodes < 1 || episodes > 100000)
                throw new ArgumentOutOfRangeException("episodes");
            if (firstSeed > int.MaxValue - episodes)
                throw new ArgumentOutOfRangeException("firstSeed");

            var selectors = new Exp3EpisodeSelectorV1[SignalingGameV1.Players];
            for (int seat = 0; seat < selectors.Length; seat++)
                selectors[seat] = new Exp3EpisodeSelectorV1(StrategyNames.Length,
                    unchecked(selectorSeed + seat * 104729), exploration);

            var log = new StringBuilder(
                "schema,episode,environment_seed,seat,strategy,realized_return,feedback_01,collective_return,signals,harvest_actions\n");
            for (int episode = 0; episode < episodes; episode++)
            {
                var strategies = new SignalingGameV1.IPolicy[SignalingGameV1.Players];
                var indices = new int[SignalingGameV1.Players];
                for (int seat = 0; seat < strategies.Length; seat++)
                {
                    indices[seat] = selectors[seat].Choose();
                    strategies[seat] =
                        SignalingExperimentRunnerV1.NewPolicy(StrategyNames[indices[seat]]);
                }

                int environmentSeed = firstSeed + episode;
                SignalingGameV1.RunSummary outcome =
                    SignalingGameV1.Evaluate(environmentSeed, strategies);
                double collective = 0;
                foreach (double value in outcome.Returns) collective += value;

                for (int seat = 0; seat < strategies.Length; seat++)
                {
                    double feedback = NormalizeEpisodeReturn(
                        outcome.Returns[seat], outcome.Steps);
                    selectors[seat].Observe(feedback);
                    log.Append(Schema).Append(',').Append(episode).Append(',')
                        .Append(environmentSeed).Append(',').Append(seat).Append(',')
                        .Append(StrategyNames[indices[seat]]).Append(',')
                        .Append(F(outcome.Returns[seat])).Append(',')
                        .Append(F(feedback)).Append(',')
                        .Append(F(collective)).Append(',')
                        .Append(outcome.SignalCount).Append(',')
                        .Append(outcome.HarvestCount).Append('\n');
                }
            }

            var final = new StringBuilder("schema,seat,strategy,selection_probability,updates\n");
            for (int seat = 0; seat < selectors.Length; seat++)
            {
                double[] probabilities = selectors[seat].Probabilities();
                for (int strategy = 0; strategy < probabilities.Length; strategy++)
                {
                    final.Append(Schema).Append(',').Append(seat).Append(',')
                        .Append(StrategyNames[strategy]).Append(',')
                        .Append(F(probabilities[strategy])).Append(',')
                        .Append(selectors[seat].Updates).Append('\n');
                }
            }
            return new Output
            {
                Episodes = episodes,
                EpisodesCsv = log.ToString(),
                FinalPolicyCsv = final.ToString(),
                ManifestJson = "{\n"
                    + "  \"schema\": \"" + Schema + "\",\n"
                    + "  \"environment\": \"cells.signaling-game.v1\",\n"
                    + "  \"physical_chemistry\": false,\n"
                    + "  \"first_environment_seed\": " + firstSeed + ",\n"
                    + "  \"selector_seed\": " + selectorSeed + ",\n"
                    + "  \"episodes\": " + episodes + ",\n"
                    + "  \"episode_horizon\": " + SignalingGameV1.DefaultHorizon + ",\n"
                    + "  \"exploration\": " + F(exploration) + ",\n"
                    + "  \"feedback\": \"normalized individual realized episode return, [-1,4] per decision mapped to [0,1]\",\n"
                    + "  \"strategies\": [\"silent\", \"follower\", \"signaler\", \"random\"],\n"
                    + "  \"learning_mode\": \"independent EXP3 with simultaneously adapting opponents\",\n"
                    + "  \"source_revision\": \"unverified\",\n"
                    + "  \"note\": \"Training outcomes only; no held-out policy evaluation, convergence or equilibrium claim.\"\n"
                    + "}\n"
            };
        }

        private static string F(double value) =>
            value.ToString("R", CultureInfo.InvariantCulture);
    }
}
