Shader "Custom/InkPuddleShader"
{
    Properties
    {
        _PuddleColor ("Puddle Color", Color) = (0.05, 0.05, 0.05, 1.0)
        _PuddleSize ("Puddle Size (0-1)", Range(0.0, 1.0)) = 0.0
        
        [Header(Organic Shape Settings)]
        _NoiseScale ("Shape Irregularity Scale", Float) = 8.0
        _NoiseStrength ("Shape Irregularity Strength", Range(0.0, 0.5)) = 0.15
        _Smoothness ("Edge Smoothness", Range(0.001, 0.1)) = 0.01

        [Header(Wet Glow Settings)]
        [HDR] _GlowColor ("Wet Edge Glow Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _GlowPower ("Glow Power (Falloff)", Range(0.1, 5.0)) = 1.5
        _GlowIntensity ("Glow Intensity", Range(0.0, 5.0)) = 2.0
    }

    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent" 
            "RenderPipeline"="UniversalPipeline"
        }
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "PuddlePass"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
                float3 normalOS     : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float3 normalWS     : NORMAL;
                float3 viewDirWS    : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _PuddleColor;
                float _PuddleSize;
                float _NoiseScale;
                float _NoiseStrength;
                float _Smoothness;
                
                float4 _GlowColor;
                float _GlowPower;
                float _GlowIntensity;
            CBUFFER_END

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

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewDirWS = normalize(_WorldSpaceCameraPos - positionWS);
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 1. Calcul du masque de la flaque organique
                // Distance par rapport au centre de la texture (0.5, 0.5)
                float2 centerUV = input.uv - 0.5;
                float dist = length(centerUV);

                // Ajout d'irrégularité organique via du bruit basé sur l'angle UV
                float angle = atan2(centerUV.y, centerUV.x);
                float2 noiseCoords = float2(cos(angle), sin(angle)) * _NoiseScale + 0.5;
                float noise = Noise2D(noiseCoords) * _NoiseStrength;

                // Le rayon de la flaque s'étend de 0 à 0.5 (du centre aux bords)
                float maxRadius = 0.48; // Légère marge pour éviter le clipping sur les bords du quad
                float targetRadius = _PuddleSize * maxRadius;

                // Calcul du seuil de découpe liquide
                float edgeThreshold = targetRadius - noise;
                
                // Masque liquide progressif
                float alpha = smoothstep(edgeThreshold, edgeThreshold - _Smoothness, dist);

                // Si le pixel est en dehors de la flaque, on le rejette pour optimiser
                if (alpha <= 0.0001)
                {
                    discard;
                }

                // 2. Reflet mouillé brillant (Rim Light sur la flaque)
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(input.viewDirWS);
                
                // Fresnel classique sur surface plane
                float fresnel = pow(1.0 - saturate(dot(normalWS, viewDirWS)), _GlowPower);
                
                // Le reflet mouillé est plus intense près des bords de la flaque (effet ménisque de liquide)
                float edgeFactor = smoothstep(edgeThreshold - _Smoothness * 5.0, edgeThreshold, dist);
                half3 rimGlow = fresnel * _GlowColor.rgb * _GlowIntensity * (1.0 + edgeFactor * 2.0);

                // Couleur finale liquide
                half3 finalColor = _PuddleColor.rgb + rimGlow;

                return half4(finalColor, alpha * _PuddleColor.a);
            }
            ENDHLSL
        }
    }
    FallBack "Packages/com.unity.render-pipelines.universal/FallbackError"
}
