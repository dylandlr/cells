using UnityEngine;
using Cells.Physiology;

namespace Cells.Rendering
{
    /// <summary>Draws local membrane strength and openings from simulation sectors.
    /// Rendering never changes the organism transform or chemical sampling geometry.</summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class MembraneStateRenderer : MonoBehaviour
    {
        public MembraneIntegrity membrane;
        public Color healthy = new Color(.51f, .91f, .79f);
        public Color injured = new Color(1f, .46f, .3f);
        const int Size = 256;
        public const float TextureHalfExtent = 1.2f;
        SpriteRenderer body;
        SpriteRenderer boundary;
        Texture2D texture;
        Sprite sprite;
        readonly Color[] pixels = new Color[Size * Size];
        readonly float[] opacity = new float[Size * Size];
        readonly float[] highlight = new float[Size * Size];
        readonly int[] sector = new int[Size * Size];
        readonly Color[] sectorColors = new Color[MembraneIntegrity.SegmentCount];
        readonly Color32[] previousColors = new Color32[MembraneIntegrity.SegmentCount];
        readonly float[] stretches = new float[MembraneIntegrity.SegmentCount];
        Material deformableMaterial;
        MaterialPropertyBlock properties;
        int lastGeometry = -1;
        int lastRevision = -1;
        Color lastHealthy, lastInjured;
        float lastThreshold = -1f;
        float lastContourAmplitude = float.NaN, lastContourPhase;
        public SpriteRenderer BoundaryRenderer => boundary;

        void Awake()
        {
            body = GetComponent<SpriteRenderer>();
            if (!membrane) membrane = GetComponent<MembraneIntegrity>();
            var obj = new GameObject("Spatial membrane boundary");
            obj.transform.SetParent(transform, false);
            boundary = obj.AddComponent<SpriteRenderer>();
            texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            sprite = Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(.5f, .5f), Size / (2f * TextureHalfExtent), 0, SpriteMeshType.FullRect);
            boundary.sprite = sprite;
            var shader = Resources.Load<Shader>("DeformableMicroscope");
            if (shader)
            {
                deformableMaterial = new Material(shader);
                body.sharedMaterial = boundary.sharedMaterial = deformableMaterial;
                properties = new MaterialPropertyBlock();
            }
            RebuildContour();
        }

        void RebuildContour()
        {
            // Geometry is invariant; avoid trigonometry/exponentials on every injury update.
            for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                int i = y * Size + x;
                Vector2 offset = new Vector2((x + .5f) / (Size / 2f) - 1f, (y + .5f) / (Size / 2f) - 1f) * TextureHalfExtent;
                float radius = offset.magnitude / (membrane ? membrane.ContourScale(offset) : 1f);
                sector[i] = MembraneIntegrity.SegmentAt(offset);
                float rim = Mathf.Exp(-Mathf.Pow((radius - .90f) / .012f, 2f));
                float halo = .48f * Mathf.Exp(-Mathf.Pow((radius - .922f) / .019f, 2f));
                opacity[i] = Mathf.Clamp01(rim + halo);
                highlight[i] = halo / Mathf.Max(.001f, rim + halo);
            }
            lastContourAmplitude = membrane ? membrane.contourAmplitude : 0f;
            lastContourPhase = membrane ? membrane.contourPhase : 0f;
            lastRevision = -1;
        }

        void LateUpdate() => Refresh();

        public void Refresh()
        {
            if (!membrane || !boundary) return;
            if (properties != null && lastGeometry != membrane.GeometryRevision)
            {
                for (int i = 0; i < stretches.Length; i++) stretches[i] = membrane.GetStretch(i);
                body.GetPropertyBlock(properties);
                properties.SetFloatArray("_SectorStretch", stretches);
                body.SetPropertyBlock(properties);
                boundary.GetPropertyBlock(properties);
                properties.SetFloatArray("_SectorStretch", stretches);
                boundary.SetPropertyBlock(properties);
                lastGeometry = membrane.GeometryRevision;
            }
            boundary.sortingLayerID = body.sortingLayerID;
            boundary.sortingOrder = body.sortingOrder + 1;
            body.color = Color.Lerp(Color.white, new Color(.82f, .79f, .73f), 1f - membrane.integrity);
            if (membrane.ruptured) body.color *= new Color(.95f, .95f, .95f, .85f);
            if (lastContourAmplitude != membrane.contourAmplitude || lastContourPhase != membrane.contourPhase) RebuildContour();
            if (lastRevision == membrane.Revision && healthy == lastHealthy && injured == lastInjured && lastThreshold == membrane.ruptureThreshold) return;
            bool paletteChanged = lastRevision < 0;
            for (int i = 0; i < sectorColors.Length; i++)
            {
                float strength = membrane.GetIntegrity(i);
                sectorColors[i] = Color.Lerp(healthy, injured, 1f - strength);
                sectorColors[i].a = strength > membrane.ruptureThreshold ? 1f : 0f;
                Color32 quantized = sectorColors[i];
                if (!quantized.Equals(previousColors[i])) paletteChanged = true;
                previousColors[i] = quantized;
            }
            lastRevision = membrane.Revision;
            lastHealthy = healthy; lastInjured = injured; lastThreshold = membrane.ruptureThreshold;
            if (!paletteChanged) return;
            for (int i = 0; i < pixels.Length; i++)
            {
                // A failed sector has no halo or outline bridging its physical opening.
                Color tint = Color.Lerp(sectorColors[sector[i]], Color.white, highlight[i]);
                tint.a = opacity[i] * sectorColors[sector[i]].a;
                pixels[i] = tint;
            }
            texture.SetPixels(pixels); texture.Apply(false);
            lastRevision = membrane.Revision;
            lastHealthy = healthy; lastInjured = injured; lastThreshold = membrane.ruptureThreshold;
        }

        void OnEnable() { if (boundary) boundary.enabled = true; }
        void OnDisable() { if (boundary) boundary.enabled = false; }
        void OnDestroy()
        {
            if (boundary) Destroy(boundary.gameObject);
            if (sprite) Destroy(sprite);
            if (texture) Destroy(texture);
            if (deformableMaterial) Destroy(deformableMaterial);
        }
    }
}
