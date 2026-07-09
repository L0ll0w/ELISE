Shader "Gamelogic/Fx/BuiltIn/Dithering/Alpha Texture Bias Dither"
{
	Properties
	{
		_MainTex ("Main Texture", 2D) = "white" {}
		_MainColor ("Main Color", Color) = (1,1,1,1)
		_Alpha("Alpha", Range(0, 1)) = 1
		_Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5
		
		_DitherPatternTex ("Dither Pattern", 2D) = "white" {}
		_DitherPatternTiling ("Dither Pattern Tiling", Vector) = (1, 1, 1, 1)
		
		_DitherAmountMin ("Offset 0", Range(-1, 1)) = -0.5
		_DitherAmountMax ("Offset 1", Range(-1, 1)) = 0.5
	}
	SubShader
	{
		Tags
		{
			"Queue" = "AlphaTest"
			"RenderType" = "TransparentCutout"
		}
		
		LOD 200

		CGPROGRAM
		#pragma surface surf Standard fullforwardshadows alpha:clip
		#pragma target 3.0
		#pragma multi_compile_fog
		
		#include "Packages/za.co.gamelogic.fx.dithering/Runtime/Core/Shaders/Dithering.hlsl"

		struct Input
		{
			float2 uv_MainTex;
			float4 screenPos;
		};
		
		float4 _MainColor;

		float _Cutoff;
		float _Alpha;
		
		sampler2D _DitherPatternTex;
		float4 _DitherPatternTiling;
		
		float _DitherAmountMin;
		float _DitherAmountMax;
		
		void surf(Input input, inout SurfaceOutputStandard o)
		{
			fixed4 color = SAMPLE(_MainTex, input.uv_MainTex) * _MainColor;
			color.a = color.a * _Alpha; 
			
			float2 screen_uv = input.screenPos.xy / input.screenPos.w;
			float2 pixelCoord = screen_uv * _ScreenParams.xy;
			
			/* 1.5 (instead of the usual 0.5) makes this correctly line up with the pixelate post effect shader.
				
				TODO: Is this a mistake in the post-effect pixelate?
				I do not want to change that^ since it works correctly with the post effect dithering. 
			*/
			pixelCoord = floor(pixelCoord) + 1.5;
			float2 tiled_uv = apply_tiling(pixelCoord, _DitherPatternTiling);
			tiled_uv /= _ScreenParams.xy;
			float bias = SAMPLE(_DitherPatternTex, tiled_uv).r;
			float offset = lerp(_DitherAmountMin, _DitherAmountMax, bias);
			
			clip(color.a + offset - _Cutoff);
			
			o.Albedo = color.rgb;
			o.Alpha = color.a;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
