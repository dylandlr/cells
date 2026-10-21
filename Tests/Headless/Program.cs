using System;
using System.Collections.Generic;
using System.IO;
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

    private static void Main(string[] args)
    {
        if(args.Length>0 && args[0]=="--commons-delayed")
        {
            if(args.Length!=2)throw new ArgumentException("Usage: --commons-delayed <output-dir>");
            var result=CommonsDelayedLearningV1.Run();
            Directory.CreateDirectory(args[1]);
            File.WriteAllText(Path.Combine(args[1],"episodes.csv"),result.EpisodesCsv);
            File.WriteAllText(Path.Combine(args[1],"summary.csv"),result.SummaryCsv);
            File.WriteAllText(Path.Combine(args[1],"manifest.json"),result.ManifestJson);
            Console.WriteLine("Commons delayed learner exported.");
            return;
        }
        if(args.Length>0 && args[0]=="--commons-intentions")
        {
            if(args.Length!=2)throw new ArgumentException("Usage: --commons-intentions <output-dir>");
            var report=CommonsIntentionStudyV1.Run();
            Directory.CreateDirectory(args[1]);
            File.WriteAllText(Path.Combine(args[1],"episodes.csv"),report.EpisodesCsv);
            File.WriteAllText(Path.Combine(args[1],"summary.csv"),report.SummaryCsv);
            File.WriteAllText(Path.Combine(args[1],"manifest.json"),report.ManifestJson);
            Console.WriteLine("Commons intention experiment exported.");
            return;
        }
        if(args.Length>0 && args[0]=="--commons001")
        {
            if(args.Length!=2)
                throw new ArgumentException("Usage: --commons001 <output-dir>");
            var output=CommonsLearningV1.Run();
            Directory.CreateDirectory(args[1]);
            File.WriteAllText(Path.Combine(args[1],"episodes.csv"),output.EpisodesCsv);
            File.WriteAllText(Path.Combine(args[1],"summary.csv"),output.SummaryCsv);
            File.WriteAllText(Path.Combine(args[1],"manifest.json"),output.ManifestJson);
            Console.WriteLine("Commons study exported.");
            return;
        }
        if(args.Length>0 && args[0]=="--messageuse001")
        {
            if(args.Length!=2)
                throw new ArgumentException("Usage: --messageuse001 <output-dir>");
            var output=MessageUseExperiment001V4.Run();
            Directory.CreateDirectory(args[1]);
            File.WriteAllText(Path.Combine(args[1],"episodes.csv"),output.EpisodesCsv);
            File.WriteAllText(Path.Combine(args[1],"summary.csv"),output.SummaryCsv);
            File.WriteAllText(Path.Combine(args[1],"manifest.json"),output.ManifestJson);
            Console.WriteLine("Message-use E001 exported.");
            return;
        }
        if(args.Length>0 && args[0]=="--heldout001")
        {
            if(args.Length!=2)
                throw new ArgumentException("Usage: --heldout001 <output-dir>");
            var output=AdaptiveHeldOut001V3.Run();
            Directory.CreateDirectory(args[1]);
            File.WriteAllText(Path.Combine(args[1],"episodes.csv"),output.EpisodesCsv);
            File.WriteAllText(Path.Combine(args[1],"paired.csv"),output.PairsCsv);
            File.WriteAllText(Path.Combine(args[1],"summary.csv"),output.SummaryCsv);
            File.WriteAllText(Path.Combine(args[1],"manifest.json"),output.ManifestJson);
            Console.WriteLine("Held-out Experiment 001 exported.");
            return;
        }
        if(args.Length>0 && args[0]=="--adaptive001")
        {
            if(args.Length!=2 && args.Length!=4)
                throw new ArgumentException("Usage: --adaptive001 <output-dir> [first-seed count]");
            int seed=args.Length==4?int.Parse(args[2]):210000;
            int count=args.Length==4?int.Parse(args[3]):30;
            var output=AdaptiveExperiment001V2.Run(seed,count);
            Directory.CreateDirectory(args[1]);
            File.WriteAllText(Path.Combine(args[1],"episodes.csv"),output.EpisodesCsv);
            File.WriteAllText(Path.Combine(args[1],"paired.csv"),output.PairedCsv);
            File.WriteAllText(Path.Combine(args[1],"summary.csv"),output.SummaryCsv);
            File.WriteAllText(Path.Combine(args[1],"manifest.json"),output.ManifestJson);
            Console.WriteLine("Adaptive E001 exported: "+count+" paired seeds to "+args[1]);
            return;
        }
        if(args.Length>0 && args[0]=="--experiment001")
        {
            if(args.Length!=2 && args.Length!=4)
                throw new ArgumentException("Usage: --experiment001 <output-dir> [first-seed count]");
            int seed=args.Length==4?int.Parse(args[2]):100001;
            int count=args.Length==4?int.Parse(args[3]):30;
            var output=Experiment001V1.Run(seed,count);
            Directory.CreateDirectory(args[1]);
            File.WriteAllText(Path.Combine(args[1],"episodes.csv"),output.EpisodesCsv);
            File.WriteAllText(Path.Combine(args[1],"agents.csv"),output.AgentCsv);
            File.WriteAllText(Path.Combine(args[1],"steps.csv"),output.StepsCsv);
            File.WriteAllText(Path.Combine(args[1],"paired.csv"),output.PairedCsv);
            File.WriteAllText(Path.Combine(args[1],"summary.csv"),output.SummaryCsv);
            File.WriteAllText(Path.Combine(args[1],"manifest.json"),output.ManifestJson);
            Console.WriteLine("Experiment 001 pilot exported: "+count+" paired seeds to "+args[1]);
            return;
        }
        Console.WriteLine("cells: headless research verification (Unity-independent)");
        passed += PlanarDragChecks.Run();
        passed += Experiment001ValidityChecks.Run();
        passed += RenewableCommonsChecks.Run();
        var delayed=CommonsDelayedLearningV1.Run(910000,5,1010000,3,25);
        Check(delayed.EpisodesCsv==CommonsDelayedLearningV1.Run(910000,5,1010000,3,25).EpisodesCsv,
            "delayed learner reproducible frozen test");
        Check(delayed.EpisodesCsv.TrimEnd().Split('\n').Length==10,
            "delayed learner 3 seeds x 3 controls");
        Throws<ArgumentException>(()=>CommonsDelayedLearningV1.Run(910000,5,910002,3,25),
            "delayed learner rejects overlapping seeds");
        var intentions=CommonsIntentionStudyV1.Run(810000,3,20);
        Check(intentions.EpisodesCsv==CommonsIntentionStudyV1.Run(810000,3,20).EpisodesCsv,
            "commons intention seeded replay");
        Check(intentions.EpisodesCsv.TrimEnd().Split('\n').Length==13,
            "intention comparison 4 conditions x 3 seeds");
        var commons=CommonsLearningV1.Run(710000,3,20);
        Check(commons.EpisodesCsv==CommonsLearningV1.Run(710000,3,20).EpisodesCsv,
            "renewable commons study reproducible");
        Check(commons.EpisodesCsv.TrimEnd().Split('\n').Length==13,
            "renewable commons four policies across three seeds");
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

        var messageUse=MessageUseExperiment001V4.Run(510000,5,610000,3);
        var messageRepeat=MessageUseExperiment001V4.Run(510000,5,610000,3);
        Check(messageUse.EpisodesCsv==messageRepeat.EpisodesCsv,
            "message use exact deterministic replay");
        Check(messageUse.EpisodesCsv.TrimEnd().Split('\n').Length==10,
            "message use three held-out seeds and three conditions");
        Throws<ArgumentException>(()=>MessageUseExperiment001V4.Run(510000,5,510002,3),
            "message use rejects overlapping seed splits");
        var heldout=AdaptiveHeldOut001V3.Run(310000,5,410000,3);
        var heldoutAgain=AdaptiveHeldOut001V3.Run(310000,5,410000,3);
        Check(heldout.PairsCsv==heldoutAgain.PairsCsv,"heldout exact deterministic replay");
        Check(heldout.EpisodesCsv.TrimEnd().Split('\n').Length==10,
            "heldout 3 seeds x 3 conditions");
        Throws<ArgumentException>(()=>AdaptiveHeldOut001V3.Run(310000,5,310001,3),
            "heldout must reject training seed overlap");
        var adaptive001=AdaptiveExperiment001V2.Run(210000,3);
        var adaptiveRepeat=AdaptiveExperiment001V2.Run(210000,3);
        Check(adaptive001.PairedCsv==adaptiveRepeat.PairedCsv,
            "Adaptive experiment deterministic replay");
        Check(adaptive001.EpisodesCsv.TrimEnd().Split('\n').Length==19,
            "Adaptive experiment three seeds by six conditions");
        Check(adaptive001.PairedCsv.TrimEnd().Split('\n').Length==4,
            "Adaptive experiment three matched seed records");
        var e001 = Experiment001V1.Run(100001, 5);
        var e001Again = Experiment001V1.Run(100001, 5);
        Check(e001.PairedCsv == e001Again.PairedCsv,
            "Experiment 001 deterministic paired seed outcomes");
        Check(e001.EpisodesCsv.TrimEnd().Split('\n').Length == 11,
            "Experiment 001 five seeds times two conditions");
        Check(e001.StepsCsv.TrimEnd().Split('\n').Length == 3201,
            "Experiment 001 five seeds times two conditions times 80 steps times four agents");
        Check(e001.AgentCsv.TrimEnd().Split('\n').Length == 41,
            "Experiment 001 five seeds times two conditions times four agents");
        Check(e001.PairedCsv.TrimEnd().Split('\n').Length == 6,
            "Experiment 001 five paired outcomes");
        var samplePolicies = new SignalingGameV1.IPolicy[] {
            new SignalingGameV1.PeriodicSignaler(),
            new SignalingGameV1.SignalFollower(),
            new SignalingGameV1.SignalFollower(),
            new SignalingGameV1.SignalFollower()
        };
        var controlOn = SignalingGameV1.Evaluate(100001, samplePolicies,
            SignalingGameV1.DefaultHorizon, true);
        var controlOff = SignalingGameV1.Evaluate(100001, samplePolicies,
            SignalingGameV1.DefaultHorizon, false);
        Check(controlOn.InvalidActions == 0 && controlOff.InvalidActions == 0,
            "Experiment 001 all controller actions valid");

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
