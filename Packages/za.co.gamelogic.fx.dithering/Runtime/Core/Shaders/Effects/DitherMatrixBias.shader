Shader "Gamelogic/Fx/Dithering/DitherMatrixBias"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}

		_PixelSize("Pixel Size", Vector) = (1,1,0,0)
		_PixelOffset("Pixel Offset", Vector) = (0,0,0,0)
		
		_MatrixOffset("Pixel Offset", Vector) = (0,0,0,0)
		
		_LevelCount("Quantization Levels", Vector) = (1,1,1,0)
		_Smoothness("Smoothness", Float) = 0

		_DitherAmountMin("Dither Amount Min", Float) = 0
		_DitherAmountMax("Dither Amount Max", Float) = 1

		_MatrixRWidth("Matrix R Width", Int) = 2
		_MatrixRHeight("Matrix R Height", Int) = 2

		_MatrixGWidth("Matrix G Width", Int) = 2
		_MatrixGHeight("Matrix G Height", Int) = 2

		_MatrixBWidth("Matrix B Width", Int) = 2
		_MatrixBHeight("Matrix B Height", Int) = 2
	}

	SubShader
	{
		Tags { "RenderType"="Opaque" }

		Pass
		{
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

			#include "Packages/za.co.gamelogic.fx/Runtime/Core/Shaders/Gamelogic.hlsl"

			#define MAX_MATRIX_SIZE 256

			#define SAMPLE_MATRIX(NAME, ARRAY, WIDTH, HEIGHT)               \
				float NAME(float2 uv)                                      \
				{                                                          \
					int2 p = int2(uv * _ScreenParams.xy);                  \
					int2 index;                                            \
					index.x = p.x % WIDTH;                                 \
					index.y = p.y % HEIGHT;                                \
					int flatIndex = index.y * WIDTH + index.x;             \
					\
					return ARRAY[flatIndex];                               \
				}
			
			
			//int flatIndex = get_color(p.x, p.y, _MatrixOffset.x, _MatrixOffset.y, _MatrixOffset.z);       \
					
			
			static int floor_mod(int m, int n)
			{
				int mod = m % n;
				// If m is negative and mod is not 0, we adjust it because C#'s % operator
				// does not produce a floor modulus result directly.
				if (m < 0 && mod != 0)
				{
					mod += n;
				}

				return mod;
			}
			
			int get_color(int x, int y, int ux, int vx, int vy)
			{
				int colorCount = ux * vy;

				float a = (x * vy - y * vx) / (float)colorCount;
				float b = y * ux / (float)colorCount;

				int m = floor(a);
				int n = floor(b);

				int baseVectorX = m * ux + n * vx;
				int baseVectorY = n * vy;

				int offsetX = floor_mod(x - baseVectorX, ux);
				int offsetY = y - baseVectorY;

				int colorIndex = floor(offsetX + offsetY * ux);

				return colorIndex;
			}

			float _DitherAmountMin;
			float _DitherAmountMax;
			int3 _LevelCount;

			float _MatrixR[MAX_MATRIX_SIZE];
			int _MatrixRWidth;
			int _MatrixRHeight;

			float _MatrixG[MAX_MATRIX_SIZE];
			int _MatrixGWidth;
			int _MatrixGHeight;

			float _MatrixB[MAX_MATRIX_SIZE];
			int _MatrixBWidth;
			int _MatrixBHeight;

			float2 _PixelSize;
			float2 _PixelOffset;
			int3 _MatrixOffset;
			float _Smoothness;
			
			float4 _MainTex_TexelSize;

			SAMPLE_MATRIX(sample_matrix_r, _MatrixR, _MatrixRWidth, _MatrixRHeight)
			SAMPLE_MATRIX(sample_matrix_g, _MatrixG, _MatrixGWidth, _MatrixGHeight)
			SAMPLE_MATRIX(sample_matrix_b, _MatrixB, _MatrixBWidth, _MatrixBHeight)
			
			#if USE_UV_MAP
			DECLARE_TEX(_UVMap)
			float4 _UVMap_ST;
			#endif

			float4 Frag(INPUT i) : SV_Target
			{
				float4 color = SAMPLE_MAIN(UV_FROM_INPUT(i));
				float3 bias0;
				float3 bias1;
				
				float2 texel_size = _MainTex_TexelSize.xy;
				
				NEW_DEPTH_BAND_INFO(depth_band_info, _UVMap, UV_FROM_INPUT(i));

				float2 uv0 = (depth_band_info.uv.item0 - _PixelOffset * texel_size) / _PixelSize;
				float2 uv1 = (depth_band_info.uv.item1 - _PixelOffset * texel_size) / _PixelSize;
				
				//uv = pixelate_uv(uv, _PixelSize, PI/ 4, _MainTex_TexelSize);
				bias0.r = sample_matrix_r(uv0);
				bias0.g = sample_matrix_g(uv0);
				bias0.b = sample_matrix_b(uv0);
				
				
				bias1.r = sample_matrix_r(uv1);
				bias1.g = sample_matrix_g(uv1);
				bias1.b = sample_matrix_b(uv1);
				
				//return float4(bias,1);
				
				float3 bias = lerp(bias0, bias1, depth_band_info.fraction_between_bands);
				color.rgb += lerp(_DitherAmountMin, _DitherAmountMax, bias);
				color.rgb = quantize_smooth(color.rgb, _LevelCount, _Smoothness);
				color = saturate(color);

				return color;
			}
			ENDHLSL
		}
	}
}
