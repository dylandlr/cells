using NUnit.Framework;
using Cells.Experiments;

namespace Cells.Tests.Editor
{
    public sealed class SignalingGameV1Tests
    {
        private static SignalingGameV1.IPolicy[] Four(SignalingGameV1.IPolicy policy)
        {
            return new SignalingGameV1.IPolicy[] { policy, policy, policy, policy };
        }

        [Test]
        public void SameSeedAndPoliciesProduceIdenticalReturns()
        {
            var a = SignalingGameV1.Evaluate(631, Four(new SignalingGameV1.RandomPolicy()));
            var b = SignalingGameV1.Evaluate(631, Four(new SignalingGameV1.RandomPolicy()));
            CollectionAssert.AreEqual(a.Returns, b.Returns);
            CollectionAssert.AreEqual(a.FinalPatches, b.FinalPatches);
            Assert.AreEqual(a.TotalHarvest, b.TotalHarvest);
            Assert.AreEqual(80, a.Steps);
        }

        [Test]
        public void SignalArrivesAtNextDecisionAndCostsEnergy()
        {
            var game = new SignalingGameV1(12);
            Assert.AreEqual(0, game.Observe(1).ReceivedSignals[2]);
            game.Advance(new[] {
                new SignalingGameV1.Action(5), // signal patch 2
                new SignalingGameV1.Action(6),
                new SignalingGameV1.Action(6),
                new SignalingGameV1.Action(6)
            });
            Assert.AreEqual(1, game.Observe(1).ReceivedSignals[2]);
            Assert.AreEqual(-SignalingGameV1.SignalCost, game.Summary().Returns[0], 1e-9);
            game.Advance(new[] {
                new SignalingGameV1.Action(6),
                new SignalingGameV1.Action(6),
                new SignalingGameV1.Action(6),
                new SignalingGameV1.Action(6)
            });
            Assert.AreEqual(0, game.Observe(1).ReceivedSignals[2]);
        }

        [Test]
        public void ForagingAtSamePatchDividesYieldAmongAgents()
        {
            var one = new SignalingGameV1(97, 1);
            one.Advance(new[] {
                new SignalingGameV1.Action(0), new SignalingGameV1.Action(6),
                new SignalingGameV1.Action(6), new SignalingGameV1.Action(6)
            });
            var together = new SignalingGameV1(97, 1);
            together.Advance(new[] {
                new SignalingGameV1.Action(0), new SignalingGameV1.Action(0),
                new SignalingGameV1.Action(6), new SignalingGameV1.Action(6)
            });
            Assert.Less(together.Summary().Returns[0], one.Summary().Returns[0]);
            Assert.AreEqual(1, together.Summary().Steps);
        }

        [Test]
        public void ObservationDoesNotRevealOtherAgentsEnergyOrHiddenYield()
        {
            var game = new SignalingGameV1(32);
            var observation = game.Observe(0);
            Assert.AreEqual(3, observation.LocalCue.Length);
            Assert.AreEqual(-1, observation.LocalCue[1]);
            Assert.AreEqual(-1, observation.LocalCue[2]);
            Assert.AreEqual(0, observation.Agent);
        }

        [Test]
        public void EpisodeEndsAfterExactlyConfiguredDecisions()
        {
            var game = new SignalingGameV1(631, 2);
            var rest = new[] {
                new SignalingGameV1.Action(6), new SignalingGameV1.Action(6),
                new SignalingGameV1.Action(6), new SignalingGameV1.Action(6)
            };
            game.Advance(rest);
            game.Advance(rest);
            Assert.IsTrue(game.IsFinished);
            Assert.Throws<System.InvalidOperationException>(() => game.Advance(rest));
        }

        [Test]
        public void InvalidActionsArePenalizedAndCounted()
        {
            var game = new SignalingGameV1(631, 1);
            game.Advance(new[] {
                new SignalingGameV1.Action(-1), new SignalingGameV1.Action(6),
                new SignalingGameV1.Action(6), new SignalingGameV1.Action(6)
            });
            Assert.AreEqual(1, game.Summary().InvalidActions);
            Assert.AreEqual(-1.0, game.Summary().Returns[0], 1e-9);
        }
    }
}
