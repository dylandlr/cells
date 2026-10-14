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
        const int Size = 64;
        SpriteRenderer body;
        SpriteRenderer boundary;
        Texture2D texture;
        Sprite sprite;
        readonly Color[] pixels = new Color[Size * Size];
        int lastRevision = -1;
        Color lastHealthy, lastInjured;
        float lastThreshold = -1f;
        public SpriteRenderer BoundaryRenderer => boundary;

        void Awake()
        {
            body = GetComponent<SpriteRenderer>();
            if (!membrane) membrane = GetComponent<MembraneIntegrity>();
            var obj = new GameObject("Spatial membrane boundary");
            obj.transform.SetParent(transform, false);
            boundary = obj.AddComponent<SpriteRenderer>();
            texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            sprite = Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(.5f, .5f), Size / 2f);
            boundary.sprite = sprite;
        }

        void LateUpdate() => Refresh();

        public void Refresh()
        {
            if (!membrane || !boundary) return;
            boundary.sortingLayerID = body.sortingLayerID;
            boundary.sortingOrder = body.sortingOrder + 1;
            body.color = Color.Lerp(healthy, injured, (1f - membrane.integrity) * .35f);
            if (membrane.ruptured) body.color *= new Color(.6f, .6f, .6f, 1f);
            if (lastRevision == membrane.Revision && healthy == lastHealthy && injured == lastInjured && lastThreshold == membrane.ruptureThreshold) return;
            for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                Vector2 offset = new Vector2((x + .5f) / (Size / 2f) - 1f, (y + .5f) / (Size / 2f) - 1f);
                float radius = offset.magnitude;
                float strength = membrane.GetIntegrity(MembraneIntegrity.SegmentAt(offset));
                pixels[y * Size + x] = radius >= .83f && radius < .96f && strength > membrane.ruptureThreshold
                    ? Color.Lerp(healthy, injured, 1f - strength) : Color.clear;
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
        }
    }
}
