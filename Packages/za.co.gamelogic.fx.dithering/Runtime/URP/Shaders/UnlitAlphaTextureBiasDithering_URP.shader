Shader "Gamelogic/Fx/URP/Dithering/Unlit Alpha Texture Bias Dither"
{
	Properties
	{
		_MainTex("Main Texture", 2D) = "white" {}
		_MainColor("Main Color", Color) = (1,1,1,1)
		_Alpha("Alpha", Range(0,1)) = 1
		_Cutoff("Alpha Cutoff", Range(0,1)) = 0.5

		_DitherPatternTex("Dither Pattern", 2D) = "white" {}
		_DitherPatternTiling("Dither Pattern Tiling", Vector) = (1,1,0,0)

		_DitherAmountMin("Offset 0", Range(-1,1)) = -0.5
		_DitherAmountMax("Offset 1", Range(-1,1)) = 0.5

		_PixelSize("Pixel Size", Vector) = (1,1,0,0)
	}

	SubShader
	{
		Tags
		{
			"RenderType"="TransparentCutout"
			"Queue"="AlphaTest"
			"RenderPipeline"="UniversalPipeline"
		}

		Pass
		{
			Name "Unlit Alpha Texture Bias Dither"
			Tags { "LightMode"="UniversalForward" }

			Cull Back
			ZWrite On
			Blend One Zero

			HLSLPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag
			#pragma multi_compile_fog
			#pragma shader_feature GAMELOGIC_HAS_URP

			#ifdef GAMELOGIC_HAS_URP
			
			#define UNITY_PIPELINE_URP 1
			
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
			float4 _MainTex_ST;

			TEXTURE2D(_DitherPatternTex); SAMPLER(sampler_DitherPatternTex);

			float4 _MainColor;
			float _Alpha;
			float _Cutoff;

			float4 _DitherPatternTiling;

			float _DitherAmountMin;
			float _DitherAmountMax;

			float2 _PixelSize;
			
			struct Attributes
			{
				float4 positionOS : POSITION;
				float2 uv         : TEXCOORD0;
			};

			struct Varyings
			{
				float4 positionHCS : SV_POSITION;
				float2 uv : TEXCOORD0;
				float4 screenPos : TEXCOORD1;
			};

			Varyings Vert(Attributes v)
			{
				Varyings o;
				o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.screenPos = ComputeScreenPos(o.positionHCS);
				return o;
			}

			float4 Frag(Varyings i) : SV_Target
			{
				float4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * _MainColor;
				color.a *= _Alpha;

				float2 screen_uv = i.screenPos.xy / i.screenPos.w;
				float2 pixel_coord = screen_uv * _ScreenParams.xy;

				pixel_coord = floor(pixel_coord) + 1.5;
				float2 tiled_uv = pixel_coord * _DitherPatternTiling.xy + _DitherPatternTiling.zw;
				tiled_uv /= _ScreenParams.xy;
				float bias = SAMPLE_TEXTURE2D(_DitherPatternTex, sampler_DitherPatternTex, tiled_uv).r;
				float offset = lerp(_DitherAmountMin, _DitherAmountMax, bias);

				clip(color.a + offset - _Cutoff);

				return color;
			}
			
			#else
			
			#include "Packages/za.co.gamelogic.fx/Runtime/Core/Shaders/Gamelogic.hlsl"
			
			float4 Frag() : SV_Target { return YELLOW; }

			#endif
			
			ENDHLSL
		}
	}
	Fallback Off
}
