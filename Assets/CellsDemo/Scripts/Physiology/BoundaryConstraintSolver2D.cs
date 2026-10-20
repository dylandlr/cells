using System;
using UnityEngine;

namespace Cells.Physiology
{
    /// <summary>
    /// Experimental position-based membrane ring, not yet attached to cells.
    /// Preserves edge lengths and signed enclosed area by iterative
    /// projections. Assumes a simple, consistently wound initial polygon.
    /// These are effective 2D constraints, not calibrated lipid physics.
    /// </summary>
    public sealed class BoundaryConstraintSolver2D
    {
        public readonly Vector2[] positions;
        readonly float[] edgeLengths;
        readonly float targetArea;
        public float EdgeError { get; private set; }
        public float RelativeAreaError { get; private set; }

        public BoundaryConstraintSolver2D(BoundaryLoop2D initial)
        {
            if(initial==null || !initial.Valid)throw new ArgumentException("Invalid initial loop");
            positions=(Vector2[])initial.vertices.Clone();
            edgeLengths=new float[positions.Length];
            for(int i=0;i<positions.Length;i++)
                edgeLengths[i]=Vector2.Distance(positions[i],positions[(i+1)%positions.Length]);
            targetArea=initial.SignedArea;
        }

        /// <summary>Bounded experimental displacement field followed by
        /// restorative constraints. All-or-nothing on failure, including the
        /// externally applied displacement. Local y=0 is the neck plane.</summary>
        public bool TryConstrict(float strength, float band, float maxDisplacement=.06f,
            int iterations=12)
        {
            if(!float.IsFinite(strength) || strength<0f || strength>1f ||
                !float.IsFinite(band) || band<=0f ||
                !float.IsFinite(maxDisplacement) || maxDisplacement<=0f)
                return false;
            var original=(Vector2[])positions.Clone();
            for(int i=0;i<positions.Length;i++)
            {
                var p=positions[i];
                float weight=Mathf.Exp(-Mathf.Pow(p.y/band,2f));
                float shift=Mathf.Min(Mathf.Abs(p.x)*strength*weight,maxDisplacement);
                positions[i].x-=Mathf.Sign(p.x)*shift;
            }
            if(!Step(iterations,.55f,.22f))
            {
                Array.Copy(original,positions,positions.Length);
                return false;
            }
            return true;
        }

        public bool Step(int iterations=8,float edgeStiffness=.5f,float areaStiffness=.2f)
        {
            if(iterations<1 || iterations>100 || !float.IsFinite(edgeStiffness) ||
                !float.IsFinite(areaStiffness) || edgeStiffness<0 || areaStiffness<0 ||
                edgeStiffness>1 || areaStiffness>1)return false;
            var saved=(Vector2[])positions.Clone();
            int n=positions.Length;
            for(int iteration=0;iteration<iterations;iteration++)
            {
                for(int i=0;i<n;i++)
                {
                    int j=(i+1)%n;
                    Vector2 delta=positions[j]-positions[i];
                    float length=delta.magnitude;
                    if(length<=1e-7f || !float.IsFinite(length))return Restore(saved);
                    Vector2 adjustment=(delta/length)*(length-edgeLengths[i])*.5f*edgeStiffness;
                    positions[i]+=adjustment;
                    positions[j]-=adjustment;
                }
                float area=SignedArea();
                var gradient=new Vector2[n];
                float magnitude=0f;
                for(int i=0;i<n;i++)
                {
                    Vector2 before=positions[(i+n-1)%n],after=positions[(i+1)%n];
                    gradient[i]=new Vector2(after.y-before.y,before.x-after.x)*.5f;
                    magnitude+=gradient[i].sqrMagnitude;
                }
                if(magnitude<=1e-9f || !float.IsFinite(magnitude))return Restore(saved);
                float lambda=(targetArea-area)*areaStiffness/magnitude;
                for(int i=0;i<n;i++)positions[i]+=gradient[i]*lambda;
            }
            var final=new BoundaryLoop2D(positions);
            if(!final.Valid || Mathf.Sign(final.SignedArea)!=Mathf.Sign(targetArea))
                return Restore(saved);
            EdgeError=0f;
            for(int i=0;i<n;i++)
                EdgeError=Mathf.Max(EdgeError,Mathf.Abs(
                    Vector2.Distance(positions[i],positions[(i+1)%n])-edgeLengths[i]));
            RelativeAreaError=Mathf.Abs(final.SignedArea-targetArea)/
                Mathf.Max(1e-6f,Mathf.Abs(targetArea));
            return true;
        }

        float SignedArea()
        {
            float twice=0f;
            for(int i=0;i<positions.Length;i++)
            {
                var a=positions[i];var b=positions[(i+1)%positions.Length];
                twice+=a.x*b.y-a.y*b.x;
            }
            return twice*.5f;
        }

        bool Restore(Vector2[] saved)
        {
            Array.Copy(saved,positions,saved.Length);
            return false;
        }
    }
}
