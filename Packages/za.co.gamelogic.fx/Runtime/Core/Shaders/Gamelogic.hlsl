#ifndef GAMELOGIC_HLSL_INCLUDED
#define GAMELOGIC_HLSL_INCLUDED

/*
	Pipeline specifics
*/
// Question: Should we prefix these macros with GAMELOGIC_ to avoid conflicts?
#if defined(UNITY_PIPELINE_URP)
	//#error "UNITY_PIPELINE_URP"

	#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
	#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

#ifdef GAMELOGIC_HAS_URP_RENDER_GRAPH

#endif 

	#define SAMPLE(tex, uv) SAMPLE_TEXTURE2D(tex, sampler##tex, uv)
	#define SAMPLE_BIAS(tex, uv, bias) SAMPLE_TEXTURE2D_BIAS(tex, sampler##tex, uv, bias)
	#define TO_CLIP(pos) TransformObjectToHClip(pos)
	#define INPUT Varyings

	#define DECLARE_TEX(name) \
	TEXTURE2D(name);     \
	SAMPLER(sampler##name);

#ifdef GAMELOGIC_HAS_URP_RENDER_GRAPH
	#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
	#define UV_FROM_INPUT(input) input.texcoord
	#define SAMPLE_MAIN(uv) SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, (uv)).rgba
#else
	

	struct Attributes
	{
		float4 positionOS : POSITION;
		float2 uv : TEXCOORD0;
	};

	struct Varyings
	{
		float4 positionHCS : SV_POSITION;
		float2 uv : TEXCOORD0;
	};

	Varyings Vert(Attributes input)
	{
		Varyings o;
		o.positionHCS = TO_CLIP(input.positionOS.xyz);
		o.uv = input.uv;
		return o;
	}

	TEXTURE2D(_MainTex);
	SAMPLER(sampler_MainTex);

	#define SAMPLE_MAIN(uv) SAMPLE(_MainTex, (uv))
	#define UV_FROM_INPUT(input) (input.uv)
#endif

#elif defined(UNITY_PIPELINE_HDRP)
	#error "UNITY_PIPELINE_HDRP"
#else
	//#error "UNITY_PIPELINE_BUILTIN"

	#include "UnityCG.cginc"
	#define SAMPLE(tex, uv) tex2D(tex, uv)
	#define SAMPLE_BIAS(tex, uv, bias) tex2Dbias(tex, float4(uv, 0.0, bias))

	#define TO_CLIP(pos) UnityObjectToClipPos(pos)
	
	#define DECLARE_TEX(name) \
	sampler2D name;

	#define PI 3.14159265359


	struct appdata
	{
		float4 vertex : POSITION;
		float2 uv : TEXCOORD0;
	};

	struct v2f
	{
		float4 pos : SV_POSITION;
		float2 uv : TEXCOORD0;
		float3 worldPos: TEXCOORD1;
	};

	v2f Vert(appdata v)
	{
		v2f o;
		o.pos = TO_CLIP(v.vertex);
		o.uv = v.uv;
		o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

		return o;
	}

	#define INPUT v2f

	sampler2D _MainTex;
	#define SAMPLE_MAIN(uv) SAMPLE(_MainTex, (uv))
	#define UV_FROM_INPUT(input) input.uv
#endif

#if defined(UNITY_PIPELINE_URP)
#define LINEAR_EYE_DEPTH(raw) LinearEyeDepth(raw, _ZBufferParams)
#else
#define LINEAR_EYE_DEPTH(raw) LinearEyeDepth(raw)
#endif

#define TEXEL_SIZE float4(1.0 / _ScreenParams.x, 1.0 / _ScreenParams.y, _ScreenParams.x, _ScreenParams.y)

/*
	Types.
*/
struct Float2Pair
{
	float2 item0;
	float2 item1;
};

struct DepthBandInfo
{
	float eye_depth;
	float log_depth;
	float fraction_between_bands;
	Float2Pair uv;
};

struct FloatPair
{
	float item0;
	float item1;
};

struct Float4Pair
{
	float4 item0;
	float4 item1;
};




/*
	Constants.
*/

#define PHI	1.61803398875

#define RED float4(1, 0, 0, 1)
#define GREEN float4(0, 1, 0, 1)
#define BLUE float4(0, 0, 1, 1)
#define WHITE float4(1, 1, 1, 1)
#define BLACK float4(0, 0, 0, 1)
#define YELLOW float4(1, 1, 0, 1)
#define CYAN float4(0, 1, 1, 1)
#define MAGENTA float4(1, 0, 1, 1)

#define WHITE_INV BLACK
#define BLACK_INV WHITE

#define DEPTH_EPSILON 1e-3

#define DEFAULT_ORIENTATION_TILE_SIZE 8

/*
	Macros.
*/
#define RGB1(color) float4((color).rgb, 1)
#define RGB0(color) float4((color).rgb, 0)

#if USE_UV_MAP 
#if APPLY_DEPTH_COMPENSATION
#define NEW_DEPTH_BAND_INFO(band_info, uv_map, input_uv)\
	DepthBandInfo band_info;\
	float2 __uv = SAMPLE(uv_map, TRANSFORM_TEX(input_uv, uv_map)).xy;\
	__uv = fix_wrapping_in_quad(__uv);\
	band_info = build_depth_band_info(input_uv, __uv);
#else
#define NEW_DEPTH_BAND_INFO(band_info, uv_map, input_uv)\
	DepthBandInfo band_info;\
	float2 __uv = SAMPLE(uv_map, TRANSFORM_TEX(input_uv, uv_map)).xy;\
	band_info = build_depth_band_info_without_scale(__uv);
#endif
#else
#define NEW_DEPTH_BAND_INFO(band_info, uv_map, input_uv)\
	DepthBandInfo band_info;\
	band_info = build_depth_band_info_without_scale(input_uv);
#endif

#if USE_ORIENTATION_MAP
#define NEW_BASE_ORIENTATION(orientation, depth_band_info)\
float2 uv = make_quad_coherent(depth_band_info.uv.item0, 16);\
float4 orientation1 = SAMPLE(_OrientationMap, TRANSFORM_TEX(uv, _OrientationMap));\
float base_orientation1 = orientation1.r * PI + radians(_BaseOrientation);\
FloatPair orientation;\
orientation.item0 = base_orientation1;\
orientation.item1 = base_orientation1;
#else
#define NEW_BASE_ORIENTATION(orientation, depth_band_info)\
	FloatPair orientation;\
	orientation.item0 = radians(_BaseOrientation);\
	orientation.item1 = radians(_BaseOrientation);
#endif

float sqr(float x)
{
	return x * x;
}

/*
FloatPair orientation;\
	Float2Pair uvs = depth_band_info.uv;\
	uvs.item0 = make_quad_coherent(uvs.item0, 16);\
	uvs.item1 = make_quad_coherent(uvs.item1, 16);\
	Float2Pair transformed_uvs;\
	TRANSFORM_TEX_PAIR(uvs, _OrientationMap, transformed_uvs);\
	\
	Float4Pair samples;\
	SAMPLE_PAIR(_OrientationMap, transformed_uvs, samples);\
	orientation = get_r(samples);\
	orientation = multiply(orientation, PI);\
	orientation = add(orientation, radians(_BaseOrientation));
*/
/*
	Functions.
*/
float3 adjust_gamma(float3 color, float gamma)
{	
	return abs(gamma - 1) < 0.1f ? color.rgb : pow(color.rgb, 1.0 / gamma);
}



float4 adjust_gamma(float4 color, float gamma)
{
	return float4(adjust_gamma(color.rgb, gamma), 1);
}

float to_luminosity(float3 color)
{
	const float3 luma = float3(0.299, 0.587, 0.114);
	return dot(color, luma);
}

float to_luminosity(float4 color)
{
	return to_luminosity(color.rgb);
}


float3 desaturate(float3 color)
{
	float luminosity = to_luminosity(color);	
	return float3(luminosity, luminosity, luminosity);
}

float4 desaturate(float4 color)
{
	return float4(desaturate(color.rgb), 1);
}

float3 quantize(float3 color, int3 level_count)
{
	return floor(color * level_count) / level_count;
}

float3 quantize_smooth(float3 color, int3 level_count, float smoothness)
{
	smoothness = saturate(smoothness);
	
	int3 threshold_count = level_count - 1;

	if (smoothness <= 0.0001)
	{
		return round(color * threshold_count) / threshold_count;
	}

	float3 level_frac_part = frac(color * threshold_count);
	float left_edge = 0.5 - smoothness * 0.5;
	float right_edge = 0.5 + smoothness * 0.5;
	float3 edge_transition = smoothstep(left_edge, right_edge, level_frac_part);

	float3 quantized = floor(color * threshold_count) / threshold_count;
	float3 next_quantized = (floor(color * threshold_count) + 1.0) / threshold_count;
	float3 smoothed = lerp(quantized, next_quantized, edge_transition);
	
	return smoothed;
}

float2 rotate(float2 uv, float angle)
{
	float cos_a = cos(angle);
	float sin_a = sin(angle);
	return float2(uv.x * cos_a - uv.y * sin_a, uv.x * sin_a + uv.y * cos_a);
}

float4 quantize_smooth(float4 color, int3 level_count, float smoothness)
{
	return float4(quantize_smooth(color.rgb, level_count, smoothness), 1);
}

float2 pixelate_uv(float2 uv, float2 texel_size)
{
	return (floor(uv / texel_size) + 0.5) * texel_size;
}

float2 pixelate_uv(float2 uv, float2 scale, float angle, float2 texel_size)
{
	uv /= texel_size;
	uv /= scale;
	uv = rotate(uv, -angle);

	uv = floor(uv) + 0.5;

	uv = rotate(uv, angle);
	uv *= scale;
	uv *= texel_size;
	
	return uv;
}

float2 pixelate_uv(float2 uv, float2 factor, float2 texel_size)
{
	float2 pixel_size = texel_size * factor;
	return (floor(uv / pixel_size) + 0.5) * pixel_size;
}

float2 apply_tiling(float2 uv, float4 tiling)
{
	return uv * tiling.xy + tiling.zw;
}

/*
	Internal Functions.
*/

/**
	Maps a color to a gradient defined by three colors (low, mid, high) based on the luminosity of the input color.

	@param color The input color to be mapped.
	@param low_color The color representing the low end of the gradient.
	@param mid_color The color representing the middle of the gradient.
	@param high_color The color representing the high end of the gradient.
	@param low_value The luminosity value corresponding to the low_color.
	@param mid_value The luminosity value corresponding to the mid_color.
	@param high_value The luminosity value corresponding to the high_color.
	@return The resulting color after mapping to the gradient.
*/
float4 tri_tone_map(float4 color, float4 low_color, float4 mid_color, float4 high_color, float low_value, float mid_value, float high_value)
{
	float4 result;
	float luminosity = to_luminosity(color.rgb);

	if (luminosity < mid_value)
	{
		// TODO @herman this line below represents an inverse_lerp(a, b, t). Should we add such function?
		// e.g: float t = inverse_lerp(_LowValue, _MidValue, luminosity);
		float t = (luminosity - low_value) / (mid_value - low_value);
		t = saturate(t);
		result = lerp(low_color, mid_color, t);
		result = lerp(result, color, 1 - result.a);
	}
	else
	{
		float t = (luminosity - mid_value) / (high_value - mid_value);
		t = saturate(t);
		result = lerp(mid_color, high_color, t);
		result = lerp(result, color, 1 - result.a);
	}

	result.a = 1.0;
	return result;
}

/**
	Maps a color to a gradient defined by four colors (low, mid0, mid1, high) based on the luminosity of the input color.

	@param color The input color to be mapped.
	@param low_color The color representing the low end of the gradient.
	@param mid0_color The color representing the first middle of the gradient.
	@param mid1_color The color representing the second middle of the gradient.
	@param high_color The color representing the high end of the gradient.
	@param low_value The luminosity value corresponding to the low_color.
	@param mid0_value The luminosity value corresponding to the mid0_color.
	@param mid1_value The luminosity value corresponding to the mid1_color.
	@param high_value The luminosity value corresponding to the high_color.
	@return The resulting color after mapping to the gradient.
*/
float4 quad_tone_map(
	float4 color,
	float4 low_color,
	float4 mid0_color,
	float4 mid1_color,
	float4 high_color,
	float low_value,
	float mid0_value,
	float mid1_value,
	float high_value)
{
	float4 result;
	float luminosity = to_luminosity(color.rgb);

	if (luminosity < mid0_value)
	{
		float t = (luminosity - low_value) / (mid0_value - low_value);
		t = saturate(t);
		result = lerp(low_color, mid0_color, t);
		result = lerp(result, color, 1 - result.a);
	}
	else if (luminosity < mid1_value)
	{
		float t = (luminosity - mid0_value) / (mid1_value - mid0_value);
		t = saturate(t);
		result = lerp(mid0_color, mid1_color, t);
		result = lerp(result, color, 1 - result.a);
	}
	else
	{
		float t = (luminosity - mid1_value) / (high_value - mid1_value);
		t = saturate(t);
		result = lerp(mid1_color, high_color, t);
		result = lerp(result, color, 1 - result.a);
	}

	result.a = 1.0;
	return result;
}


float3 rgb_to_hsl(float3 color)
{
	float r = color.r;
	float g = color.g;
	float b = color.b;

	float hue, saturation, luminance;

	float max_rgb = max(r, max(g, b));

	float tolerance = 0.01;
				
	if (max_rgb <= tolerance)
	{
		hue = 0;
		saturation = 0;
		luminance = 0;
							
		return float3(hue, saturation, luminance);
	}

	float min_rgb = min(r, min(g, b));
	float dif = max_rgb - min_rgb;

	if (dif > tolerance)
	{
		if (g >= r && g >= b)
		{
			hue = (b - r) / dif * 60.0 + 120.0;
		}
		else if (b >= g && b >= r)
		{
			hue = (r - g) / dif * 60.0 + 240.0;
		}
		else if (b > g)
		{
			hue = (g - b) / dif * 60.0 + 360.0;
		}
		else
		{
			hue = (g - b) / dif * 60.0;
		}
		if (hue < 0)
		{
			hue = hue + 360.0;
		}
	}
	else
	{
		hue = 0;
	}

	hue *= 1.0 / 360.0;
	saturation = (dif / max_rgb) * 1;
	luminance = max_rgb;

	hue = clamp(hue, 0, 1);
	saturation = clamp(saturation, 0, 1);
	luminance = clamp(luminance, 0, 1);

	return float3(hue, saturation, luminance);
}

float3 hsl_to_rgb(float3 hsl)
{
	float hue = hsl.x;
	float saturation = hsl.y;
	float luminance = hsl.z;
	
	float r = luminance;
	float g = luminance;
	float b = luminance;

	if (!(saturation > 0))
	{
		return saturate(float3(r, g, b));
	}

	float max = luminance;
	float dif = luminance * saturation;
	float min = luminance - dif;

	float hh = hue * 360;

	if (hh < 60)
	{
		r = max;
		g = hh * dif / 60 + min;
		b = min;
	}
	else if (hh < 120)
	{
		r = -(hh - 120) * dif / 60 + min;
		g = max;
		b = min;
	}
	else if (hh < 180)
	{
		r = min;
		g = max;
		b = (hh - 120) * dif / 60 + min;
	}
	else if (hh < 240)
	{
		r = min;
		g = -(hh - 240) * dif / 60 + min;
		b = max;
	}
	else if (hh < 300)
	{
		r = (hh - 240) * dif / 60 + min;
		g = min;
		b = max;
	}
	else if (hh <= 360)
	{
		r = max;
		g = min;
		b = -(hh - 360) * dif / 60 + min;
	}
	else
	{
		r = 0;
		g = 0;
		b = 0;
	}

	return saturate(float3(r, g, b));
}

float linear_error(float3 a, float3 b)
{
	float3 diff = a - b;
	return dot(diff, diff);
}

//?
float make_quad_coherent(float v)
{
	float dx = ddx(v);
	float dy = ddy(v);
	return v - 0.5 * (dx + dy);
}

float2 make_quad_coherent(float2 uv, float tile_size)
{
	float2 screen_size = _ScreenParams.xy;
	float2 pixel = uv * screen_size;
	pixel = floor(pixel / tile_size) * tile_size;
	return pixel / screen_size;
}
float2 make_quad_coherent2(float2 uv)
{
	return uv - 0.5 * (ddx(uv) + ddy(uv));
}
/*
Fixes interpolation artifacts caused by sampling a wrapped (periodic) value across a 2×2 pixel quad.

This function detects whether the given value crosses a unit-period wrap seam (0 ↔ 1)
within the current pixel quad using screen-space derivatives, and locally unwraps the
value to restore continuity before further processing (e.g. rotation or filtering).

ASSUMPTIONS AND LIMITATIONS:

1.	The input value is unit-period wrapped, i.e. in the range [0, 1) with period 1.
	Typical sources are frac() or modulo-based UV wrapping.

2.	At most one wrap seam per quad per axis is expected. Extremely high-frequency
	signals that wrap multiple times within a 2×2 quad are not supported.

3.	Wrapped values on either side of the seam are assumed to lie near 0 and near 1
	respectively. The heuristic identifies the wrapped side using a < 0.5 threshold.

4.	The input must be sampled using linear filtering. Point sampling can invalidate
	the derivative-based seam detection.

5.	This function must be applied immediately after sampling the wrapped signal.
	Any nonlinear operations performed beforehand (rotation, additional modulo,
	scaling, etc.) will break the validity of ddx/ddy for seam detection.

6. The underlying signal is assumed to be locally smooth except for the wrap discontinuity itself.

Under these conditions, the function converts values such as:
	0.99 ↔ 0.01
into a locally continuous representation:
	0.99 ↔ 1.01

This correction is quad-coherent and cannot resolve sub-quad wrapping artifacts,
which is a fundamental GPU limitation.

Parameters:
	wrapping_variable – A unit-period wrapped value (e.g. frac’d UVs) sampled from a texture.

Returns:
	A locally unwrapped version of the input value that is continuous within the quad.
*/

float2 fix_wrapping_in_quad(float2 wrapping_variable)
{
	float2 duv_dx = ddx(wrapping_variable);
	float2 duv_dy = ddy(wrapping_variable);

	float2 wrap_x = step(0.5, abs(duv_dx));
	float2 wrap_y = step(0.5, abs(duv_dy));

	float2 wrapped = max(wrap_x, wrap_y);
	float2 uv_fixed = wrapping_variable;

	if (wrapped.x)
	{
		uv_fixed.x = wrapping_variable.x < 0.5 ? wrapping_variable.x + 1.0 : wrapping_variable.x;
	}

	if (wrapped.y)
	{
		uv_fixed.y = wrapping_variable.y < 0.5 ? wrapping_variable.y + 1.0 : wrapping_variable.y;
	}
				
	return uv_fixed;
}

float2 fix_wrapping_in_quad(float wrapping_variable)
{
	float duv_dx = ddx(wrapping_variable);
	float duv_dy = ddy(wrapping_variable);

	float wrap_x = step(0.5, abs(duv_dx));
	float wrap_y = step(0.5, abs(duv_dy));

	float wrapped = max(wrap_x, wrap_y);
	float uv_fixed = wrapping_variable;

	if (wrapped)
	{
		uv_fixed = wrapping_variable < 0.5 ? wrapping_variable + 1.0 : wrapping_variable;
	}
	
	return uv_fixed;
}

#if USE_UV_MAP
#if !defined(UNITY_PIPELINE_URP)
UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
#endif

float get_eye_depth(float2 uv)
{
	#if defined(UNITY_PIPELINE_URP)
	float raw_depth = SampleSceneDepth(uv);	
	#else	
	float raw_depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, make_quad_coherent(uv, 2));
	#endif
	float eye_depth = LINEAR_EYE_DEPTH(raw_depth);	
	eye_depth = max(eye_depth, DEPTH_EPSILON);
	return eye_depth;
}

