#if UNITY_EDITOR
using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Cells.Experiments;

namespace Cells.EditorTools
{
    /// <summary>Seed-matched physical intervention. Runs synchronously in the Unity Editor.</summary>
    public static class PhysicalSignalAssayExport
    {
        [MenuItem("Cells/Research/Run Physical Signal Assay (30 Paired Seeds)")]
        public static void RunThirtyPairs()
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string folder = Path.Combine(root, "ExperimentRuns", "physical-signal-v1");
            Directory.CreateDirectory(folder);
            var rows = new StringBuilder(
                "schema,seed,signaling,agent,net_energy,consumed_mass,detected_steps,collective_harvest,emitted_mass,emission_energy,remaining_signal,decayed_signal\n");
            for (int k = 0; k < 30; k++)
            {
                int seed = 631 + k;
                for (int condition = 0; condition < 2; condition++)
                {
                    var r = PhysicalSignalAssayV1.Run(seed, condition == 1);
                    for (int i = 0; i < PhysicalSignalAssayV1.Agents; i++)
                    {
                        rows.Append("cells.physical-signaling.v1,").Append(seed).Append(',')
                            .Append(condition).Append(',').Append(i).Append(',')
                            .Append(N(r.NetEnergy[i])).Append(',')
                            .Append(N(r.ConsumedMass[i])).Append(',')
                            .Append(r.Receptions[i]).Append(',')
                            .Append(N(r.TotalHarvestMass)).Append(',')
                            .Append(N(r.SignalMassEmitted)).Append(',')
                            .Append(N(r.SignalEnergySpent)).Append(',')
                            .Append(N(r.SignalMassRemaining)).Append(',')
                            .Append(N(r.SignalMassDecayed)).Append('\n');
                    }
                }
            }
            File.WriteAllText(Path.Combine(folder, "agents.csv"), rows.ToString());
            File.WriteAllText(Path.Combine(folder, "manifest.json"),
                "{\n  \"schema\": \"cells.physical-signaling.v1\",\n" +
                "  \"seeds\": 30,\n  \"first_seed\": 631,\n  \"arms\": [\"silent\", \"signaling\"],\n" +
                "  \"agents_per_episode\": 4,\n  \"decisions\": 8,\n  \"transport_steps\": 80,\n" +
                "  \"source_revision\": \"unverified\",\n" +
                "  \"fluid_enabled\": false,\n" +
                "  \"notes\": \"Spatial diffusion and metabolism assay; simplified discrete movement, uncalibrated biology, no learning or strategic equilibrium claims.\"\n}\n");
            Debug.Log("cells physical signaling: exported 60 episodes and 240 agent rows to " + folder);
            EditorUtility.RevealInFinder(folder);
        }

        private static string N(float value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }
    }
}
#endif
