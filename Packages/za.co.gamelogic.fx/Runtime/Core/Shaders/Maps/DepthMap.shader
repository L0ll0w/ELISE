/*	Renders a depth map from the camera's perspective.
	Supports both linear and logarithmic depth encoding.
*/

Shader "Gamelogic/Fx/Maps/DepthMap"
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
			
			#if defined(UNITY_PIPELINE_URP)
			struct appdata1
			{
				float3 position_os : POSITION;
			};

			struct v2f1
			{
				float4 pos : SV_POSITION;
				float  eye_depth : TEXCOORD0;
			};

			v2f1 Vert(appdata1 v)
			{
				v2f1 o;
				float3 pos_ws = TransformObjectToWorld(v.position_os);
				float4 pos_vs = mul(UNITY_MATRIX_V, float4(pos_ws, 1));
				o.pos = TransformWorldToHClip(pos_ws);
				o.eye_depth = -pos_vs.z;
				return o;
			}
			#else
			struct appdata1
			{
				float4 vertex : POSITION;
			};

			struct v2f1
			{
				float4 pos : SV_POSITION;
				float  eye_depth : TEXCOORD0;
			};

			v2f1 Vert(appdata1 v)
			{
				v2f1 o;
				float4 pos_ws = mul(unity_ObjectToWorld, v.vertex);
				float4 pos_vs = mul(UNITY_MATRIX_V, pos_ws);
				o.pos = UnityObjectToClipPos(v.vertex);
				o.eye_depth = -pos_vs.z;
				return o;
			}
			#endif
			
			/* Encoding:
				0 - Linear depth (near..far)
				1 - Logarithmic depth
			*/
			int _Encoding;

			/* Camera near plane distance. */
			float _NearPlane;
			
			/* Camera far plane distance. */
			float _FarPlane;

			float4 Frag(v2f1 input) : SV_Target
			{
				float depth = max(input.eye_depth, _NearPlane);

				float normalized_depth = (depth - _NearPlane) / max(_FarPlane - _NearPlane, 0.0001);

				if (_Encoding == 1)
				{
					float log_near = log(_NearPlane + 1.0);
					float log_far  = log(_FarPlane  + 1.0);
					normalized_depth =(log(depth + 1.0) - log_near) / max(log_far - log_near, 0.0001);
				}

				normalized_depth = saturate(normalized_depth);
				return float4(normalized_depth, normalized_depth, normalized_depth, 1);
			}
			ENDHLSL
		}
	}
}
