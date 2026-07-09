/* Renders out the background of a UV map. 
*/
Shader "Gamelogic/Fx/Maps/UVBackground"
{
	Properties
	{
		_Tiling ("UV Tiling", Vector) = (1, 1, 0, 0)
	}
	
	SubShader
	{
		Pass
		{
			ZWrite Off
			ZTest Always
			Cull Off

			HLSLPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag
			#pragma shader_feature GAMELOGIC_HAS_URP

			#ifdef GAMELOGIC_HAS_URP
			#define UNITY_PIPELINE_URP 1
			#endif
			
			#include "Packages/za.co.gamelogic.fx/Runtime/Core/Shaders/Gamelogic.hlsl"
			
			#if defined(UNITY_PIPELINE_URP)
			
			struct v2f1
			{
				float4 position_cs : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			v2f1 Vert(uint vertex_id : SV_VertexID)
			{
				v2f1 o;

				// Fullscreen triangle
				float2 pos = float2(
					(vertex_id == 2) ? 3.0 : -1.0,
					(vertex_id == 1) ? 3.0 : -1.0
				);

				o.position_cs = float4(pos, 0.0, 1.0);
				o.uv = pos * 0.5 + 0.5;

				return o;
			}
			
			#else

			struct v2f1
			{
				float4 pos : SV_POSITION;
				float2 uv  : TEXCOORD0;
			};

			v2f1 Vert(uint id : SV_VertexID)
			{
				float2 p = float2((id << 1) & 2, id & 2);
				v2f1 o;
				o.pos = float4(p * 2 - 1, 0, 1);
				o.uv  = p;
				return o;
			}
			#endif
			
			/*	The tiling of the background.
			*/
			float4 _Tiling;

			float4 Frag(v2f1 i) : SV_Target
			{
				return float4(i.uv * _Tiling.xy + _Tiling.zw, 0, 1);
			}
			ENDHLSL
		}
	}
}
