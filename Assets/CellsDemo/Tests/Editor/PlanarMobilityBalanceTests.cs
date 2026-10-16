using System;
using NUnit.Framework;
using Cells.Physiology;

namespace Cells.Tests
{
    public sealed class PlanarMobilityBalanceTests
    {
        [Test]
        public void DiagonalResistanceBalancesForceAndTorque()
        {
            PlanarMobilityBalance.Solve(2,0,0,4,0,8,4,-8,16,
                out double x,out double y,out double w);
            Assert.AreEqual(-2,x,1e-10);
            Assert.AreEqual(2,y,1e-10);
            Assert.AreEqual(-2,w,1e-10);
        }

        [Test]
        public void CouplingCreatesRotationEvenWhenStrokeTorqueIsZero()
        {
            PlanarMobilityBalance.Solve(5,0,1,5,0,3,1,0,0,
                out double x,out double y,out double w);
            Assert.Less(x,0);
            Assert.Greater(w,0);
            Assert.AreEqual(-1,5*x+w,1e-10);
            Assert.AreEqual(0,x+3*w,1e-10);
            Assert.AreEqual(0,y,1e-10);
        }

        [Test]
        public void GeneralSymmetricResistanceSatisfiesAllThreeBalances()
        {
            const double a=4,b=.4,c=.9,d=3,e=-.3,f=2;
            const double fx=.7,fy=-1.2,tau=.65;
            PlanarMobilityBalance.Solve(a,b,c,d,e,f,fx,fy,tau,
                out double x,out double y,out double w);
            Assert.AreEqual(-fx,a*x+b*y+c*w,1e-10);
            Assert.AreEqual(-fy,b*x+d*y+e*w,1e-10);
            Assert.AreEqual(-tau,c*x+e*y+f*w,1e-10);
        }

        [Test]
        public void NonPositiveDefiniteResistanceIsRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                PlanarMobilityBalance.Solve(1,0,2,1,0,1,1,1,1,
                    out _,out _,out _));
        }

        [Test]
        public void ZeroStrokeCreatesNoSwimming()
        {
            PlanarMobilityBalance.Solve(4,.1,.2,5,.3,3,0,0,0,
                out double x,out double y,out double w);
            Assert.AreEqual(0,x,1e-12);
            Assert.AreEqual(0,y,1e-12);
            Assert.AreEqual(0,w,1e-12);
        }
    }
}
