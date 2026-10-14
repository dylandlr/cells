Shader "Cells/Hero/TranslucentMembrane"
{
    Properties
    {
        _Tint ("Membrane tint", Color) = (0.12,0.48,0.43,1)
        _BaseMap ("Surface detail", 2D) = "white" {}
        _NormalMap ("Microdetail normal", 2D) = "bump" {}
        _NormalStrength ("Normal strength", Range(0,1)) = 0.3
        _Opacity ("Interior opacity", Range(0,1)) = 0.065
        _RimOpacity ("Rim opacity", Range(0,1)) = 0.48
        _RimPower ("Rim softness", Range(0.1,8)) = 2.4
        _RimTint ("Rim highlight", Color) = (0.33,0.71,0.64,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent+40" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            sampler2D _BaseMap;
            sampler2D _NormalMap;
            float4 _BaseMap_ST;
            float4 _Tint, _RimTint;
            float _NormalStrength, _Opacity, _RimOpacity, _RimPower;
            struct Input { float4 vertex:POSITION; float3 normal:NORMAL; float4 tangent:TANGENT; float2 uv:TEXCOORD0; };
            struct Output { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float3 worldPos:TEXCOORD1; float3 normal:TEXCOORD2; float3 tangent:TEXCOORD3; float3 bitangent:TEXCOORD4; };
            Output vert(Input v)
            {
                Output o;
                o.pos=UnityObjectToClipPos(v.vertex);
                o.worldPos=mul(unity_ObjectToWorld,v.vertex).xyz;
                o.uv=TRANSFORM_TEX(v.uv,_BaseMap);
                o.normal=UnityObjectToWorldNormal(v.normal);
                o.tangent=UnityObjectToWorldDir(v.tangent.xyz);
                o.bitangent=cross(o.normal,o.tangent)*v.tangent.w*unity_WorldTransformParams.w;
                return o;
            }
            float4 frag(Output i):SV_Target
            {
                float3 tn=UnpackNormal(tex2D(_NormalMap,i.uv));
                tn.xy*=_NormalStrength;
                float3 n=normalize(i.tangent*tn.x+i.bitangent*tn.y+i.normal*tn.z);
                float3 view=normalize(_WorldSpaceCameraPos-i.worldPos);
                float rim=pow(saturate(1-dot(n,view)),_RimPower);
                float3 key=normalize(float3(-0.35,0.8,-0.48));
                float highlight=pow(saturate(dot(n,normalize(view+key))),55);
                float3 base=tex2D(_BaseMap,i.uv).rgb*_Tint.rgb;
                float3 col=base*(0.7+0.3*saturate(dot(n,key)))+_RimTint.rgb*rim*.65+highlight*.16;
                return float4(col,lerp(_Opacity,_RimOpacity,rim)*_Tint.a);
            }
            ENDCG
        }
    }
    Fallback Off
}
