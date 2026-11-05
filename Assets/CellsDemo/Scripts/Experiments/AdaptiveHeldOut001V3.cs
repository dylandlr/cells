using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Cells.Experiments
{
    /// <summary>Train mutable followers on disjoint seeds and evaluate immutable
    /// copies of their values on held-out world seeds, with matched zero-Q
    /// controls. No genome evolution or learned signaling policy.</summary>
    public static class AdaptiveHeldOut001V3
    {
        private static readonly AdaptiveExperiment001V2.Genome Traits =
            new AdaptiveExperiment001V2.Genome(.18,.12,.75);

        private static SignalingGameV1.IPolicy[] Policies(
            AdaptiveExperiment001V2.AdaptivePolicy[] learners,
            double[][,] frozen=null)
        {
            var p=new SignalingGameV1.IPolicy[4];
            p[0]=new SignalingGameV1.PeriodicSignaler();
            for(int i=1;i<4;i++)
            {
                learners[i]=frozen==null
                    ?new AdaptiveExperiment001V2.AdaptivePolicy(Traits)
                    :new AdaptiveExperiment001V2.AdaptivePolicy(Traits,frozen[i]);
                p[i]=learners[i];
            }
            return p;
        }
        public sealed class Output
        {
            public string EpisodesCsv,PairsCsv,SummaryCsv,ManifestJson;
        }
        public static Output Run(int firstTrainSeed=310000,int trainEpisodes=200,
            int firstTestSeed=410000,int testSeeds=30)
        {
            if(trainEpisodes<1 || trainEpisodes>10000 || testSeeds<2 || testSeeds>10000
                || firstTrainSeed<0 || firstTestSeed<0 ||
                (long)firstTrainSeed+trainEpisodes>int.MaxValue ||
                (long)firstTestSeed+testSeeds>int.MaxValue)
                throw new ArgumentOutOfRangeException("Invalid seeds");
            if((long)firstTrainSeed<firstTestSeed+ (long)testSeeds &&
               (long)firstTestSeed<firstTrainSeed+ (long)trainEpisodes)
                throw new ArgumentException("Train and held-out seed intervals overlap");
            var learners=new AdaptiveExperiment001V2.AdaptivePolicy[4];
            var policies=Policies(learners);
            // Training is continuous across episodes; Q-values persist, environmental
            // world resets per seed. Each episode retains policy RNG seeding.
            for(int episode=0;episode<trainEpisodes;episode++)
                SignalingGameV1.Evaluate(firstTrainSeed+episode,policies,80,true,
                    (step,obs,actions,rewards)=>{
                        for(int i=1;i<4;i++)learners[i].Learn(rewards[i]);
                    });
            var frozen=new double[4][,];
            for(int i=1;i<4;i++)frozen[i]=learners[i].Snapshot();
            var episodes=new StringBuilder("seed,condition,collective_return,signals\n");
            var pairs=new StringBuilder("seed,untrained_on,trained_on,trained_off,trained_minus_untrained,trained_on_minus_off\n");
            var comparison=new List<PairedAssayAnalysisV1.Record>();
            var communication=new List<PairedAssayAnalysisV1.Record>();
            for(int n=0;n<testSeeds;n++)
            {
                int seed=firstTestSeed+n;
                double[] totals=new double[3];
                for(int condition=0;condition<3;condition++)
                {
                    var held=new AdaptiveExperiment001V2.AdaptivePolicy[4];
                    var frozenForCondition=condition==0?new double[4][,]:frozen;
                    if(condition==0)
                        for(int i=1;i<4;i++)frozenForCondition[i]=new double[2,SignalingGameV1.Patches];
                    var controlled=Policies(held,frozenForCondition);
                    var result=SignalingGameV1.Evaluate(seed,controlled,80,condition!=2);
                    foreach(double reward in result.Returns)totals[condition]+=reward;
                    string label=condition==0?"untrained_on":condition==1?"trained_on":"trained_off";
                    episodes.Append(seed).Append(',').Append(label).Append(',')
                        .Append(F(totals[condition])).Append(',')
                        .Append(result.SignalCount).Append('\n');
                }
                comparison.Add(new PairedAssayAnalysisV1.Record(seed,totals[0],totals[1]));
                communication.Add(new PairedAssayAnalysisV1.Record(seed,totals[2],totals[1]));
                pairs.Append(seed).Append(',').Append(F(totals[0]))
                    .Append(',').Append(F(totals[1])).Append(',').Append(F(totals[2]))
                    .Append(',').Append(F(totals[1]-totals[0]))
                    .Append(',').Append(F(totals[1]-totals[2])).Append('\n');
            }
            return new Output{
                EpisodesCsv=episodes.ToString(),PairsCsv=pairs.ToString(),
                SummaryCsv=PairedAssayAnalysisV1.Header+
                    PairedAssayAnalysisV1.ToCsv("trained_minus_untrained",
                        PairedAssayAnalysisV1.EstimatePaired(comparison))+
                    PairedAssayAnalysisV1.ToCsv("trained_on_minus_off",
                        PairedAssayAnalysisV1.EstimatePaired(communication)),
                ManifestJson="{\"schema\":\"cells.adaptive-heldout-001-v3\",\"train_first_seed\":"+
                    firstTrainSeed+",\"train_episodes\":"+trainEpisodes+
                    ",\"test_first_seed\":"+firstTestSeed+",\"test_seeds\":"+testSeeds+
                    ",\"test_updates\":0,\"policy_class\":\"shared_adaptive\","+
                    "\"limitations\":\"symbolic resource game; no learned messaging or genome evolution\"}"
            };
        }
        private static string F(double x)=>x.ToString("R",CultureInfo.InvariantCulture);
    }
}
