using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Cells.Experiments
{
    /// <summary>
    /// Immutable protocol for sequential, paired Unity chemotaxis assays.
    /// Execute one row per freshly reset world. This class intentionally
    /// does not fabricate simulation outcomes or reuse a contaminated field.
    /// </summary>
    public static class ChemotaxisAssayPlanV1
    {
        public const string Schema = "cells.chemotaxis-assay.v1";
        public const string Spatial = "spatial";
        public const string Temporal = "temporal";
        public const string Stochastic = "stochastic";
        static readonly string[] Policies = { Spatial, Temporal, Stochastic };

        public struct Trial
        {
            public int seed;
            public string controller;
            public int ordinal;
        }

        public static Trial[] Build(int firstSeed, int replicates)
        {
            if (replicates < 1 || replicates > 10000)
                throw new ArgumentOutOfRangeException(nameof(replicates));
            if ((long)firstSeed + replicates - 1 > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(firstSeed));
            var rows = new Trial[replicates * Policies.Length];
            int n=0;
            for (int i=0;i<replicates;i++)
                for (int j=0;j<Policies.Length;j++)
                    rows[n]=new Trial { seed=firstSeed+i,controller=Policies[j],ordinal=n++ };
            return rows;
        }

        public static string Csv(Trial[] rows)
        {
            if (rows==null) throw new ArgumentNullException(nameof(rows));
            var b=new StringBuilder("schema,ordinal,environment_seed,controller,world_reset_required\n");
            foreach (var t in rows)
                b.Append(Schema).Append(',').Append(t.ordinal).Append(',')
                 .Append(t.seed).Append(',').Append(t.controller)
                 .Append(",true\n");
            return b.ToString();
        }

        public struct Outcome
        {
            public Trial trial;
            public float elapsedSeconds;
            public float pathLength;
            public float nutrientAbsorbed;
            public float nutrientDigested;
            public float motorEnergySpent;
            public float finalEnergy;
            public int tumbles;
            public bool alive;
        }

        public static string OutcomeCsv(IEnumerable<Outcome> outcomes)
        {
            if (outcomes==null) throw new ArgumentNullException(nameof(outcomes));
            var b=new StringBuilder("schema,ordinal,seed,controller,elapsed_seconds,path_length,nutrient_absorbed,nutrient_digested,motor_energy_spent,final_energy,tumbles,alive\n");
            foreach (var o in outcomes)
            {
                b.Append(Schema).Append(',').Append(o.trial.ordinal).Append(',')
                 .Append(o.trial.seed).Append(',').Append(o.trial.controller).Append(',')
                 .Append(F(o.elapsedSeconds)).Append(',').Append(F(o.pathLength)).Append(',')
                 .Append(F(o.nutrientAbsorbed)).Append(',').Append(F(o.nutrientDigested)).Append(',')
                 .Append(F(o.motorEnergySpent)).Append(',').Append(F(o.finalEnergy)).Append(',')
                 .Append(o.tumbles).Append(',').Append(o.alive ? "true" : "false").Append('\n');
            }
            return b.ToString();
        }

        static string F(float v) => v.ToString("R",CultureInfo.InvariantCulture);
    }
}
