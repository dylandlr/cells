using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Cells.Experiments
{
    /// <summary>Experiment 001 pilot: identical frozen policy composition,
    /// seed and signaling costs; only public message delivery is switched.
    /// This is an abstract strategic experiment, not genomic biology.</summary>
    public static class Experiment001V1
    {
        public sealed class Output
        {
            public string EpisodesCsv, PairedCsv, SummaryCsv, ManifestJson, AgentCsv, StepsCsv;
        }

        public static Output Run(int firstSeed=100001,int seeds=30)
        {
            if(seeds<2 || seeds>100000 || firstSeed<0 ||
               (long)firstSeed+seeds>int.MaxValue)
                throw new ArgumentOutOfRangeException("seeds");
            var rows=new StringBuilder("seed,delivery,collective_return,signals,invalid_actions\n");
            var pairs=new StringBuilder("seed,off_return,on_return,on_minus_off\n");
            var agents=new StringBuilder("seed,delivery,seat,policy,realized_return,final_patch\n");
            var stepsCsv=new StringBuilder("seed,delivery,step,seat,own_patch,action,received_signals,reward,foragers_on_selected_patch,signaled_patch,signal_alignment,changed_patch,counterfactual_no_message_action,message_changed_action\n");
            var differences=new List<PairedAssayAnalysisV1.Record>();
            for(int i=0;i<seeds;i++)
            {
                int seed=firstSeed+i;
                double[] total=new double[2];
                for(int condition=0;condition<2;condition++)
                {
                    var policies=new SignalingGameV1.IPolicy[] {
                        new SignalingGameV1.PeriodicSignaler(),
                        new SignalingGameV1.SignalFollower(),
                        new SignalingGameV1.SignalFollower(),
                        new SignalingGameV1.SignalFollower()
                    };
                    var result=SignalingGameV1.Evaluate(seed,policies,
                        SignalingGameV1.DefaultHorizon,condition==1,
                        (step, observed, actions, rewards) => {
                            int[] occupancy=new int[SignalingGameV1.Patches];
                            foreach(var action in actions)
                                if(action.Code>=0 && action.Code<SignalingGameV1.Patches)
                                    occupancy[action.Code]++;
                            for(int seat=0;seat<SignalingGameV1.Players;seat++)
                            {
                                int action=actions[seat].Code;
                                int received=0;
                                foreach(int signal in observed[seat].ReceivedSignals)
                                    received+=signal;
                                int signaledPatch=-1;
                                int bestSignal=0;
                                for(int patch=0;patch<SignalingGameV1.Patches;patch++)
                                    if(observed[seat].ReceivedSignals[patch]>bestSignal)
                                    {
                                        bestSignal=observed[seat].ReceivedSignals[patch];
                                        signaledPatch=patch;
                                    }
                                int counterfactual=-1;
                                int messageChanged=0;
                                if(seat>0 && condition==1)
                                {
                                    var noMessage=observed[seat];
                                    noMessage.ReceivedSignals=new int[SignalingGameV1.Patches];
                                    counterfactual=policies[seat].Decide(noMessage,
                                        new Random(0)).Code;
                                    messageChanged=counterfactual!=action?1:0;
                                }
                                stepsCsv.Append(seed).Append(',')
                                    .Append(condition==1?"on":"off").Append(',')
                                    .Append(step).Append(',').Append(seat).Append(',')
                                    .Append(observed[seat].OwnPatch).Append(',')
                                    .Append(action).Append(',').Append(received).Append(',')
                                    .Append(F(rewards[seat])).Append(',')
                                    .Append(action>=0 && action<SignalingGameV1.Patches?
                                        occupancy[action]:0).Append(',')
                                    .Append(signaledPatch).Append(',')
                                    .Append(signaledPatch>=0 && action==signaledPatch?1:0)
                                    .Append(',').Append(action>=0 &&
                                        action<SignalingGameV1.Patches &&
                                        action!=observed[seat].OwnPatch?1:0).Append(',')
                                    .Append(counterfactual).Append(',')
                                    .Append(messageChanged).Append('\n');
                            }
                        });
                    for(int seat=0;seat<result.Returns.Length;seat++)
                    {
                        total[condition]+=result.Returns[seat];
                        agents.Append(seed).Append(',').Append(condition==1?"on":"off")
                            .Append(',').Append(seat).Append(',')
                            .Append(seat==0?"signaler":"follower").Append(',')
                            .Append(F(result.Returns[seat])).Append(',')
                            .Append(result.FinalPatches[seat]).Append('\n');
                    }
                    rows.Append(seed).Append(',').Append(condition==1?"on":"off")
                        .Append(',').Append(F(total[condition])).Append(',')
                        .Append(result.SignalCount).Append(',')
                        .Append(result.InvalidActions).Append('\n');
                }
                differences.Add(new PairedAssayAnalysisV1.Record(seed,total[0],total[1]));
                pairs.Append(seed).Append(',').Append(F(total[0])).Append(',')
                    .Append(F(total[1])).Append(',')
                    .Append(F(total[1]-total[0])).Append('\n');
            }
            var effect=PairedAssayAnalysisV1.EstimatePaired(differences);
            return new Output {
                EpisodesCsv=rows.ToString(),
                AgentCsv=agents.ToString(),
                StepsCsv=stepsCsv.ToString(),
                PairedCsv=pairs.ToString(),
                SummaryCsv=PairedAssayAnalysisV1.Header+
                    PairedAssayAnalysisV1.ToCsv("delivery_on_minus_off",effect),
                ManifestJson="{\"schema\":\"cells.experiment001.v1\",\"first_seed\":"+
                    firstSeed+",\"seeds\":"+seeds+
                    ",\"horizon\":80,\"control\":\"message_delivery_only\","+
                    "\"policy\":\"periodic_signaler_plus_three_followers\","+
                    "\"note\":\"symbolic resource game, not genomic traits\"}"
            };
        }

        static string F(double number) =>
            number.ToString("R",CultureInfo.InvariantCulture);
    }
}
