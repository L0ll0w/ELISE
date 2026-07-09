Shader "Gamelogic/Fx/Maps/UVMap"
{
	Properties
	{
		_MainTex ("Main Texture", 2D) = "white" {}
		_UvScaleOffset ("Uv Scale Offset", Vector) = (1, 1, 0, 0)
	}

	SubShader
	{
		Tags
		{
			"RenderType"="Opaque"
		}
		LOD 100

		Pass
		{
			Name "UVMap"
			Cull Off
			ZClip Off
			ZTest LEqual
			ZWrite On


			HLSLPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag
			#pragma shader_feature GAMELOGIC_HAS_URP
			#pragma shader_feature ADJUST_BY_MATERIAL_TILING
			#pragma shader_feature ADJUST_BY_OBJECT_DISTANCE
			#pragma shader_feature ADJUST_BY_OBJECT_SCALE

			#ifdef GAMELOGIC_HAS_URP
			#define UNITY_PIPELINE_URP 1
			#endif

			#include "Packages/za.co.gamelogic.fx/Runtime/Core/Shaders/Gamelogic.hlsl"
			
			float4 _MainTex_ST;
			
			#if defined(UNITY_PIPELINE_URP)
			CBUFFER_START(UnityPerMaterial)
			float4 _UvScaleOffset;
			CBUFFER_END
			#else
			float4 _UvScaleOffset;
			#endif
						
			float get_object_distance_factor()
			{
				float3 object_pos_ws = unity_ObjectToWorld._m03_m13_m23;
				float3 camera_pos_ws = _WorldSpaceCameraPos;

				float dist = distance(object_pos_ws, camera_pos_ws);
				float inv_dist = 1.0 / max(dist, 0.0001);
				
				return inv_dist;
			}
			
			float get_object_scale_factor()
			{
				float3 scale_x = unity_ObjectToWorld._m00_m10_m20;
				float3 scale_y = unity_ObjectToWorld._m01_m11_m21;
				float3 scale_z = unity_ObjectToWorld._m02_m12_m22;

				float sx = length(scale_x);
				float sy = length(scale_y);
				float sz = length(scale_z);

				float object_scale = (sx + sy + sz) / 3.0;
				
				return object_scale;
			}

			float4 Frag(INPUT input) : SV_Target
			{
				float2 uv = input.uv;
				
				#if defined(ADJUST_BY_MATERIAL_TILING)
				uv = TRANSFORM_TEX(uv, _MainTex);
				#endif

				#if defined(ADJUST_BY_OBJECT_DISTANCE)
				uv *= get_object_distance_factor();
				#endif
				
				#if defined(ADJUST_BY_OBJECT_SCALE)
				uv *= get_object_scale_factor();				
				#endif
				
				uv = frac(uv);

				return float4(uv, 0, 1);
			}
			
			ENDHLSL
		}
	}

	Fallback Off
}
