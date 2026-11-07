using System;

namespace Cells.Experiments
{
    /// <summary>
    /// EXP3 over a frozen library of episode-long strategies.
    /// Feedback is the selected strategy's realized episode return ONLY.
    /// Requires bounded rewards in [0,1]; the caller is responsible for
    /// a documented, policy-independent normalization.
    /// </summary>
    public sealed class Exp3EpisodeSelectorV1
    {
        readonly double[] logWeights;
        readonly Random random;
        readonly double gamma;
        int pending = -1;
        double selectionProbability;

        public int Count { get { return logWeights.Length; } }
        public int Updates { get; private set; }

        public Exp3EpisodeSelectorV1(int strategyCount, int seed, double exploration = .12)
        {
            if (strategyCount < 2) throw new ArgumentOutOfRangeException("strategyCount");
            if (double.IsNaN(exploration) || double.IsInfinity(exploration)
                || exploration <= 0 || exploration > 1)
                throw new ArgumentOutOfRangeException("exploration");
            logWeights = new double[strategyCount];
            random = new Random(seed);
            gamma = exploration;
        }

        public double[] Probabilities()
        {
            var probs = new double[Count];
            double max = logWeights[0];
            for (int i = 1; i < Count; i++) max = Math.Max(max, logWeights[i]);
            double sum = 0;
            for (int i = 0; i < Count; i++)
            {
                probs[i] = Math.Exp(logWeights[i] - max);
                sum += probs[i];
            }
            for (int i = 0; i < Count; i++)
                probs[i] = (1 - gamma) * (probs[i] / sum) + gamma / Count;
            return probs;
        }

        /// <summary>Choose once per episode. Repeated choice before feedback is forbidden.</summary>
        public int Choose()
        {
            if (pending >= 0) throw new InvalidOperationException("Previous episode awaits feedback.");
            double[] probabilities = Probabilities();
            double draw = random.NextDouble();
            double cumulative = 0;
            int chosen = Count - 1;
            for (int i = 0; i < Count; i++)
            {
                cumulative += probabilities[i];
                if (draw < cumulative) { chosen = i; break; }
            }
            pending = chosen;
            selectionProbability = probabilities[chosen];
            return chosen;
        }

        /// <summary>
        /// Importance-weighted EXP3 update using only the selected arm's reward.
        /// The sampling probability is captured at selection time, never recomputed
        /// after other agents have changed weights.
        /// </summary>
        public void Observe(double reward01)
        {
            if (pending < 0) throw new InvalidOperationException("Choose a strategy before feedback.");
            if (double.IsNaN(reward01) || double.IsInfinity(reward01)
                || reward01 < 0 || reward01 > 1)
                throw new ArgumentOutOfRangeException("reward01", "EXP3 requires [0,1] rewards.");
            logWeights[pending] += gamma * (reward01 / selectionProbability) / Count;
            pending = -1;
            Updates++;
        }

        public bool AwaitingFeedback { get { return pending >= 0; } }
    }
}
