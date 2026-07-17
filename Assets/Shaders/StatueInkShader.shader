Shader "Custom/StatueInkShader"
{
    Properties
    {
        [Header(Colors and Texture)]
        _BaseMap ("Base Map (Albedo)", 2D) = "white" {}
        _BaseColor ("Base Color (Light)", Color) = (1.0, 1.0, 1.0, 1.0)
        _ShadowColor ("Shadow Color (Dark)", Color) = (0.7, 0.6, 0.85, 1.0)
        
        [Header(Rim Light Glow)]
        [HDR] _RimColor ("Rim Color (Glow)", Color) = (1.0, 0.9, 0.95, 1.0)
        _RimPower ("Rim Power", Range(0.1, 10.0)) = 3.0
        _RimIntensity ("Rim Intensity", Range(0.0, 5.0)) = 1.5

        [Header(Toon Shading)]
        _ToonThreshold ("Toon Threshold", Range(0.0, 1.0)) = 0.3
        _ToonSmoothness ("Toon Smoothness", Range(0.01, 1.0)) = 0.2

        [Header(Local Axis Orientation)]
        _LocalUp ("Local Up Direction", Vector) = (0.0, 1.0, 0.0, 0.0)
        _LocalRight ("Local Right Direction", Vector) = (1.0, 0.0, 0.0, 0.0)
        _LocalForward ("Local Forward Direction", Vector) = (0.0, 0.0, 1.0, 0.0)

        [Header(Ink Flow Settings)]
        _InkColor ("Ink Color", Color) = (0.05, 0.05, 0.05, 1.0)
        _InkProgress ("Ink Flow Progress (0-1)", Range(0.0, 1.0)) = 0.0
        _EyeY ("Eye Height (Local Y)", Float) = 2.8
        _FeetY ("Feet Height (Local Y)", Float) = 0.0

        [Header(Eye Tear Placement)]
        _FaceCenterX ("Face Center X (Local)", Float) = 0.0
        _FaceCenterZ ("Face Center Z (Local)", Float) = 0.5
        _EyeSpacing ("Eye Spacing (Width)", Float) = 0.4
        
        [Header(Tear Look and Path)]
        _DripWidth ("Tear Line Width", Range(0.01, 0.5)) = 0.05
        _DripBlur ("Tear Edge Softness", Range(0.001, 0.1)) = 0.01
        _WiggleFreq ("Tear Path Wavy Freq", Float) = 3.0
        _WiggleStrength ("Tear Path Wavy Strength", Range(0.0, 0.2)) = 0.03
    }

    SubShader
    {
        Tags 
        { 
            "RenderType"="Opaque" 
            "Queue"="Geometry"
            "RenderPipeline"="UniversalPipeline" 
        }
        
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            
            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

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
                float3 positionOS   : TEXCOORD3;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _ShadowColor;
                float4 _RimColor;
                float _RimPower;
                float _RimIntensity;
                float _ToonThreshold;
                float _ToonSmoothness;

                float4 _LocalUp;
                float4 _LocalRight;
                float4 _LocalForward;

                float4 _InkColor;
                float _InkProgress;
                float _EyeY;
                float _FeetY;

                float _FaceCenterX;
                float _FaceCenterZ;
                float _EyeSpacing;
                
                float _DripWidth;
                float _DripBlur;
                float _WiggleFreq;
                float _WiggleStrength;
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
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                output.positionOS = input.positionOS.xyz;
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Normalisation des vecteurs
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(_WorldSpaceCameraPos - input.positionWS);
                
                // Projection des coordonnées locales sur les axes personnalisés
                float3 localUp = normalize(_LocalUp.xyz);
                float3 localRight = normalize(_LocalRight.xyz);
                float3 localForward = normalize(_LocalForward.xyz);

                float height = dot(input.positionOS, localUp);
                float side = dot(input.positionOS, localRight);
                float depth = dot(input.positionOS, localForward);

                // 1. Calcul de la couleur de base Toon (avec ou sans texture)
                float4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half3 albedo = texColor.rgb * _BaseColor.rgb;
                half3 shadowAlbedo = albedo * _ShadowColor.rgb;

                // Récupération de la lumière principale directionnelle
                Light mainLight = GetMainLight();
                float3 lightDir = mainLight.direction;
                
                // Ombrage Toon
                float NdotL = dot(normalWS, lightDir);
                float halfLambert = NdotL * 0.5 + 0.5;
                float toonDiff = smoothstep(_ToonThreshold, _ToonThreshold + _ToonSmoothness, halfLambert);
                half3 toonColor = lerp(shadowAlbedo, albedo, toonDiff);
                
                // Lueur Rim Light
                float fresnel = pow(1.0 - saturate(dot(normalWS, viewDirWS)), _RimPower);
                half3 rimGlow = fresnel * _RimColor.rgb * _RimIntensity;
                half3 finalBodyColor = toonColor + rimGlow;

                // 2. CALCUL DES LIGNES DE LARMES D'ENCRE (Double trace yeux)
                // Déformation sinueuse des larmes le long de la hauteur projetée
                float2 wiggle = float2(
                    Noise2D(float2(height * _WiggleFreq, 1.0)),
                    Noise2D(float2(height * _WiggleFreq, 2.0))
                ) * _WiggleStrength;

                // Pour que les larmes suivent les courbes 3D du corps (Z) sans se couper au niveau du ventre,
                // on calcule la distance uniquement sur l'axe horizontal X local (side).
                float distLeft = abs(side + wiggle.x - (_FaceCenterX - _EyeSpacing * 0.5));
                float distRight = abs(side + wiggle.x - (_FaceCenterX + _EyeSpacing * 0.5));

                // Masque des deux lignes de larmes
                float leftDrip = smoothstep(_DripWidth + _DripBlur, _DripWidth - _DripBlur, distLeft);
                float rightDrip = smoothstep(_DripWidth + _DripBlur, _DripWidth - _DripBlur, distRight);
                
                // On limite l'encre à la face avant du modèle (pour éviter qu'elle traverse à l'arrière)
                float frontMask = smoothstep(_FaceCenterZ - 0.8, _FaceCenterZ - 0.3, depth);
                float dripMask = saturate(leftDrip + rightDrip) * frontMask;

                // 3. Masque d'écoulement progressif (Flow progress)
                // L'encre progresse entre les yeux (_EyeY) et les pieds (_FeetY)
                float targetY = lerp(_EyeY, _FeetY, _InkProgress);
                
                // Léger décalage de pointe entre les deux larmes pour le réalisme
                float tipNoise = Noise2D(float2(side, depth) * 15.0) * 0.05;
                float flowMask = smoothstep(targetY + tipNoise - 0.01, targetY + tipNoise + 0.01, height);

                // Empêcher l'encre d'apparaître au-dessus des yeux
                float heightLimit = smoothstep(_EyeY + 0.02, _EyeY - 0.01, height);

                // Masque d'encre final combiné
                float inkMask = dripMask * flowMask * heightLimit;

                // 4. Rendu de l'encre (brillante et sombre)
                float inkFresnel = pow(1.0 - saturate(dot(normalWS, viewDirWS)), 2.0);
                half3 inkBaseColor = _InkColor.rgb;
                half3 inkGlow = inkFresnel * half3(1.0, 1.0, 1.0) * 1.8;
                half3 finalInkColor = inkBaseColor + inkGlow;

                // Mélange final entre le corps de la statue et l'encre
                half3 finalColor = lerp(finalBodyColor, finalInkColor, inkMask);

                return half4(finalColor, texColor.a);
            }
            ENDHLSL
        }
    }
    FallBack "Packages/com.unity.render-pipelines.universal/FallbackError"
}
