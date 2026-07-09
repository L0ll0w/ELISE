/*
	A post process that adjusts the gamma of the image.
	See <see href="../common/docs/effects-reference-common.html#adjust-gamma"/>.
*/
Shader "Gamelogic/Fx/AdjustGamma"
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
			Name "AdjustGamma"
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
			
			/*	Gamma correction.
				1 = no change.
				Lower values brighten midtones, higher values darken midtones.
			*/
			//[Range(0.1, 5.0)]
			float _Gamma;

			float4 Frag(INPUT i) : SV_Target
			{
				float4 color = SAMPLE_MAIN(UV_FROM_INPUT(i));
				color = adjust_gamma(color, _Gamma);
				
				return color;
			}

			ENDHLSL
		}
	}

	Fallback Off
}
