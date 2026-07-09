Shader "Gamelogic/Fx/BlendTexture"
{
	Properties
	{
		_MainTex ("Render Texture", 2D) = "white" {}
		_OverlayTex ("Overlay Texture", 2D) = "white" {}
		_Opacity ("Overlay Opacity", Float) = 1.0
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
			Name "BlendTexture"
			Cull Off
			ZClip Off
			ZTest Always
			ZWrite Off

			HLSLPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag
			#pragma shader_feature GAMELOGIC_HAS_URP
			#pragma shader_feature GAMELOGIC_HAS_URP_RENDER_GRAPH
			
			/* When USE_MASK is defined, the shader will use _MaskTex to control the blending strength per pixel.
			*/
			#pragma shader_feature USE_MASK
			
			/* When IGNORE_OVERLAY_ALPHA is defined, the alpha channel of _OverlayTex will be ignored when calculating overlay strength.
			*/
			#pragma shader_feature IGNORE_OVERLAY_ALPHA
			
			#ifdef GAMELOGIC_HAS_URP
			#define UNITY_PIPELINE_URP 1
			#endif

			#include "Packages/za.co.gamelogic.fx/Runtime/Core/Shaders/Gamelogic.hlsl"

			DECLARE_TEX(_OverlayTex)
			float4 _OverlayTex_ST;
			
			#if defined(USE_MASK)
			DECLARE_TEX(_MaskTex)
			float4 _MaskTex_ST;
			#endif
			
			float _Opacity;

			float4 Frag(INPUT i) : SV_Target
			{
				float2 uv = UV_FROM_INPUT(i);
				float4 color = SAMPLE_MAIN(uv);
				float4 overlay = SAMPLE(_OverlayTex, TRANSFORM_TEX(uv, _OverlayTex));
				float overlay_strength = _Opacity;
				
				#if !defined(IGNORE_OVERLAY_ALPHA)
				overlay_strength *= overlay.a;
				#endif
				
				#if defined(USE_MASK)
				float mask_value = SAMPLE(_MaskTex, TRANSFORM_TEX(uv, _MaskTex)).r;
				overlay_strength *= mask_value;
				#endif
								
				color.rgb = lerp(color.rgb, overlay.rgb, overlay_strength);

				return color;
			}
			ENDHLSL
		}
	}

	Fallback Off
}
