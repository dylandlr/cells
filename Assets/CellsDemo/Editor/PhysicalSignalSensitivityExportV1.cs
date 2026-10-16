#if UNITY_EDITOR
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Cells.Experiments;

namespace Cells.EditorTools
{
    /// <summary>
    /// Prespecified transport/sensor sensitivity grid. Diagnostic, not hyperparameter
    /// optimization: do NOT select the best setting and evaluate it on these same seeds.
    /// </summary>
    public static class PhysicalSignalSensitivityExportV1
    {
        [MenuItem("Cells/Research/Probe Signal Detection Sensitivity (10 Seeds)")]
        public static void Run()
        {
            float[] thresholds = { .0001f, .001f, .01f };
            float[] diffusion = { .06f, .18f, .36f };
            const int firstSeed = 7100;
            const int repetitions = 10;
            var csv = new StringBuilder(
                "schema,seed,threshold,diffusivity,production,response,seat,first_detection_step,detected_steps,peak_local_concentration,final_source_distance,consumed_nutrient,net_energy,emitted_signal,remaining_signal,decayed_signal,initial_nutrient,remaining_nutrient\n");

            foreach (float d in diffusion)
            foreach (float threshold in thresholds)
            for (int n = 0; n < repetitions; n++)
            {
                int seed = firstSeed + n;
                // Two matched arms expose whether sensitivity changes receiver behavior.
                foreach (bool response in new[] { false, true })
                {
                    var result = PhysicalSignalAssayV1.RunCalibrated(
                        seed, true, response, threshold, d, .13f);
                    for (int seat = 0; seat < PhysicalSignalAssayV1.Agents; seat++)
                    {
                        csv.Append("cells.signal-sensitivity.v1,").Append(seed).Append(',')
                            .Append(F(threshold)).Append(',').Append(F(d)).Append(",1,")
                            .Append(response ? "1," : "0,").Append(seat).Append(',')
                            .Append(result.FirstDetectionStep[seat]).Append(',')
                            .Append(result.Receptions[seat]).Append(',')
                            .Append(F(result.PeakReceiverConcentration[seat])).Append(',')
                            .Append(F(result.FinalSourceDistance[seat])).Append(',')
                            .Append(F(result.ConsumedMass[seat])).Append(',')
                            .Append(F(result.NetEnergy[seat])).Append(',')
                            .Append(F(result.SignalMassEmitted)).Append(',')
                            .Append(F(result.SignalMassRemaining)).Append(',')
                            .Append(F(result.SignalMassDecayed)).Append(',')
                            .Append(F(result.NutrientMassInitial)).Append(',')
                            .Append(F(result.NutrientMassRemaining)).Append('\n');
                    }
                }
            }

            var folder = Path.GetFullPath(Path.Combine(
                Application.dataPath, "..", "ExperimentRuns", "signal-sensitivity-v1"));
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "observations.csv"), csv.ToString());
            File.WriteAllText(Path.Combine(folder, "manifest.json"),
                "{\n"
                + "  \"schema\": \"cells.signal-sensitivity.v1\",\n"
                + "  \"first_seed\": 7100,\n"
                + "  \"seed_count\": 10,\n"
                + "  \"receptor_thresholds\": [0.0001, 0.001, 0.01],\n"
                + "  \"diffusion_coefficients\": [0.06, 0.18, 0.36],\n"
                + "  \"signal_decay_coefficient\": 0.13,\n"
                + "  \"arms\": \"production enabled, behavioral response {off,on}\",\n"
                + "  \"episodes\": 180,\n"
                + "  \"source_revision\": \"unverified\",\n"
                + "  \"analysis_status\": \"diagnostic only; no selection on evaluation seeds\",\n"
                + "  \"fluid_advection\": false\n"
                + "}\n");
            Debug.Log("cells: detection sensitivity CSV ready (180 planned episodes): " + folder);
            EditorUtility.RevealInFinder(folder);
        }

        static string F(float value) { return value.ToString("R", CultureInfo.InvariantCulture); }
    }
}
#endif
