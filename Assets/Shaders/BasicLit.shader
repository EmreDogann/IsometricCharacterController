Shader "PlayerObstruction/BasicLit" {
	
	// Properties are options set per material, exposed by the material inspector
	Properties {
		[Header(Surface Options)] // Create a text header
		// [MainTexture] and [MainColor] allows Material.color to use the correct property
		[MainTexture] _ColorMap("Color", 2D) = "white" {}
		[MainColor] _ColorTint("Tint", Color) = (1, 1, 1, 1)
		_Smoothness("Smoothness", Float) = 0
	}

	// Subshaders allow for different behaviour and options for different pipelines and platforms
	SubShader {
		// These tags are shared by all passes in this sub shader
		Tags {
			"RenderPipeline" = "UniversalPipeline"
		}

		// Shaders can have several passes which are used to render different data about the material
		// Each pass has it's own vertex and fragment function and shader variant keywords
		Pass {
			Name "ForwardLit" // For debugging
			Tags {
				"LightMode" = "UniversalForward" // "UniversalForward" tells Unity this is the main lighting pass of this shader
			}

			// Begin HLSL code. 
			HLSLPROGRAM // URP shader code is written in HLSL, which is a similar to a streamlined version of C++.

			#define _SPECULAR_COLOR

			// Shader variants
			// The single underscore means that Unity will also compile a variant of the shader with none of the keywords.
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
			#pragma multi_compile_fragment _ _SHADOWS_SOFT

			// Register out programmable stage functions
			#pragma vertex Vertex
			#pragma fragment Fragment

			// Pull in URP library functions and our own common functions
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

			// Textures
			TEXTURE2D(_ColorMap); SAMPLER(sampler_ColorMap);
			float4 _ColorMap_ST; // This is automatically set by Unity. Used in TRANSFORM_TEX to apply UV tiling
			float4 _ColorTint;
			float _Smoothness;
			
			struct Attributes {
				float3 positionOS : POSITION; // Position in object space
				float3 normalOS : NORMAL;
				float2 uv : TEXCOORD0; // Material texture UVs
			};

			struct Interpolators {
				float4 positionCS : SV_POSITION;
				float2 uv : TEXCOORD0;
				float3 positionWS : TEXCOORD1;
				float3 normalWS : TEXCOORD2; // The rastrerizater will interpolate any field tag with the "TEXCOORD" semantic.
			};

			Interpolators Vertex(Attributes input) {
				Interpolators output;

				VertexPositionInputs posnInputs = GetVertexPositionInputs(input.positionOS);
				VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS);

				output.positionCS = posnInputs.positionCS;
				output.uv = TRANSFORM_TEX(input.uv, _ColorMap);
				output.normalWS = normInputs.normalWS;
				output.positionWS = posnInputs.positionWS;

				return output;
			}

			float4 Fragment(Interpolators input) : SV_TARGET {
				float2 uv = input.uv;
				float4 colorSample = SAMPLE_TEXTURE2D(_ColorMap, sampler_ColorMap, uv);

				InputData lightingInput = (InputData)0;
				lightingInput.positionWS = input.positionWS;
				lightingInput.normalWS = normalize(input.normalWS);
				lightingInput.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
				lightingInput.shadowCoord = TransformWorldToShadowCoord(input.positionWS);

				SurfaceData surfaceInput = (SurfaceData)0;
				surfaceInput.albedo = colorSample.rgb * _ColorTint.rgb;
				surfaceInput.alpha = colorSample.a * _ColorTint.a;
				surfaceInput.specular = 1;
				surfaceInput.smoothness = _Smoothness;

				return UniversalFragmentBlinnPhong(lightingInput, surfaceInput);
			}

			ENDHLSL
		}

		// For casting shadows - Creates a shadow map.
		Pass {
			Name "ShadowCaster"
			Tags {
				"LightMode" = "ShadowCaster"
			}

			// Tunr off color buffer
			ColorMask 0

			// Begin HLSL code. 
			HLSLPROGRAM

			#pragma vertex Vertex
			#pragma fragment Fragment

			// Pull in URP library functions and our own common functions
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			
			struct Attributes {
				float3 positionOS : POSITION; // Position in object space
				float3 normalOS : NORMAL;
			};

			struct Interpolators {
				float4 positionCS : SV_POSITION;
			};

			float3 _LightDirection;

			// This function offsets the clip space position by the depth and normal shadow biases.
			float4 GetShadowCasterPositionCS(float3 positionWS, float3 normalWS) {
				float3 lightDirectionWS = _LightDirection;
				float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));

				// We have to make sure that the shadow bias didn't push the shadow out of the camera's view area.
				// This is slightly different depending on the graphics API due to some APIs have flipped Z values.
			#if	UNITY_REVERSED_Z	
				positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
			#else
				positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
			#endif
				return positionCS;
			}

			Interpolators Vertex(Attributes input) {
				Interpolators output;

				VertexPositionInputs posnInputs = GetVertexPositionInputs(input.positionOS);
				VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS);

				output.positionCS = GetShadowCasterPositionCS(posnInputs.positionWS, normInputs.normalWS);
				return output;
			}

			float4 Fragment(Interpolators input) : SV_TARGET {
				return 0;
			}

			ENDHLSL
		}
	}
}