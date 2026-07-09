Shader "Gamelogic/Fx/ConvexHullMap"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_LevelCount ("Level Count", Integer) = 3
		_BackgroundColor ("Background Color", Color) = (0.5, 0.5, 0.5, 1)

		_PrimaryColor0 ("Primary Color 1", Color) = (1, 0, 1, 1)
		_PrimaryColor1 ("Primary Color 2", Color) = (1, 1, 0, 1)
		_PrimaryColor2 ("Primary Color 3", Color) = (0, 1, 1, 1)
		_PrimaryColor3 ("Primary Color 4", Color) = (1, 1, 1, 1)
		_PrimaryColor4 ("Primary Color 5", Color) = (0, 0, 0, 1)
		_PrimaryColor5 ("Primary Color 6", Color) = (0.5, 0.5, 0.5, 1)
		_PrimaryColor6 ("Primary Color 7", Color) = (0.2, 0.2, 0.2, 1)
		_PrimaryColor7 ("Primary Color 8", Color) = (0.8, 0.8, 0.8, 1)
		_PrimaryColor8 ("Primary Color 9", Color) = (0.3, 0.3, 0.3, 1)
		_PrimaryColor9 ("Primary Color 10", Color) = (0.1, 0.1, 0.1, 1)
		_PrimaryColorCount ("Primary Color Count", Integer) = 5
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
			Name "ConvexHullMap"
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
			#define PRIMARY_COLOR_COUNT 10

			int _LevelCount;
			float3 _PrimaryColor0;
			float3 _PrimaryColor1;
			float3 _PrimaryColor2;
			float3 _PrimaryColor3;
			float3 _PrimaryColor4;
			float3 _PrimaryColor5;
			float3 _PrimaryColor6;
			float3 _PrimaryColor7;
			float3 _PrimaryColor8;
			float3 _PrimaryColor9;
			
			float3 _BackgroundColor;
			int _PrimaryColorCount;

			float4 Frag(INPUT input) : SV_Target
			{
				float3 color = SAMPLE_MAIN(UV_FROM_INPUT(input)).rgb;
				float3 background = _BackgroundColor;

				float3 primaries[PRIMARY_COLOR_COUNT] =
				{
					_PrimaryColor0,
					_PrimaryColor1,
					_PrimaryColor2,
					_PrimaryColor3,
					_PrimaryColor4,
					_PrimaryColor5,
					_PrimaryColor6,
					_PrimaryColor7,
					_PrimaryColor8,
					_PrimaryColor9
				};

				int k;

				for (int i = 0; i < _LevelCount; i++)
				{
					for (int j = 0; j < _PrimaryColorCount; j++)
					{
						float3 primary = primaries[j];
						float3 background_diff = color - background;
						float3 primary_diff = primary - background;
						float numerator = dot(background_diff, primary_diff);
						float denominator = dot(primary_diff, primary_diff);

						if (denominator < 0.00001)
						{
							continue;
						}

						float t = numerator / denominator;
						t = clamp(t, 0, 1);
						background = lerp(background, primary, t);
						k++;
					}
				}

				return RGB1(background);
			}
			ENDHLSL
		}
	}

	Fallback Off
}
