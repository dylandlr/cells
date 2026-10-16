using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Cells.Physiology;

namespace Cells.Editor
{
    /// <summary>
    /// Reproducible isolated RFT assay for local or licensed batch Unity runs.
    /// Calls Tick methods in strict motor -> filament -> hydrodynamics order
    /// without advancing a scene or relying on rendered frame timing.
    /// </summary>
    public static class FlagellarExperimentRunner
    {
        const float Step = .02f;
        const int Warmup = 100;
        const int Iterations = 500;

        struct Condition
        {
            public float Frequency, Amplitude, Integrity;
            public Condition(float frequency,float amplitude,float integrity)
            {
                Frequency=frequency;Amplitude=amplitude;Integrity=integrity;
            }
        }

        static readonly Condition[] Conditions =
        {
            new Condition(1.1f,.12f,1f),
            new Condition(2.2f,.24f,1f),
            new Condition(3.3f,.36f,1f),
            new Condition(2.2f,.24f,.35f),
            new Condition(2.2f,.24f,0f),
            // Isolate changes to a single dimension to permit comparisons.
            new Condition(1.1f,.24f,1f),
            new Condition(3.3f,.24f,1f),
            new Condition(2.2f,.12f,1f),
            new Condition(2.2f,.36f,1f)
        };

        [MenuItem("Cells/Experiments/Export flagellar propulsion CSV")]
        public static void ExportCsv()
        {
            string output=Path.GetFullPath("Build/Experiments/flagellar-propulsion.csv");
            Run(output);
            Debug.Log("CELLS_FLAGELLAR_ASSAY_OK: "+output);
        }

        // For a licensed Unity batch invocation:
        // Unity -batchmode -nographics -projectPath <project>
        //   -executeMethod Cells.Editor.FlagellarExperimentRunner.RunBatch -quit
        public static void RunBatch()
        {
            try { ExportCsv(); }
            catch(Exception ex)
            {
                Debug.LogException(ex);
                EditorApplication.Exit(1);
            }
        }

        static string F(float value) =>
            value.ToString("R",CultureInfo.InvariantCulture);

        public static void Run(string path)
        {
            var builder=new StringBuilder();
            builder.AppendLine("frequency_hz,amplitude,integrity,net_x,net_y,net_distance,absolute_path,peak_speed,stroke_dissipation,motor_atp,steps,warmup");
            foreach(var condition in Conditions)
            {
                var organism=new GameObject("isolated propulsion assay");
                try
                {
                    var metabolism=organism.AddComponent<CellMetabolism>();
                    metabolism.energy=100f;
                    var motor=organism.AddComponent<CellFlagellarActuator>();
                    motor.metabolism=metabolism;
                    motor.motorActivation=1f;
                    motor.beatFrequencyHz=condition.Frequency;
                    motor.waveAmplitude=condition.Amplitude;
                    motor.structuralIntegrity=condition.Integrity;
                    motor.fluid=null;
                    var tail=organism.AddComponent<SoftFlagellum2D>();
                    tail.motor=motor;
                    tail.Initialize();
                    var hydro=organism.AddComponent<FlagellarHydrodynamics2D>();
                    hydro.motor=motor;
                    hydro.tail=tail;
                    hydro.fluid=null;
                    hydro.fluidReactionGain=0f;
                    hydro.ResetHistory();
                    hydro.Tick(Step);
                    float startEnergy=metabolism.energy;
                    float pathLength=0f,dissipation=0f,peakSpeed=0f;
                    Vector2 net=Vector2.zero;
                    for(int step=0;step<Iterations;step++)
                    {
                        motor.Tick(Step);
                        tail.Tick(Step);
                        hydro.Tick(Step);
                        if(step<Warmup) continue;
                        Vector2 velocity=hydro.LocalSwimmingVelocity;
                        if(!float.IsFinite(velocity.x)||!float.IsFinite(velocity.y)||
                            !float.IsFinite(hydro.SegmentDissipation))
                            throw new InvalidOperationException("Non-finite propulsion diagnostic");
                        if(velocity.magnitude>hydro.maximumSwimSpeed+1e-4f)
                            throw new InvalidOperationException("Propulsion speed exceeded cap");
                        net+=velocity*Step;
                        pathLength+=velocity.magnitude*Step;
                        peakSpeed=Mathf.Max(peakSpeed,velocity.magnitude);
                        dissipation+=hydro.SegmentDissipation;
                    }
                    if(condition.Integrity==0f && (pathLength>1e-5f || dissipation>1e-5f))
                        throw new InvalidOperationException("Disabled-tail control was not stationary");
                    builder.Append(F(condition.Frequency)).Append(',')
                        .Append(F(condition.Amplitude)).Append(',')
                        .Append(F(condition.Integrity)).Append(',')
                        .Append(F(net.x)).Append(',').Append(F(net.y)).Append(',')
                        .Append(F(net.magnitude)).Append(',')
                        .Append(F(pathLength)).Append(',')
                        .Append(F(peakSpeed)).Append(',')
                        .Append(F(dissipation)).Append(',')
                        .Append(F(startEnergy-metabolism.energy)).Append(',')
                        .Append(Iterations).Append(',').Append(Warmup).AppendLine();
                }
                finally { UnityEngine.Object.DestroyImmediate(organism); }
            }
            string directory=Path.GetDirectoryName(Path.GetFullPath(path));
            if(!Directory.Exists(directory))Directory.CreateDirectory(directory);
            File.WriteAllText(path,builder.ToString(),new UTF8Encoding(false));
        }
    }
}
