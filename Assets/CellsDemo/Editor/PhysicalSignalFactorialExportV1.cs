#if UNITY_EDITOR
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Cells.Experiments;

namespace Cells.EditorTools
{
    /// <summary>
    /// 2x2 production x behavioral response intervention with matched seeds.
    /// A physical detection is counted even when the behavior switch is off,
    /// so detection can be distinguished from actually acting on information.
    /// </summary>
    public static class PhysicalSignalFactorialExportV1
    {
        [MenuItem("Cells/Research/Run Physical Signaling 2x2 Factorial (30 Seeds)")]
        public static void Run()
        {
            const int firstSeed = 631;
            const int count = 30;
            var folder = Path.GetFullPath(Path.Combine(
                Application.dataPath, "..", "ExperimentRuns", "physical-factorial-v1"));
            Directory.CreateDirectory(folder);
            var agents = new StringBuilder(
                "seed,produce,response,seat,net_energy,nutrient_mass,physical_detection_steps,emitted_mass,emission_energy,remaining_signal,decayed_signal\n");
            var pairs = new StringBuilder(
                "seed,receiver_00,receiver_01,receiver_10,receiver_11,response_when_silent,response_when_produced,interaction,source_production_cost\n");
            var interaction = new List<PairedAssayAnalysisV1.Record>();
            var effectWithProduction = new List<PairedAssayAnalysisV1.Record>();
            var placeboResponse = new List<PairedAssayAnalysisV1.Record>();
            var senderCost = new List<PairedAssayAnalysisV1.Record>();

            for (int n = 0; n < count; ++n)
            {
                int seed = firstSeed + n;
                var result = new PhysicalSignalAssayV1.Result[2, 2];
                var receiverReturn = new double[2, 2];
                for (int produce = 0; produce < 2; ++produce)
                for (int respond = 0; respond < 2; ++respond)
                {
                    var run = PhysicalSignalAssayV1.Run(seed, produce == 1, respond == 1);
                    result[produce, respond] = run;
                    for (int seat = 0; seat < PhysicalSignalAssayV1.Agents; ++seat)
                    {
                        if (seat > 0) receiverReturn[produce, respond] += run.NetEnergy[seat];
                        agents.Append(seed).Append(',').Append(produce).Append(',')
                            .Append(respond).Append(',').Append(seat).Append(',')
                            .Append(F(run.NetEnergy[seat])).Append(',')
                            .Append(F(run.ConsumedMass[seat])).Append(',')
                            .Append(run.Receptions[seat]).Append(',')
                            .Append(F(run.SignalMassEmitted)).Append(',')
                            .Append(F(run.SignalEnergySpent)).Append(',')
                            .Append(F(run.SignalMassRemaining)).Append(',')
                            .Append(F(run.SignalMassDecayed)).Append('\n');
                    }
                }

                double placebo = receiverReturn[0, 1] - receiverReturn[0, 0];
                double responseBenefit = receiverReturn[1, 1] - receiverReturn[1, 0];
                double differenceInDifferences = responseBenefit - placebo;
                double sourcePenalty = result[1, 0].NetEnergy[0] -
                    result[0, 0].NetEnergy[0];

                placeboResponse.Add(new PairedAssayAnalysisV1.Record(
                    seed, receiverReturn[0, 0], receiverReturn[0, 1]));
                effectWithProduction.Add(new PairedAssayAnalysisV1.Record(
                    seed, receiverReturn[1, 0], receiverReturn[1, 1]));
                interaction.Add(new PairedAssayAnalysisV1.Record(
                    seed, 0, differenceInDifferences));
                senderCost.Add(new PairedAssayAnalysisV1.Record(seed,
                    result[0, 0].NetEnergy[0], result[1, 0].NetEnergy[0]));

                pairs.Append(seed).Append(',')
                    .Append(F(receiverReturn[0, 0])).Append(',')
                    .Append(F(receiverReturn[0, 1])).Append(',')
                    .Append(F(receiverReturn[1, 0])).Append(',')
                    .Append(F(receiverReturn[1, 1])).Append(',')
                    .Append(F(placebo)).Append(',')
                    .Append(F(responseBenefit)).Append(',')
                    .Append(F(differenceInDifferences)).Append(',')
                    .Append(F(sourcePenalty)).Append('\n');
            }

            var summary = new StringBuilder(PairedAssayAnalysisV1.Header);
            summary.Append(PairedAssayAnalysisV1.ToCsv("receiver_response_while_no_signal",
                PairedAssayAnalysisV1.EstimatePaired(placeboResponse)));
            summary.Append(PairedAssayAnalysisV1.ToCsv("receiver_response_with_signal",
                PairedAssayAnalysisV1.EstimatePaired(effectWithProduction)));
            summary.Append(PairedAssayAnalysisV1.ToCsv("receiver_production_x_response_interaction",
                PairedAssayAnalysisV1.EstimatePaired(interaction)));
            summary.Append(PairedAssayAnalysisV1.ToCsv("source_production_net_energy",
                PairedAssayAnalysisV1.EstimatePaired(senderCost)));

            File.WriteAllText(Path.Combine(folder, "agents.csv"), agents.ToString());
            File.WriteAllText(Path.Combine(folder, "paired_by_seed.csv"), pairs.ToString());
            File.WriteAllText(Path.Combine(folder, "paired_summary.csv"), summary.ToString());
            File.WriteAllText(Path.Combine(folder, "manifest.json"),
                "{\n"
                + "  \"schema\": \"cells.physical-factorial.v1\",\n"
                + "  \"first_seed\": " + firstSeed + ",\n"
                + "  \"seed_count\": " + count + ",\n"
                + "  \"arms\": \"produce{0,1} x respond{0,1}\",\n"
                + "  \"episodes\": " + (count * 4) + ",\n"
                + "  \"agents\": " + PhysicalSignalAssayV1.Agents + ",\n"
                + "  \"physical_fluid_coupled\": false,\n"
                + "  \"source_revision\": \"unverified\",\n"
                + "  \"primary_estimand\": \"receiver production x response difference-in-differences\",\n"
                + "  \"note\": \"Diffusion-based toy assay; simulated results must be validated in Unity; no equilibrium or biological efficacy claim.\"\n"
                + "}\n");
            Debug.Log("cells: wrote physical signaling 2x2 factorial to " + folder +
                " (120 episodes, 480 agent rows). Verify mass ledger and zero-production placebo.");
            EditorUtility.RevealInFinder(folder);
        }

        private static string F(double value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }
    }
}
