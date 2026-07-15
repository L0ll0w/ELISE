Shader "Custom/DreamcoreCloud"
{
    Properties
    {
        [Header(Colors)]
        _BaseColor ("Base Color (Light)", Color) = (0.95, 0.8, 0.95, 1.0)
        _ShadowColor ("Shadow Color (Dark)", Color) = (0.7, 0.6, 0.85, 1.0)
        _RimColor ("Rim Color (Glow)", Color) = (1.0, 0.9, 0.95, 1.0)
        _RimPower ("Rim Power", Range(0.1, 10.0)) = 3.0
        _RimIntensity ("Rim Intensity", Range(0.0, 5.0)) = 1.5
        _Opacity ("Opacity", Range(0.0, 1.0)) = 0.9

        [Header(Toon Shading)]
        _ToonThreshold ("Toon Threshold", Range(0.0, 1.0)) = 0.3
        _ToonSmoothness ("Toon Smoothness", Range(0.01, 1.0)) = 0.2

        [Header(Vertex Wobble Animation)]
        _WobbleSpeed ("Wobble Speed", Float) = 1.5
        _WobbleScale ("Wobble Scale (Size)", Float) = 0.15
        _WobbleFrequency ("Wobble Frequency", Float) = 1.0

        [Header(Soft Intersections)]
        _DepthFadeDistance ("Depth Fade Distance", Float) = 1.5
    }

    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent" 
            "RenderPipeline"="UniversalPipeline" 
        }
        
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            // Requis pour compiler les variantes de lumières URP
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD1;
                float3 normalWS     : NORMAL;
                float2 uv           : TEXCOORD0;
                float4 screenPos    : TEXCOORD3;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _ShadowColor;
                float4 _RimColor;
                float _RimPower;
                float _RimIntensity;
                float _Opacity;
                float _ToonThreshold;
                float _ToonSmoothness;
                float _WobbleSpeed;
                float _WobbleScale;
                float _WobbleFrequency;
                float _DepthFadeDistance;
            CBUFFER_END

            // Fonction pour calculer l'ondulation du nuage
            float3 CalculateWobble(float3 posWS, float3 normalWS, float time)
            {
                // Un bruit de vague sinusoïdale basé sur la position monde
                float wave = sin(posWS.x * _WobbleFrequency + time * _WobbleSpeed) * 
                             cos(posWS.z * _WobbleFrequency + time * _WobbleSpeed) * 
                             sin(posWS.y * _WobbleFrequency * 0.5 + time * _WobbleSpeed * 0.7);
                
                return normalWS * wave * _WobbleScale;
            }

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                
                // Position monde initiale
                float3 posWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                
                // Application de l'ondulation (Vertex Displacement)
                float3 offset = CalculateWobble(posWS, normalWS, _Time.y);
                posWS += offset;
                
                // Mettre à jour la position dans l'espace de projection
                output.positionCS = TransformWorldToHClip(posWS);
                output.positionWS = posWS;
                output.normalWS = normalize(normalWS);
                output.uv = input.uv;
                
                // Calcul des coordonnées d'écran pour le depth fade
                output.screenPos = ComputeScreenPos(output.positionCS);
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Normalisation des vecteurs
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(_WorldSpaceCameraPos - input.positionWS);
                
                // Récupération de la lumière principale directionnelle
                Light mainLight = GetMainLight();
                float3 lightDir = mainLight.direction;
                
                // Ombrage Toon / Soft Lambert
                float NdotL = dot(normalWS, lightDir);
                float halfLambert = NdotL * 0.5 + 0.5; // Intervalle [0, 1]
                float toonDiff = smoothstep(_ToonThreshold, _ToonThreshold + _ToonSmoothness, halfLambert);
                
                // Couleur finale de base (Mélange pastel)
                half3 finalColor = lerp(_ShadowColor.rgb, _BaseColor.rgb, toonDiff);
                
                // Lueur périphérique (Rim Light / Fresnel) pour l'effet Dreamcore
                float fresnel = pow(1.0 - saturate(dot(normalWS, viewDirWS)), _RimPower);
                half3 rimGlow = fresnel * _RimColor.rgb * _RimIntensity;
                finalColor += rimGlow;
                
                // Calcul du Depth Fade (Fondu aux intersections)
                float alpha = _Opacity;
                
                #if defined(_SCREEN_SPACE_OCCLUSION) || 1 // Toujours tenter si URP supporté
                    // Convertir les coordonnées écran
                    float2 screenUV = input.screenPos.xy / input.screenPos.w;
                    
                    // Lire la profondeur de la scène
                    float rawDepth = SampleSceneDepth(screenUV);
                    float sceneDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
                    
                    // Profondeur actuelle du pixel
                    float pixelDepth = input.screenPos.w;
                    
                    // Calcul du fondu linéaire
                    float depthFade = saturate((sceneDepth - pixelDepth) / _DepthFadeDistance);
                    alpha *= depthFade;
                #endif

                return half4(finalColor, alpha);
            }
            ENDHLSL
        }
    }
    FallBack "Packages/com.unity.render-pipelines.universal/FallbackError"
}
