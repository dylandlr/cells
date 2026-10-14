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
        public MaterialForm form = MaterialForm.Dissolved;
        [Min(0)] public int visualSeed = 631;
        [Range(0f, 1f)] public float particulateCoverage = .8f;
        [Range(0f, 1f)] public float fineDetailZoomThreshold = .5f;
        [Min(.001f)] public float concentrationAtFullOpacity = .2f;
        [Range(0,1)] public float maxAlpha = .75f;
        [Min(1)] public int updateEveryFrames = 3;
        Texture2D texture;
        Sprite sprite;
        SpriteRenderer target;

        void Start()
        {
            target = GetComponent<SpriteRenderer>();
            if (!field) field = FindAnyObjectByType<MultiChemicalField2D>();
            if (!field) return;
            texture = new Texture2D(field.width, field.height, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            sprite = Sprite.Create(texture,
                new Rect(0,0,field.width,field.height), new Vector2(.5f,.5f), 1f/field.cellSize);
            target.sprite = sprite;
            transform.SetParent(field.transform, false);
        }

        void LateUpdate()
        {
            if (!field || !texture || Time.frameCount % Mathf.Max(1, updateEveryFrames) != 0) return;
            float detail = Camera.main ? Mathf.Clamp01((8f - Camera.main.orthographicSize) / 6f) : 0f;
            for (int y=0;y<field.height;y++)
            for (int x=0;x<field.width;x++)
            {
                float c = field.SampleCell(substanceId,x,y);
                float coverage = Mathf.Clamp01(c / concentrationAtFullOpacity);
                // Point-sampled microtexture is deterministic and modulates actual concentration,
                // never creates a visible particle when the real concentration is zero.
                float mask = ChemicalVisualPattern.Mask(form, x, y, visualSeed);
                float opacity = form == MaterialForm.Dissolved ? coverage :
                    Mathf.Lerp(coverage, coverage * mask * particulateCoverage, detail);
                texture.SetPixel(x,y,new Color(tint.r,tint.g,tint.b,opacity*maxAlpha));
            }
            texture.Apply(false);
        }

        void OnDestroy()
        {
            if (target) target.sprite = null;
            if (sprite) Destroy(sprite);
            if (texture) Destroy(texture);
        }
    }
}
