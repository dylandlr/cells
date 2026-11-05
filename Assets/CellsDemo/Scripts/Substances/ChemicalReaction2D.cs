using UnityEngine;

namespace Cells.Substances
{
    /// <summary>Demonstration neutralization, not a calibrated biochemical pathway.</summary>
    [DefaultExecutionOrder(10)]
    public sealed class ChemicalReaction2D : MonoBehaviour
    {
        public MultiChemicalField2D field;
        public string reactantA = "toxin-B";
        public string reactantB = "neutralizer-C";
        public string product = "waste-D";
        [Min(0f)] public float rate = .3f;
        public float ReactedMass { get; private set; }
        public void Tick(float dt)
        {
            if (field) ReactedMass += 2f * field.React(reactantA, reactantB, product, rate, dt);
        }
        void FixedUpdate() => Tick(Time.fixedDeltaTime);
    }
}
