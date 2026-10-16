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
            var collective = new System.Collections.Generic.List<PairedAssayAnalysisV1.Record>();
            var receivers = new System.Collections.Generic.List<PairedAssayAnalysisV1.Record>();
            var source = new System.Collections.Generic.List<PairedAssayAnalysisV1.Record>();
            var harvest = new System.Collections.Generic.List<PairedAssayAnalysisV1.Record>();
            var detected = new System.Collections.Generic.List<PairedAssayAnalysisV1.Record>();
            var pairs = new StringBuilder("seed,control_collective,treatment_collective,delta_collective,control_receivers,treatment_receivers,delta_receivers,control_harvest,treatment_harvest,delta_harvest,treatment_detected_steps\n");
            for (int k = 0; k < 30; k++)
            {
                int seed = 631 + k;
                var arms = new PhysicalSignalAssayV1.Result[2];
                for (int condition = 0; condition < 2; condition++)
                {
                    var r = PhysicalSignalAssayV1.Run(seed, condition == 1);
                    arms[condition] = r;
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
                var off = arms[0];
                var on = arms[1];
                double offAll = 0, onAll = 0, offReceivers = 0, onReceivers = 0;
                int detections = 0;
                for (int i = 0; i < PhysicalSignalAssayV1.Agents; i++)
                {
                    offAll += off.NetEnergy[i];
                    onAll += on.NetEnergy[i];
                    if (i > 0)
                    {
                        offReceivers += off.NetEnergy[i];
                        onReceivers += on.NetEnergy[i];
                        detections += on.Receptions[i];
                    }
                }
                collective.Add(new PairedAssayAnalysisV1.Record(seed, offAll, onAll));
                receivers.Add(new PairedAssayAnalysisV1.Record(seed, offReceivers, onReceivers));
                source.Add(new PairedAssayAnalysisV1.Record(seed, off.NetEnergy[0], on.NetEnergy[0]));
                harvest.Add(new PairedAssayAnalysisV1.Record(seed, off.TotalHarvestMass, on.TotalHarvestMass));
                detected.Add(new PairedAssayAnalysisV1.Record(seed, 0, detections));
                pairs.Append(seed).Append(',').Append(N(offAll)).Append(',')
                    .Append(N(onAll)).Append(',').Append(N(onAll - offAll)).Append(',')
                    .Append(N(offReceivers)).Append(',').Append(N(onReceivers)).Append(',')
                    .Append(N(onReceivers - offReceivers)).Append(',')
                    .Append(N(off.TotalHarvestMass)).Append(',').Append(N(on.TotalHarvestMass)).Append(',')
                    .Append(N(on.TotalHarvestMass - off.TotalHarvestMass)).Append(',')
                    .Append(detections).Append('\n');
            }
            var summary = new StringBuilder(PairedAssayAnalysisV1.Header);
            summary.Append(PairedAssayAnalysisV1.ToCsv("collective_energy", PairedAssayAnalysisV1.EstimatePaired(collective)));
            summary.Append(PairedAssayAnalysisV1.ToCsv("receivers_energy", PairedAssayAnalysisV1.EstimatePaired(receivers)));
            summary.Append(PairedAssayAnalysisV1.ToCsv("source_energy", PairedAssayAnalysisV1.EstimatePaired(source)));
            summary.Append(PairedAssayAnalysisV1.ToCsv("harvest_mass", PairedAssayAnalysisV1.EstimatePaired(harvest)));
            summary.Append(PairedAssayAnalysisV1.ToCsv("receiver_detected_steps", PairedAssayAnalysisV1.EstimatePaired(detected)));
            int seedsWithDetection = 0;
            foreach (var record in detected)
                if (record.Treatment > 0) seedsWithDetection++;
            if (seedsWithDetection == 0)
                Debug.LogWarning("cells: no receiver detected signal on any paired seed. " +
                    "Do NOT interpret utility differences as communication effects. " +
                    "Inspect signal diffusion, threshold, source timing and spatial geometry.");
            else
                Debug.Log("cells physical signaling: receiver detection occurred in " +
                    seedsWithDetection + " of 30 paired seeds.");
            File.WriteAllText(Path.Combine(folder, "paired_by_seed.csv"), pairs.ToString());
            File.WriteAllText(Path.Combine(folder, "paired_summary.csv"), summary.ToString());
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
