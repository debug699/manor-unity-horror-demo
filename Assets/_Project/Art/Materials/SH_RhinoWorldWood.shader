Shader "Manor/Rhino World Wood"
{
    Properties
    {
        _BaseColor("Wood Color", Color) = (0.18, 0.078, 0.035, 1)
        _GrainColor("Grain Color", Color) = (0.045, 0.018, 0.009, 1)
        _Scale("Grain Scale", Range(0.2, 8)) = 2.1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Cull Off
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _GrainColor;
                half _Scale;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionHCS : SV_POSITION; float3 positionWS : TEXCOORD0; float3 normalWS : TEXCOORD1; };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionHCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                // Rhino-generated sheets sometimes have no usable UVs. World-space grain keeps
                // the wood readable without depending on those missing coordinates.
                float grainAxis = input.positionWS.y * 0.35 + input.positionWS.x * 0.08 + input.positionWS.z * 0.14;
                float coarse = frac(sin(floor(grainAxis * _Scale * 3.7)) * 43758.5453);
                float fine = sin(grainAxis * _Scale * 37.0 + sin(grainAxis * _Scale * 4.0) * 2.1) * 0.5 + 0.5;
                half grain = saturate(fine * 0.62 + coarse * 0.38);
                half3 wood = lerp(_GrainColor.rgb, _BaseColor.rgb, grain);
                half3 normal = normalize(input.normalWS);
                half light = saturate(dot(normal, normalize(half3(-0.35, 0.75, 0.40)))) * 0.58 + 0.34;
                return half4(wood * light, 1);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
}
