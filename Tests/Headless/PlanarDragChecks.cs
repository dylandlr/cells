using System;
using Cells.Physiology;

internal static class PlanarDragChecks
{
    public static int Run()
    {
        int checks = 0;
        void Near(double actual, double expected, string name)
        {
            if (Math.Abs(actual - expected) > 1e-9 * Math.Max(1, Math.Abs(expected)))
                throw new Exception(name + ": " + actual + " != " + expected);
            checks++;
        }
        PlanarDragBalance.Solve(2, 0, 4, 6, -8, out double x, out double y);
        Near(x, -3, "diagonal x"); Near(y, 2, "diagonal y");
        PlanarDragBalance.Solve(2, 1, 2, 3, 0, out x, out y);
        Near(x, -2, "coupled x"); Near(y, 1, "coupled y");
        PlanarDragBalance.Solve(2, 1, 2, 0, 0, out x, out y);
        Near(x, 0, "zero stroke x"); Near(y, 0, "zero stroke y");
        var rng = new Random(631);
        for (int i = 0; i < 1000; i++)
        {
            double a = .1 + rng.NextDouble() * 5, c = .1 + rng.NextDouble() * 5;
            double angle = rng.NextDouble() * 2 * Math.PI;
            double co = Math.Cos(angle), si = Math.Sin(angle);
            double fx = rng.NextDouble() * 4 - 2, fy = rng.NextDouble() * 4 - 2;
            double xx = a * co * co + c * si * si;
            double xy = (a - c) * co * si;
            double yy = a * si * si + c * co * co;
            double rotatedX = co * fx - si * fy, rotatedY = si * fx + co * fy;
            PlanarDragBalance.Solve(xx, xy, yy, rotatedX, rotatedY, out x, out y);
            Near(xx * x + xy * y + rotatedX, 0, "force residual x");
            Near(xy * x + yy * y + rotatedY, 0, "force residual y");
            Near(x, -co * fx / a + si * fy / c, "rotation covariance x");
            Near(y, -si * fx / a - co * fy / c, "rotation covariance y");
        }
        foreach (var bad in new[] { new[] { 0d, 0, 1 }, new[] { 1d, 2, 1 },
            new[] { double.NaN, 0, 1 }, new[] { 1d, 1, 1 } })
        {
            bool rejected = false;
            try { PlanarDragBalance.Solve(bad[0], bad[1], bad[2], 1, 1, out x, out y); }
            catch (ArgumentOutOfRangeException) { rejected = true; }
            if (!rejected) throw new Exception("Invalid drag accepted");
            checks++;
        }
        Console.WriteLine("Planar drag balance: " + checks + " analytic/residual/rotation checks passed");
        return checks;
    }
}
