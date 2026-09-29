Shader "Manor/Facade Door Cutout"
{
    Properties
    {
        _BaseMap("Base Map", 2D) = "white" {}
        _BaseColor("Base Color", Color) = (1,1,1,1)
    }
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
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionHCS : SV_POSITION; float3 positionWS : TEXCOORD0; float3 normalWS : TEXCOORD1; float2 uv : TEXCOORD2; };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionHCS = positions.positionCS;
                output.positionWS = positions.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                bool cameraInside = abs(_WorldSpaceCameraPos.x) < 22.3 && _WorldSpaceCameraPos.z > -16.8 && _WorldSpaceCameraPos.z < 10.8 && _WorldSpaceCameraPos.y > -0.5 && _WorldSpaceCameraPos.y < 7.2;
                if (cameraInside) discard;
                bool front = input.positionWS.z > 9.55;
                bool mainDoor = abs(input.positionWS.x) < 1.72 && input.positionWS.y > -.15 && input.positionWS.y < 4.25;
                bool serviceWindow = abs(input.positionWS.x - 19.0) < 1.02 && input.positionWS.y > .65 && input.positionWS.y < 2.15;
                if (front && (mainDoor || serviceWindow)) discard;
                half3 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb * _BaseColor.rgb;
                half3 normal = normalize(input.normalWS);
                half lighting = saturate(dot(normal, normalize(half3(-0.35, 0.75, 0.4)))) * 0.55 + 0.35;
                return half4(baseColor * lighting, 1.0);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
}
