/*
	Ordinary surface shader that uses dithered alpha cutoff instead of normal alpha blending. 
*/
Shader "Gamelogic/Fx/BuiltIn/Dithering/Alpha Matrix Bias Dither"
{
	Properties
	{
		_MainTex ("Main Texture", 2D) = "white" {}
		_MainColor ("Main Color", Color) = (1,1,1,1)
		_Alpha("Alpha", Range(0, 1)) = 1
		_Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5
		
		_DitherAmountMin ("Dither Amount Min", Range(-1, 1)) = -0.5
		_DitherAmountMax ("Dither Amount Max", Range(-1, 1)) = 0.5
		_PixelSize ("Pixel Size", Vector) = (1, 1, 0, 0)
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
		#pragma surface surf Standard alpha:clip fullforwardshadows
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
		float _DitherAmountMin;
		float _DitherAmountMax;
		float2 _PixelSize;
		
		void surf(Input input, inout SurfaceOutputStandard o)
		{
			fixed4 color = tex2D(_MainTex, input.uv_MainTex) * _MainColor;
			color.a = color.a * _Alpha; 
			float bias = sample_matrix(input.screenPos, _PixelSize);
			float offset = lerp(_DitherAmountMin, _DitherAmountMax, bias);

			clip(color.a + offset - _Cutoff);
			
			o.Albedo = color.rgb;
			o.Alpha = color.a + offset - _Cutoff > 0 ? 1 : 0; // Retain alpha for shadow casting
		}
		ENDCG
	}
	FallBack "Diffuse"
}
