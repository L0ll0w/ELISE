Shader "Gamelogic/Fx/GaussianBlur"
{
	Properties
	{
		_MainTex ("Render Texture", 2D) = "white" {}

		_KernelOffset( "Kernel Offset", float ) = 0.0
		_KernelSize ("Kernel Size", Integer) = 1.0
		_KernelJumpSize ("Kernel Jump Size", float) = 1.0
		_Direction ("Kernel Direction", Vector) = (1, 0, 0, 0)

		_Sigma ("Sigma", float) = 1.0
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
			Name "GaussianBlur"
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
			float _Sigma;

			float gaussian(float value, float sigma)
			{
				float sigma_sqr = sigma * sigma;
				float decay = exp(-((value * value) / (2 * sigma_sqr)));
				float normalizator = 1.0 / sqrt(2.0 * PI * sigma_sqr);
				return normalizator * decay;
			}

			float3 gaussian_blur_1D(float2 uv)
			{
				float3 color_sum = 0.0;
				float weight_sum = 0.0;

				float2 unit_offset = _Direction * TEXEL_SIZE.xy * _KernelJumpSize;

				for (int x = 0; x < _KernelSize; x++)
				{
					float sample_index = x + _KernelOffset;
					float2 offset = sample_index * unit_offset;

					float weight = gaussian(sample_index, _Sigma);

					color_sum += SAMPLE_MAIN(uv + offset).rgb * weight;
					weight_sum += weight;
				}

				return color_sum / weight_sum;
			}

			float4 Frag(INPUT i) : SV_Target
			{
				float3 blurred_color = gaussian_blur_1D(UV_FROM_INPUT(i));
				return RGB1(blurred_color);
			}
			ENDHLSL
		}
	}

	Fallback Off
}
