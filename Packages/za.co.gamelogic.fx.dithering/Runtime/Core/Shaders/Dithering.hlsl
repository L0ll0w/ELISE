#ifndef DITHERING_HLSL_INCLUDED
#define DITHERING_HLSL_INCLUDED

#include "Packages/za.co.gamelogic.fx/Runtime/Core/Shaders/Gamelogic.hlsl"

float2 calculate_tiled_uv(float4 raw_screen_position, float4 tiling)
{
	float4 screen_position = ComputeScreenPos(raw_screen_position);
	float2 screen_uv = screen_position.xy / screen_position.w;
	float2 uv_tiled = screen_uv * tiling.xy + tiling.zw;

	return uv_tiled;
}

/*
float2 calculate_tiled_uv(float2 uv, float4 tiling)
{
	return uv * tiling.xy + tiling.zw;
}
*/

float sample_matrix(float4 raw_screen_position, float2 pixel_size)
{
	float4 sp = ComputeScreenPos(raw_screen_position);
	float2 screen_uv = sp.xy / sp.w;

	float2 pixel_coord = screen_uv * _ScreenParams.xy;
	pixel_coord = floor(pixel_coord + 0.5);

	const int matrix_size = 8;
	int2 matrix_coord = (int2)(pixel_coord / pixel_size) % matrix_size;

	static const float bayer8x8[64] =
	{
		0, 48, 12, 60, 3, 51, 15, 63,
		32, 16, 44, 28, 35, 19, 47, 31,
		8, 56, 4, 52, 11, 59, 7, 55,
		40, 24, 36, 20, 43, 27, 39, 23,
		2, 50, 14, 62, 1, 49, 13, 61,
		34, 18, 46, 30, 33, 17, 45, 29,
		10, 58, 6, 54, 9, 57, 5, 53,
		42, 26, 38, 22, 41, 25, 37, 21
	};

	int index = matrix_coord.y * matrix_size + matrix_coord.x;

	return bayer8x8[index] / 63.0;
}

int pixel_mod(float x, int image_width, int grid_width)
{
	return floor(x * image_width) % grid_width;
}

int2 pixel_mod(float2 uv, int2 image_size, int2 grid_size)
{
	return floor(uv * image_size) % grid_size;
}

#endif 
