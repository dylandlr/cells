using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cells.Substances
{
    public enum ChemicalEventKind { Synthesized, Secreted, Absorbed, Damaged, Died, Added }

    [Serializable]
    public struct ChemicalEvent
    {
        public long sequence;
        public float simulationTime;
        public int organismId;
        public string substanceId;
        public ChemicalEventKind kind;
        public float quantity;
        public Vector2 location;
        public string sourceStructure;
    }

    /// <summary>Truthful event evidence shared by simulation and the human inspector.</summary>
    public sealed class ChemicalEventLog : MonoBehaviour
    {
        public int capacity = 4096;
        readonly List<ChemicalEvent> history = new List<ChemicalEvent>();
        long nextSequence;

        public void Record(float time, int organismId, string substanceId,
            ChemicalEventKind kind, float quantity, Vector2 location, string structure)
        {
            history.Add(new ChemicalEvent
            {
                sequence = ++nextSequence,
                simulationTime = time,
                organismId = organismId,
                substanceId = substanceId,
                kind = kind,
                quantity = quantity,
                location = location,
                sourceStructure = structure
            });
            if (history.Count > Mathf.Max(1, capacity)) history.RemoveAt(0);
        }

        public void QueryOrganism(int organismId, List<ChemicalEvent> output)
        {
            output.Clear();
            foreach (var e in history)
                if (e.organismId == organismId) output.Add(e);
        }
    }
}
