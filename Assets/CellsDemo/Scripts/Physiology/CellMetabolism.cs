using UnityEngine;

namespace Cells.Physiology
{
    /// <summary>Single authoritative reserve for chemical exchange and exposure prototypes.</summary>
    public sealed class CellMetabolism : MonoBehaviour
    {
        [Min(0f)] public float energy = 8f;
        [Min(0f)] public float basalCostPerSecond = .02f;
        public bool alive = true;
        public float cumulativeDamage;

        public float Spend(float requested)
        {
            if (!alive || requested <= 0f) return 0f;
            float paid = Mathf.Min(energy, requested);
            energy -= paid;
            if (energy <= 0f) alive = false;
            return paid;
        }

        public void Feed(float energyGain)
        {
            if (alive) energy += Mathf.Max(0f, energyGain);
        }

        public void Injure(float damage)
        {
            if (!alive) return;
            float value = Mathf.Max(0f, damage);
            cumulativeDamage += value;
            energy = Mathf.Max(0f, energy - value);
            if (energy <= 0f) alive = false;
        }

        public void Tick(float dt)
        {
            if (!alive || dt <= 0f) return;
            Spend(basalCostPerSecond * dt);
            if (energy <= 0f) alive = false;
        }

        void FixedUpdate() => Tick(Time.fixedDeltaTime);
    }
}
