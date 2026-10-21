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
            public string EpisodesCsv,SummaryCsv,ManifestJson,TrainingCsv,CheckpointsCsv,FirstEpisodeQCsv,SenderChallengesCsv,DeliveryChallengesCsv,ExchangeTimeSeriesCsv,ProbabilisticExchangeCsv,ReliabilityMatrixCsv;
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
            // Train once per treatment using the same world seeds and
            // independent communication RNG; evaluate all on common held-out seeds.
            foreach(int trainRate in new[]{100,90,75,60,50})
            {
                var cohort=new Policy[4];
                for(int seat=1;seat<4;seat++)cohort[seat]=new Policy(true);
                for(int ep=0;ep<trainEpisodes;ep++)
                {
                    int seed=trainStart+ep;
                    Simulate(new RenewableCommonsV0(seed,true,1,trainRate/100.0),
                        cohort,Generators(seed),horizon,true);
                    for(int seat=1;seat<4;seat++)cohort[seat].Finish();
                }
                var snapshots=new double[4][,];
                for(int seat=1;seat<4;seat++)snapshots[seat]=cohort[seat].Snapshot();
                foreach(int testRate in new[]{25,40,50,60,75,90,100})
                    for(int n=0;n<testSeeds;n++)
                    {
                        int seed=testStart+n;
                        var frozenCohort=new Policy[4];
                        for(int seat=1;seat<4;seat++)frozenCohort[seat]=new Policy(snapshots[seat]);
                        var measurement=Simulate(new RenewableCommonsV0(seed,true,1,testRate/100.0),
                            frozenCohort,Generators(seed),horizon,false);
                        reliability.Append(trainRate).Append(',').Append(testRate)
                            .Append(',').Append(seed).Append(',').Append(F(measurement.Return))
                            .Append(',').Append(measurement.Repairs).Append(',')
                            .Append(measurement.Survivors).Append(',')
                            .Append(measurement.Responses).Append('\n');
                    }
            }
            var trained=new Policy[4];
            for(int i=1;i<4;i++)trained[i]=new Policy(true,useImmediateReward);
            var trainingTrace=new StringBuilder("episode,seed,collective_return,successful_repairs,survivors,accepted_intentions\n");
            var checkpoints=new StringBuilder("checkpoint,seed,collective_return,repairs,survivors,responses\n");
            var firstQ=new StringBuilder("seat,state,harvest_q,respond_q,preferred_action\n");
            var reliability=new StringBuilder("train_delivery,test_delivery,seed,return,repairs,survivors,responses\n");
            for(int episode=0;episode<trainEpisodes;episode++)
            {
                var env=new RenewableCommonsV0(trainStart+episode,true);
                var rng=Generators(trainStart+episode);
                var trainingOutcome=Simulate(env,trained,rng,horizon,true);
                trainingTrace.Append(episode).Append(',').Append(trainStart+episode)
                    .Append(',').Append(F(trainingOutcome.Return))
                    .Append(',').Append(trainingOutcome.Repairs)
                    .Append(',').Append(trainingOutcome.Survivors)
                    .Append(',').Append(trainingOutcome.Responses).Append('\n');
                for(int i=1;i<4;i++)trained[i].Finish();
                if(episode==0)
                    for(int seat=1;seat<4;seat++)
                    {
                        var table=trained[seat].Snapshot();
                        for(int state=0;state<8;state++)
                            firstQ.Append(seat).Append(',').Append(state)
                                .Append(',').Append(F(table[state,0]))
                                .Append(',').Append(F(table[state,1]))
                                .Append(',').Append(table[state,1]>table[state,0]?"respond":"harvest")
                                .Append('\n');
                    }
                int completed=episode+1;
                if(completed==1 || completed==5 || completed==10 || completed==25 ||
                   completed==50 || completed==100 || completed==200 || completed==trainEpisodes)
                {
                    var saved=new double[4][,];
                    for(int i=1;i<4;i++)saved[i]=trained[i].Snapshot();
                    for(int n=0;n<testSeeds;n++)
                    {
                        int seed=testStart+n;
                        var frozenPolicies=new Policy[4];
                        for(int i=1;i<4;i++)frozenPolicies[i]=new Policy(saved[i]);
                        var evaluation=Simulate(new RenewableCommonsV0(seed,true),
                            frozenPolicies,Generators(seed),horizon,false);
                        checkpoints.Append(completed).Append(',').Append(seed).Append(',')
                            .Append(F(evaluation.Return)).Append(',').Append(evaluation.Repairs)
                            .Append(',').Append(evaluation.Survivors).Append(',')
                            .Append(evaluation.Responses).Append('\n');
                    }
                }
            }
            var frozen=new double[4][,];
            for(int i=1;i<4;i++)frozen[i]=trained[i].Snapshot();
            var csv=new StringBuilder("seed,condition,collective_return,successful_repairs,accepted_intentions,survivors,remaining_reserve\n");
            var paired=new List<PairedAssayAnalysisV1.Record>();
            var challenges=new StringBuilder("seed,sender_policy,collective_return,repairs,survivors,responses\n");
            var deliveries=new StringBuilder("seed,delivery_stride,collective_return,repairs,survivors,responses\n");
            var timeseries=new StringBuilder("seed,delivery_stride,window,repairs\n");
            var probabilistic=new StringBuilder("seed,reception_probability,window,repairs,survivors,collective_return\n");
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
                foreach(int stride in new[]{1,2,3,4})
                {
                    var followers=new Policy[4];
                    for(int seat=1;seat<4;seat++)followers[seat]=new Policy(frozen[seat]);
                    var env=new RenewableCommonsV0(seed,true,stride);
                    var windows=new int[(horizon+9)/10];
                    var measured=Simulate(env,followers,Generators(seed),horizon,false,4,
                        (step,repairs)=>windows[step/10]+=repairs);
                    for(int w=0;w<windows.Length;w++)
                        timeseries.Append(seed).Append(',').Append(stride).Append(',')
                            .Append(w).Append(',').Append(windows[w]).Append('\n');
                    deliveries.Append(seed).Append(',').Append(stride).Append(',')
                        .Append(F(measured.Return)).Append(',').Append(measured.Repairs)
                        .Append(',').Append(measured.Survivors).Append(',')
                        .Append(measured.Responses).Append('\n');
                }
                foreach(int rate in new[]{25,40,50,60,75,90,100})
                {
                    var followers=new Policy[4];
                    for(int seat=1;seat<4;seat++)followers[seat]=new Policy(frozen[seat]);
                    var world=new RenewableCommonsV0(seed,true,1,rate/100.0);
                    var windows=new int[(horizon+9)/10];
                    var result=Simulate(world,followers,Generators(seed),horizon,false,4,
                        (time,count)=>windows[time/10]+=count);
                    for(int w=0;w<windows.Length;w++)
                        probabilistic.Append(seed).Append(',').Append(rate).Append(',')
                            .Append(w).Append(',').Append(windows[w]).Append(',')
                            .Append(result.Survivors).Append(',')
                            .Append(F(result.Return)).Append('\n');
                }
                foreach(int period in new[]{4,6,0})
                {
                    var controllers=new Policy[4];
                    for(int seat=1;seat<4;seat++)controllers[seat]=new Policy(frozen[seat]);
                    var result=Simulate(new RenewableCommonsV0(seed,true),controllers,
                        Generators(seed),horizon,false,period);
                    challenges.Append(seed).Append(',').Append(period==0?"never_signal":period==4?"baseline_period4":"period6")
                        .Append(',').Append(F(result.Return)).Append(',').Append(result.Repairs)
                        .Append(',').Append(result.Survivors).Append(',').Append(result.Responses).Append('\n');
                }
            }
            return new Output{
                EpisodesCsv=csv.ToString(),TrainingCsv=trainingTrace.ToString(),
                ReliabilityMatrixCsv=reliability.ToString(),
                CheckpointsCsv=checkpoints.ToString(),
                FirstEpisodeQCsv=firstQ.ToString(),SenderChallengesCsv=challenges.ToString(),
                DeliveryChallengesCsv=deliveries.ToString(),ExchangeTimeSeriesCsv=timeseries.ToString(),ProbabilisticExchangeCsv=probabilistic.ToString(),
                SummaryCsv=PairedAssayAnalysisV1.Header+
                    PairedAssayAnalysisV1.ToCsv(useImmediateReward?"immediate_trained_minus_untrained":"delayed_trained_minus_untrained",
                        PairedAssayAnalysisV1.EstimatePaired(paired)),
                ManifestJson="{\"schema\":\"cells.commons-delayed-v1\",\"train_start\":"+
                    trainStart+",\"train_episodes\":"+trainEpisodes+
                    ",\"test_start\":"+testStart+",\"test_seeds\":"+testSeeds+
                    ",\"gamma\":"+(useImmediateReward?"0":"0.95")+",\"test_updates\":0,\"horizon\":"+horizon+"}"
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
            Random[] rng,int horizon,bool training,int senderPeriod=4,Action<int,int> onRepairs=null)
        {
            int repairs=0;
            var responsesBefore=new int[4];
            for(int i=1;i<4;i++)responsesBefore[i]=agents[i].Responses;
            for(int t=0;t<horizon;t++)
            {
                var actions=new RenewableCommonsV0.Act[4];
                var lead=env.Observe(0);
                actions[0]=new RenewableCommonsV0.Act(!lead.Alive?9:
                    senderPeriod>0&&t%senderPeriod==0&&lead.LocalStock<=2?6+lead.Location:
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
                if(onRepairs!=null)onRepairs(t,log.SuccessfulRepairs);
                if(training)
                    for(int i=1;i<4;i++)if(acted[i])agents[i].Reward(log.Rewards[i]);
            }
            var outcome=new Outcome{Repairs=repairs};
            foreach(double v in env.Returns())outcome.Return+=v;
            for(int i=0;i<4;i++)
            {
                if(env.Observe(i).Alive)outcome.Survivors++;
                if(i>0)outcome.Responses+=agents[i].Responses-responsesBefore[i];
            }
            return outcome;
        }
        private static string F(double value)=>value.ToString("R",CultureInfo.InvariantCulture);
    }
}
