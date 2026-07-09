Shader "Gamelogic/Fx/Pixelate"
{
	Properties
	{
		_MainTex ("Render Texture", 2D) = "white" {}
		_PixelSize ("Pixel Size", Vector) = (2, 2, 0, 0)
		_PixelOffset ("Pixel Offset", Vector) = (0, 0, 0, 0)
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
			Name "Pixelate"
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

			float2 _PixelSize;
			float2 _PixelOffset;

			float4 Frag(INPUT i) : SV_Target
			{
				float2 uv = UV_FROM_INPUT(i);
				float2 texel_size = TEXEL_SIZE.xy;
				
				uv -= _PixelOffset * texel_size;
				uv = pixelate_uv(uv, _PixelSize, 0, texel_size);
				
				
				float4 color = SAMPLE_MAIN(uv);

				return color;
			}
			ENDHLSL
		}
	}

	Fallback Off
}
