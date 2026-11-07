#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using Cells.Experiments;

namespace Cells.EditorTools
{
    public static class RestrictedDeviationExportV1
    {
        [MenuItem("Cells/Research/Evaluate Restricted Strategy Deviations (30 Seeds)")]
        public static void Run()
        {
            string directory = Path.GetFullPath(Path.Combine(
                Application.dataPath, "..", "ExperimentRuns", "restricted-deviations-v1"));
            Directory.CreateDirectory(directory);
            var result = RestrictedDeviationStudyV1.Run();
            File.WriteAllText(Path.Combine(directory, "paired_samples.csv"), result.SamplesCsv);
            File.WriteAllText(Path.Combine(directory, "deviation_estimates.csv"), result.EstimatesCsv);
            File.WriteAllText(Path.Combine(directory, "manifest.json"), result.ManifestJson);
            Debug.Log("cells: evaluated " + result.Comparisons +
                " restricted seat-specific unilateral deviations across " +
                result.Seeds + " paired seeds. No equilibrium claim. " + directory);
            EditorUtility.RevealInFinder(directory);
        }
    }
}
#endif
