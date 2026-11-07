using System;

namespace Cells.Physiology
{
    /// <summary>
    /// Force- and torque-free planar resistive-force balance for a swimmer.
    /// R [vx, vy, omega]^T = -[fx, fy, torque]^T.
    /// Uses Cholesky factorization of a symmetric positive definite resistance matrix.
    /// No Unity dependencies; coefficients and angular velocity are in consistent local units.
    /// </summary>
    public static class PlanarMobilityBalance
    {
        public static void Solve(
            double xx, double xy, double xw, double yy, double yw, double ww,
            double fx, double fy, double torque,
            out double vx, out double vy, out double omega)
        {
            double[] input = {xx, xy, xw, yy, yw, ww, fx, fy, torque};
            foreach (double v in input)
                if (!double.IsFinite(v))
                    throw new ArgumentOutOfRangeException(nameof(xx), "Finite inputs required");
            if (xx <= 0 || yy <= 0 || ww <= 0)
                throw new ArgumentOutOfRangeException(nameof(xx), "Positive diagonal resistance required");

            double scale = Math.Max(xx, Math.Max(yy, ww));
            double a = xx / scale, b = xy / scale, c = xw / scale;
            double d = yy / scale, e = yw / scale, f = ww / scale;
            double l00 = Math.Sqrt(a);
            double l10 = b / l00, l20 = c / l00;
            double pivot1 = d - l10 * l10;
            if (!(pivot1 > 1e-12))
                throw new ArgumentOutOfRangeException(nameof(xy), "Resistance must be positive definite");
            double l11 = Math.Sqrt(pivot1);
            double l21 = (e - l20 * l10) / l11;
            double pivot2 = f - l20 * l20 - l21 * l21;
            if (!(pivot2 > 1e-12))
                throw new ArgumentOutOfRangeException(nameof(xw), "Resistance must be positive definite");
            double l22 = Math.Sqrt(pivot2);

            double y0 = (-fx / scale) / l00;
            double y1 = (-fy / scale - l10 * y0) / l11;
            double y2 = (-torque / scale - l20 * y0 - l21 * y1) / l22;
            omega = y2 / l22;
            vy = (y1 - l21 * omega) / l11;
            vx = (y0 - l10 * vy - l20 * omega) / l00;
            if (!double.IsFinite(vx) || !double.IsFinite(vy) || !double.IsFinite(omega))
                throw new ArgumentOutOfRangeException(nameof(fx), "Unrepresentable movement");
        }
    }
}
