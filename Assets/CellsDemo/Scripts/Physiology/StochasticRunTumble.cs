using System;

namespace Cells.Physiology
{
    /// <summary>Seeded Bernoulli tumble decisions based on a bounded instantaneous hazard.
    /// Decision probability is 1-exp(-rate*dt), preserving behavior across time steps.</summary>
    public sealed class StochasticRunTumble
    {
        readonly Random rng;
        public double baselineRate = .12;
        public double unfavorableRateGain = 1.25;
        public double tumbleAngleRadians = Math.PI * .65;
        public int TumbleCount { get; private set; }
        public double LastHazard { get; private set; }
        public StochasticRunTumble(int seed) { rng = new Random(seed); }
        public bool Step(double receptorResponse,double dt,out double turnRadians)
        {
            turnRadians=0;
            if (!double.IsFinite(receptorResponse) || !double.IsFinite(dt) || dt<=0) return false;
            LastHazard=Math.Max(0,baselineRate)+Math.Max(0,unfavorableRateGain)*Math.Max(0,-receptorResponse);
            double p=-Math.Expm1(-LastHazard*dt);
            if (rng.NextDouble()>=p) return false;
            turnRadians=(rng.NextDouble()*2-1)*Math.Max(0,tumbleAngleRadians);
            TumbleCount++;
            return true;
        }
    }
}
