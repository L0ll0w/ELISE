/*
	A smooth 4-way threshold shader using inverse lerp (linear blend).
	Interpolates between Low–Mid0, Mid0–Mid1, and Mid1–High based on lightness.
	Values below LowValue use LowColor, above HighValue use HighColor.
*/
Shader "Gamelogic/Fx/QuadToneMap"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
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
			Name "QuadToneMap"
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

			/* Color used for values below _LowValue. */
			//[Color]
			float4 _LowColor;

			/* Color used for the first mid range (between _LowValue and _Mid0Value). */
			//[Color]
			float4 _Mid0Color;

			/* Color used for the second mid range (between _Mid0Value and _Mid1Value). */
			//[Color]
			float4 _Mid1Color;

			/* Color used for values above _Mid1Value. */
			//[Color]
			float4 _HighColor;

			/* Lower threshold for lightness. */
			//[Range(0, 1)]
			float _LowValue;

			/* Threshold separating low and mid0 regions. */
			//[Range(0, 1)]
			float _Mid0Value;

			/* Threshold separating mid0 and mid1 regions. */
			//[Range(0, 1)]
			float _Mid1Value;

			/* Upper threshold for lightness. */
			//[Range(0, 1)]
			float _HighValue;

			float4 Frag(INPUT i) : SV_Target
			{
				float4 color = SAMPLE_MAIN(UV_FROM_INPUT(i));

				color = quad_tone_map(
					color,
					_LowColor,
					_Mid0Color,
					_Mid1Color,
					_HighColor,
					_LowValue,
					_Mid0Value,
					_Mid1Value,
					_HighValue);

				return color;
			}
			ENDHLSL
		}
	}
}
