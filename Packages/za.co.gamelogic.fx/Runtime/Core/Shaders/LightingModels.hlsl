#ifndef LIGHTING_MODELS_INCLUDED
#define LIGHTING_MODELS_INCLUDED

float4 LightingUnlit(SurfaceOutput s, half3 light_direction, half attenuation)
{
	float4 c;
	c.rgb = s.Albedo;
	c.a = s.Alpha;

	return c;
}
#endif
