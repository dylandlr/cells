using NUnit.Framework;
using UnityEngine;
using Cells.Physiology;

namespace Cells.Tests.Editor
{
    public sealed class BoundaryEarClip2DTests
    {
        [Test]
        public void ConcaveOutlineTriangulatesWithAreaPreserved()
        {
            var points=new [] {
                new Vector2(-2f,1f),new Vector2(-1f,1.7f),
                new Vector2(0f,.3f),new Vector2(1f,1.7f),
                new Vector2(2f,1f),new Vector2(2f,-1f),
                new Vector2(1f,-1.7f),new Vector2(0f,-.3f),
                new Vector2(-1f,-1.7f),new Vector2(-2f,-1f)
            };
            var loop=new BoundaryLoop2D(points);
            Assert.IsTrue(BoundaryEarClip2D.TryTriangulate(loop,out var indices));
            Assert.AreEqual((points.Length-2)*3,indices.Length);
            float triangleArea=0f;
            for(int i=0;i<indices.Length;i+=3)
            {
                Vector2 a=points[indices[i]],b=points[indices[i+1]],c=points[indices[i+2]];
                triangleArea+=Mathf.Abs((b.x-a.x)*(c.y-a.y)-(b.y-a.y)*(c.x-a.x))*.5f;
            }
            Assert.AreEqual(Mathf.Abs(loop.SignedArea),triangleArea,1e-4f);
            System.Array.Reverse(points);
            var reversed=new BoundaryLoop2D(points);
            Assert.IsTrue(BoundaryEarClip2D.TryTriangulate(reversed,out var reversedIndices));
            Assert.AreEqual(indices.Length,reversedIndices.Length);
        }

        [Test]
        public void SelfIntersectingOutlineCannotTriangulate()
        {
            var loop=new BoundaryLoop2D(new [] {
                new Vector2(-1f,-1f),new Vector2(1f,1f),
                new Vector2(-1f,1f),new Vector2(1f,-1f)
            });
            Assert.IsFalse(BoundaryEarClip2D.TryTriangulate(loop,out var indices));
            Assert.AreEqual(0,indices.Length);
        }
    }
}
