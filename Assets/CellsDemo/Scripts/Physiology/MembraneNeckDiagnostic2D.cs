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
            int n=SoftCellBody2D.Nodes;
            for(int i=0;i<n;i++)
            {
                Vector2 v=body.Node(i);
                if(!float.IsFinite(v.x) || !float.IsFinite(v.y))return result;
            }
            result.finite=true;
            result.area=Mathf.Abs(body.SignedArea());
            result.equatorWidth=Vector2.Distance(body.Node(0),body.Node(n/2));
            float upper=Vector2.Distance(body.Node(n/8),body.Node(3*n/8));
            float lower=Vector2.Distance(body.Node(5*n/8),body.Node(7*n/8));
            result.shoulderWidth=(upper+lower)*.5f;
            result.ratio=result.shoulderWidth>1e-6f
                ? result.equatorWidth/result.shoulderWidth : float.PositiveInfinity;
            result.simple=IsSimple(body);
            result.valid=result.simple && result.area>1e-4f &&
                float.IsFinite(result.ratio) && result.equatorWidth>1e-4f;
            result.narrowed=result.valid && result.ratio<=maximumRatio;
            return result;
        }

        static bool IsSimple(SoftCellBody2D body)
        {
            int n=SoftCellBody2D.Nodes;
            for(int i=0;i<n;i++)
            for(int j=i+1;j<n;j++)
            {
                if(j==i+1 || (i==0 && j==n-1))continue;
                if(Crosses(body.Node(i),body.Node((i+1)%n),
                    body.Node(j),body.Node((j+1)%n)))return false;
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
