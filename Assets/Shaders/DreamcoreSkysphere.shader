Shader "ELISE/DreamcoreSkysphere"
{
    Properties
    {
        [Header(Gradient Colors Dreamcore Garden)]
        _ZenithColor ("Zenith Color (Haut - Lilas Profond)", Color) = (0.24, 0.18, 0.44, 1.0)
        _MiddleColor ("Middle Color (Milieu - Lavande Doux)", Color) = (0.52, 0.36, 0.58, 1.0)
        _HorizonColor ("Horizon Color (Brume - Peche Dore)", Color) = (0.84, 0.56, 0.48, 1.0)
        _NadirColor ("Nadir Color (Bas - Indigo Jardin)", Color) = (0.12, 0.10, 0.22, 1.0)
        
        [Header(Gradient Adjustments)]
        _HorizonHeight ("Horizon Height", Range(-0.3, 0.3)) = 0.02
        _HorizonBlend ("Horizon Softness", Range(0.05, 1.0)) = 0.35
        _ZenithBlend ("Zenith Softness", Range(0.1, 1.5)) = 0.75
        _SkyExposure ("Sky Exposure", Range(0.2, 2.0)) = 1.0

        [Header(Sun Disc and Corona)]
        _SunDirection ("Sun Direction (Auto si vide)", Vector) = (-0.68, 0.65, -0.34, 0.0)
        _SunDiscColor ("Sun Core Disc Color", Color) = (1.5, 1.3, 0.9, 1.0)
        _SunDiscIntensity ("Sun Disc Intensity", Range(0.0, 3.0)) = 1.2
        _SunCoronaColor ("Sun Corona (Aura Doree)", Color) = (1.4, 0.95, 0.6, 1.0)
        _SunCoronaIntensity ("Sun Corona Intensity", Range(0.0, 3.0)) = 1.4
        
        [Header(Sun Outer Glow)]
        _SunOuterGlowColor ("Sun Outer Glow (Rose Peche)", Color) = (1.1, 0.6, 0.7, 1.0)
        _SunOuterGlowIntensity ("Sun Outer Glow Intensity", Range(0.0, 2.0)) = 0.75
        _SunRayIntensity ("Sun Ray Flare Intensity", Range(0.0, 1.5)) = 0.35
        _SunRayCount ("Sun Ray Count", Range(4.0, 24.0)) = 10.0

        [Header(Dreamcore Clouds)]
        [Toggle] _EnableClouds ("Enable Clouds", Float) = 1.0
        _CloudColor ("Cloud Color (Rose Poudre)", Color) = (0.88, 0.78, 0.85, 1.0)
        _CloudOpacity ("Cloud Opacity", Range(0.0, 1.0)) = 0.45
        _CloudScale ("Cloud Scale", Float) = 2.8
        _CloudSpeed ("Cloud Drift Speed", Float) = 0.015
        _CloudDensity ("Cloud Density", Range(0.0, 1.0)) = 0.42
        _CloudSoftness ("Cloud Softness", Range(0.01, 1.0)) = 0.35

        [Header(Twinkling Stars)]
        [Toggle] _EnableStars ("Enable Stars", Float) = 1.0
        _StarColor ("Star Color", Color) = (1.2, 1.1, 1.0, 1.0)
        _StarDensity ("Star Density", Float) = 45.0
        _StarBrightness ("Star Brightness", Range(0.0, 3.0)) = 1.2
        _StarTwinkleSpeed ("Star Twinkle Speed", Float) = 2.0
    }

    SubShader
    {
        Tags 
        { 
            "Queue"="Background" 
            "RenderType"="Background" 
            "PreviewType"="Skybox" 
            "RenderPipeline"="UniversalPipeline"
        }
        
        Cull Off 
        ZWrite Off
        ZTest LEqual

        Pass
        {
            Name "DreamcoreSkyboxPass"
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 viewDir      : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _ZenithColor;
                float4 _MiddleColor;
                float4 _HorizonColor;
                float4 _NadirColor;
                float _HorizonHeight;
                float _HorizonBlend;
                float _ZenithBlend;
                float _SkyExposure;

                float4 _SunDirection;
                float4 _SunDiscColor;
                float _SunDiscIntensity;
                float4 _SunCoronaColor;
                float _SunCoronaIntensity;
                float4 _SunOuterGlowColor;
                float _SunOuterGlowIntensity;
                float _SunRayIntensity;
                float _SunRayCount;

                float _EnableClouds;
                float4 _CloudColor;
                float _CloudOpacity;
                float _CloudScale;
                float _CloudSpeed;
                float _CloudDensity;
                float _CloudSoftness;

                float _EnableStars;
                float4 _StarColor;
                float _StarDensity;
                float _StarBrightness;
                float _StarTwinkleSpeed;
            CBUFFER_END

            // Bruit 3D simple pour les nuages et étoiles
            float Hash3D(float3 p)
            {
                p = frac(p * float3(443.8975, 397.2973, 491.1871));
                p += dot(p.xyz, p.yzx + 19.19);
                return frac(p.x * p.y * p.z);
            }

            float ValueNoise3D(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                float n000 = Hash3D(i + float3(0,0,0));
                float n100 = Hash3D(i + float3(1,0,0));
                float n010 = Hash3D(i + float3(0,1,0));
                float n110 = Hash3D(i + float3(1,1,0));
                float n001 = Hash3D(i + float3(0,0,1));
                float n101 = Hash3D(i + float3(1,0,1));
                float n011 = Hash3D(i + float3(0,1,1));
                float n111 = Hash3D(i + float3(1,1,1));

                float x00 = lerp(n000, n100, f.x);
                float x10 = lerp(n010, n110, f.x);
                float x01 = lerp(n001, n101, f.x);
                float x11 = lerp(n011, n111, f.x);

                float y0 = lerp(x00, x10, f.y);
                float y1 = lerp(x01, x11, f.y);

                return lerp(y0, y1, f.z);
            }

            float FBM3D(float3 p)
            {
                float value = 0.0;
                float amplitude = 0.5;
                for (int i = 0; i < 3; i++)
                {
                    value += amplitude * ValueNoise3D(p);
                    p *= 2.05;
                    amplitude *= 0.5;
                }
                return value;
            }

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(positionWS);

                #if UNITY_REVERSED_Z
                    output.positionCS.z = 1.0e-5f; // Far plane URP fiable
                #else
                    output.positionCS.z = output.positionCS.w - 1.0e-5f;
                #endif

                output.viewDir = input.positionOS.xyz;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 viewDir = normalize(input.viewDir);
                float y = viewDir.y;

                // 1. Dégradé Dreamcore équilibré et doux (Nadir -> Horizon -> Middle -> Zenith)
                float adjustedY = y - _HorizonHeight;
                half3 skyColor;

                if (adjustedY >= 0.0)
                {
                    // Demi-sphère céleste
                    float tHorizonMid = saturate(adjustedY / max(0.01, _HorizonBlend));
                    tHorizonMid = smoothstep(0.0, 1.0, tHorizonMid);
                    half3 lowerSky = lerp(_HorizonColor.rgb, _MiddleColor.rgb, tHorizonMid);

                    float tMidZenith = saturate((adjustedY - _HorizonBlend * 0.4) / max(0.01, _ZenithBlend));
                    tMidZenith = smoothstep(0.0, 1.0, tMidZenith);
                    skyColor = lerp(lowerSky, _ZenithColor.rgb, tMidZenith);
                }
                else
                {
                    // Demi-sphère inférieure (vers la terre / sous-sol)
                    float tNadir = saturate(-adjustedY / max(0.01, _HorizonBlend * 1.2));
                    tNadir = smoothstep(0.0, 1.0, tNadir);
                    skyColor = lerp(_HorizonColor.rgb, _NadirColor.rgb, tNadir);
                }

                // 2. Halo Solaire Dreamcore strictement localisé autour du Soleil
                float3 sunDir = normalize(_SunDirection.xyz);
                if (length(_SunDirection.xyz) < 0.01)
                {
                    Light mainLight = GetMainLight();
                    sunDir = -mainLight.direction;
                }

                float sunDot = max(0.0, dot(viewDir, sunDir));

                // A. Disque solaire central brillant
                float sunDisc = smoothstep(0.9975, 0.9992, sunDot);
                half3 sunColor = sunDisc * _SunDiscColor.rgb * _SunDiscIntensity;

                // B. Couronne dorée chaude (resserrée, ne bave pas sur tout le ciel)
                float coronaFactor = pow(sunDot, 90.0);
                sunColor += coronaFactor * _SunCoronaColor.rgb * _SunCoronaIntensity;

                // C. Halo périphérique rose/pêche doux (s'éteint complètement au-delà de 30°)
                float outerGlowFactor = pow(sunDot, 18.0);
                sunColor += outerGlowFactor * _SunOuterGlowColor.rgb * _SunOuterGlowIntensity;

                // D. Rayons solaires stylisés subtils (Godrays légers face au soleil)
                if (_SunRayIntensity > 0.01 && sunDot > 0.82)
                {
                    float3 crossSun = cross(viewDir, sunDir);
                    float angleOnSunDisc = atan2(crossSun.y, crossSun.x);
                    float rays = pow(0.5 + 0.5 * sin(angleOnSunDisc * _SunRayCount + _Time.y * 0.35), 4.0);
                    sunColor += rays * pow(sunDot, 36.0) * _SunCoronaColor.rgb * _SunRayIntensity;
                }

                skyColor += sunColor;

                // 3. Nuages Procéduraux Pastel (Flottants, doux et transparents)
                if (_EnableClouds > 0.5)
                {
                    float cloudAltitude = smoothstep(0.02, 0.20, y) * smoothstep(0.70, 0.40, y);
                    if (cloudAltitude > 0.01)
                    {
                        float3 cloudPos = viewDir * _CloudScale;
                        cloudPos.x += _Time.y * _CloudSpeed;
                        cloudPos.z += _Time.y * (_CloudSpeed * 0.5);

                        float cloudNoise = FBM3D(cloudPos);
                        float cloudMask = smoothstep(1.0 - _CloudDensity, 1.0 - _CloudDensity + _CloudSoftness, cloudNoise);
                        cloudMask *= cloudAltitude * _CloudOpacity;

                        skyColor = lerp(skyColor, _CloudColor.rgb, saturate(cloudMask));
                    }
                }

                // 4. Étoiles Scintillantes Délicates (Au zénith, opposées au soleil)
                if (_EnableStars > 0.5 && y > 0.3)
                {
                    float starMask = smoothstep(0.3, 0.65, y) * (1.0 - pow(sunDot, 3.0));
                    float3 starGrid = viewDir * _StarDensity;
                    float3 starCell = floor(starGrid);
                    float starHash = Hash3D(starCell);

                    if (starHash > 0.968)
                    {
                        float twinkle = 0.5 + 0.5 * sin(_Time.y * _StarTwinkleSpeed + starHash * 6.28);
                        float starStrength = pow(starHash, 16.0) * _StarBrightness * twinkle * starMask;
                        skyColor += _StarColor.rgb * starStrength;
                    }
                }

                // Exposition finale équilibrée
                skyColor *= _SkyExposure;

                return half4(skyColor, 1.0);
            }
            ENDHLSL
        }
    }

    FallBack "Skybox/Procedural"
}
