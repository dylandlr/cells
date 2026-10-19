using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cells.Physiology
{
    [Serializable]
    public sealed class GeneticMutationEvent
    {
        public int parentId;
        public int descendantId;
        public int generation;
        public string target;
        public float oldValue;
        public float newValue;
    }

    /// <summary>One rare mutation opportunity per recorded replication event.
    /// No mutation occurs in FixedUpdate. Seeded and independent of rendering rate.</summary>
    public sealed class GeneticMutationScheduler
    {
        readonly System.Random random;
        public readonly List<GeneticMutationEvent> events = new List<GeneticMutationEvent>();
        public double eventProbability = .03;
        public float maximumWeightStep = .35f;

        public GeneticMutationScheduler(int seed) { random = new System.Random(seed); }

        public RegulatoryGenome Replicate(RegulatoryGenome parent, int parentId,
            int descendantId, int generation)
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            if (!double.IsFinite(eventProbability) || eventProbability < 0 || eventProbability > 1)
                throw new ArgumentOutOfRangeException(nameof(eventProbability));
            var result = parent.Copy();
            if (random.NextDouble() >= eventProbability || maximumWeightStep <= 0f)
                return result;
            int locus = random.Next(6);
            float oldValue = Read(result,locus);
            float delta = (float)(random.NextDouble()*2-1)*maximumWeightStep;
            float replacement = Mathf.Clamp(oldValue+delta,-4f,4f);
            Write(result,locus,replacement);
            if (replacement != oldValue)
                events.Add(new GeneticMutationEvent {
                    parentId=parentId,descendantId=descendantId,generation=generation,
                    target=Names[locus],oldValue=oldValue,newValue=replacement
                });
            return result;
        }

        static readonly string[] Names = {
            "nutrientToMotor", "energyToMotor", "nutrientToUptake",
            "energyToUptake", "motorBias", "uptakeBias"
        };
        static float Read(RegulatoryGenome g,int i) => i==0?g.nutrientToMotor:
            i==1?g.energyToMotor:i==2?g.nutrientToUptake:
            i==3?g.energyToUptake:i==4?g.motorBias:g.uptakeBias;
        static void Write(RegulatoryGenome g,int i,float value)
        {
            switch(i)
            {
                case 0: g.nutrientToMotor=value; break;
                case 1: g.energyToMotor=value; break;
                case 2: g.nutrientToUptake=value; break;
                case 3: g.energyToUptake=value; break;
                case 4: g.motorBias=value; break;
                default: g.uptakeBias=value; break;
            }
        }
    }
}
