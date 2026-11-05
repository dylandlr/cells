Shader "Cells/DeformableMicroscope"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="False" }
        Cull Off Lighting Off ZWrite Off
        Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment DeformedFrag
            #pragma target 3.5
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"
            float _SectorStretch[32];
            fixed4 DeformedFrag(v2f input) : SV_Target
            {
                float2 position = (input.texcoord - .5) * 2;
                float sector = frac(atan2(position.y, position.x) / 6.28318530718 + 1) * 32;
                int left = (int)floor(sector);
                float stretch = lerp(_SectorStretch[left], _SectorStretch[(left + 1) % 32], frac(sector));
                float2 uv = position / max(.72, stretch) * .5 + .5;
                fixed4 color = SampleSpriteTexture(uv) * input.color;
                color.rgb *= color.a;
                return color;
            }
            ENDCG
        }
    }
}
