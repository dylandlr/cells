using NUnit.Framework;
using Cells.Experiments;

namespace Cells.Tests
{
    public sealed class ChemotaxisAssayPlanV1Tests
    {
        [Test]
        public void EachSeedReceivesEveryControllerExactlyOnce()
        {
            var rows=ChemotaxisAssayPlanV1.Build(631,4);
            Assert.AreEqual(12,rows.Length);
            for(int i=0;i<4;i++)
            {
                Assert.AreEqual(631+i,rows[i*3].seed);
                Assert.AreEqual(631+i,rows[i*3+1].seed);
                Assert.AreEqual(631+i,rows[i*3+2].seed);
                var found = new System.Collections.Generic.HashSet<string> {
                    rows[i*3].controller, rows[i*3+1].controller, rows[i*3+2].controller
                };
                Assert.AreEqual(3,found.Count);
                Assert.IsTrue(found.Contains(ChemotaxisAssayPlanV1.Spatial));
                Assert.IsTrue(found.Contains(ChemotaxisAssayPlanV1.Temporal));
                Assert.IsTrue(found.Contains(ChemotaxisAssayPlanV1.Stochastic));
                Assert.AreEqual(
                    new [] {ChemotaxisAssayPlanV1.Spatial,ChemotaxisAssayPlanV1.Temporal,ChemotaxisAssayPlanV1.Stochastic}[(i)%3],
                    rows[i*3].controller);
            }
        }

        [Test]
        public void ProtocolCsvIsDeterministicAndMarksRequiredResets()
        {
            var a=ChemotaxisAssayPlanV1.Csv(ChemotaxisAssayPlanV1.Build(31,2));
            var b=ChemotaxisAssayPlanV1.Csv(ChemotaxisAssayPlanV1.Build(31,2));
            Assert.AreEqual(a,b);
            StringAssert.Contains("31,spatial,true",a);
            StringAssert.Contains("32,stochastic,true",a);
        }

        [Test]
        public void OutputsDoNotPretendTrialsHaveBeenExecuted()
        {
            var planned=ChemotaxisAssayPlanV1.Build(1,2);
            var csv=ChemotaxisAssayPlanV1.OutcomeCsv(new ChemotaxisAssayPlanV1.Outcome[0]);
            Assert.AreEqual(6,planned.Length);
            Assert.AreEqual(1,csv.TrimEnd().Split('\n').Length);
        }
    }
}
