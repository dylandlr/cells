using UnityEngine;

namespace Cells.Hero
{
    /// <summary>Visual deformation controls. Does not replace the biological simulation.</summary>
    [DisallowMultipleComponent]
    public sealed class CellHeroAnimation : MonoBehaviour
    {
        [SerializeField, Range(0.05f, 2f)] private float beatFrequency = 0.28f;
        [SerializeField, Range(0f, 8f)] private float flagellumAmplitude = 2.8f;
        [SerializeField, Range(0f, 1f)] private float membraneDrift = 0.35f;
        [SerializeField, Range(0f, 1f)] private float osmoticSwell;
        [SerializeField] private bool inspectionView;
        [SerializeField] private bool animateVisuals = true;
        private Transform[] bones;
        private Quaternion[] restRotations;
        private SkinnedMeshRenderer[] skins;
        private Renderer[] shells;
        private int[] driftIndices;
        private int[] swellIndices;
        private float clock;

        private void Awake()
        {
            var all = GetComponentsInChildren<Transform>(true);
            var selected = new System.Collections.Generic.List<Transform>();
            foreach (var t in all)
                if (t.name.StartsWith("FlagellumA_") || t.name.StartsWith("FlagellumB_"))
                    selected.Add(t);
            bones = selected.ToArray();
            restRotations = new Quaternion[bones.Length];
            for (int i = 0; i < bones.Length; i++) restRotations[i] = bones[i].localRotation;
            skins = GetComponentsInChildren<SkinnedMeshRenderer>(true);
            driftIndices = new int[skins.Length];
            swellIndices = new int[skins.Length];
            for (int i = 0; i < skins.Length; i++)
            {
                driftIndices[i] = FindShape(skins[i].sharedMesh, "MembraneDrift");
                swellIndices[i] = FindShape(skins[i].sharedMesh, "OsmoticSwell");
            }
            var outer = new System.Collections.Generic.List<Renderer>();
            foreach (var r in GetComponentsInChildren<Renderer>(true))
                if (r.name.StartsWith("Membrane_") || r.name.StartsWith("Cytoplasm_")) outer.Add(r);
            shells = outer.ToArray();
        }

        private static int FindShape(Mesh mesh, string suffix)
        {
            if (mesh == null) return -1;
            for (int i = 0; i < mesh.blendShapeCount; i++)
                if (mesh.GetBlendShapeName(i).EndsWith(suffix)) return i;
            return -1;
        }

        public void SetInspectionView(bool enabled) { inspectionView = enabled; }
        public void SetOsmoticSwell(float normalized) { osmoticSwell = Mathf.Clamp01(normalized); }
        public void SetFlagellumDrive(float cyclesPerSecond, float degrees)
        {
            beatFrequency = Mathf.Clamp(cyclesPerSecond, 0.05f, 2f);
            flagellumAmplitude = Mathf.Clamp(degrees, 0f, 8f);
        }

        private void LateUpdate()
        {
            if (bones == null) return;
            if (animateVisuals) clock += Time.deltaTime;
            for (int i = 0; i < bones.Length; i++)
            {
                var b = bones[i];
                int segment;
                if (!int.TryParse(b.name.Substring(b.name.Length - 2), out segment)) continue;
                float phase = clock * beatFrequency * Mathf.PI * 2f - segment * 0.55f;
                if (b.name.StartsWith("FlagellumB_")) phase += 1.2f;
                float angle = animateVisuals ? Mathf.Sin(phase) * flagellumAmplitude : 0f;
                b.localRotation = restRotations[i] * Quaternion.Euler(angle * 0.45f, 0f, angle);
            }
            for (int i = 0; i < skins.Length; i++)
            {
                if (driftIndices[i] >= 0)
                    skins[i].SetBlendShapeWeight(driftIndices[i],
                        animateVisuals ? membraneDrift * (50f + 35f * Mathf.Sin(clock * 0.7f)) : 0f);
                if (swellIndices[i] >= 0) skins[i].SetBlendShapeWeight(swellIndices[i], osmoticSwell * 100f);
            }
            foreach (var r in shells) r.enabled = !inspectionView;
        }

        private void OnDisable()
        {
            if (bones != null)
                for (int i = 0; i < bones.Length; i++)
                    if (bones[i] != null) bones[i].localRotation = restRotations[i];
        }
    }
}