float get_log_depth(float eye_depth)
{
	float reference_depth = 1; // Depth where the scale is 1
	float log_depth = log2(eye_depth/reference_depth);
	log_depth = make_quad_coherent(log_depth);
				
	return log_depth;
}

float2 apply_band_scale(float2 input_uv, float band_depth)
{
	float scale = exp2(-band_depth);
	//scale = lerp(1.0, scale, _DepthScaleStrength);
	return input_uv * scale;
}

float get_hatch_fade(float eye_depth, float fade_strength, float fade_start, float fade_end)
{
	float computed_fade = saturate(1.0 - (eye_depth - fade_start) / (fade_end - fade_start));
	return  lerp(1.0, computed_fade, fade_strength);
}

/*	Builds depth band info used to adjust the scale of screenspace textures sampled from a UV map
	rendered by objects in the scene. Without adjusting the scale, the screen space textures would have a smaller 
	scale on more distant objects.
	
	The input_uv is the original input UV, used to sample the depth texture. 
	The sampled_uv is the UV sampled from a map, that needs to be used by the shader elsewhere, and
	scaled based on its depth.
*/
DepthBandInfo build_depth_band_info(float2 input_uv, float2 sampled_uv)
{
	DepthBandInfo context;

	context.eye_depth = get_eye_depth(input_uv);
	context.log_depth = get_log_depth(context.eye_depth);
	context.fraction_between_bands = frac(context.log_depth);

	context.uv.item0 = apply_band_scale(sampled_uv, floor(context.log_depth));
	context.uv.item1 = apply_band_scale(sampled_uv, floor(context.log_depth) + 1.0);

	return context;
}
#endif

