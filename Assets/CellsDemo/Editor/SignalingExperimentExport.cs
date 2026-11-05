#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Cells.Experiments;

namespace Cells.EditorTools
{
    /// <summary>Runs pure C# simulations without entering Play Mode or loading a scene.</summary>
    public static class SignalingExperimentExport
    {
        [MenuItem("Cells/Research/Run Signaling Experiment (30 Seeds)")]
        public static void RunThirtySeeds()
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string folder = Path.Combine(projectRoot, "ExperimentRuns", "signaling-v1");
            Directory.CreateDirectory(folder);
            // Using an explicit provenance marker avoids falsely claiming a Git SHA.
            var output = SignalingExperimentRunnerV1.Run(
                SignalingExperimentRunnerV1.DefaultReplicates, 631, "unverified");
            File.WriteAllText(Path.Combine(folder, "agents.csv"), output.AgentCsv);
            File.WriteAllText(Path.Combine(folder, "episodes.csv"), output.EpisodeCsv);
            File.WriteAllText(Path.Combine(folder, "manifest.json"), output.ManifestJson);
            Debug.Log("cells: exported " + output.Episodes + " episodes to " + folder
                + ". Source revision in manifest is UNVERIFIED; record Git HEAD separately.");
            EditorUtility.RevealInFinder(folder);
        }
    }
}
#endif
