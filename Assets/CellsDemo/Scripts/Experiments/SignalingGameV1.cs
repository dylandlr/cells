using System;
using System.Collections.Generic;

namespace Cells.Experiments
{
    /// <summary>
    /// Headless, seed-reproducible four-player stochastic resource game.
    /// This is an experimental abstraction, NOT the physical chemical solver.
    /// One step represents one strategic decision; cells do not see hidden patch yields.
    /// </summary>
    public sealed class SignalingGameV1
    {
        public const int Players = 4;
        public const int Patches = 3;
        public const int DefaultHorizon = 80;
        public const int ActionCount = 7; // forage 0..2, signal 0..2, rest 6

        public struct Observation
        {
            public int Agent;
            public int Step;
            public int OwnPatch;
            public double Energy;
            public int[] LocalCue;       // {-1,0,1}; -1 means out of sensing range
            public int[] ReceivedSignals;// number of signals for each patch last step
        }

        public struct Action
        {
            public int Code;
            public Action(int code) { Code = code; }
        }

        public struct RunSummary
        {
            public double[] Returns;
            public int[] FinalPatches;
            public int SignalCount;
            public int HarvestCount;
            public int InvalidActions;
            public int Steps;
            public double TotalHarvest;
        }

        public interface IPolicy
        {
            Action Decide(Observation observation, Random policyRandom);
        }

        /// <summary>Never broadcasts. Greedily forages the highest locally sensed patch.</summary>
        public sealed class SilentGreedy : IPolicy
        {
            public Action Decide(Observation o, Random rng)
            {
                int best = o.OwnPatch;
                int bestCue = o.LocalCue[best];
                for (int p = 0; p < Patches; ++p)
                    if (o.LocalCue[p] > bestCue) { best = p; bestCue = o.LocalCue[p]; }
                return new Action(best);
            }
        }

        /// <summary>Follows publicly available signals when they outweigh local cues.</summary>
        public sealed class SignalFollower : IPolicy
        {
            public Action Decide(Observation o, Random rng)
            {
                int best = o.OwnPatch;
                double score = double.NegativeInfinity;
                for (int p = 0; p < Patches; ++p)
                {
                    double candidate = (o.LocalCue[p] < 0 ? -0.5 : o.LocalCue[p])
                                     + 0.75 * o.ReceivedSignals[p];
                    if (candidate > score) { score = candidate; best = p; }
                }
                return new Action(best);
            }
        }

        /// <summary>Signals a visible rich patch every N-th decision; otherwise harvests.</summary>
        public sealed class PeriodicSignaler : IPolicy
        {
            private readonly int period;
            public PeriodicSignaler(int period = 4) { this.period = Math.Max(2, period); }
            public Action Decide(Observation o, Random rng)
            {
                int best = o.OwnPatch;
                for (int p = 0; p < Patches; ++p)
                    if (o.LocalCue[p] > o.LocalCue[best]) best = p;
                return new Action(o.Step % period == 0 && o.LocalCue[best] > 0 ? 3 + best : best);
            }
        }

        public sealed class RandomPolicy : IPolicy
        {
            public Action Decide(Observation o, Random rng) { return new Action(rng.Next(ActionCount)); }
        }

        private readonly int horizon;
        private readonly Random environmentRandom;
        private readonly int[] hiddenYield = new int[Patches];
        private readonly int[] locations = new int[Players];
        private readonly double[] energy = new double[Players];
        private readonly double[] returns = new double[Players];
        private readonly int[] signals = new int[Patches];
        private readonly int[] nextSignals = new int[Patches];
        private int step;
        private int signalCount;
        private int harvestCount;
        private int invalidActions;
        private double totalHarvest;

        public int StepCount { get { return step; } }
        public bool IsFinished { get { return step >= horizon; } }
        public const double SignalCost = 0.4;
        public const double ForageCost = 0.15;
        public const double MovementCost = 0.2;
        public const double RestGain = 0.1;

