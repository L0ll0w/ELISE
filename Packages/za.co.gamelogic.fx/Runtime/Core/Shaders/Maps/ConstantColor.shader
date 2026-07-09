/*
	Renders out a constant color.

	Unlike many of the other map shaders, this shader does not compute its own data. It is intended to be 
	computed by the CPU. This shader therefore can be used to build different maps.  
*/
Shader "Gamelogic/Fx/Maps/ConstantColor"
{
	Properties
	{
		_MainTex ("Render Texture", 2D) = "white" {}
		_Color ("Color", Color) = (1,1,0,1)
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
			Name "ConstantColor"
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

			/*	The constant color to render.
			*/
			//[Color]
			float4 _Color;

			float4 Frag(INPUT input) : SV_Target
			{
				return _Color;
			}
			ENDHLSL
		}
	}

	Fallback Off
}
