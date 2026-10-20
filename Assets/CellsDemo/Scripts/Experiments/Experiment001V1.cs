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
            public string EpisodesCsv, PairedCsv, SummaryCsv, ManifestJson;
        }

        public static Output Run(int firstSeed=100001,int seeds=30)
        {
            if(seeds<2 || seeds>100000 || firstSeed<0 ||
               (long)firstSeed+seeds>int.MaxValue)
                throw new ArgumentOutOfRangeException("seeds");
            var rows=new StringBuilder("seed,delivery,collective_return,signals,invalid_actions\n");
            var pairs=new StringBuilder("seed,off_return,on_return,on_minus_off\n");
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
                        SignalingGameV1.DefaultHorizon,condition==1);
                    foreach(double value in result.Returns)total[condition]+=value;
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
