/* Renders a normal map based on the world-space normals of the geometry.

It supports two encodings: 
	1. Raw normals mapped to RGB.
	2. Flipped normals facing a reference vector (0, 0, 1).
*/
Shader "Gamelogic/Fx/Maps/NormalMap"
{
	SubShader
	{
		Tags { "RenderType"="Opaque" }

		Pass
		{
			Cull Back
			ZWrite On
			ZTest LEqual

			HLSLPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag
			#pragma shader_feature GAMELOGIC_HAS_URP
			
			#ifdef GAMELOGIC_HAS_URP
			#define UNITY_PIPELINE_URP 1
			#endif
			
			#include "Packages/za.co.gamelogic.fx/Runtime/Core/Shaders/Gamelogic.hlsl"
			
			#define REFERENCE_VECTOR float3(0, 0, -1)
					
			#if defined(UNITY_PIPELINE_URP)
			struct appdata1
			{
				float3 positionOS : POSITION;
				float3 normalOS : NORMAL;
			};

			struct v2f1
			{
				float4 pos : SV_POSITION;
				float3 normalWS : TEXCOORD0;
			};

			v2f1 Vert(appdata1 v)
			{
				v2f1 o;
				o.pos = TransformObjectToHClip(v.positionOS);
				o.normalWS = TransformObjectToWorldNormal(v.normalOS);
				return o;
			}
			#else
			struct appdata1
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
			};

			struct v2f1
			{
				float4 pos : SV_POSITION;
				float3 normalWS : TEXCOORD0;
			};

			v2f1 Vert(appdata1 v)
			{
				v2f1 o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.normalWS = UnityObjectToWorldNormal(v.normal);
				return o;
			}
			#endif
			
			/* Encoding:
				0 - Raw normals
				1 - Flipped normals facing the REFERENCE_VECTOR
			*/
			int _Encoding;
			
			/*	Maps a direction to a hemisphere defined by REFERENCE_VECTOR.
				If the direction is opposite to REFERENCE_VECTOR, it is flipped.
			*/
			float3 flip_if_opposite(float3 direction)
			{
				float dot_product = dot(direction, REFERENCE_VECTOR);
		
				return (dot_product < 0.0) ? -direction : direction;
			}

			float4 Frag(v2f1 input) : SV_Target
			{
				float3 normal = normalize(input.normalWS);
				
				switch (_Encoding)
				{
					case 0:
						return float4(normal * 0.5 + 0.5, 1);
					case 1:
						return float4(flip_if_opposite(normal) * 0.5 + 0.5, 1);
					default:
						return float4(1, 0, 1, 1); // Magenta
				}
			}
			ENDHLSL
		}
	}
}
