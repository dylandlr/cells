using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Cells.Experiments
{
    /// <summary>
    /// Reproducible experiment table generation. No Unity APIs, file I/O, or mutable
    /// global state, so the runner can be exercised in Edit Mode or headlessly.
    /// Outcomes are descriptive; no equilibrium or cooperation claims are inferred.
    /// </summary>
    public static class SignalingExperimentRunnerV1
    {
        public const string SchemaVersion = "cells.signaling-experiment.v1";
        public const int DefaultReplicates = 30;

        public sealed class Output
        {
            public string AgentCsv;
            public string EpisodeCsv;
            public string ManifestJson;
            public int Episodes;
        }

        private struct Profile
        {
            public string Name;
            public string[] Seats;
            public Profile(string name, string[] seats) { Name = name; Seats = seats; }
        }

        private static readonly string[] Silent = { "silent", "silent", "silent", "silent" };
        private static readonly string[] Random = { "random", "random", "random", "random" };
        private static readonly string[] Mixed = { "signaler", "follower", "follower", "follower" };
        private static readonly string[] MixedControl = { "silent", "follower", "follower", "follower" };

        /// <summary>
        /// Predeclared policy profiles. Each mixed assignment is rotated through all
        /// agent seats to avoid the fixed starting-location confound.
        /// Comparison is paired by seed AND rotation; profiles are not randomized.
        /// </summary>
        private static IEnumerable<Profile> Profiles()
        {
            yield return new Profile("all_silent", Silent);
            yield return new Profile("all_random", Random);
            for (int rotation = 0; rotation < SignalingGameV1.Players; rotation++)
            {
                yield return new Profile("mixed_no_signal_r" + rotation, Rotate(MixedControl, rotation));
                yield return new Profile("mixed_signaling_r" + rotation, Rotate(Mixed, rotation));
            }
        }

        private static string[] Rotate(string[] roles, int rotation)
        {
            var output = new string[SignalingGameV1.Players];
            for (int i = 0; i < output.Length; i++)
                output[(i + rotation) % output.Length] = roles[i];
            return output;
        }

        public static SignalingGameV1.IPolicy NewPolicy(string name)
        {
            switch (name)
            {
                case "silent": return new SignalingGameV1.SilentGreedy();
                case "random": return new SignalingGameV1.RandomPolicy();
                case "signaler": return new SignalingGameV1.PeriodicSignaler();
                case "follower": return new SignalingGameV1.SignalFollower();
                default: throw new ArgumentException("Unknown frozen policy: " + name);
            }
        }

        /// <param name="replicates">Number of paired environment seeds.</param>
        /// <param name="firstSeed">First environment seed, incremented by one per replicate.</param>
        /// <param name="sourceRevision">Git revision supplied by caller; use "unverified" if unknown.</param>
        public static Output Run(int replicates = DefaultReplicates,
            int firstSeed = 631, string sourceRevision = "unverified")
        {
            if (replicates < 1 || replicates > 100000)
                throw new ArgumentOutOfRangeException("replicates");
            if (firstSeed > int.MaxValue - replicates)
                throw new ArgumentOutOfRangeException("firstSeed");
            if (sourceRevision == null) sourceRevision = "unverified";

            var agents = new StringBuilder("schema,profile,seed,seat,policy,return,final_patch\n");
            var episodes = new StringBuilder(
                "schema,profile,seed,collective_return,total_harvest,signals,harvest_actions,invalid_actions,steps\n");
            int episodeCount = 0;

            for (int r = 0; r < replicates; ++r)
            {
                int seed = firstSeed + r;
                foreach (Profile profile in Profiles())
                {
                    var policies = new SignalingGameV1.IPolicy[SignalingGameV1.Players];
                    for (int i = 0; i < policies.Length; ++i)
                        policies[i] = NewPolicy(profile.Seats[i]);

                    SignalingGameV1.RunSummary result = SignalingGameV1.Evaluate(seed, policies);
                    double collective = 0;
                    for (int seat = 0; seat < SignalingGameV1.Players; seat++)
                    {
                        collective += result.Returns[seat];
                        agents.Append(SchemaVersion).Append(',').Append(profile.Name).Append(',')
                            .Append(seed).Append(',').Append(seat).Append(',')
                            .Append(profile.Seats[seat]).Append(',')
                            .Append(Format(result.Returns[seat])).Append(',')
                            .Append(result.FinalPatches[seat]).Append('\n');
                    }

                    episodes.Append(SchemaVersion).Append(',').Append(profile.Name).Append(',')
                        .Append(seed).Append(',').Append(Format(collective)).Append(',')
                        .Append(Format(result.TotalHarvest)).Append(',')
                        .Append(result.SignalCount).Append(',')
                        .Append(result.HarvestCount).Append(',')
                        .Append(result.InvalidActions).Append(',')
                        .Append(result.Steps).Append('\n');
                    episodeCount++;
                }
            }

            return new Output
            {
                AgentCsv = agents.ToString(),
                EpisodeCsv = episodes.ToString(),
                Episodes = episodeCount,
                ManifestJson = "{\n"
                    + "  \"schema\": \"" + SchemaVersion + "\",\n"
                    + "  \"experiment\": \"abstract_costly_signaling\",\n"
                    + "  \"physics_coupled\": false,\n"
                    + "  \"source_revision\": \"" + EscapeJson(sourceRevision) + "\",\n"
                    + "  \"first_seed\": " + firstSeed + ",\n"
                    + "  \"replicates\": " + replicates + ",\n"
                    + "  \"horizon\": " + SignalingGameV1.DefaultHorizon + ",\n"
                    + "  \"agents\": " + SignalingGameV1.Players + ",\n"
                    + "  \"episodes\": " + episodeCount + ",\n"
                    + "  \"profiles\": [\"all_silent\", \"all_random\", \"mixed_no_signal_r0..r3\", \"mixed_signaling_r0..r3\"],\n"
                    + "  \"comparison_unit\": \"seed_x_rotation\",\n"
                    + "  \"primary_endpoint\": \"paired_collective_return\",\n"
                    + "  \"limitations\": \"Toy game. Public signals are symbolic and not spatial chemistry; no training, no equilibrium estimates.\"\n"
                    + "}\n"
            };
        }

        private static string Format(double x)
        {
            return x.ToString("R", CultureInfo.InvariantCulture);
        }

        private static string EscapeJson(string value)
        {
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"")
                .Replace("\r", "\\r").Replace("\n", "\\n")
                .Replace("\t", "\\t");
        }
    }
}
