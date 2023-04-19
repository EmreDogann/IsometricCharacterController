Shader "Dissolve/DissolveBasedOnViewDistance" {
    Properties {
        _MainTex("Albedo (RGB)", 2D) = "white" {}
        _Center("Dissolve Center", Vector) = (0,0,0,0)
        _Interpolation("Dissolve Interpolation", Range(0,5)) = 0.8
        _DissTexture("Dissolve Texture", 2D) = "white" {}
    }

    SubShader {
        Tags {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Lit"
            "IgnoreProjector" = "True"
            "ShaderModel" = "4.5"
        }

        Pass {
            Cull Off

            HLSLPROGRAM
            // This line defines the name of the vertex shader. 
            #pragma vertex vert
            // This line defines the name of the fragment shader. 
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Input {
                float2 uv_MainTex;
                float2 uv_DissTexture;
                float3 worldPos;
                float viewDist;
            };

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv_MainTex : TEXCOORD0;
                float2 uv_DissTexture : TEXCOORD1;
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float2 uv_MainTex : TEXCOORD0;
                float2 uv_DissTexture : TEXCOORD1;
                float3 viewDist : TEXCOORD2;
                float3 positionWS : TEXCOORD3;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D(_DissTexture);
            SAMPLER(sampler_DissTexture);

            CBUFFER_START(UnityPerMaterial)
                // The following line declares the _BaseMap_ST variable, so that you
                // can use the _BaseMap variable in the fragment shader. The _ST 
                // suffix is necessary for the tiling and offset function to work.
                float4 _MainTex_ST;
                float4 _DissTexture_ST;
                float _Interpolation;
                float4 _Center;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS);
                float3 viewDirWS = GetWorldSpaceViewDir(posInputs.positionWS);

                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.uv_MainTex = TRANSFORM_TEX(input.uv_MainTex, _MainTex);
                output.uv_DissTexture = TRANSFORM_TEX(input.uv_DissTexture, _DissTexture);
                output.viewDist = length(viewDirWS);
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float l = length(_Center - input.positionWS.xyz);
                clip(saturate(input.viewDist - l + (SAMPLE_TEXTURE2D(_DissTexture, sampler_DissTexture, input.uv_DissTexture) * _Interpolation * saturate(input.viewDist))) - 0.5);
                //half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv_MainTex);
            }
            ENDHLSL
        }
    }
}