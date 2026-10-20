using UnityEngine;

namespace Cells.Physiology
{
    /// <summary>
    /// Two spatially tracked effective genome equivalents. The second is
    /// present only after precursor-funded template synthesis completes.
    /// Their positions are a kinematic segregation model inside the cell,
    /// not molecular chromosome mechanics or a proof of successful scission.
    /// </summary>
    [DefaultExecutionOrder(-18)]
    [RequireComponent(typeof(GeneticTemplateSynthesis2D))]
    public sealed class GenomeCopySegregation2D : MonoBehaviour
    {
        public GeneticTemplateSynthesis2D synthesis;
        public SoftCellBody2D body;
        [Min(.01f)] public float segregationSeconds = 6f;
        [Range(.05f,.75f)] public float separationRadiusFraction = .40f;
        public float Progress { get; private set; }
        public bool Complete => synthesis && synthesis.ReplicationReady &&
            Progress >= .999f;
        public Vector2 FirstCopyPosition { get; private set; }
        public Vector2 SecondCopyPosition { get; private set; }
        public int PresentCopies => synthesis && synthesis.ReplicationReady ? 2 : 1;

        void Awake()
        {
            if (!synthesis) synthesis = GetComponent<GeneticTemplateSynthesis2D>();
            if (!body) body = GetComponent<SoftCellBody2D>();
        }
        void FixedUpdate() => Tick(Time.fixedDeltaTime);

        public void Tick(float dt)
        {
            if (!synthesis || !body || !body.Initialized ||
                !float.IsFinite(dt) || dt <= 0f) return;
            if (synthesis.ReplicationReady)
                Progress = Mathf.Clamp01(Progress+dt/Mathf.Max(.01f,segregationSeconds));
            else Progress = 0f;
            float offset = body.CurrentRadius*separationRadiusFraction*Progress;
            FirstCopyPosition = body.Core+Vector2.up*offset;
            SecondCopyPosition = body.Core-Vector2.up*offset;
        }
        public void ResetForNewCell()
        {
            Progress=0f;
            FirstCopyPosition=SecondCopyPosition=body ? body.Core : Vector2.zero;
        }
    }
}
