## Macro Suggestions

### Passing `_MainTex_TexelSize` as a function argument (not via `.xy`)

**File:** `Pixelate.shader`

**Snippet:**
```hlsl
float4 _MainTex_TexelSize;
...
uv = pixelate_uv(uv, _PixelSize, 0, _MainTex_TexelSize);
```

`_MainTex_TexelSize` (float4) was passed directly to a function expecting `float2`, relying on implicit float4→float2 truncation to get `.xy`.

**Resolution used:** Replaced with `TEXEL_SIZE` directly — the existing macro expands to `float4(...)` so implicit truncation still works the same way.

**Suggestion:** No new macro needed. If this pattern recurs frequently, consider adding `TEXEL_SIZE_XY` as a `float2` convenience macro:
```hlsl
#define TEXEL_SIZE_XY float2(1.0 / _ScreenParams.x, 1.0 / _ScreenParams.y)
```
This would make intent explicit and avoid relying on implicit truncation.

---

### Passing raw `input.uv` / `i.uv` to `NEW_DEPTH_BAND_INFO` macro

**Files:** `DitherMatrixBias.shader`, `DitherTextureBias.shader`, `SimpleHatch.shader`, `MixboxConvexHullHatch.shader`

**Snippet:**
```hlsl
NEW_DEPTH_BAND_INFO(depth_band_info, _UVMap, input.uv);
```

`input.uv` was passed as the `input_uv` argument to `NEW_DEPTH_BAND_INFO`, which uses it both to sample the UV map and to sample the depth texture.

**Resolution used:** Replaced with `UV_FROM_INPUT(input)`:
```hlsl
NEW_DEPTH_BAND_INFO(depth_band_info, _UVMap, UV_FROM_INPUT(input));
```

**Suggestion:** No new macro needed — `UV_FROM_INPUT` is the correct abstraction here. The macro itself is pipeline-agnostic since it receives the UV as a parameter.

---

### `tex2D(_MainTex, uv)` instead of `SAMPLE(_MainTex, uv)` in helper function

**File:** `ImageGradient.shader`

**Snippet:**
```hlsl
float sample_luminosity(float2 uv)
{
    return to_luminosity(tex2D(_MainTex, uv).rgb);
}
```

Uses the legacy CG `tex2D` function instead of the `SAMPLE` macro to read from `_MainTex` inside a helper function.

**Resolution used:** Replaced with `SAMPLE_MAIN(uv)`:
```hlsl
return to_luminosity(SAMPLE_MAIN(uv).rgb);
```

**Suggestion:** No new macro needed. Consider adding a lint/grep check for remaining `tex2D(_MainTex` usages in HLSLPROGRAM shaders, as these will silently fail in URP.

---

### `_MainTex_TexelSize.x` — single component access only

**Files:** `CurvePowerMean.shader`, `LinePowerMean.shader`

**Snippet:**
```hlsl
float2 step_size = _MainTex_TexelSize.x * _KernelXJumpSize;
float texel = _MainTex_TexelSize.x;
```

Only the `.x` component (1/width) is used, not `.xy`.

**Resolution used:** Replaced with `TEXEL_SIZE.x` — works identically since `TEXEL_SIZE` is `float4` and `.x` = `1.0 / _ScreenParams.x`.

**Suggestion:** No new macro needed.
