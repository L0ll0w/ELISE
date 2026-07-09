#if defined(GAMELOGIC_HAS_MIXBOX)

// These defines should be defined in the shader that includes this file before it is included. 
// ITERATION_COUNT defines how many iterations the golden section search will perform.
// ERROR(c1, desired) should compute the error metric between two colors (e.g. squared distance).
// MIX(current, layer_color, t) should compute the mix between current and layer_color with factor t.

float golden_section_search(float3 current, float3 layer_color, float3 desired)
{
	const float inv_phi = 0.61803398875; // 1 / φ
	const float inv_phi_sq = 1.0 - inv_phi; // ≈ 0.381966...

	float a = 0.0;
	float b = 1.0;

	float t1 = a + inv_phi_sq * (b - a);
	float t2 = a + inv_phi * (b - a);

	float3 c1 = MIX(current, layer_color, t1);
	float3 c2 = MIX(current, layer_color, t2);
	
	float f1 = ERROR(c1, desired);
	float f2 = ERROR(c2, desired);

	for (int i = 0; i < ITERATION_COUNT; ++i)
	{
		if (f1 < f2)
		{
			b = t2;
			t2 = t1;
			f2 = f1;

			t1 = a + inv_phi_sq * (b - a);
			c1 = MIX(current, layer_color, t1);
			f1 = ERROR(c1,  desired);
		}
		else
		{
			a = t1;
			t1 = t2;
			f1 = f2;

			t2 = a + inv_phi * (b - a);
			c2 = MIX(current, layer_color, t2);
			f2 = ERROR(c2 , desired);
		}
	}

	return 0.5 * (a + b);
}

#endif