DepthBandInfo build_depth_band_info_without_scale(float2 screen_uv)
{
	DepthBandInfo info;

	info.eye_depth = 0.0;
	info.log_depth = 0.0;
	info.fraction_between_bands = 0.0;

	info.uv.item0 = screen_uv;
	info.uv.item1 = screen_uv;
	
	
	return info;
}

#define SAMPLE_BIAS_PAIR(tex, uv_pair, bias, out_samples) \
	out_samples.item0 = SAMPLE_BIAS(tex, uv_pair.item0, bias); \
	out_samples.item1 = SAMPLE_BIAS(tex, uv_pair.item1, bias);

#define SAMPLE_PAIR(tex, uv_pair, out_samples) \
	out_samples.item0 = SAMPLE(tex, (uv_pair).item0);\
	out_samples.item1 = SAMPLE(tex, (uv_pair).item1);

#define TRANSFORM_TEX_PAIR(pair, tex, out_pair) \
	out_pair.item0 = TRANSFORM_TEX((pair).item0, tex); \
	out_pair.item1 = TRANSFORM_TEX((pair).item1, tex);

Float2Pair rotate(Float2Pair uvs, FloatPair angles)
{
	Float2Pair rotated_uvs;
	rotated_uvs.item0 = rotate(uvs.item0, angles.item0);
	rotated_uvs.item1 = rotate(uvs.item1, angles.item1);
	return rotated_uvs;
}

