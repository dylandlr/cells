using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Cells.Experiments
{
    // Experimental discounted-return learner. Delayed individual rewards are
    // credited to preceding decisions within each episode (Monte Carlo).
    // Explicit intention features, no engineered pro-cooperation reward.
    public static class CommonsDelayedLearningV1
    {
        private sealed class Policy
        {
            private readonly double[,] q=new double[8,2];
            private readonly bool update;
            private readonly bool immediate;
            private readonly List<int> states=new List<int>();
            private readonly List<int> choices=new List<int>();
            private readonly List<double> rewards=new List<double>();
            public int Responses {get;private set;}
            public Policy(bool train,bool useImmediate=false){update=train;immediate=useImmediate;}
            public Policy(double[,] snapshot)
            {
                update=false;
                Array.Copy(snapshot,q,snapshot.Length);
            }
            public double[,] Snapshot()=> (double[,])q.Clone();
            public int Decide(RenewableCommonsV0.View v,Random rng)
            {
                int advertised=-1;
                for(int p=0;p<3;p++)if(v.Heard[p]>0){advertised=p;break;}
                // locality, scarcity and an intention visible at this step.
                int state=(v.LocalStock<=2?1:0)+(advertised>=0?2:0)+
                    (v.Energy<2.5?4:0);
                int action=rng.NextDouble()<.12?rng.Next(2):
                    q[state,1]>q[state,0]?1:0;
                states.Add(state);choices.Add(action);
                if(action==1 && advertised>=0)
                {Responses++;return 3+advertised;}
                return v.Location;
            }
            public void Reward(double reward){rewards.Add(reward);}
            public void Finish()
            {
                if(update)
                {
                    double future=0;
                    for(int t=rewards.Count-1;t>=0;t--)
                    {
                        future=immediate?rewards[t]:rewards[t]+.95*future;
                        int s=states[t],a=choices[t];
                        q[s,a]+=.08*(future-q[s,a]);
                    }
                }
                states.Clear();choices.Clear();rewards.Clear();
            }
        }
        public sealed class Output
        {
            public string EpisodesCsv,SummaryCsv,ManifestJson;
        }
        public static Output Run(int trainStart=910000,int trainEpisodes=400,
            int testStart=1010000,int testSeeds=30,int horizon=100,
            bool useImmediateReward=false)
        {
            if(trainStart<0||testStart<0||trainEpisodes<1||trainEpisodes>5000||
                testSeeds<2||testSeeds>1000||horizon<1||horizon>1000||
                (long)trainStart+trainEpisodes>int.MaxValue||
                (long)testStart+testSeeds>int.MaxValue||
                ((long)trainStart<testStart+(long)testSeeds &&
                 (long)testStart<trainStart+(long)trainEpisodes))
                throw new ArgumentException("Invalid or overlapping seeds");
            var trained=new Policy[4];
            for(int i=1;i<4;i++)trained[i]=new Policy(true,useImmediateReward);
            for(int episode=0;episode<trainEpisodes;episode++)
            {
                var env=new RenewableCommonsV0(trainStart+episode,true);
                var rng=Generators(trainStart+episode);
                Simulate(env,trained,rng,horizon,true);
                for(int i=1;i<4;i++)trained[i].Finish();
            }
            var frozen=new double[4][,];
            for(int i=1;i<4;i++)frozen[i]=trained[i].Snapshot();
            var csv=new StringBuilder("seed,condition,collective_return,successful_repairs,accepted_intentions,survivors,remaining_reserve\n");
            var paired=new List<PairedAssayAnalysisV1.Record>();
            for(int n=0;n<testSeeds;n++)
            {
                int seed=testStart+n;
                double[] returns=new double[3];
                for(int variant=0;variant<3;variant++)
                {
                    var controllers=new Policy[4];
                    for(int i=1;i<4;i++)controllers[i]=
                        variant==0?new Policy(new double[8,2]):new Policy(frozen[i]);
                    var env=new RenewableCommonsV0(seed,variant!=2);
                    var result=Simulate(env,controllers,Generators(seed),horizon,false);
                    returns[variant]=result.Return;
                    csv.Append(seed).Append(',').Append(variant==0?"untrained_on":
                        variant==1?"trained_on":"trained_off").Append(',')
                        .Append(F(result.Return)).Append(',')
                        .Append(result.Repairs).Append(',').Append(result.Responses)
                        .Append(',').Append(result.Survivors).Append(',')
                        .Append(env.SubstrateReserve).Append('\n');
                }
                paired.Add(new PairedAssayAnalysisV1.Record(seed,returns[0],returns[1]));
            }
            return new Output{
                EpisodesCsv=csv.ToString(),
                SummaryCsv=PairedAssayAnalysisV1.Header+
                    PairedAssayAnalysisV1.ToCsv(useImmediateReward?"immediate_trained_minus_untrained":"delayed_trained_minus_untrained",
                        PairedAssayAnalysisV1.EstimatePaired(paired)),
                ManifestJson="{\"schema\":\"cells.commons-delayed-v1\",\"train_start\":"+
                    trainStart+",\"train_episodes\":"+trainEpisodes+
                    ",\"test_start\":"+testStart+",\"test_seeds\":"+testSeeds+
                    ",\"gamma\":0.95,\"test_updates\":0,\"horizon\":"+horizon+"}"
            };
        }
        private sealed class Outcome
        {
            public double Return;
            public int Repairs,Responses,Survivors;
        }
        private static Random[] Generators(int seed)
        {
            var r=new Random[4];
            for(int i=0;i<4;i++)r[i]=new Random(unchecked(seed*7919+i*104729));
            return r;
        }
        private static Outcome Simulate(RenewableCommonsV0 env,Policy[] agents,
            Random[] rng,int horizon,bool training)
        {
            int repairs=0;
            for(int t=0;t<horizon;t++)
            {
                var actions=new RenewableCommonsV0.Act[4];
                var lead=env.Observe(0);
                actions[0]=new RenewableCommonsV0.Act(!lead.Alive?9:
                    t%4==0&&lead.LocalStock<=2?6+lead.Location:
                    lead.LocalStock<=2?3+lead.Location:lead.Location);
                bool[] acted=new bool[4];
                for(int i=1;i<4;i++)
                {
                    var v=env.Observe(i);
                    if(v.Alive)
                    {acted[i]=true;actions[i]=new RenewableCommonsV0.Act(agents[i].Decide(v,rng[i]));}
                    else actions[i]=new RenewableCommonsV0.Act(9);
                }
                var log=env.Advance(actions);
                repairs+=log.SuccessfulRepairs;
                if(training)
                    for(int i=1;i<4;i++)if(acted[i])agents[i].Reward(log.Rewards[i]);
            }
            var outcome=new Outcome{Repairs=repairs};
            foreach(double v in env.Returns())outcome.Return+=v;
            for(int i=0;i<4;i++)
            {
                if(env.Observe(i).Alive)outcome.Survivors++;
                if(i>0)outcome.Responses+=agents[i].Responses;
            }
            return outcome;
        }
        private static string F(double value)=>value.ToString("R",CultureInfo.InvariantCulture);
    }
}
