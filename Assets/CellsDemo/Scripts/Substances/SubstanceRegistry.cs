using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cells.Substances
{
    /// <summary>Human metadata registry. Do not pass this class to organism AI policies.</summary>
    public sealed class SubstanceRegistry : MonoBehaviour
    {
        public SubstanceDefinition[] definitions = Array.Empty<SubstanceDefinition>();
        readonly Dictionary<string, SubstanceDefinition> byId = new Dictionary<string, SubstanceDefinition>();

        void Awake()
        {
            byId.Clear();
            foreach (var definition in definitions)
            {
                if (!definition || string.IsNullOrWhiteSpace(definition.stableId)) continue;
                if (byId.ContainsKey(definition.stableId))
                    Debug.LogError("Duplicate chemical ID: " + definition.stableId, this);
                else byId.Add(definition.stableId, definition);
            }
        }

        public bool TryGet(string id, out SubstanceDefinition definition)
            => byId.TryGetValue(id, out definition);
    }
}
