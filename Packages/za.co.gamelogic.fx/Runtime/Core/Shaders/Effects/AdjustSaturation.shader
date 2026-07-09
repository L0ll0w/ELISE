/*
	A post process that adjusts the saturation of the image.
	See <see href="../common/docs/effects-reference-common.html#adjust-saturation"/>.
*/
Shader "Gamelogic/Fx/AdjustSaturation"
{
	Properties
	{
		_MainTex ("Render Texture", 2D) = "white" {}
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
			Name "AdjustSaturation"
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

			/*	Saturation.
				1 = unchanged
				0 = completely grayscale.
				Values below 1 desaturate. 
				Values above 1 enhance saturation. 
			*/
			//[Range(0, 2)]
			float _Saturation;

			float4 Frag(INPUT i) : SV_Target
			{
				float4 color = SAMPLE_MAIN(UV_FROM_INPUT(i));
				float luminosity = to_luminosity(color);
				color = lerp(float4(luminosity, luminosity, luminosity, color.a), color, _Saturation);

				return color;
			}
			ENDHLSL
		}
	}

	Fallback Off
}
