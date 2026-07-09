/*
	A post process that produces an outline.

	Tips:
		- Remember the alpha of both the line color and background color is respected, so for example to get
			an outline over the original image, set the background color alpha to 0 and the line color alpha to 1.
		- Finding the threshold can be tricky. Suppose the background is white and the line color is black.
			1. Set the edge factor to 1.
			2. Find a low value where the image is almost black, and a high value where the image is white. 
			3. These are your threshold bounds. Set the threshold to the average of these two values. If it is black, 
				increase the threshold,	if it is white, decrease the threshold. Continue with this until outlines start
				to appear. 
			4. If this value is inconveniently big or small, adjust the edge factor, and adjust the threshold accordingly.
*/
Shader "Gamelogic/Fx/SimpleOutline"
{
	Properties
	{
		_MainTex ("Render Texture", 2D) = "white" {}
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
			Name "SimpleOutline"
			Cull Off
			ZClip Off
			ZTest Always
			ZWrite Off

			HLSLPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag
			#pragma shader_feature GAMELOGIC_HAS_URP
			#pragma shader_feature GAMELOGIC_HAS_URP_RENDER_GRAPH
			
			/* Select which data source is used for edge detection:
				- Default scene inputs
				- Depth map 
				- Explicit source texture
			*/
			#pragma shader_feature _ USE_OUTLINE_SOURCE_TEX USE_NORMALS_TEXTURE USE_DEPTH_TEXTURE

#ifdef GAMELOGIC_HAS_URP
			#define UNITY_PIPELINE_URP 1
#endif

			#include "Packages/za.co.gamelogic.fx/Runtime/Core/Shaders/Gamelogic.hlsl"
			
			#if defined(UNITY_PIPELINE_URP) && USE_DEPTH_TEXTURE
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
			#endif

			#if defined(UNITY_PIPELINE_URP) && USE_NORMALS_TEXTURE
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
			#endif
			
			static const float3x3 _KernelMatrix = float3x3(
				-1, 0, 1,
				-2, 0, 2,
				-1, 0, 1
			);
			
			
#if USE_OUTLINE_SOURCE_TEX
			/* Alternate source texture for edge detection. */
			DECLARE_TEX(_OutlineSourceTex);
			
#elif USE_NORMALS_TEXTURE 
			#if !defined(UNITY_PIPELINE_URP)			
			sampler2D _CameraDepthNormalsTexture;
			#endif
#elif USE_DEPTH_TEXTURE
			#if !defined(UNITY_PIPELINE_URP)
			UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
			#endif
#else
			// Nothing needed here.
#endif
			
			/* Settings. */
			/*	Factor used to multiply sample offsets. A way to fake bigger kernel sizes.*/
			//[Min(0)]
			float _JumpSize = 1.0;
			
			/*	Factor used to multiply the edge value before taking a threshold. 
				Use this to make tweaking the threshold more convenient.*/
			//[Min(0)]
			float _EdgeFactor = 1;
			
			/* Threshold above which a pixel is considered an edge.*/
			//[Min(0)]
			float _Threshold = 0.5;
			
			/* Color used to draw the detected edges. 
				Alpha determines how much of the original 
				image shines through.
			*/
			//[Color]
			float4 _LineColor = float4(0, 0, 0, 1);
			
			/*	Background color when no edge is detected.
				Alpha determines how much of the original 
				image shines through.
			*/
			//[Color]
			float4 _BackgroundColor = float4(1, 1, 1, 1);
			
			#if USE_OUTLINE_SOURCE_TEX
			float3 sample(float2 uv, float2 offset, float offset_size)
			{
				return SAMPLE(_OutlineSourceTex, uv + offset * offset_size).rgb;
			}
			#elif USE_NORMALS_TEXTURE
			float3 sample(float2 uv, float2 offset, float offset_size)
			{
				#if defined(UNITY_PIPELINE_URP)
				float3 normal = SampleSceneNormals(uv + offset * offset_size);
				return normal * 0.5 + 0.5;
				#else
				float4 packed = tex2D(_CameraDepthNormalsTexture, uv + offset * offset_size);
				float3 normal = DecodeViewNormalStereo(packed);
				return normal * 0.5 + 0.5;
				#endif
			}
			#elif USE_DEPTH_TEXTURE
			float3 sample(float2 uv, float2 offset, float offset_size)
			{		
				#if defined(UNITY_PIPELINE_URP)
				float raw = SampleSceneDepth(uv + offset * offset_size);
				float depth = LinearEyeDepth(raw, _ZBufferParams);
				#else
				float raw = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv + offset * offset_size);
				float depth = LINEAR_EYE_DEPTH(raw);
				#endif
				
				return (depth * rcp(depth + 1.0)).rrr;
			}
			#else
			float3 sample(float2 uv, float2 offset, float offset_size)
			{
				return SAMPLE_MAIN(uv + offset * offset_size).rgb;
			}
			#endif
			
			
			float4 Frag(INPUT input) : SV_Target
			{
				float2 uv = UV_FROM_INPUT(input);
				float2 offset_size = _JumpSize * TEXEL_SIZE.xy;
				
				float3 sample0 = sample(uv, float2(-1, -1), offset_size);
				float3 sample1 = sample(uv, float2(0, -1), offset_size);
				float3 sample2 = sample(uv, float2(1, -1), offset_size);
				float3 sample3 = sample(uv, float2(-1, 0), offset_size);
				float3 sample4 = sample(uv, float2(0, 0), offset_size);
				float3 sample5 = sample(uv, float2(1, 0), offset_size);
				float3 sample6 = sample(uv, float2(-1, 1), offset_size);
				float3 sample7 = sample(uv, float2(0, 1), offset_size);
				float3 sample8 = sample(uv, float2(1, 1), offset_size);
				
				float3 color_x0 = sample0 * _KernelMatrix[0][0];
				float3 color_x1 = sample1 * _KernelMatrix[0][1];
				float3 color_x2 = sample2 * _KernelMatrix[0][2];
				float3 color_x3 = sample3 * _KernelMatrix[1][0];
				float3 color_x4 = sample4 * _KernelMatrix[1][1];
				float3 color_x5 = sample5 * _KernelMatrix[1][2];
				float3 color_x6 = sample6 * _KernelMatrix[2][0];
				float3 color_x7 = sample7 * _KernelMatrix[2][1];
				float3 color_x8 = sample8 * _KernelMatrix[2][2];
				
				float3 color_y0 = sample0 * _KernelMatrix[0][0];
				float3 color_y1 = sample1 * _KernelMatrix[1][0];
				float3 color_y2 = sample2 * _KernelMatrix[2][0];
				float3 color_y3 = sample3 * _KernelMatrix[0][1];
				float3 color_y4 = sample4 * _KernelMatrix[1][1];
				float3 color_y5 = sample5 * _KernelMatrix[2][1];
				float3 color_y6 = sample6 * _KernelMatrix[0][2];
				float3 color_y7 = sample7 * _KernelMatrix[1][2];
				float3 color_y8 = sample8 * _KernelMatrix[2][2];
				
#if USE_OUTLINE_SOURCE_TEX || USE_DEPTH_TEXTURE || USE_NORMALS_TEXTURE
				float4 color = SAMPLE_MAIN(uv);
#else
				float4 color = RGB1(sample4);
#endif
				
				float3 sum_x 
					= color_x0 + color_x1 + color_x2 
					+ color_x3 + color_x4 + color_x5 
					+ color_x6 + color_x7 + color_x8;
				
				float3 sum_y 
					= color_y0 + color_y1 + color_y2 
					+ color_y3 + color_y4 + color_y5 
					+ color_y6 + color_y7 + color_y8;
				
				//return float4(-sum_x.r*10, sum_y.r, -sum_x.r, 1);
				
				float edge = sqrt(dot(sum_x, sum_x) + dot(sum_y, sum_y)) * _EdgeFactor;
				
				//return float4(edge, edge * 10, edge * 100, 1); // Debug output
				
				return 
					edge > _Threshold 
						? lerp(color, _LineColor, _LineColor.a) 
						: lerp(color, _BackgroundColor, _BackgroundColor.a);
			}
			ENDHLSL
		}
	}

	Fallback Off
}
