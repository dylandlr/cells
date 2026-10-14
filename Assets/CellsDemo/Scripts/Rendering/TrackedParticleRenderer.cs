using System.Collections.Generic;
using UnityEngine;
using Cells.Substances;

namespace Cells.Rendering
{
    /// <summary>Draws tracked particles at their simulated coordinates, without animating fake positions.</summary>
    public sealed class TrackedParticleRenderer : MonoBehaviour
    {
        public TrackedParticleWorld world;
        public Color color = new Color(.8f,.9f,.4f);
        [Min(1)] public int maxVisible = 2000;
        public string substanceId = "nutrient-A";
        readonly List<Transform> visuals = new List<Transform>();
        Sprite pixelSprite;

        void Awake()
        {
            var tex = new Texture2D(2,2,TextureFormat.RGBA32,false);
            tex.filterMode = FilterMode.Point;
            tex.SetPixels(new[] { Color.white,Color.white,Color.white,Color.white });
            tex.Apply();
            pixelSprite = Sprite.Create(tex, new Rect(0,0,2,2), new Vector2(.5f,.5f),2f);
        }

        void LateUpdate()
        {
            if (!world) return;
            int n = 0;
            foreach (var p in world.Particles)
            {
                if (p.substanceId != substanceId) continue;
                if (n >= maxVisible) break;
                while (visuals.Count <= n)
                {
                    var obj = new GameObject("Tracked particle visual");
                    obj.transform.SetParent(transform);
                    var sprite = obj.AddComponent<SpriteRenderer>();
                    sprite.sprite = pixelSprite; sprite.color = color; sprite.sortingOrder = 3;
                    visuals.Add(obj.transform);
                }
                var t = visuals[n++];
                t.gameObject.SetActive(true);
                t.position = new Vector3(p.position.x,p.position.y,0f);
                t.localScale = Vector3.one * Mathf.Max(.03f, p.radius * 2f);
            }
            for (int i=n;i<visuals.Count;i++) visuals[i].gameObject.SetActive(false);
        }

        void OnDestroy()
        {
            if (pixelSprite)
            {
                var tex = pixelSprite.texture;
                Destroy(pixelSprite);
                if (tex) Destroy(tex);
            }
        }
    }
}
