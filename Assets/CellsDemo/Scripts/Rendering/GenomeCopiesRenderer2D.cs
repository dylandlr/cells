using UnityEngine;
using Cells.Physiology;

namespace Cells.Rendering
{
    /// <summary>Simple original procedural chromosome-equivalent markers.
    /// Markers follow measured, kinematic segregation state; they do not
    /// exert mechanical forces or imply molecular chromosome resolution.</summary>
    [RequireComponent(typeof(GenomeCopySegregation2D))]
    public sealed class GenomeCopiesRenderer2D : MonoBehaviour
    {
        public GenomeCopySegregation2D copies;
        [Min(.01f)] public float markerRadius=.065f;
        SpriteRenderer first, second;
        Sprite sprite;
        Texture2D texture;
        void Awake()
        {
            if(!copies) copies=GetComponent<GenomeCopySegregation2D>();
            texture=new Texture2D(16,16,TextureFormat.RGBA32,false);
            texture.filterMode=FilterMode.Bilinear;
            var px=new Color[256];
            for(int y=0;y<16;y++)
            for(int x=0;x<16;x++)
            {
                float r=Vector2.Distance(new Vector2(x,y),new Vector2(7.5f,7.5f))/7f;
                px[y*16+x]=new Color(1f,1f,1f,Mathf.Clamp01((1f-r)*4f));
            }
            texture.SetPixels(px);texture.Apply();
            sprite=Sprite.Create(texture,new Rect(0,0,16,16),new Vector2(.5f,.5f),16f);
            first=Marker("Inherited genome equivalent",new Color(.95f,.76f,1f,.95f));
            second=Marker("Synthesized genome equivalent",new Color(.67f,.87f,1f,.95f));
        }
        SpriteRenderer Marker(string label,Color tint)
        {
            var go=new GameObject(label);
            go.transform.SetParent(transform,false);
            var sr=go.AddComponent<SpriteRenderer>();
            sr.sprite=sprite;
            sr.color=tint;
            sr.sortingOrder=16;
            go.transform.localScale=Vector3.one*(2f*markerRadius);
            return sr;
        }
        void LateUpdate()
        {
            if(!copies)return;
            first.transform.localPosition=copies.FirstCopyPosition;
            second.transform.localPosition=copies.SecondCopyPosition;
            second.enabled=copies.PresentCopies==2;
        }
        void OnDestroy()
        {
            if(sprite)Destroy(sprite);
            if(texture)Destroy(texture);
        }
    }
}
