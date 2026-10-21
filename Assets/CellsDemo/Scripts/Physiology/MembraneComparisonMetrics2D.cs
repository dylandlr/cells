using System;
using UnityEngine;

namespace Cells.Physiology
{
    /// <summary>Shared, uncalibrated planar contour diagnostics. Comparing
    /// systems via these values does not equate their force models.</summary>
    public static class MembraneComparisonMetrics2D
    {
        [Serializable]
        public struct Metrics
        {
            public bool valid;
            public float neckRatio, absoluteArea, perimeter, normalizedAreaDrift;
        }

        public static Metrics Measure(Vector2[] points,float baselineArea)
        {
            var result=new Metrics();
            if(points==null || points.Length!=32 || !float.IsFinite(baselineArea) ||
                baselineArea<=0f)return result;
            var loop=new BoundaryLoop2D(points);
            if(!loop.Valid)return result;
            float shoulder=(Vector2.Distance(points[4],points[12])+
                            Vector2.Distance(points[20],points[28]))*.5f;
            if(shoulder<=1e-7f)return result;
            result.valid=true;
            result.neckRatio=Vector2.Distance(points[0],points[16])/shoulder;
            result.absoluteArea=Mathf.Abs(loop.SignedArea);
            result.perimeter=loop.Perimeter;
            result.normalizedAreaDrift=Mathf.Abs(result.absoluteArea-baselineArea)/baselineArea;
            return result;
        }

        public static Metrics Measure(SoftCellBody2D body,float baselineArea)
        {
            if(!body || !body.Initialized)return new Metrics();
            var points=new Vector2[SoftCellBody2D.Nodes];
            for(int i=0;i<points.Length;i++)points[i]=body.Node(i);
            return Measure(points,baselineArea);
        }
    }
}
