using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Cells.Physiology;
using Cells.Experiments;

namespace Cells.Demo
{
    /// <summary>
    /// Sequential Play Mode runner. Fresh generated field/cell hierarchy each trial;
    /// the results CSV is written only for trials which actually finished.
    /// Attach to an empty object in a dedicated assay scene, not ChemistryDemo.
    /// </summary>
    public sealed class ChemotaxisBatchRunner : MonoBehaviour
    {
        public bool runOnStart = true;
        [Min(1)] public int replicates = 5;
        public int firstSeed = 631;
        [Min(.1f)] public float horizonSeconds = 30f;
        public string outputFile = "chemotaxis-outcomes-v1.csv";
        public bool finished;
        public bool running;
        public string LastOutputPath { get; private set; }
        readonly List<ChemotaxisAssayPlanV1.Outcome> completed =
            new List<ChemotaxisAssayPlanV1.Outcome>();

        IEnumerator Start()
        {
            if (runOnStart) yield return Run();
        }

        GameObject activeTrialRoot;
        public int CompletedTrials => completed.Count;

        void OnDisable()
        {
            // Unity can stop an iterator when this component is disabled.
            // Preserve only completed trials; never write a successful batch marker.
            if (activeTrialRoot)
            {
                activeTrialRoot.SetActive(false);
                Destroy(activeTrialRoot);
                activeTrialRoot = null;
            }
            if (running && completed.Count > 0)
                SaveCompleted("partial");
            running = false;
        }

        void SaveCompleted(string suffix)
        {
            string safeName = Path.GetFileName(outputFile);
            if (string.IsNullOrEmpty(safeName)) safeName = "chemotaxis-outcomes-v1.csv";
            if (suffix != null)
                safeName = Path.GetFileNameWithoutExtension(safeName) + "-" + suffix + ".csv";
            string outputPath = Path.Combine(Application.persistentDataPath,safeName);
            File.WriteAllText(outputPath,ChemotaxisAssayPlanV1.OutcomeCsv(completed));
            LastOutputPath = outputPath;
        }

        public IEnumerator Run()
        {
            if (finished || running) yield break;
            running = true;
            completed.Clear();
            try
            {
                var schedule = ChemotaxisAssayPlanV1.Build(firstSeed,replicates);
                for (int index = 0; index < schedule.Length; index++)
                {
                    var trial = schedule[index];
                    activeTrialRoot = new GameObject("Assay " + trial.ordinal + " " + trial.controller);
                    var demo = activeTrialRoot.AddComponent<ChemistryDemoBootstrap>();
                    demo.autoBuild = false;
                    demo.assayMode = true;
                    demo.seed = trial.seed;
                    demo.Build();
                    var swimmer = demo.AssaySwimmer;
                    var recorder = demo.AssayRecorder;
                    if (!swimmer || !recorder)
                        throw new InvalidOperationException("Assay bootstrap missing focal swimmer or recorder");
                    swimmer.useTemporalChemotaxis = trial.controller != ChemotaxisAssayPlanV1.Spatial;
                    swimmer.stochasticTumbles = trial.controller == ChemotaxisAssayPlanV1.Stochastic;
                    recorder.controllerLabel = trial.controller;
                    recorder.BeginTrial();

                    float stepTime = Time.fixedDeltaTime;
                    if (!(stepTime > 0f) || !float.IsFinite(stepTime))
                        throw new InvalidOperationException("Invalid physics fixedDeltaTime");
                    int steps = Mathf.CeilToInt(horizonSeconds / stepTime);
                    while (recorder && recorder.RecordedSteps < steps)
                        yield return new WaitForFixedUpdate();
                    if (!recorder || recorder.RecordedSteps != steps)
                        throw new InvalidOperationException("Assay fixed-step synchronization failed");

                    completed.Add(new ChemotaxisAssayPlanV1.Outcome {
                        trial = trial,
                        elapsedSeconds = recorder.ElapsedSeconds,
                        pathLength = recorder.PathLength,
                        nutrientAbsorbed = recorder.NutrientAbsorbed,
                        nutrientDigested = recorder.NutrientDigested,
                        motorEnergySpent = recorder.MotorEnergySpent,
                        finalEnergy = recorder.LastEnergy,
                        tumbles = recorder.TumbleCount,
                        alive = recorder.Alive
                    });
                    activeTrialRoot.SetActive(false);
                    Destroy(activeTrialRoot);
                    activeTrialRoot = null;
                    yield return null; // Wait for deferred destruction before next world.
                }
                SaveCompleted(null);
                finished = true;
                Debug.Log("CELL_ASSAY_COMPLETE " + LastOutputPath + " trials=" + completed.Count);
            }
            finally
            {
                if (activeTrialRoot)
                {
                    activeTrialRoot.SetActive(false);
                    Destroy(activeTrialRoot);
                    activeTrialRoot = null;
                }
                if (!finished && completed.Count > 0)
                {
                    try { SaveCompleted("partial"); }
                    catch (Exception e) { Debug.LogError("CELL_ASSAY_PARTIAL_WRITE_FAILED " + e); }
                }
                running = false;
            }
        }
    }
}
