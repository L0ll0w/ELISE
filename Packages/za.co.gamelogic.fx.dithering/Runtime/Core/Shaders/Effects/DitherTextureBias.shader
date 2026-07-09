Shader "Gamelogic/Fx/Dithering/DitherTextureBias"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}

		_DitherPatternTex ("Dither Pattern", 2D) = "white" {}

		_DitherAmountMin ("Dither Amount Min", Float) = -0.5
		_DitherAmountMax ("Dither Amount Max", Float) = 0.5

		_LevelCount ("Quantization Levels", Vector) = (1, 1, 1, 0)
		_Smoothness ("Quantization Smoothness", Range(0,1)) = 0.0
		
		_UVMap ("UV Map", 2D) = "white" {}
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
			Name "DitherTextureBias"
			Cull Off
			ZClip Off
			ZTest Always
			ZWrite Off

			HLSLPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag
			#pragma shader_feature GAMELOGIC_HAS_URP
			#pragma shader_feature GAMELOGIC_HAS_URP_RENDER_GRAPH
			
			/* Enable this to use a UV map to sample the dither textures. */
			#pragma shader_feature USE_UV_MAP
			
			/* Enable this to apply depth compensation when using a UV map. */
			#pragma shader_feature APPLY_DEPTH_COMPENSATION

			
			#ifdef GAMELOGIC_HAS_URP
				#define UNITY_PIPELINE_URP 1
			#endif

			#include "Packages/za.co.gamelogic.fx.dithering/Runtime/Core/Shaders/Dithering.hlsl"
			
			DECLARE_TEX(_DitherPatternTex)
			float4 _DitherPatternTex_ST;
			float _MipMapBias = 0.5;
			
			DECLARE_TEX(_UVMap)
			float4 _UVMap_ST;
						
			float _DitherAmountMin;
			float _DitherAmountMax;
			float _ColorScale;

			float _Smoothness;
			float _Quantization;
			int4 _LevelCount;

			float4 Frag(INPUT input) : SV_Target
			{
				float4 color = SAMPLE_MAIN(UV_FROM_INPUT(input));
				
				NEW_DEPTH_BAND_INFO(depth_band_info, _UVMap, UV_FROM_INPUT(input));
				
				Float2Pair tiled_uv;
				TRANSFORM_TEX_PAIR(depth_band_info.uv, _DitherPatternTex, tiled_uv);
					
				Float4Pair bias_pair;
				SAMPLE_BIAS_PAIR(_DitherPatternTex, tiled_uv, _MipMapBias, bias_pair);
				
				float bias = lerp(bias_pair, depth_band_info.fraction_between_bands).rgb;

				color.rgb += lerp(_DitherAmountMin, _DitherAmountMax, bias);
				color = saturate(color);
				color = quantize_smooth(color, _LevelCount.xyz, _Smoothness);

				return color;
			}
			ENDHLSL
		}
	}
}
