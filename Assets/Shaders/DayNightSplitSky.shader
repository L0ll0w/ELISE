Shader "Custom/DayNightSplitSky"
{
    Properties
    {
        [Header(Day Settings)]
        _DayZenithColor ("Day Zenith Color", Color) = (0.15, 0.45, 0.9, 1.0)
        _DayHorizonColor ("Day Horizon Color", Color) = (0.55, 0.75, 0.95, 1.0)

        [Header(Night Settings)]
        _NightZenithColor ("Night Zenith Color (Navy)", Color) = (0.05, 0.03, 0.15, 1.0)
        _NightHorizonColor ("Night Horizon Color (Magenta)", Color) = (0.35, 0.1, 0.3, 1.0)

        [Header(Stars)]
        _StarDensity ("Star Density", Float) = 45.0
        _StarSize ("Star Size", Range(0.01, 0.5)) = 0.15
        _StarTwinkleSpeed ("Star Twinkle Speed", Float) = 2.5
        _StarBrightness ("Star Brightness", Float) = 3.5

        [Header(Boundary Settings)]
        _SplitDirection ("Split Direction Vector", Vector) = (1.0, 0.0, 0.0, 0.0)
        _SplitOffset ("Split Offset (Win/Loss)", Range(-1.5, 1.5)) = 0.0
        _TransitionWidth ("Transition Width", Range(0.01, 2.0)) = 0.2

        [Header(Boundary Glow)]
        [HDR] _BoundaryGlowColor ("Glow Color", Color) = (1.0, 0.8, 0.9, 1.0)
        _BoundaryGlowIntensity ("Glow Intensity", Range(0.0, 5.0)) = 1.5
        _BoundaryGlowWidth ("Glow Width (Falloff)", Range(1.0, 32.0)) = 8.0
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

        Pass
        {
            Name "Skybox"
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

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
                float4 _DayZenithColor;
                float4 _DayHorizonColor;
                float4 _NightZenithColor;
                float4 _NightHorizonColor;
                float _StarDensity;
                float _StarSize;
                float _StarTwinkleSpeed;
                float _StarBrightness;
                float4 _SplitDirection;
                float _SplitOffset;
                float _TransitionWidth;
                float4 _BoundaryGlowColor;
                float _BoundaryGlowIntensity;
                float _BoundaryGlowWidth;
            CBUFFER_END

            // Fonction de hash simple 3D
            float Hash3D(float3 p)
            {
                p = frac(p * float3(443.8975, 397.2973, 491.1871));
                p += dot(p.xyz, p.yzx + 19.19);
                return frac(p.x * p.y * p.z);
            }

            // Génération procédurale d'étoiles animées
            float3 GenerateStars(float3 viewDir, float density, float size, float twinkleSpeed)
            {
                float3 gridPos = viewDir * density;
                float3 cellId = floor(gridPos);
                float3 localPos = frac(gridPos) - 0.5;

                float cellHash = Hash3D(cellId);

                // Affiche des étoiles dans environ 4% des cellules de grille
                if (cellHash > 0.96)
                {
                    // Décalage aléatoire de l'étoile dans sa cellule
                    float3 starOffset = float3(
                        frac(cellHash * 123.4) - 0.5,
                        frac(cellHash * 456.7) - 0.5,
                        frac(cellHash * 789.0) - 0.5
                    ) * 0.7;

                    float dist = length(localPos - starOffset);
                    float2 d = localPos.xy - starOffset.xy;

                    // Cœur brillant de l'étoile
                    float core = smoothstep(size, size * 0.1, dist);

                    // Branches en croix (Flares)
                    float flareX = saturate(1.0 - abs(d.x) / (size * 4.0)) * saturate(1.0 - abs(d.y) / (size * 0.3));
                    float flareY = saturate(1.0 - abs(d.y) / (size * 4.0)) * saturate(1.0 - abs(d.x) / (size * 0.3));

                    // Certaines étoiles (les 1% au hash le plus élevé) sont des super-géantes scintillantes
                    float isSuperGiant = smoothstep(0.985, 0.99, cellHash);
                    float finalSizeMultiplier = lerp(1.0, 1.8, isSuperGiant);
                    float flareStrength = lerp(0.5, 1.2, isSuperGiant);

                    // Recalcul avec multiplicateur de taille pour les géantes
                    if (isSuperGiant > 0.0)
                    {
                        core = smoothstep(size * finalSizeMultiplier, (size * finalSizeMultiplier) * 0.1, dist);
                        flareX = saturate(1.0 - abs(d.x) / (size * finalSizeMultiplier * 5.0)) * saturate(1.0 - abs(d.y) / (size * finalSizeMultiplier * 0.25));
                        flareY = saturate(1.0 - abs(d.y) / (size * finalSizeMultiplier * 5.0)) * saturate(1.0 - abs(d.x) / (size * finalSizeMultiplier * 0.25));
                    }

                    float starGlow = core + (flareX + flareY) * flareStrength;

                    // Scintillement sinusoïdal basé sur le temps et le hash de cellule
                    float twinkle = sin(_Time.y * twinkleSpeed + cellHash * 6.28) * 0.45 + 0.55;

                    // Choix de couleur pastel (Dreamcore) basé sur le hash
                    float3 starColor = float3(1.0, 1.0, 1.0); // Blanc
                    if (cellHash > 0.99)      starColor = float3(1.0, 0.65, 0.85); // Rose pastel
                    else if (cellHash > 0.98) starColor = float3(0.6, 0.9, 1.0);  // Cyan pastel
                    else if (cellHash > 0.97) starColor = float3(1.0, 0.95, 0.6); // Jaune pastel

                    return starGlow * twinkle * starColor * _StarBrightness;
                }
                return float3(0.0, 0.0, 0.0);
            }

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                
                // Positionne les sommets à l'infini (sur le Far Plane de rendu)
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(positionWS);
                
                #if UNITY_REVERSED_Z
                    output.positionCS.z = 1.0e-5f; // Très proche de 0 en Z inversé (Far plane URP)
                #else
                    output.positionCS.z = output.positionCS.w - 1.0e-5f;
                #endif

                // Le vecteur de vue locale est simplement la position de la sphère
                output.viewDir = input.positionOS.xyz;
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 viewDir = normalize(input.viewDir);

                // 1. Calcul de la séparation Jour / Nuit
                float3 splitDir = normalize(_SplitDirection.xyz);
                float splitVal = dot(viewDir, splitDir) + _SplitOffset;
                
                // Lissage de la transition
                float transition = smoothstep(-_TransitionWidth * 0.5, _TransitionWidth * 0.5, splitVal);

                // 2. Ciel de Jour (Gradient Horizon -> Zenith)
                float dayT = saturate(viewDir.y * 1.5 + 0.1);
                float3 dayColor = lerp(_DayHorizonColor.rgb, _DayZenithColor.rgb, dayT);

                // 3. Ciel de Nuit (Gradient Horizon -> Zenith)
                float nightT = saturate(viewDir.y * 1.5 + 0.1);
                float3 nightSkyColor = lerp(_NightHorizonColor.rgb, _NightZenithColor.rgb, nightT);

                // Étoiles procédurales (visibles uniquement en hauteur et côté nuit)
                float3 stars = GenerateStars(viewDir, _StarDensity, _StarSize, _StarTwinkleSpeed);
                stars *= (1.0 - transition) * saturate(viewDir.y * 3.0); // Coupe à l'horizon et côté Jour

                float3 nightColor = nightSkyColor + stars;

                // 4. Mélange final Jour / Nuit
                float3 finalColor = lerp(nightColor, dayColor, transition);

                // 5. Lueur de frontière (style néon/dreamcore sur la coupure)
                if (_BoundaryGlowIntensity > 0.0)
                {
                    // Lueur maximale à la frontière (transition = 0.5)
                    float glowMask = 1.0 - abs(transition - 0.5) * 2.0;
                    glowMask = pow(saturate(glowMask), _BoundaryGlowWidth);
                    float3 glow = _BoundaryGlowColor.rgb * glowMask * _BoundaryGlowIntensity;
                    finalColor += glow;
                }

                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
}
