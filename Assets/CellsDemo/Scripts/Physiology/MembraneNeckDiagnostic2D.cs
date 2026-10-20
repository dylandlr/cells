using UnityEngine;

namespace Cells.Physiology
{
    /// <summary>
    /// Read-only geometric diagnostic for the current one-loop soft membrane.
    /// A valid constriction must measurably narrow the equator, preserve a
    /// finite non-self-intersecting closed polygon, and keep positive area.
    /// This does not change membrane topology or prove physical scission.
    /// </summary>
    public static class MembraneNeckDiagnostic2D
    {
        public struct Result
        {
            public bool finite, simple, valid, narrowed;
            public float equatorWidth, shoulderWidth, ratio, area;
        }

        public static Result Measure(SoftCellBody2D body, float maximumRatio=.70f)
        {
            var result=new Result();
            if (!body || !body.Initialized || !float.IsFinite(maximumRatio) ||
                maximumRatio<=0f || maximumRatio>=1f) return result;
            var vertices=new Vector2[SoftCellBody2D.Nodes];
            for(int i=0;i<vertices.Length;i++)vertices[i]=body.Node(i);
            return MeasureContour(vertices,maximumRatio);
        }

        public static Result MeasureContour(Vector2[] vertices,float maximumRatio=.70f)
        {
            var result=new Result();
            if(vertices==null || vertices.Length!=SoftCellBody2D.Nodes ||
                !float.IsFinite(maximumRatio) || maximumRatio<=0f || maximumRatio>=1f)
                return result;
            int n=vertices.Length;
            for(int i=0;i<n;i++)
            {
                Vector2 v=vertices[i];
                if(!float.IsFinite(v.x) || !float.IsFinite(v.y))return result;
            }
            result.finite=true;
            float twiceArea=0f;
            for(int i=0;i<n;i++)twiceArea+=Cross(Vector2.zero,vertices[i],vertices[(i+1)%n]);
            result.area=Mathf.Abs(twiceArea)*.5f;
            result.equatorWidth=Vector2.Distance(vertices[0],vertices[n/2]);
            float upper=Vector2.Distance(vertices[n/8],vertices[3*n/8]);
            float lower=Vector2.Distance(vertices[5*n/8],vertices[7*n/8]);
            result.shoulderWidth=(upper+lower)*.5f;
            result.ratio=result.shoulderWidth>1e-6f
                ? result.equatorWidth/result.shoulderWidth : float.PositiveInfinity;
            result.simple=IsSimple(vertices);
            result.valid=result.simple && result.area>1e-4f &&
                float.IsFinite(result.ratio) && result.equatorWidth>1e-4f;
            result.narrowed=result.valid && result.ratio<=maximumRatio;
            return result;
        }

        static bool IsSimple(Vector2[] vertices)
        {
            int n=SoftCellBody2D.Nodes;
            for(int i=0;i<n;i++)
            for(int j=i+1;j<n;j++)
            {
                if(j==i+1 || (i==0 && j==n-1))continue;
                if(Crosses(vertices[i],vertices[(i+1)%n],
                    vertices[j],vertices[(j+1)%n]))return false;
            }
            return true;
        }

        static float Cross(Vector2 a,Vector2 b,Vector2 c) =>
            (b.x-a.x)*(c.y-a.y)-(b.y-a.y)*(c.x-a.x);

        static bool Crosses(Vector2 a,Vector2 b,Vector2 c,Vector2 d)
        {
            float abC=Cross(a,b,c),abD=Cross(a,b,d);
            float cdA=Cross(c,d,a),cdB=Cross(c,d,b);
            // Conservatively reject touching or near-collinear non-neighbor
            // segments, including degeneracies in the neck.
            const float epsilon=1e-7f;
            return abC*abD<=epsilon && cdA*cdB<=epsilon;
        }
    }
}
