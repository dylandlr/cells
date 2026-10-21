using NUnit.Framework;
using System;
using Cells.Physiology;

namespace Cells.Tests
{
    public sealed class MicrohydrodynamicsReferenceTests
    {
        [Test]
        public void MicronScaleSwimmerIsDeepInViscousRegime()
        {
            // Example: 2 um characteristic length, 20 um/s, water-like fluid.
            double re = MicrohydrodynamicsReference.ReynoldsNumber(
                1000.0, 20e-6, 2e-6, 1e-3);
            Assert.AreEqual(4e-5, re, 1e-12);
            Assert.Less(re, 1e-3);
        }

        [Test]
        public void IdealSphereResistanceHasExpectedRadiusScaling()
        {
            const double viscosity = 1e-3;
            double translation1 = MicrohydrodynamicsReference
                .SphericalStokesTranslationDrag(viscosity, 1e-6);
            double translation2 = MicrohydrodynamicsReference
                .SphericalStokesTranslationDrag(viscosity, 2e-6);
            double rotation1 = MicrohydrodynamicsReference
                .SphericalStokesRotationDrag(viscosity, 1e-6);
            double rotation2 = MicrohydrodynamicsReference
                .SphericalStokesRotationDrag(viscosity, 2e-6);
            Assert.AreEqual(2.0, translation2 / translation1, 1e-12);
            Assert.AreEqual(8.0, rotation2 / rotation1, 1e-12);
        }

        [Test]
        public void InvalidFluidPropertiesAreRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                MicrohydrodynamicsReference.ReynoldsNumber(1000, 1e-5, 2e-6, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                MicrohydrodynamicsReference.SphericalStokesTranslationDrag(1e-3, double.NaN));
        }
    }
}