FloatPair add(FloatPair pair, float offset)
{
	FloatPair translated_uvs;
	translated_uvs.item0 = pair.item0 + offset;
	translated_uvs.item1 = pair.item1 + offset;
	return translated_uvs;
}

Float2Pair translate(Float2Pair pair, float2 offset)
{
	Float2Pair translated_uvs;
	translated_uvs.item0 = pair.item0 + offset;
	translated_uvs.item1 = pair.item1 + offset;
	return translated_uvs;
}


FloatPair multiply(FloatPair pair, float factor)
{
	FloatPair scaled_uvs;
	scaled_uvs.item0 = pair.item0 * factor;
	scaled_uvs.item1 = pair.item1 * factor;
	return scaled_uvs;
}

float4 lerp(Float4Pair pair, float t)
{
	return lerp(pair.item0, pair.item1, t);
}

FloatPair get_r(Float4Pair pair)
{
	FloatPair result;
	result.item0 = pair.item0.r;
	result.item1 = pair.item1.r;
	return result;
}

FloatPair to_luminosity(Float4Pair pair)
{
	FloatPair result;
	result.item0 = to_luminosity(pair.item0);
	result.item1 = to_luminosity(pair.item1);
	return result;
}

/*	Visualizes a wrapped scalar value in the range [0, 1] as a 2D color gradient.
	The gradient is design to look smooth if the value is smooth (but taking 
	wrapping into account), for example	1 - e is close to 0 + e in color if e is small. 
	
*/
float2 wrapped_visualize(float x)
{
	x = saturate(x);
	float r = x * 2;
	r = abs(1 - r);
	
	float g = frac(x + .25) * 2;
	g = abs(1 - g);
	
	return float2(r, g);
}
#endif
