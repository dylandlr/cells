using System;
using System.Globalization;
using System.Text;

namespace Cells.Experiments
{
    /// <summary>
    /// Revised E001 exploratory prototype. Fixed inherited parameters, mutable
    /// learned Q-values, and paired communication ablation on an abstract game.
    /// NOT a claim of biological genomes or demonstrated emergence.
    /// </summary>
    public static class AdaptiveExperiment001V2
    {
        public sealed class Genome
        {
            public readonly double LearningRate;
            public readonly double Exploration;
            public readonly double SignalWeight;
            public Genome(double learningRate, double exploration, double signalWeight)
            {
                if(learningRate<=0 || learningRate>1 || exploration<0 || exploration>1 ||
                   signalWeight<0 || signalWeight>4)
                    throw new ArgumentOutOfRangeException("Invalid inherited trait");
                LearningRate=learningRate;
                Exploration=exploration;
                SignalWeight=signalWeight;
            }
        }

        public sealed class AdaptivePolicy : SignalingGameV1.IPolicy
        {
            private readonly Genome genome;
            private readonly double[,] q=new double[2,SignalingGameV1.Patches];
            private int previousCue=-1,previousAction=-1;
            public AdaptivePolicy(Genome fixedGenome) { genome=fixedGenome; }
            public SignalingGameV1.Action Decide(SignalingGameV1.Observation o,Random rng)
            {
                int cue=0;
                for(int p=0;p<SignalingGameV1.Patches;p++)
                    if(o.LocalCue[p]>0)cue=1;
                int best=0;
                double score=double.NegativeInfinity;
                for(int p=0;p<SignalingGameV1.Patches;p++)
                {
                    double value=q[cue,p]+(o.LocalCue[p]<0?0:.2*o.LocalCue[p])+
                        genome.SignalWeight*o.ReceivedSignals[p];
                    if(value>score){score=value;best=p;}
                }
                int action=rng.NextDouble()<genome.Exploration?
                    rng.Next(SignalingGameV1.Patches):best;
                previousCue=cue;
                previousAction=action;
                return new SignalingGameV1.Action(action);
            }
            public double[,] Snapshot()
            {
                return (double[,])q.Clone();
            }
            public AdaptivePolicy(Genome fixedGenome, double[,] initialValues) : this(fixedGenome)
            {
                if(initialValues==null || initialValues.GetLength(0)!=2 ||
                   initialValues.GetLength(1)!=SignalingGameV1.Patches)
                    throw new ArgumentException("Invalid value table");
                for(int i=0;i<2;i++)for(int p=0;p<SignalingGameV1.Patches;p++)
                {
                    if(double.IsNaN(initialValues[i,p]) || double.IsInfinity(initialValues[i,p]))
                        throw new ArgumentException("Non-finite value");
                    q[i,p]=initialValues[i,p];
                }
            }
            public void Learn(double reward)
            {
                if(previousAction<0)return;
                q[previousCue,previousAction]+=
                    genome.LearningRate*(reward-q[previousCue,previousAction]);
            }
        }

