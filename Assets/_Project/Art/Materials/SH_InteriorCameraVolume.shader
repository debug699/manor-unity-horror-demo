Shader "Manor/Interior Camera Volume"
{
    Properties { _BaseMap("Base Map", 2D) = "white" {} _BaseColor("Base Color", Color) = (1,1,1,1) }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial) float4 _BaseMap_ST; float4 _BaseColor; CBUFFER_END
            struct A { float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0; };
            struct V { float4 positionHCS:SV_POSITION; float3 normalWS:TEXCOORD0; float2 uv:TEXCOORD1; };
            V Vert(A i) { V o; o.positionHCS=TransformObjectToHClip(i.positionOS.xyz); o.normalWS=TransformObjectToWorldNormal(i.normalOS); o.uv=TRANSFORM_TEX(i.uv,_BaseMap); return o; }
            half4 Frag(V i):SV_Target
            {
                bool inside = abs(_WorldSpaceCameraPos.x) < 22.3 && _WorldSpaceCameraPos.z > -16.8 && _WorldSpaceCameraPos.z < 10.8 && _WorldSpaceCameraPos.y > -4.5 && _WorldSpaceCameraPos.y < 7.2;
                if (!inside) discard;
                half3 color=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb*_BaseColor.rgb;
                half light=saturate(dot(normalize(i.normalWS),normalize(half3(-0.3,0.8,0.35))))*0.45+0.38;
                return half4(color*light,1.0);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
}
