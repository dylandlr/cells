using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Cells.Experiments
{
    /// <summary>Exploratory commons baseline comparison: no claim of emergent
    /// cooperation. Fixed-state tabular reward learning across episodes.</summary>
    public static class CommonsLearningV1
    {
        private sealed class Controller
        {
            private readonly double[,] values=new double[3,10];
            private readonly bool updates;
            private int state,action;
            public Controller(bool learn){updates=learn;}
            public int Decide(RenewableCommonsV0.View v,Random rng)
            {
                state=v.LocalStock==0?0:v.LocalStock<=2?1:2;
                if(rng.NextDouble()<.15){action=rng.Next(10);return action;}
                double best=double.NegativeInfinity;
                action=0;
                for(int a=0;a<10;a++)
                    if(values[state,a]>best){best=values[state,a];action=a;}
                return action;
            }
            public void Learn(double reward)
            {
                if(updates)values[state,action]+=.15*(reward-values[state,action]);
            }
        }
        public sealed class Output
        {
            public string EpisodesCsv,SummaryCsv,ManifestJson;
        }
        public static Output Run(int firstSeed=710000,int seeds=30,int horizon=100)
        {
            if(firstSeed<0||seeds<2||seeds>5000||horizon<1||horizon>1000
                ||(long)firstSeed+seeds>int.MaxValue)throw new ArgumentException("Invalid study scope");
            var csv=new StringBuilder("seed,condition,collective_return,repairs,harvests,final_stock,alive,reserve_remaining\n");
            var records=new List<PairedAssayAnalysisV1.Record>();
            for(int n=0;n<seeds;n++)
            {
                int seed=firstSeed+n;
                double[] totals=new double[4];
                for(int condition=0;condition<4;condition++)
                {
                    var world=new RenewableCommonsV0(seed,true);
                    var agents=new Controller[4];
                    var rng=new Random[4];
                    for(int seat=0;seat<4;seat++)
                    {agents[seat]=new Controller(condition==0);rng[seat]=new Random(unchecked(seed*7919+seat*104729));}
                    int repairs=0,harvests=0;
                    for(int t=0;t<horizon;t++)
                    {
                        var actions=new RenewableCommonsV0.Act[4];
                        for(int seat=0;seat<4;seat++)
                        {
                            var v=world.Observe(seat);
                            int action;
                            if(!v.Alive)action=9;
                            else if(condition==2)action=v.Location;
                            else if(condition==3)
                                action=v.LocalStock<=2?3: v.Location;
                            else action=agents[seat].Decide(v,rng[seat]);
                            actions[seat]=new RenewableCommonsV0.Act(action);
                        }
                        var trace=world.Advance(actions);
                        repairs+=trace.SuccessfulRepairs;
                        harvests+=trace.HarvestedUnits;
                        if(condition<2)
                            for(int seat=0;seat<4;seat++)agents[seat].Learn(trace.Rewards[seat]);
                    }
                    foreach(double value in world.Returns())totals[condition]+=value;
                    int stocks=0,alive=0;
                    foreach(int value in world.Stocks())stocks+=value;
                    for(int seat=0;seat<4;seat++)if(world.Observe(seat).Alive)alive++;
                    string label=condition==0?"learner":condition==1?"frozen":
                        condition==2?"greedy":"scheduled_repair";
                    csv.Append(seed).Append(',').Append(label).Append(',')
                        .Append(F(totals[condition])).Append(',').Append(repairs)
                        .Append(',').Append(harvests).Append(',').Append(stocks)
                        .Append(',').Append(alive).Append(',')
                        .Append(world.SubstrateReserve).Append('\n');
                }
                records.Add(new PairedAssayAnalysisV1.Record(seed,totals[1],totals[0]));
            }
            return new Output{
                EpisodesCsv=csv.ToString(),
                SummaryCsv=PairedAssayAnalysisV1.Header+
                    PairedAssayAnalysisV1.ToCsv("learner_minus_frozen",
                        PairedAssayAnalysisV1.EstimatePaired(records)),
                ManifestJson="{\"schema\":\"cells.commons.learning-v1\",\"first_seed\":"+
                    firstSeed+",\"seeds\":"+seeds+",\"horizon\":"+horizon+
                    ",\"conditions\":4,\"evaluation\":\"online-learning-exploratory\","+
                    "\"limitations\":\"no learned communication; policy has only three local-stock states\"}"
            };
        }
        private static string F(double x)=>x.ToString("R",CultureInfo.InvariantCulture);
    }
}
