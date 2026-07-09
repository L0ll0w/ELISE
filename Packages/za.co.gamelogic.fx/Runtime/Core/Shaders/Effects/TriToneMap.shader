/*
	A smooth 3-way threshold shader using inverse lerp (linear blend).
	Interpolates between LowColor–MidColor and MidColor–HighColor based on lightness.
	Values below LowValue use LowColor, above HighValue use HighColor.
*/
Shader "Gamelogic/Fx/TriToneMap"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}

		_LowColor ("Low Color", Color) = (0,0,0.3,1)
		_MidColor ("Mid Color", Color) = (0.7,0.3,0.5,1)
		_HighColor ("High Color", Color) = (1,1,.7,1)

		_LowValue ("Low Value", Range(0, 1)) = 0.0
		_MidValue ("Mid Value", Range(0, 1)) = 0.5
		_HighValue ("High Value", Range(0, 1)) = 1.0		
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
			Name "TriToneMap"
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

			float4 _LowColor;
			float4 _MidColor;
			float4 _HighColor;

			float _LowValue;
			float _MidValue;
			float _HighValue;

			float4 Frag(INPUT i) : SV_Target
			{
				float4 color = SAMPLE_MAIN(UV_FROM_INPUT(i));
				color = tri_tone_map(color, _LowColor, _MidColor, _HighColor, _LowValue, _MidValue, _HighValue);

				return color;
			}
			ENDHLSL
		}
	}
}
