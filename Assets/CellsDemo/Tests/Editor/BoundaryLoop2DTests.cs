using NUnit.Framework;
using UnityEngine;
using Cells.Physiology;

namespace Cells.Tests.Editor
{
    public sealed class BoundaryLoop2DTests
    {
        [Test]
        public void ConcaveTwoLobedContourRemainsSimpleWithoutCenterFan()
        {
            var loop=new BoundaryLoop2D(new [] {
                new Vector2(-2f,1f),new Vector2(-1f,1.7f),
                new Vector2(0f,.3f),new Vector2(1f,1.7f),
                new Vector2(2f,1f),new Vector2(2f,-1f),
                new Vector2(1f,-1.7f),new Vector2(0f,-.3f),
                new Vector2(-1f,-1.7f),new Vector2(-2f,-1f)
            });
            Assert.IsTrue(loop.Valid);
            Assert.Greater(Mathf.Abs(loop.SignedArea),.1f);
            Assert.Greater(loop.Perimeter,0f);
            Assert.AreEqual(0f,loop.Centroid.x,1e-5f);
            Assert.AreEqual(0f,loop.Centroid.y,1e-5f);
        }

        [Test]
        public void SelfCrossingLoopIsRejected()
        {
            var loop=new BoundaryLoop2D(new [] {
                new Vector2(-1f,-1f),new Vector2(1f,1f),
                new Vector2(-1f,1f),new Vector2(1f,-1f)
            });
            Assert.IsFalse(loop.Valid);
            Assert.IsFalse(loop.Simple);
        }

        [Test]
        public void OrientationReversalChangesAreaSignButNotPerimeter()
        {
            var forward=new BoundaryLoop2D(new [] {
                Vector2.zero, Vector2.right,Vector2.up
            });
            var reverse=new BoundaryLoop2D(new [] {
                Vector2.up,Vector2.right,Vector2.zero
            });
            Assert.AreEqual(forward.SignedArea,-reverse.SignedArea,1e-6f);
            Assert.AreEqual(forward.Perimeter,reverse.Perimeter,1e-6f);
            Assert.IsTrue(forward.Valid);
            Assert.IsTrue(reverse.Valid);
        }
    }
}
