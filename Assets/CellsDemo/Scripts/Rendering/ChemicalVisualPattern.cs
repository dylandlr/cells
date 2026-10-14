using UnityEngine;
using Cells.Substances;

namespace Cells.Rendering
{
    /// <summary>Deterministic visual microtexture. Not a particle physics model.</summary>
    public static class ChemicalVisualPattern
    {
        public static float Mask(MaterialForm form, int x, int y, int seed)
        {
            unchecked
            {
                uint h = (uint)(x * 73856093) ^ (uint)(y * 19349663) ^ (uint)seed;
                h ^= h >> 16; h *= 0x7feb352du; h ^= h >> 15;
                float noise = (h & 0xffff) / 65535f;
                switch (form)
                {
                    case MaterialForm.Dissolved: return 1f;
                    case MaterialForm.Suspension: return noise > .63f ? 1f : .12f;
                    case MaterialForm.Colloid: return noise > .42f ? .85f : .35f;
                    case MaterialForm.Crystal: return ((x+y+seed)%7==0) ? 1f : .08f;
                    case MaterialForm.Droplet: return ((x*x+y*y+seed)%17<5) ? .95f : .12f;
                    case MaterialForm.VesicleBound: return noise > .77f ? 1f : .06f;
                    case MaterialForm.Gel: return .5f + .3f * Mathf.Sin(x*.25f+y*.2f);
                    case MaterialForm.GasBubble: return noise > .88f ? 1f : .05f;
                    case MaterialForm.MembraneBound: return noise > .7f ? .8f : .1f;
                    default: return 1f;
                }
            }
        }
    }
}
