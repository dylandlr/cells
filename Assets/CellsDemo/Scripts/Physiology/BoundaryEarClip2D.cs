using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cells.Physiology
{
    /// <summary>Ear clipping on a simple polygon. Produces boundary-only
    /// triangles, independent of any interior central node. No holes supported.</summary>
    public static class BoundaryEarClip2D
    {
        static float Cross(Vector2 a,Vector2 b,Vector2 c) =>
            (b.x-a.x)*(c.y-a.y)-(b.y-a.y)*(c.x-a.x);

        static bool InsideOrOnTriangle(Vector2 p, Vector2 a,Vector2 b,Vector2 c,
            float orientation)
        {
            const float tolerance=1e-7f;
            return orientation*Cross(a,b,p)>=-tolerance &&
                   orientation*Cross(b,c,p)>=-tolerance &&
                   orientation*Cross(c,a,p)>=-tolerance;
        }

        public static bool TryTriangulate(BoundaryLoop2D loop,out int[] triangles)
        {
            triangles=Array.Empty<int>();
            if(loop==null || !loop.Valid)return false;
            var points=loop.vertices;
            int count=points.Length;
            var remaining=new List<int>(count);
            for(int i=0;i<count;i++)remaining.Add(i);
            var result=new List<int>((count-2)*3);
            float orientation=Mathf.Sign(loop.SignedArea);
            int guard=0;
            while(remaining.Count>3 && guard++<count*count)
            {
                bool clipped=false;
                for(int i=0;i<remaining.Count;i++)
                {
                    int a=remaining[(i+remaining.Count-1)%remaining.Count];
                    int b=remaining[i];
                    int c=remaining[(i+1)%remaining.Count];
                    if(orientation*Cross(points[a],points[b],points[c])<=1e-7f)
                        continue;
                    bool contains=false;
                    for(int j=0;j<remaining.Count;j++)
                    {
                        int candidate=remaining[j];
                        if(candidate==a || candidate==b || candidate==c)continue;
                        if(InsideOrOnTriangle(points[candidate],points[a],
                            points[b],points[c],orientation))
                        { contains=true;break; }
                    }
                    if(contains)continue;
                    result.Add(a);result.Add(b);result.Add(c);
                    remaining.RemoveAt(i);
                    clipped=true;
                    break;
                }
                if(!clipped)return false;
            }
            if(remaining.Count!=3 ||
                orientation*Cross(points[remaining[0]],points[remaining[1]],
                    points[remaining[2]])<=1e-7f)return false;
            result.Add(remaining[0]);result.Add(remaining[1]);result.Add(remaining[2]);
            triangles=result.ToArray();
            return true;
        }
    }
}
