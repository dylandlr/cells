#if UNITY_INCLUDE_TESTS
using System;
using NUnit.Framework;
using Cells.Experiments;

namespace Cells.Tests
{
    public sealed class EmpiricalPayoffTensorV1Tests
    {
        [Test]
        public void All256ProfilesRoundTripWithoutCollisions()
        {
            for (int i = 0; i < EmpiricalPayoffTensorV1.Profiles; i++)
                Assert.AreEqual(i, EmpiricalPayoffTensorV1.Encode(
                    EmpiricalPayoffTensorV1.Decode(i)));
        }

        [Test]
        public void CompleteTensorHasFourSeatPayoffsForEveryProfile()
        {
            var tensor = EmpiricalPayoffTensorV1.Build(2000, 2);
            Assert.AreEqual(512, tensor.EpisodeCount);
            Assert.AreEqual(257, tensor.Csv().TrimEnd().Split('\n').Length);
            for (int p = 0; p < 256; p++)
                for (int seat = 0; seat < 4; seat++)
                {
                    double payoff = tensor.Payoff(p, seat);
                    Assert.IsFalse(double.IsNaN(payoff) || double.IsInfinity(payoff));
                }
        }

        [Test]
        public void PureProfileExpectationMatchesIndexedPayoff()
        {
            var tensor = EmpiricalPayoffTensorV1.Build(2100, 2);
            var pure = EmpiricalPayoffTensorV1.Homogeneous(2);
            int id = EmpiricalPayoffTensorV1.Encode(new[] { 2, 2, 2, 2 });
            var expected = tensor.ExpectedPayoffs(pure);
            for (int seat = 0; seat < 4; seat++)
                Assert.That(expected[seat], Is.EqualTo(tensor.Payoff(id, seat)).Within(1e-9));
        }

        [Test]
        public void RestrictedDeviationCannotProduceNegativeGain()
        {
            var tensor = EmpiricalPayoffTensorV1.Build(2100, 2);
            var gains = tensor.RestrictedDeviationGains(EmpiricalPayoffTensorV1.Uniform());
            foreach (double gain in gains)
                Assert.GreaterOrEqual(gain, 0);
        }

        [Test]
        public void IdenticalSeedRangesReproduceTheSameTensor()
        {
            var a = EmpiricalPayoffTensorV1.Build(2300, 2);
            var b = EmpiricalPayoffTensorV1.Build(2300, 2);
            Assert.AreEqual(a.Csv(), b.Csv());
        }

        [Test]
        public void MalformedMixturesAreRejected()
        {
            var tensor = EmpiricalPayoffTensorV1.Build(2400, 2);
            var bad = EmpiricalPayoffTensorV1.Uniform();
            bad[0][0] = double.NaN;
            Assert.Throws<ArgumentException>(() => tensor.ExpectedPayoffs(bad));
            bad = EmpiricalPayoffTensorV1.Uniform();
            bad[1] = new[] { 1d, 1d, 0d, 0d };
            Assert.Throws<ArgumentException>(() => tensor.RestrictedDeviationGains(bad));
        }
    }
}
#endif
