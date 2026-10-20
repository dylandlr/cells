using System;
using UnityEngine;

namespace Cells.Physiology
{
    /// <summary>
    /// An ordered planar boundary with no distinguished central vertex.
    /// Geometry-only research prototype: not coupled to fluid, metabolism,
    /// collision response, or lineage. A single simple loop cannot represent
    /// actual two-component scission without an explicit topology change.
    /// </summary>
    [Serializable]
    public sealed class BoundaryLoop2D
    {
        public Vector2[] vertices;

        public BoundaryLoop2D(Vector2[] points)
        {
            if(points==null || points.Length<3)
                throw new ArgumentException("Closed boundary requires at least three points.");
            vertices=(Vector2[])points.Clone();
        }

        public int Count => vertices.Length;
        public float SignedArea
        {
            get
            {
                double twiceArea=0;
                for(int i=0;i<Count;i++)
                {
                    Vector2 a=vertices[i], b=vertices[(i+1)%Count];
                    twiceArea+=(double)a.x*b.y-(double)a.y*b.x;
                }
                return (float)(twiceArea*.5);
            }
        }
        public float Perimeter
        {
            get
            {
                float length=0f;
                for(int i=0;i<Count;i++)
                    length+=Vector2.Distance(vertices[i],vertices[(i+1)%Count]);
                return length;
            }
        }
        public Vector2 Centroid
        {
            get
            {
                float twiceArea=0f;
                Vector2 weighted=Vector2.zero;
                for(int i=0;i<Count;i++)
                {
                    var a=vertices[i];
                    var b=vertices[(i+1)%Count];
                    float cross=a.x*b.y-a.y*b.x;
                    twiceArea+=cross;
                    weighted+=(a+b)*cross;
                }
                return Mathf.Abs(twiceArea)<1e-7f
                    ? Vector2.zero : weighted/(3f*twiceArea);
            }
        }
        public bool Finite
        {
            get
            {
                foreach(var v in vertices)
                    if(!float.IsFinite(v.x)||!float.IsFinite(v.y))return false;
                return true;
            }
        }
        public bool Simple
        {
            get
            {
                if(!Finite)return false;
                for(int i=0;i<Count;i++)
                for(int j=i+1;j<Count;j++)
                {
                    if(j==i+1 || (i==0 && j==Count-1))continue;
                    if(Intersect(vertices[i],vertices[(i+1)%Count],
                        vertices[j],vertices[(j+1)%Count]))return false;
                }
                return true;
            }
        }
        public bool Valid => Finite && Simple && Mathf.Abs(SignedArea)>1e-5f
            && Perimeter>1e-4f;

        static float Cross(Vector2 a,Vector2 b,Vector2 c)
        {
            return (b.x-a.x)*(c.y-a.y)-(b.y-a.y)*(c.x-a.x);
        }
        static bool OnSegment(Vector2 a,Vector2 b,Vector2 p)
        {
            const float e=1e-6f;
            return p.x>=Mathf.Min(a.x,b.x)-e && p.x<=Mathf.Max(a.x,b.x)+e &&
                p.y>=Mathf.Min(a.y,b.y)-e && p.y<=Mathf.Max(a.y,b.y)+e;
        }
        static bool Intersect(Vector2 a,Vector2 b,Vector2 c,Vector2 d)
        {
            const float e=1e-6f;
            float x=Cross(a,b,c), y=Cross(a,b,d), z=Cross(c,d,a), w=Cross(c,d,b);
            if(Mathf.Abs(x)<=e && OnSegment(a,b,c))return true;
            if(Mathf.Abs(y)<=e && OnSegment(a,b,d))return true;
            if(Mathf.Abs(z)<=e && OnSegment(c,d,a))return true;
            if(Mathf.Abs(w)<=e && OnSegment(c,d,b))return true;
            return ((x>e&&y< -e)||(x< -e&&y>e)) &&
                ((z>e&&w< -e)||(z< -e&&w>e));
        }
    }
}
