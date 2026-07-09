Shader "Gamelogic/Fx/Min"
{
	Properties
	{
		_MainTex ("Render Texture", 2D) = "white" {}

		_KernelOffset( "Kernel Offset", float ) = 0.0
		_KernelSize ("Kernel Size", Integer) = 1.0
		_KernelJumpSize ("Kernel Jump Size", float) = 1.0
		_Direction ("Kernel Direction", Vector) = (1, 0, 0, 0)
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
			Name "Min"
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

			float3 min_filter_1D(float2 uv)
			{
				const float large_number = 1e30;
				float3 min_color = float3(large_number, large_number, large_number);

				float2 unit_offset = _Direction * TEXEL_SIZE.xy * _KernelJumpSize;

				for (int x = 0; x < _KernelSize; x++)
				{
					float2 offset = (x + _KernelOffset) * unit_offset;
					float3 color = SAMPLE_MAIN(uv + offset).rgb;

					min_color = min(min_color, color);
				}

				return min_color;
			}

			float4 Frag(INPUT i) : SV_Target
			{
				float3 blurred_color = min_filter_1D(UV_FROM_INPUT(i));
				return RGB1(blurred_color);
			}
			ENDHLSL
		}
	}

	Fallback Off
}
