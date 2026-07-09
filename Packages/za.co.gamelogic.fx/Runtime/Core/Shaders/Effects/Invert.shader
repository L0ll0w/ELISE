/*
	Inverts the colors of the image.
*/
Shader "Gamelogic/Fx/Invert"
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
			Name "Invert"
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

			float4 Frag(INPUT i) : SV_Target
			{
				float4 color = SAMPLE_MAIN(UV_FROM_INPUT(i));
				color.rgb = 1.0 - color.rgb;

				return color;
			}
			ENDHLSL
		}
	}

	Fallback Off
}
