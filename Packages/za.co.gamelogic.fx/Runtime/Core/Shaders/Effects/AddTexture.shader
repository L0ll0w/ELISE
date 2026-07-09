Shader "Gamelogic/Fx/AddTexture"
{
	Properties
	{
		_MainTex ("Render Texture", 2D) = "white" {}
		_OverlayTex ("Overlay Texture", 2D) = "white" {}
		_Min ("Min", Vector) = (-.1, -.1, -.1, 0)
		_Max ("Max", Vector) = (.1, .1, .1, 0)
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
			Name "AddTexture"
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
			float4 _Min;
			float4 _Max;

			float4 Frag(INPUT i) : SV_Target
			{
				float2 uv = UV_FROM_INPUT(i);
				float4 color = SAMPLE_MAIN(uv);
				float2 overlay_uv = apply_tiling(uv, _OverlayTex_ST);
				float4 overlay = SAMPLE(_OverlayTex, overlay_uv);
				color += lerp(_Min, _Max, overlay);

				return color;
			}
			ENDHLSL
		}
	}

	Fallback Off
}
