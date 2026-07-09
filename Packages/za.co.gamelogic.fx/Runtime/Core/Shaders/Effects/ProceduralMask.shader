/*	Blends a source texture with a processed overlay using a procedurally computed mask.
	Supports two mask shapes: a half-plane (line) and an ellipse.
	All spatial parameters are in aspect-corrected normalised screen-height units.
*/
Shader "Gamelogic/Fx/ProceduralMask"
{
	Properties
	{
		_MainTex ("Source", 2D) = "white" {}
		_OverlayTex ("Processed", 2D) = "white" {}
		_Opacity ("Effect Strength", Float) = 1.0
		_MaskType ("Mask Type (0 = HalfPlane, 1 = Ellipse)", Int) = 0
		_Center ("Center (normalised screen coords)", Vector) = (0.5, 0.5, 0, 0)
		_Angle ("Angle (degrees)", Float) = 0
		_Softness ("Softness", Float) = 0.02
		_Invert ("Invert", Float) = 0
		_Radii ("Radii (ellipse only)", Vector) = (0.3, 0.2, 0, 0)
	}

	SubShader
	{
		Tags
		{
			"RenderType"="Opaque"
			"Queue"="Overlay"
		}

		Pass
		{
			Name "ProceduralMask"
			Cull Off
			ZClip Off
			ZTest Always
			ZWrite Off

			HLSLPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag
			#pragma shader_feature GAMELOGIC_HAS_URP
			#pragma shader_feature GAMELOGIC_HAS_URP_RENDER_GRAPH

			#ifdef GAMELOGIC_HAS_URP
			#define UNITY_PIPELINE_URP 1
			#endif

			#include "Packages/za.co.gamelogic.fx/Runtime/Core/Shaders/Gamelogic.hlsl"

			DECLARE_TEX(_OverlayTex)
			float4 _OverlayTex_ST;

			float _Opacity;
			int _MaskType;
			float2 _Center;
			float _Angle;
			float _Softness;
			float _Invert;
			float2 _Radii;

			/* Signed distance from a half-plane whose boundary is a line through center
				oriented at angle_deg. Positive on the left side when looking along the line.
			*/
			float halfplane_sdf(float2 position, float2 center, float angle_deg)
			{
				float angle_rad = radians(angle_deg);
				float2 normal = float2(-sin(angle_rad), cos(angle_rad));
				return dot(position - center, normal);
			}

			/*	Signed distance from an ellipse boundary: negative inside, positive outside.
			*/
			float ellipse_sdf(float2 position, float2 center, float angle_deg, float2 radii)
			{
				float2 local = position - center;
				float angle_rad = radians(-angle_deg);
				float c = cos(angle_rad), s = sin(angle_rad);
				local = float2(local.x * c - local.y * s, local.x * s + local.y * c);
				local /= max(radii, 0.0001);
				return length(local) - 1.0;
			}

			float4 Frag(INPUT i) : SV_Target
			{
				float2 uv = UV_FROM_INPUT(i);
				float4 source  = SAMPLE_MAIN(uv);
				float4 overlay = SAMPLE(_OverlayTex, TRANSFORM_TEX(uv, _OverlayTex));

				/*	Convert to aspect-corrected coordinates so radii and softness
					are in consistent screen-height units and circles look circular.
				*/
				float aspect = _ScreenParams.x / _ScreenParams.y;
				float2 position = float2(uv.x * aspect, uv.y);
				float2 center = float2(_Center.x * aspect, _Center.y);

				float sdf;
				
				if (_MaskType == 0)
				{
					sdf = halfplane_sdf(position, center, _Angle);
				}
				else
				{
					sdf = ellipse_sdf(position, center, _Angle, _Radii);
				}

				if (_Invert > 0.5)
				{
					sdf = -sdf;
				}

				float softness = max(_Softness, 0.0001);
				float mask = smoothstep(-softness, softness, sdf);

				float4 result = lerp(source, overlay, mask * _Opacity);
				result.a = 1.0;
				return result;
			}
			ENDHLSL
		}
	}

	Fallback Off
}
