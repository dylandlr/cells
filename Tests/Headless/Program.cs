using System;
using System.Collections.Generic;
using Cells.Experiments;

internal static class Program
{
    private static int passed;

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception("FAILED: " + message);
        passed++;
    }

    private static void Almost(double actual, double expected, double tolerance, string message)
    {
        Check(Math.Abs(actual - expected) <= tolerance,
            message + " (actual=" + actual + ", expected=" + expected + ")");
    }

    private static void Throws<T>(Action action, string message) where T : Exception
    {
        try { action(); }
        catch (T) { passed++; return; }
        throw new Exception("FAILED (expected " + typeof(T).Name + "): " + message);
    }

    private static void Main()
    {
        Console.WriteLine("cells: headless research verification (Unity-independent)");
        var randoms = new SignalingGameV1.IPolicy[] {
            new SignalingGameV1.RandomPolicy(), new SignalingGameV1.RandomPolicy(),
            new SignalingGameV1.RandomPolicy(), new SignalingGameV1.RandomPolicy()
        };
        var first = SignalingGameV1.Evaluate(631, randoms);
        var second = SignalingGameV1.Evaluate(631, randoms);
        Check(first.Steps == 80, "full episode horizon");
        for (int i = 0; i < 4; i++)
            Almost(first.Returns[i], second.Returns[i], 0, "seed reproducibility");
        Check(first.InvalidActions == 0, "valid built-in actions");

        var experiment = SignalingExperimentRunnerV1.Run(2);
        Check(experiment.Episodes == 20, "ten profile variants per seed");
        Check(experiment.AgentCsv.TrimEnd().Split('\n').Length == 81,
            "agent-record cardinality");

        var learner = new Exp3EpisodeSelectorV1(4, 101, .2);
        for (int i = 0; i < 4; ++i)
            Almost(learner.Probabilities()[i], .25, 1e-12, "uniform initialization");
        int selected = learner.Choose();
        Throws<InvalidOperationException>(() => learner.Choose(), "one choice per episode");
        Throws<ArgumentOutOfRangeException>(() => learner.Observe(double.NaN),
            "nonfinite rewards rejected");
        learner.Observe(1);
        Check(learner.Probabilities()[selected] > .25, "positive feedback grows chosen arm");

        var training = Exp3StrategyTrainingV1.Run(631, 12, .12, 7341);
        var trainingAgain = Exp3StrategyTrainingV1.Run(631, 12, .12, 7341);
        Check(training.EpisodesCsv == trainingAgain.EpisodesCsv,
            "deterministic EXP3 training");
        Check(training.FrozenSeatProbabilities.Length == 4, "frozen four-seat snapshot");
        Check(training.FirstEnvironmentSeed == 631, "training seed provenance");

        var holdout = Exp3HeldOutEvaluationV1.Evaluate(training, 100000, 5);
        var holdoutAgain = Exp3HeldOutEvaluationV1.Evaluate(training, 100000, 5);
        Check(holdout.EpisodesCsv == holdoutAgain.EpisodesCsv,
            "reproducible held-out policies");
        Check(holdout.PairedBySeedCsv.TrimEnd().Split('\n').Length == 6,
            "five paired held-out seeds");
        Throws<ArgumentException>(() =>
            Exp3HeldOutEvaluationV1.Evaluate(training, 640, 5),
            "reject overlap between training and evaluation");

        var pairs = new List<PairedAssayAnalysisV1.Record> {
            new PairedAssayAnalysisV1.Record(5, 1, 3),
            new PairedAssayAnalysisV1.Record(6, 2, 4),
            new PairedAssayAnalysisV1.Record(7, 8, 10)
        };
        var effects = PairedAssayAnalysisV1.EstimatePaired(pairs, 500);
        Almost(effects.MeanDifference, 2, 1e-12, "paired mean");
        Almost(effects.Lower95, 2, 1e-12, "paired bootstrap lower bound");
        Almost(effects.Upper95, 2, 1e-12, "paired bootstrap upper bound");
        Throws<ArgumentException>(() => PairedAssayAnalysisV1.EstimatePaired(
            new[] { pairs[0], pairs[0] }), "duplicate seeds rejected");

        var deviations = RestrictedDeviationStudyV1.Run(210000, 2);
        Check(deviations.Comparisons == 48, "all restricted seat deviations");
        Check(deviations.SamplesCsv.TrimEnd().Split('\n').Length == 97,
            "restricted deviation raw row count");

        for (int profile = 0; profile < EmpiricalPayoffTensorV1.Profiles; profile++)
            Check(EmpiricalPayoffTensorV1.Encode(EmpiricalPayoffTensorV1.Decode(profile))
                == profile, "profile index roundtrip " + profile);

        var tensor = EmpiricalPayoffTensorV1.Build(300000, 2);
        Check(tensor.EpisodeCount == 512, "complete empirical tensor");
        Check(tensor.Csv().TrimEnd().Split('\n').Length == 257,
            "one row for each joint profile");
        var pure = EmpiricalPayoffTensorV1.Homogeneous(2);
        var expected = tensor.ExpectedPayoffs(pure);
        int pureId = EmpiricalPayoffTensorV1.Encode(new[] { 2, 2, 2, 2 });
        for (int i = 0; i < 4; i++)
            Almost(expected[i], tensor.Payoff(pureId, i), 1e-9,
                "pure payoff matches tensor lookup");
        var gain = tensor.RestrictedDeviationGains(training.FrozenSeatProbabilities);
        for (int i = 0; i < 4; i++)
            Check(gain[i] >= 0 && !double.IsNaN(gain[i]),
                "nonnegative finite restricted deviation gain");

        Console.WriteLine("PASS: " + passed + " headless assertions.");
        Console.WriteLine("This does not verify Unity, physical chemistry, or scientific hypotheses.");
    }
}
