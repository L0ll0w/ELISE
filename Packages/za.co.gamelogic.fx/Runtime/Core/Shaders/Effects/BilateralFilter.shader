/**
	Filter that smooths and denoises non-edge areas.
	Test it with a noisy image, you should see smoothed non-edge areas and preserved edges.
	ref: https://en.wikipedia.org/wiki/Bilateral_filter
*/
Shader "Gamelogic/Fx/BilateralFilter"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}

		_KernelOffset( "Kernel Offset", float ) = 0.0
		_KernelSize ("Kernel Size", Integer) = 1.0
		_KernelJumpSize ("Kernel Jump Size", float) = 1.0
		_Direction ("Kernel Direction", Vector) = (1, 0, 0, 0)

		_SpatialSigma ("Spatial Sigma", Range(0.1, 10.0)) = 2.0
		_RangeSigma ("Range Sigma", Range(0.01, 1.0)) = 0.1
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
			Name "BilateralFilter"
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

			float _KernelOffset;
			int _KernelSize;
			float _KernelJumpSize;
			float2 _Direction;
			float _SpatialSigma;
			float _RangeSigma;

			float gaussian_spatial(float2 offset, float sigma)
			{
				float distance_sqr = dot(offset, offset);
				return exp(-distance_sqr / (2.0 * sigma * sigma));
			}

			float gaussian_color_diff(float3 color_diff, float sigma)
			{
				float color_diff_sqr = dot(color_diff, color_diff);
				return exp(-color_diff_sqr / (2.0 * sigma * sigma));
			}

			float4 bilateral_filter(float2 uv)
			{
				const float2 unit_pixel_offset = _Direction * _KernelJumpSize;
				const float2 unit_offset = unit_pixel_offset * TEXEL_SIZE.xy;

				float4 center_color = SAMPLE_MAIN(uv);
				float4 sum_color = 0;
				float sum_weight = 0;

				for (int x = 0; x < _KernelSize; x++)
				{
					float sample_index = x + _KernelOffset;
					float2 offset_uv = sample_index * unit_offset;
					float2 sample_uv = uv + offset_uv;
					float4 neighbor_color = SAMPLE_MAIN(sample_uv);
					float3 color_diff = center_color.rgb - neighbor_color.rgb;
					float2 pixel_offset = sample_index * unit_pixel_offset;
					float color_diff_weight = gaussian_color_diff(color_diff, _RangeSigma);
					float spatial_weight = gaussian_spatial(pixel_offset, _SpatialSigma);
					float weight = spatial_weight * color_diff_weight;

					sum_color += neighbor_color * weight;
					sum_weight += weight;
				}

				if (sum_weight > 0.0)
				{
					sum_color /= sum_weight;
				}
				
				return sum_color;
			}

			float4 Frag(INPUT i) : SV_Target
			{
				return bilateral_filter(UV_FROM_INPUT(i));
			}
			ENDHLSL
		}
	}
}