        public SignalingGameV1(int seed, int decisionHorizon = DefaultHorizon)
        {
            if (decisionHorizon < 1) throw new ArgumentOutOfRangeException("decisionHorizon");
            horizon = decisionHorizon;
            environmentRandom = new Random(seed);
            for (int p = 0; p < Patches; ++p) hiddenYield[p] = 1 + environmentRandom.Next(4);
            for (int i = 0; i < Players; ++i) { locations[i] = i % Patches; energy[i] = 5.0; }
        }

        public Observation Observe(int agent)
        {
            if (agent < 0 || agent >= Players) throw new ArgumentOutOfRangeException("agent");
            var cue = new int[Patches];
            for (int p = 0; p < Patches; ++p)
            {
                // Ring distance; only own/adjacent patches are locally sensed.
                int distance = Math.Abs(p - locations[agent]);
                cue[p] = distance == 0 ? (hiddenYield[p] >= 3 ? 1 : 0) : -1;
            }
            return new Observation
            {
                Agent = agent, Step = step, OwnPatch = locations[agent],
                Energy = energy[agent], LocalCue = cue, ReceivedSignals = (int[])signals.Clone()
            };
        }

        /// <summary>
        /// Applies all actions simultaneously. Shared harvest is divided among same-patch
        /// foragers, making competition explicit. Signaling transmits a patch index,
        /// not a verified statement about patch quality.
        /// </summary>
        public void Advance(Action[] actions)
        {
            if (IsFinished) throw new InvalidOperationException("Episode finished.");
            if (actions == null || actions.Length != Players) throw new ArgumentException("Four actions required.");
            int[] harvesters = new int[Patches];
            for (int i = 0; i < Players; ++i)
                if (actions[i].Code >= 0 && actions[i].Code < Patches)
                    harvesters[actions[i].Code]++;

            Array.Clear(nextSignals, 0, nextSignals.Length);
            for (int i = 0; i < Players; ++i)
            {
                int action = actions[i].Code;
                double reward;
                if (action >= 0 && action < Patches)
                {
                    double moved = action == locations[i] ? 0 : MovementCost;
                    locations[i] = action;
                    reward = (double)hiddenYield[action] / harvesters[action] - ForageCost - moved;
                    harvestCount++;
                    totalHarvest += (double)hiddenYield[action] / harvesters[action];
                }
                else if (action >= Patches && action < Patches * 2)
                {
                    nextSignals[action - Patches]++;
                    reward = -SignalCost;
                    signalCount++;
                }
                else if (action == 6)
                {
                    reward = RestGain;
                }
                else
                {
                    reward = -1.0;
                    invalidActions++;
                }
                energy[i] += reward;
                returns[i] += reward;
            }

            Array.Copy(nextSignals, signals, Patches);
            // Drift is shared exogenous noise, independent of submitted policies.
            // This gives all policy comparisons with the same environment seed matched noise.
            for (int p = 0; p < Patches; ++p)
            {
                if (environmentRandom.NextDouble() < 0.08)
                    hiddenYield[p] = 1 + environmentRandom.Next(4);
            }
            step++;
        }

        public RunSummary Summary()
        {
            return new RunSummary
            {
                Returns = (double[])returns.Clone(),
                FinalPatches = (int[])locations.Clone(),
                Steps = step,
                SignalCount = signalCount,
                HarvestCount = harvestCount,
                InvalidActions = invalidActions,
                TotalHarvest = totalHarvest
            };
        }

        public static RunSummary Evaluate(int seed, IPolicy[] policies, int horizon = DefaultHorizon)
        {
            if (policies == null || policies.Length != Players)
                throw new ArgumentException("Exactly four policies required.");
            var game = new SignalingGameV1(seed, horizon);
            var policyRandom = new Random[Players];
            for (int i = 0; i < Players; ++i)
            {
                if (policies[i] == null) throw new ArgumentException("Null policy.");
                policyRandom[i] = new Random(unchecked(seed * 7919 + i * 104729));
            }
            while (!game.IsFinished)
            {
                var actions = new Action[Players];
                for (int i = 0; i < Players; ++i)
                    actions[i] = policies[i].Decide(game.Observe(i), policyRandom[i]);
                game.Advance(actions);
            }
            return game.Summary();
        }
    }
}
