Shader "Gamelogic/Fx/PowerMean"
{
	Properties
	{
		_MainTex ("Render Texture", 2D) = "white" {}

		_KernelOffset( "Kernel Offset", float ) = 0.0
		_KernelSize ("Kernel Size", Integer) = 1.0
		_KernelJumpSize ("Kernel Jump Size", float) = 1.0
		_Direction ("Kernel Direction", Vector) = (1, 0, 0, 0)

		_Power ("Power (p-norm)", float) = 2.0
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
			Name "PowerMean"
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
			float _Power;

			float3 power_mean_filter_1D(float2 uv)
			{
				float3 color_sum = 0.0;

				for (int x = 0; x < _KernelSize; x++)
				{
					float sample_index = x + _KernelOffset;
					float2 offset = sample_index * (_Direction * TEXEL_SIZE.xy * _KernelJumpSize);
					float3 color = SAMPLE_MAIN(uv + offset).rgb;

					color_sum += pow(color, _Power);
				}

				float3 mean = color_sum / _KernelSize;
				return pow(mean, 1.0 / _Power);
			}

			float4 Frag(INPUT i) : SV_Target
			{
				float3 blurred_color = power_mean_filter_1D(UV_FROM_INPUT(i));
				return RGB1(blurred_color);
			}
			ENDHLSL
		}
	}

	Fallback Off
}