        public sealed class Output
        {
            public string EpisodesCsv,PairedCsv,SummaryCsv,ManifestJson;
        }
        public static Output Run(int firstSeed=210000,int seeds=30)
        {
            if(seeds<2||seeds>10000||firstSeed<0||(long)firstSeed+seeds>int.MaxValue)
                throw new ArgumentOutOfRangeException("Invalid seed range");
            var episode=new StringBuilder("seed,condition,collective_return,signals,invalid_actions\n");
            var pairs=new StringBuilder("seed,adaptive_off,adaptive_on,scripted_off,scripted_on,frozen_off,frozen_on,adaptive_on_minus_off,adaptive_on_minus_scripted_on,adaptive_on_minus_frozen_on,adaptive_off_minus_frozen_off\n");
            var comm=new System.Collections.Generic.List<PairedAssayAnalysisV1.Record>();
            var learning=new System.Collections.Generic.List<PairedAssayAnalysisV1.Record>();
            var vsFrozenOn=new System.Collections.Generic.List<PairedAssayAnalysisV1.Record>();
            var vsFrozenOff=new System.Collections.Generic.List<PairedAssayAnalysisV1.Record>();
            for(int i=0;i<seeds;i++)
            {
                int seed=firstSeed+i;
                double[] totals=new double[6];
                for(int condition=0;condition<6;condition++)
                {
                    bool adaptive=condition<2 || condition>=4;
                    bool learnEnabled=condition<2;
                    bool delivery=condition%2==1;
                    var fixedGenome=new Genome(.18,.12,.75);
                    var learners=new AdaptivePolicy[SignalingGameV1.Players];
                    var policies=new SignalingGameV1.IPolicy[SignalingGameV1.Players];
                    for(int seat=0;seat<policies.Length;seat++)
                    {
                        if(adaptive){if(seat==0)policies[seat]=new SignalingGameV1.PeriodicSignaler();
                            else{learners[seat]=new AdaptivePolicy(fixedGenome);policies[seat]=learners[seat];}}
                        else policies[seat]=seat==0?(SignalingGameV1.IPolicy)new SignalingGameV1.PeriodicSignaler():
                            new SignalingGameV1.SignalFollower();
                    }
                    var outcome=SignalingGameV1.Evaluate(seed,policies,
                        SignalingGameV1.DefaultHorizon,delivery,
                        (step,observations,actions,rewards)=>{
                            if(learnEnabled)
                                for(int seat=0;seat<learners.Length;seat++)
                                    if(learners[seat]!=null)learners[seat].Learn(rewards[seat]);
                        });
                    foreach(double reward in outcome.Returns)totals[condition]+=reward;
                    episode.Append(seed).Append(',').Append(condition>=4?"frozen_":adaptive?"adaptive_":"scripted_")
                        .Append(delivery?"on":"off").Append(',')
                        .Append(F(totals[condition])).Append(',')
                        .Append(outcome.SignalCount).Append(',')
                        .Append(outcome.InvalidActions).Append('\n');
                }
                comm.Add(new PairedAssayAnalysisV1.Record(seed,totals[0],totals[1]));
                learning.Add(new PairedAssayAnalysisV1.Record(seed,totals[3],totals[1]));
                vsFrozenOn.Add(new PairedAssayAnalysisV1.Record(seed,totals[5],totals[1]));
                vsFrozenOff.Add(new PairedAssayAnalysisV1.Record(seed,totals[4],totals[0]));
                pairs.Append(seed);
                foreach(double value in totals)pairs.Append(',').Append(F(value));
                pairs.Append(',').Append(F(totals[1]-totals[0]))
                    .Append(',').Append(F(totals[1]-totals[3]))
                    .Append(',').Append(F(totals[1]-totals[5]))
                    .Append(',').Append(F(totals[0]-totals[4])).Append('\n');
            }
            var summary=new StringBuilder(PairedAssayAnalysisV1.Header);
            summary.Append(PairedAssayAnalysisV1.ToCsv("adaptive_delivery_on_minus_off",
                PairedAssayAnalysisV1.EstimatePaired(comm)));
            summary.Append(PairedAssayAnalysisV1.ToCsv("adaptive_on_minus_scripted_on",
                PairedAssayAnalysisV1.EstimatePaired(learning)));
            summary.Append(PairedAssayAnalysisV1.ToCsv("adaptive_on_minus_frozen_on",PairedAssayAnalysisV1.EstimatePaired(vsFrozenOn)));
            summary.Append(PairedAssayAnalysisV1.ToCsv("adaptive_off_minus_frozen_off",PairedAssayAnalysisV1.EstimatePaired(vsFrozenOff)));
            return new Output {
                EpisodesCsv=episode.ToString(), PairedCsv=pairs.ToString(),
                SummaryCsv=summary.ToString(),
                ManifestJson="{\"schema\":\"cells.experiment001.adaptive-v2\",\"first_seed\":"+
                    firstSeed+",\"seeds\":"+seeds+
                    ",\"horizon\":80,\"learning_rate\":0.18,\"exploration\":0.12,"+
                    "\"signal_weight\":0.75,\"genome_frozen\":true,"+
                    "\"agent_q_updates\":true,\"note\":\"exploratory minimal learner, no evolution\"}"
            };
        }
        private static string F(double x)=>x.ToString("R",CultureInfo.InvariantCulture);
    }
}
