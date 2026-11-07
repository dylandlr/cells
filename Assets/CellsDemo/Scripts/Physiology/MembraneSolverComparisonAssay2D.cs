using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cells.Physiology
{
    /// <summary>
    /// Comparative assay: identical initial circle and shared observables,
    /// but distinct actuator mechanics. NOT a matched-force comparison.
    /// Both systems run without scene FixedUpdate on disposable objects.
    /// </summary>
    public static class MembraneSolverComparisonAssay2D
    {
        [Serializable]
        public struct Sample
        {
            public int step;
            public float time;
            public MembraneComparisonMetrics2D.Metrics legacy;
            public MembraneComparisonMetrics2D.Metrics boundary;
        }

        [Serializable]
        public sealed class Report
        {
            public string status;
            public string limitation = "Equal initial geometry and sampling, not equal forces.";
            public List<Sample> samples = new List<Sample>();
        }

        public static Report Run(int steps=100,float dt=.02f)
        {
            if(steps<1 || steps>3000 || !float.IsFinite(dt) || dt<=0f || dt>.05f)
                throw new ArgumentException("Invalid membrane comparison configuration.");
            const int n=SoftCellBody2D.Nodes;
            var go=new GameObject("Disposable legacy comparison solver");
            try
            {
                go.SetActive(false);
                go.AddComponent<MembraneIntegrity>();
                var old=go.AddComponent<SoftCellBody2D>();
                old.Initialize();
                var initial=new Vector2[n];
                for(int i=0;i<n;i++)initial[i]=old.Node(i);
                float baseline=Mathf.Abs(new BoundaryLoop2D(initial).SignedArea);
                var experimental=new BoundaryConstraintSolver2D(new BoundaryLoop2D(initial));
                var report=new Report { status="completed" };
                for(int step=0;step<=steps;step++)
                {
                    if(step>0)
                    {
                        old.divisionConstriction=.85f*step/steps;
                        old.Tick(dt);
                        if(!experimental.TryConstrict(.2f,.35f,.025f))
                        {
                            report.status="boundary-rejected";
                            break;
                        }
                    }
                    var previous=MembraneComparisonMetrics2D.Measure(old,baseline);
                    var candidate=MembraneComparisonMetrics2D.Measure(experimental.positions,baseline);
                    report.samples.Add(new Sample {
                        step=step,time=step*dt,legacy=previous,boundary=candidate
                    });
                    if(!previous.valid || !candidate.valid)
                    {
                        report.status="invalid-geometry";
                        break;
                    }
                }
                return report;
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
    }
}
