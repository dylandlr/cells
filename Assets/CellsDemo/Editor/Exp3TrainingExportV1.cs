#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using Cells.Experiments;

namespace Cells.EditorTools
{
    public static class Exp3TrainingExportV1
    {
        [MenuItem("Cells/Research/Train EXP3 Strategies (300 Episodes)")]
        public static void Run()
        {
            var directory = Path.GetFullPath(
                Path.Combine(Application.dataPath, "..", "ExperimentRuns", "exp3-strategies-v1"));
            Directory.CreateDirectory(directory);
            var result = Exp3StrategyTrainingV1.Run();
            File.WriteAllText(Path.Combine(directory, "training_episodes.csv"), result.EpisodesCsv);
            File.WriteAllText(Path.Combine(directory, "final_policy.csv"), result.FinalPolicyCsv);
            File.WriteAllText(Path.Combine(directory, "manifest.json"), result.ManifestJson);
            var heldout = Exp3HeldOutEvaluationV1.Evaluate(result, 100000, 100, 29013);
            string testFolder = Path.Combine(directory, "heldout");
            Directory.CreateDirectory(testFolder);
            File.WriteAllText(Path.Combine(testFolder, "episodes.csv"), heldout.EpisodesCsv);
            File.WriteAllText(Path.Combine(testFolder, "agents.csv"), heldout.AgentsCsv);
            File.WriteAllText(Path.Combine(testFolder, "paired_by_seed.csv"), heldout.PairedBySeedCsv);
            File.WriteAllText(Path.Combine(testFolder, "paired_summary.csv"), heldout.SummaryCsv);
            File.WriteAllText(Path.Combine(testFolder, "manifest.json"), heldout.ManifestJson);
            Debug.Log("cells: EXP3 training export complete: " + result.Episodes +
                " training episodes plus " + heldout.HeldOutSeeds +
                " independent held-out seeds. No gameplay/physics claims. " + directory);
            EditorUtility.RevealInFinder(directory);
        }
    }
}
#endif
