Shader "Custom/ELISEFog"
{
    Properties
    {
        [Header(General Settings)]
        [KeywordEnum(Linear, Exponential, ExponentialSquared)] _FogMode("Fog Mode", Float) = 0
        _FogStart("Fog Start Distance", Float) = 5.0
        _FogEnd("Fog End Distance", Float) = 50.0
        _FogDensity("Fog Density (Exp/Exp2)", Range(0.0, 0.2)) = 0.02
        _SkyboxInfluence("Skybox Influence", Range(0.0, 1.0)) = 0.0

        [Header(Color Settings)]
        [Toggle(_USE_DAY_NIGHT)] _UseDayNight("Use Day/Night Cycle", Float) = 0
        _StaticFogColor("Static Fog Color", Color) = (0.5, 0.6, 0.7, 1.0)
        _DayFogColor("Day Fog Color", Color) = (0.9, 0.95, 1.0, 1.0)
        _NightFogColor("Night Fog Color", Color) = (0.1, 0.12, 0.25, 1.0)

        [Header(Height Fog Settings)]
        [Toggle(_HEIGHT_FOG_ON)] _HeightFogEnabled("Enable Height Fog", Float) = 1
        _HeightStart("Height Start (Y)", Float) = 0.0
        _HeightScale("Height Falloff (Scale)", Range(0.01, 1.0)) = 0.1
        _HeightDensity("Height Density", Range(0.0, 0.5)) = 0.05

        [Header(Noise Settings)]
        [Toggle(_NOISE_ON)] _NoiseEnabled("Enable Animated Noise", Float) = 1
        _NoiseScale("Noise Scale", Range(0.01, 1.0)) = 0.1
        _NoiseSpeed("Noise Speed", Float) = 1.0
        _NoiseStrength("Noise Strength", Range(0.0, 1.0)) = 0.3

        [Header(Dithering Settings)]
        [Toggle(_DITHER_ON)] _DitherEnabled("Enable Dithering", Float) = 1
        _DitherStrength("Dither Strength", Range(0.0, 0.2)) = 0.02
    }

    SubShader
    {
        Tags 
        { 
            "RenderType"="Opaque" 
            "RenderPipeline"="UniversalPipeline"
        }
        LOD 100
        Cull Off 
        ZWrite Off 
        ZTest Always

        Pass
        {
            Name "ELISEFogPass"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            // Multicompile pour le mode de brouillard
            #pragma shader_feature_local _FOGMODE_LINEAR _FOGMODE_EXPONENTIAL _FOGMODE_EXPONENTIALSQUARED
            #pragma shader_feature_local _USE_DAY_NIGHT
            #pragma shader_feature_local _HEIGHT_FOG_ON
            #pragma shader_feature_local _NOISE_ON
            #pragma shader_feature_local _DITHER_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
            };

            // Déclaration de la texture rendu par la caméra (Blit)
            TEXTURE2D(_BlitTexture);
            SAMPLER(sampler_BlitTexture);

            CBUFFER_START(UnityPerMaterial)
                float _FogMode;
                float _FogStart;
                float _FogEnd;
                float _FogDensity;
                float _SkyboxInfluence;

                float _UseDayNight;
                float4 _StaticFogColor;
                float4 _DayFogColor;
                float4 _NightFogColor;

                float _HeightFogEnabled;
                float _HeightStart;
                float _HeightScale;
                float _HeightDensity;

                float _NoiseEnabled;
                float _NoiseScale;
                float _NoiseSpeed;
                float _NoiseStrength;

                float _DitherEnabled;
                float _DitherStrength;
            CBUFFER_END

            // Variables globales injectées par DayNightSkyManager
            float4 _DayNightSplitDirection;
            float3 _DayNightWorldCenter;
            float _DayNightBaseOffset;
            float _DayNightPositionSensitivity;
            float _DayNightTransitionWidth;

            // Fonction de bruit pseudo-aléatoire 2D (Value Noise)
            float Noise2D(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                
                float a = frac(sin(dot(i + float2(0.0,0.0), float2(127.1,311.7))) * 43758.5453123);
                float b = frac(sin(dot(i + float2(1.0,0.0), float2(127.1,311.7))) * 43758.5453123);
                float c = frac(sin(dot(i + float2(0.0,1.0), float2(127.1,311.7))) * 43758.5453123);
                float d = frac(sin(dot(i + float2(1.0,1.0), float2(127.1,311.7))) * 43758.5453123);
                
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            // Mouvement de brume avec bruit FBM à 2 octaves
            float FBM(float2 p)
            {
                float v = 0.0;
                float a = 0.5;
                float2 shift = float2(100.0, 100.0);
                float2x2 rot = float2x2(0.8, 0.6, -0.6, 0.8);
                
                v += a * Noise2D(p);
                p = mul(rot, p) * 2.0 + shift;
                a *= 0.5;
                v += a * Noise2D(p);
                
                return v;
            }

            // Calcul du brouillard de hauteur intégré le long du rayon
            float CalculateHeightFog(float3 camPos, float3 posWS, float dist)
            {
                float hCam = camPos.y - _HeightStart;
                float hPos = posWS.y - _HeightStart;
                
                float falloff = _HeightScale;
                
                float expCam = exp(-falloff * hCam);
                float expPos = exp(-falloff * hPos);
                
                float diffY = posWS.y - camPos.y;
                float opticalDepth = 0.0;
                
                if (abs(diffY) > 0.001)
                {
                    opticalDepth = (expCam - expPos) / (falloff * diffY);
                }
                else
                {
                    opticalDepth = expCam;
                }
                
                float heightFogFactor = 1.0 - exp(-dist * _HeightDensity * opticalDepth);
                return saturate(heightFogFactor);
            }

            // Matrice de Dithering 4x4 de Bayer
            float GetDither(float2 uv)
            {
                const float ditherTable[16] = {
                    0.0 / 16.0,  8.0 / 16.0,  2.0 / 16.0, 10.0 / 16.0,
                    12.0 / 16.0, 4.0 / 16.0, 14.0 / 16.0,  6.0 / 16.0,
                    3.0 / 16.0, 11.0 / 16.0,  1.0 / 16.0,  9.0 / 16.0,
                    15.0 / 16.0, 7.0 / 16.0, 13.0 / 16.0,  5.0 / 16.0
                };
                
                uint2 pixelPos = uint2(uv * _ScreenParams.xy);
                uint index = (pixelPos.x % 4) + (pixelPos.y % 4) * 4;
                return ditherTable[index];
            }

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                output.uv = GetFullScreenTriangleTexCoord(input.vertexID);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Échantillonner la couleur de la scène
                half4 sceneColor = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.uv);
                
                // Récupérer la profondeur brute et calculer la profondeur linéaire
                float rawDepth = SampleSceneDepth(input.uv);
                float linearDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
                
                // Détecter si le pixel appartient à la Skybox
                #if UNITY_REVERSED_Z
                    bool isSkybox = rawDepth <= 0.00001;
                #else
                    bool isSkybox = rawDepth >= 0.99999;
                #endif

                // Reconstruire la position dans l'espace monde
                float3 posWS = ComputeWorldSpacePosition(input.uv, rawDepth, UNITY_MATRIX_I_VP);
                float3 camPos = _WorldSpaceCameraPos;
                
                // Calculer la distance réelle du pixel par rapport à la caméra
                float dist = length(posWS - camPos);

                // --- 1. CALCUL DU BROUILLARD DE DISTANCE ---
                float distFogFactor = 0.0;
                
                #if _FOGMODE_LINEAR
                    distFogFactor = saturate((linearDepth - _FogStart) / (_FogEnd - _FogStart));
                #elif _FOGMODE_EXPONENTIAL
                    distFogFactor = 1.0 - exp(-linearDepth * _FogDensity);
                #elif _FOGMODE_EXPONENTIALSQUARED
                    distFogFactor = 1.0 - exp(-pow(linearDepth * _FogDensity, 2.0));
                #else // Par défaut ou fallback
                    distFogFactor = saturate((linearDepth - _FogStart) / (_FogEnd - _FogStart));
                #endif
                
                distFogFactor = saturate(distFogFactor);

                // --- 2. CALCUL DU BROUILLARD DE HAUTEUR ---
                float heightFogFactor = 0.0;
                #if _HEIGHT_FOG_ON
                    heightFogFactor = CalculateHeightFog(camPos, posWS, dist);
                #endif

                // Combiner les facteurs de brouillard (utilisation de la transmittance)
                float totalTransmittance = (1.0 - distFogFactor) * (1.0 - heightFogFactor);
                float fogFactor = saturate(1.0 - totalTransmittance);

                // --- 3. DYNAMIQUE DE BRUME / BRUIT ---
                #if _NOISE_ON
                    float2 windOffset = float2(_Time.y * _NoiseSpeed, 0.0);
                    float noiseVal = FBM((posWS.xz + windOffset) * _NoiseScale);
                    
                    // Moduler le brouillard avec le bruit (surtout dans les zones brumeuses)
                    float noiseMod = lerp(1.0 - _NoiseStrength, 1.0 + _NoiseStrength, noiseVal);
                    fogFactor = saturate(fogFactor * noiseMod);
                #endif

                // --- 4. INTEGRATION DU CYCLE JOUR/NUIT ---
                half4 fogColor = _StaticFogColor;
                
                #if _USE_DAY_NIGHT
                    // Calculer le splitOffset de la caméra
                    float3 toCam = camPos - _DayNightWorldCenter;
                    float positionOffset = dot(toCam, _DayNightSplitDirection.xyz) * _DayNightPositionSensitivity;
                    float splitOffset = clamp(_DayNightBaseOffset + positionOffset, -2.0, 2.0);
                    
                    // Calculer la direction de vue vers le pixel actuel
                    float3 viewDir = normalize(posWS - camPos);
                    float splitVal = dot(viewDir, _DayNightSplitDirection.xyz) + splitOffset;
                    
                    // Calculer le poids Jour / Nuit local
                    float transition = smoothstep(-_DayNightTransitionWidth * 0.5, _DayNightTransitionWidth * 0.5, splitVal);
                    
                    // Mélange des couleurs Jour et Nuit
                    fogColor = lerp(_NightFogColor, _DayFogColor, transition);
                #endif

                // --- 5. RETRO DITHERING ---
                #if _DITHER_ON
                    float dither = GetDither(input.uv);
                    float ditheredFactor = fogFactor + (dither - 0.5) * _DitherStrength;
                    fogFactor = saturate(ditheredFactor);
                #endif

                // --- 6. EXCLUSION DE LA SKYBOX ---
                if (isSkybox)
                {
                    fogFactor = fogFactor * _SkyboxInfluence;
                }

                // Mélanger la couleur finale
                half3 finalColor = lerp(sceneColor.rgb, fogColor.rgb, fogFactor);

                return half4(finalColor, sceneColor.a);
            }
            ENDHLSL
        }
    }
    FallBack "Packages/com.unity.render-pipelines.universal/FallbackError"
}
