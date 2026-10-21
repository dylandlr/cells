#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using Cells.Experiments;

namespace Cells.EditorTools
{
    /// <summary>Full four-strategy empirical tensor. No Play Mode required.</summary>
    public static class EmpiricalPayoffTensorExportV1
    {
        [MenuItem("Cells/Research/Evaluate Full Strategy Payoff Tensor (30 Seeds)")]
        public static void Run()
        {
            string directory = Path.GetFullPath(Path.Combine(
                Application.dataPath, "..", "ExperimentRuns", "payoff-tensor-v1"));
            Directory.CreateDirectory(directory);
            var tensor = EmpiricalPayoffTensorV1.Build(300000, 30);
            File.WriteAllText(Path.Combine(directory, "tensor.csv"), tensor.Csv());

            var training = Exp3StrategyTrainingV1.Run();
            File.WriteAllText(Path.Combine(directory, "learned_mixture.csv"),
                training.FinalPolicyCsv);
            File.WriteAllText(Path.Combine(directory, "learned_deviation.csv"),
                tensor.DeviationCsv(training.FrozenSeatProbabilities, "exp3_frozen"));
            File.WriteAllText(Path.Combine(directory, "uniform_deviation.csv"),
                tensor.DeviationCsv(EmpiricalPayoffTensorV1.Uniform(), "uniform"));
            File.WriteAllText(Path.Combine(directory, "silent_deviation.csv"),
                tensor.DeviationCsv(EmpiricalPayoffTensorV1.Homogeneous(0), "silent"));
            File.WriteAllText(Path.Combine(directory, "manifest.json"),
                "{\n  \"schema\": \"cells.payoff-tensor.v1\",\n"
                + "  \"environment\": \"cells.signaling-game.v1\",\n"
                + "  \"seed_first\": 300000,\n  \"seeds\": 30,\n"
                + "  \"profiles\": 256,\n  \"strategy_count\": 4,\n"
                + "  \"episodes\": " + tensor.EpisodeCount + ",\n"
                + "  \"training_seeds\": \"631..930\",\n"
                + "  \"evaluation_seeds\": \"300000..300029\",\n"
                + "  \"source_revision\": \"unverified\",\n"
                + "  \"warning\": \"Table means only, no propagated payoff uncertainty. Restricted independent-mixture pure deviation gains are not unrestricted Nash regret.\"\n}\n");
            Debug.Log("cells: full payoff tensor exported: " + tensor.EpisodeCount
                + " episodes. This is abstract-game restricted analysis, not Unity physiology.");
            EditorUtility.RevealInFinder(directory);
        }
    }
}
#endif
