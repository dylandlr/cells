using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cells.Physiology
{
    /// <summary>
    /// Controlled repeated-impulse boundary experiment. A failed step is
    /// reported rather than silently considered successful scission.
    /// This is not a Unity scene component or a biological cell cycle.
    /// </summary>
    public static class BoundaryConstrictionAssay2D
    {
        [Serializable]
        public struct Observation
        {
            public int iteration;
            public float neckWidth, shoulderWidth, ratio, signedArea,
                relativeAreaError, edgeError;
            public bool valid;
        }

        [Serializable]
        public sealed class Report
        {
            public string status;
            public int completedSteps;
            public List<Observation> observations=new List<Observation>();
        }

        public static Report Run(int steps=40,float strength=.20f,
            float band=.35f,float maxDisplacement=.025f)
        {
            if(steps<1 || steps>1000 || !float.IsFinite(strength) ||
               strength<0f || strength>1f)
                throw new ArgumentException("Invalid boundary assay settings.");
            const int count=32;
            var nodes=new Vector2[count];
            for(int i=0;i<count;i++)
            {
                float angle=i*2f*Mathf.PI/count;
                nodes[i]=new Vector2(Mathf.Cos(angle),1.25f*Mathf.Sin(angle));
            }
            var solver=new BoundaryConstraintSolver2D(new BoundaryLoop2D(nodes));
            var report=new Report {status="completed"};
            for(int iteration=0;iteration<=steps;iteration++)
            {
                if(iteration>0 && !solver.TryConstrict(strength,band,maxDisplacement))
                {
                    report.status="solver-rejected";
                    break;
                }
                var boundary=new BoundaryLoop2D(solver.positions);
                float equator=Vector2.Distance(solver.positions[0],solver.positions[count/2]);
                float upper=Vector2.Distance(solver.positions[count/8],solver.positions[3*count/8]);
                float lower=Vector2.Distance(solver.positions[5*count/8],solver.positions[7*count/8]);
                float shoulder=(upper+lower)*.5f;
                bool valid=boundary.Valid && shoulder>1e-6f;
                var observation=new Observation {
                    iteration=iteration,neckWidth=equator,shoulderWidth=shoulder,
                    ratio=valid ? equator/shoulder : float.PositiveInfinity,
                    signedArea=boundary.SignedArea,
                    relativeAreaError=solver.RelativeAreaError,
                    edgeError=solver.EdgeError,valid=valid
                };
                report.observations.Add(observation);
                if(!valid) {report.status="invalid-geometry";break;}
                report.completedSteps=iteration;
            }
            return report;
        }
    }
}
