using System;

namespace Cells.Physiology
{
    /// <summary>Unity-independent translation-only resistive-force balance.
    /// D u + f = 0 for a symmetric positive-definite planar drag tensor.
    /// This does not solve rotational balance or fluid back-reaction.</summary>
    public static class PlanarDragBalance
    {
        public static void Solve(double xx, double xy, double yy,
            double forceX, double forceY, out double velocityX, out double velocityY)
        {
            if (!double.IsFinite(xx) || !double.IsFinite(xy) || !double.IsFinite(yy)
                || !double.IsFinite(forceX) || !double.IsFinite(forceY)
                || xx <= 0 || yy <= 0)
                throw new ArgumentOutOfRangeException(nameof(xx), "Finite positive drag required");
            // Scale before inversion to avoid overflow from large drag coefficients.
            double scale = Math.Max(xx, yy);
            double a = xx / scale, b = xy / scale, c = yy / scale;
            double determinant = a * c - b * b;
            if (!(determinant > 1e-12))
                throw new ArgumentOutOfRangeException(nameof(xy), "Drag tensor must be positive definite and well conditioned");
            double fx = forceX / scale, fy = forceY / scale;
            velocityX = (-c * fx + b * fy) / determinant;
            velocityY = (b * fx - a * fy) / determinant;
            if (!double.IsFinite(velocityX) || !double.IsFinite(velocityY))
                throw new ArgumentOutOfRangeException(nameof(forceX), "Unrepresentable swimming velocity");
        }
    }
}
