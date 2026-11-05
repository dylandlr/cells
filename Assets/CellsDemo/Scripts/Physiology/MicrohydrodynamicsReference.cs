using System;

namespace Cells.Physiology
{
    /// <summary>
    /// SI-unit reference calculations for calibration and dimensional sanity
    /// checks. These are idealized fluid-mechanics relations, not a Unity
    /// coordinate calibration or a general solver for cell hydrodynamics.
    /// </summary>
    public static class MicrohydrodynamicsReference
    {
        public static double ReynoldsNumber(double densityKgPerM3,
            double velocityMetersPerSecond, double lengthMeters,
            double dynamicViscosityPaS)
        {
            if (!double.IsFinite(densityKgPerM3) || densityKgPerM3 <= 0 ||
                !double.IsFinite(velocityMetersPerSecond) ||
                !double.IsFinite(lengthMeters) || lengthMeters <= 0 ||
                !double.IsFinite(dynamicViscosityPaS) || dynamicViscosityPaS <= 0)
                throw new ArgumentOutOfRangeException(nameof(dynamicViscosityPaS),
                    "Finite positive fluid parameters and characteristic length are required.");
            return densityKgPerM3 * Math.Abs(velocityMetersPerSecond) * lengthMeters / dynamicViscosityPaS;
        }

        /// <summary>6 pi mu R for an isolated rigid sphere in unbounded Stokes flow.</summary>
        public static double SphericalStokesTranslationDrag(double dynamicViscosityPaS,
            double radiusMeters)
        {
            if (!double.IsFinite(dynamicViscosityPaS) || dynamicViscosityPaS <= 0 ||
                !double.IsFinite(radiusMeters) || radiusMeters <= 0)
                throw new ArgumentOutOfRangeException(nameof(radiusMeters));
            return 6.0 * Math.PI * dynamicViscosityPaS * radiusMeters;
        }

        /// <summary>8 pi mu R^3 for an isolated rigid sphere in unbounded Stokes flow.</summary>
        public static double SphericalStokesRotationDrag(double dynamicViscosityPaS,
            double radiusMeters)
        {
            if (!double.IsFinite(dynamicViscosityPaS) || dynamicViscosityPaS <= 0 ||
                !double.IsFinite(radiusMeters) || radiusMeters <= 0)
                throw new ArgumentOutOfRangeException(nameof(radiusMeters));
            return 8.0 * Math.PI * dynamicViscosityPaS *
                radiusMeters * radiusMeters * radiusMeters;
        }
    }
}
