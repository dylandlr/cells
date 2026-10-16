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
            Debug.Log("cells: EXP3 training export complete: " + result.Episodes +
                " training episodes. Training outcomes are not held-out results. " + directory);
            EditorUtility.RevealInFinder(directory);
        }
    }
}
#endif
