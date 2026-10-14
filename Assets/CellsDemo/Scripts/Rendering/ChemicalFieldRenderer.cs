using UnityEngine;
using Cells.Substances;

namespace Cells.Rendering
{
    /// <summary>Visualizes actual concentrations; never modifies transport or agent perception.</summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class ChemicalFieldRenderer : MonoBehaviour
    {
        public MultiChemicalField2D field;
        public string substanceId = "toxin-B";
        public Color tint = new Color(.67f, .31f, .95f);
        [Min(.001f)] public float concentrationAtFullOpacity = .2f;
        [Range(0,1)] public float maxAlpha = .75f;
        [Min(1)] public int updateEveryFrames = 3;
        Texture2D texture;
        Sprite sprite;
        SpriteRenderer target;

        void Start()
        {
            target = GetComponent<SpriteRenderer>();
            if (!field) field = FindFirstObjectByType<MultiChemicalField2D>();
            if (!field) return;
            texture = new Texture2D(field.width, field.height, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            sprite = Sprite.Create(texture,
                new Rect(0,0,field.width,field.height), new Vector2(.5f,.5f), 1f/field.cellSize);
            target.sprite = sprite;
            target.sortingOrder = -20;
            transform.SetParent(field.transform, false);
        }

        void LateUpdate()
        {
            if (!field || !texture || Time.frameCount % updateEveryFrames != 0) return;
            for (int y=0;y<field.height;y++)
            for (int x=0;x<field.width;x++)
            {
                float c = field.SampleCell(substanceId,x,y);
                float coverage = Mathf.Clamp01(c / concentrationAtFullOpacity);
                // Apply a continuous physically driven opacity, no fake chemical motion.
                texture.SetPixel(x,y,new Color(tint.r,tint.g,tint.b,coverage*maxAlpha));
            }
            texture.Apply(false);
        }

        void OnDisable()
        {
            if (target) target.sprite = null;
            if (sprite) Destroy(sprite);
            if (texture) Destroy(texture);
        }
    }
}
