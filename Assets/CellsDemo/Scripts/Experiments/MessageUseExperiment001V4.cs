using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Cells.Experiments
{
    /// <summary>
    /// Exploratory learnable message-use experiment. Signal sender fixed.
    /// Followers learn a binary trust/ignore decision by rewarded bandit
    /// updates. This is not learned message production or evolution.
    /// </summary>
    public static class MessageUseExperiment001V4
    {
        private sealed class TrustPolicy : SignalingGameV1.IPolicy
        {
            private readonly bool update;
            private readonly double[,] q=new double[2,2];
            private int lastContext,lastChoice;
            public int TrustDecisions {get;private set;}
            public int Exposures {get;private set;}
            public TrustPolicy(bool updateEnabled,double[,] initial=null)
            {
                update=updateEnabled;
                if(initial!=null)
                    for(int c=0;c<2;c++)for(int a=0;a<2;a++)q[c,a]=initial[c,a];
            }
            public double[,] Snapshot()=> (double[,])q.Clone();
            public SignalingGameV1.Action Decide(SignalingGameV1.Observation o,Random rng)
            {
                int incoming=0,signaled=-1;
                for(int p=0;p<3;p++)
                    if(o.ReceivedSignals[p]>incoming)
                    {incoming=o.ReceivedSignals[p];signaled=p;}
                int context=incoming>0?1:0;
                int choice=context==0?0:
                    rng.NextDouble()<0.12?rng.Next(2):
                    (q[context,1]>q[context,0]?1:0);
                int target=o.OwnPatch;
                int bestCue=o.LocalCue[target];
                for(int p=0;p<3;p++)
                    if(o.LocalCue[p]>bestCue){target=p;bestCue=o.LocalCue[p];}
                if(context==1)
                {
                    Exposures++;
                    if(choice==1){TrustDecisions++;target=signaled;}
                }
                lastContext=context;lastChoice=choice;
                return new SignalingGameV1.Action(target);
            }
            public void Learn(double reward)
            {
                if(update && lastContext==1)
                    q[1,lastChoice]+=.18*(reward-q[1,lastChoice]);
            }
        }
        public sealed class Output
        {
            public string EpisodesCsv,SummaryCsv,ManifestJson;
        }
        public static Output Run(int trainStart=510000,int trainEpisodes=200,
            int testStart=610000,int testSeeds=30)
        {
            if(trainEpisodes<1 || trainEpisodes>10000 || testSeeds<2 || testSeeds>10000 ||
               trainStart<0 || testStart<0 ||
               (long)trainStart+trainEpisodes>int.MaxValue ||
               (long)testStart+testSeeds>int.MaxValue ||
               ((long)trainStart<testStart+(long)testSeeds &&
                (long)testStart<trainStart+(long)trainEpisodes))
                throw new ArgumentException("Invalid or overlapping train/test seeds");
            var followers=new TrustPolicy[4];
            var policies=new SignalingGameV1.IPolicy[4];
            policies[0]=new SignalingGameV1.PeriodicSignaler();
            for(int i=1;i<4;i++){followers[i]=new TrustPolicy(true);policies[i]=followers[i];}
            for(int n=0;n<trainEpisodes;n++)
                SignalingGameV1.Evaluate(trainStart+n,policies,80,true,
                    (step,obs,actions,rewards)=>{
                        for(int i=1;i<4;i++)followers[i].Learn(rewards[i]);
                    });
            var frozen=new double[4][,];
            for(int i=1;i<4;i++)frozen[i]=followers[i].Snapshot();
            var rows=new StringBuilder("seed,condition,collective_return,exposed_decisions,trust_decisions\n");
            var paired=new List<PairedAssayAnalysisV1.Record>();
            for(int n=0;n<testSeeds;n++)
            {
                int seed=testStart+n;
                double[] totals=new double[3];
                for(int condition=0;condition<3;condition++)
                {
                    var local=new TrustPolicy[4];
                    var controls=new SignalingGameV1.IPolicy[4];
                    controls[0]=new SignalingGameV1.PeriodicSignaler();
                    for(int seat=1;seat<4;seat++)
                    {
                        local[seat]=new TrustPolicy(false,condition==0?null:frozen[seat]);
                        controls[seat]=local[seat];
                    }
                    bool delivery=condition!=2;
                    var result=SignalingGameV1.Evaluate(seed,controls,80,delivery);
                    foreach(double value in result.Returns)totals[condition]+=value;
                    int exposed=0,trust=0;
                    for(int seat=1;seat<4;seat++)
                    {exposed+=local[seat].Exposures;trust+=local[seat].TrustDecisions;}
                    rows.Append(seed).Append(',').Append(condition==0?"untrained_on":
                        condition==1?"trained_on":"trained_off").Append(',')
                        .Append(F(totals[condition])).Append(',')
                        .Append(exposed).Append(',').Append(trust).Append('\n');
                }
                paired.Add(new PairedAssayAnalysisV1.Record(seed,totals[0],totals[1]));
            }
            return new Output{
                EpisodesCsv=rows.ToString(),
                SummaryCsv=PairedAssayAnalysisV1.Header+
                    PairedAssayAnalysisV1.ToCsv("trained_message_use_minus_untrained",
                        PairedAssayAnalysisV1.EstimatePaired(paired)),
                ManifestJson="{\"schema\":\"cells.learn-message-use-v4\",\"train_episodes\":"+
                    trainEpisodes+",\"test_seeds\":"+testSeeds+
                    ",\"train_start\":"+trainStart+",\"test_start\":"+testStart+
                    ",\"test_updates\":0,\"sender\":\"fixed_scripted\"}"
            };
        }
        private static string F(double x)=>x.ToString("R",CultureInfo.InvariantCulture);
    }
}
