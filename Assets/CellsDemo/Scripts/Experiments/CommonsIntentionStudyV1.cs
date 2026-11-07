using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
namespace Cells.Experiments
{
    // Exploratory intention-coordination assay on conserved commons.
    // All adaptive conditions have the same seeded periodic sender, and
    // reward learning is isolated from its update-disabled twin.
    public static class CommonsIntentionStudyV1
    {
        private sealed class Follower
        {
            private readonly double[,] q=new double[4,2];
            private readonly bool learn;
            private int state,choice;
            public int Accepted {get;private set;}
            public Follower(bool learnEnabled){learn=learnEnabled;}
            public int Decide(RenewableCommonsV0.View view,Random rng,bool readMessages)
            {
                int advertised=-1;
                if(readMessages)
                    for(int p=0;p<3;p++)
                        if(view.Heard[p]>0){advertised=p;break;}
                state=(advertised>=0?2:0)+(view.LocalStock<=2?1:0);
                choice=rng.NextDouble()<.15?rng.Next(2):
                    (q[state,1]>q[state,0]?1:0);
                if(choice==1 && advertised>=0){Accepted++;return 3+advertised;}
                return view.Location; // harvest locally
            }
            public void Update(double reward)
            {
                if(learn)q[state,choice]+=.15*(reward-q[state,choice]);
            }
        }
        public sealed class Output
        {
            public string EpisodesCsv,SummaryCsv,ManifestJson;
        }
        public static Output Run(int firstSeed=810000,int seeds=30,int horizon=100)
        {
            if(firstSeed<0||seeds<2||seeds>5000||horizon<1||horizon>1000||
               (long)firstSeed+seeds>int.MaxValue)throw new ArgumentException("Invalid experiment scope");
            var csv=new StringBuilder("seed,condition,return,successful_repairs,accepted_intentions,survivors,remaining_reserve\n");
            var samples=new List<PairedAssayAnalysisV1.Record>();
            for(int index=0;index<seeds;index++)
            {
                int seed=firstSeed+index;
                var pay=new double[4];
                for(int condition=0;condition<4;condition++)
                {
                    var world=new RenewableCommonsV0(seed,condition!=2);
                    var policies=new Follower[4];
                    var rng=new Random[4];
                    for(int i=1;i<4;i++)
                    {policies[i]=new Follower(condition==0 || condition==2);
                     rng[i]=new Random(unchecked(seed*7919+i*104729));}
                    int repairs=0;
                    for(int step=0;step<horizon;step++)
                    {
                        var acts=new RenewableCommonsV0.Act[4];
                        var leader=world.Observe(0);
                        // Sender uses its local scarcity to advertise restoration
                        // intentions; its own act remains scripted, not learned.
                        acts[0]=new RenewableCommonsV0.Act(!leader.Alive?9:
                            step%4==0 && leader.LocalStock<=2 ? 6+leader.Location:
                            leader.LocalStock<=2?3+leader.Location:leader.Location);
                        for(int i=1;i<4;i++)
                        {
                            var v=world.Observe(i);
                            int action=!v.Alive?9:condition==3?
                                (v.Heard[0]>0?3: v.Location):
                                policies[i].Decide(v,rng[i],condition!=2);
                            acts[i]=new RenewableCommonsV0.Act(action);
                        }
                        var result=world.Advance(acts);
                        repairs+=result.SuccessfulRepairs;
                        if(condition!=3)
                            for(int i=1;i<4;i++)
                                if(world.Observe(i).Alive)policies[i].Update(result.Rewards[i]);
                    }
                    foreach(double v in world.Returns())pay[condition]+=v;
                    int survivors=0,accepted=0;
                    for(int i=0;i<4;i++)
                    {if(world.Observe(i).Alive)survivors++;
                     if(i>0 && condition!=3)accepted+=policies[i].Accepted;}
                    csv.Append(seed).Append(',').Append(condition==0?"learning_on":
                        condition==1?"frozen_on":condition==2?"learning_off":"scripted_on")
                        .Append(',').Append(F(pay[condition])).Append(',').Append(repairs)
                        .Append(',').Append(accepted).Append(',').Append(survivors)
                        .Append(',').Append(world.SubstrateReserve).Append('\n');
                }
                samples.Add(new PairedAssayAnalysisV1.Record(seed,pay[1],pay[0]));
            }
            return new Output{
                EpisodesCsv=csv.ToString(),
                SummaryCsv=PairedAssayAnalysisV1.Header+
                    PairedAssayAnalysisV1.ToCsv("learning_on_minus_frozen_on",
                        PairedAssayAnalysisV1.EstimatePaired(samples)),
                ManifestJson="{\"schema\":\"cells.commons-intention-v1\",\"first_seed\":"+
                    firstSeed+",\"seeds\":"+seeds+",\"horizon\":"+horizon+
                    ",\"sender\":\"scripted\",\"learning\":\"online-immediate-reward\"}"
            };
        }
        private static string F(double x)=>x.ToString("R",CultureInfo.InvariantCulture);
    }
}
