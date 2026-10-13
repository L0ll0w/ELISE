Shader "ELISE/SunBlinkingEye"
{
    Properties
    {
        [Header(Eye Base Settings)]
        [MainTexture] _BaseMap ("Eye Base Texture", 2D) = "white" {}
        [MainColor] _EyeColor ("Eye Tint", Color) = (1.0, 1.0, 1.0, 1.0)
        _EyeGlow ("Eye Glow / Emission", Range(0.5, 3.0)) = 1.2

        [Header(Blink Animation)]
        _BlinkAmount ("Blink Amount (0=Ouvert, 1=Ferme)", Range(0.0, 1.0)) = 0.0
        _EyeRadius ("Eye Radius in World Space", Float) = 5.0
        _EyeCenterWS ("Eye Center World Space", Vector) = (0, 0, 0, 0)
        _EyeUpWS ("Eye Up Direction WS", Vector) = (0, 1, 0, 0)
        _EyeRightWS ("Eye Right Direction WS", Vector) = (1, 0, 0, 0)
        _PupilLookOffset ("Pupil Look Offset (Regard)", Vector) = (0, 0, 0, 0)

        [Header(Eyelid Crease Line (Optionnel))]
        _ShowCreaseLine ("Afficher Trait de Pli (0 = Aucun trait)", Range(0.0, 1.0)) = 0.0
        _EyelidFoldColor ("Couleur Pli Paupiere", Color) = (0.95, 0.45, 0.08, 1.0)
        _EyelidFoldWidth ("Largeur Pli Paupiere", Range(0.01, 0.25)) = 0.04

        [Header(Solar Eyelid Texture)]
        _SunColor ("Soleil Centre (Incandescence)", Color) = (1.0, 0.35, 0.0, 1.0)
        _SunRimColor ("Soleil Bord (Couronne)", Color) = (0.90, 0.75, 0.0, 1.0)
        _SunIntensity ("Intensite Solaire", Range(0.5, 5.0)) = 3.22
        _SunRimPower ("Douceur Bord Solaire", Range(0.5, 8.0)) = 2.4
        _SwirlIntensity ("Ondulation Solaire", Range(0.0, 1.0)) = 0.12
        _SwirlSpeed ("Vitesse Ondulation", Range(0.0, 3.0)) = 0.8
    }

    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent" 
            "RenderPipeline"="UniversalPipeline"
            "IgnoreProjector"="True"
        }

        Cull Back
        ZWrite Off
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "SunBlinkingEyeForward"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
                float2 uv           : TEXCOORD2;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _EyeColor;
                float _EyeGlow;

                float _BlinkAmount;
                float _EyeRadius;
                float4 _EyeCenterWS;
                float4 _EyeUpWS;
                float4 _EyeRightWS;
                float4 _PupilLookOffset;
                float _ShowCreaseLine;
                float4 _EyelidFoldColor;
                float _EyelidFoldWidth;

                float4 _SunColor;
                float4 _SunRimColor;
                float _SunIntensity;
                float _SunRimPower;
                float _SwirlIntensity;
                float _SwirlSpeed;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 1. Détermination des axes d'orientation de l'oeil (script ou matrice caméra)
                float3 camUp = length(_EyeUpWS.xyz) > 0.1 ? normalize(_EyeUpWS.xyz) : UNITY_MATRIX_I_V._m01_m11_m21;
                float3 camRight = length(_EyeRightWS.xyz) > 0.1 ? normalize(_EyeRightWS.xyz) : UNITY_MATRIX_I_V._m00_m10_m20;

                // Projection de la position du pixel par rapport au centre de l'oeil
                float3 toPixelWS = input.positionWS - _EyeCenterWS.xyz;
                float eyeRadius = max(0.01, _EyeRadius);
                float eyeX = dot(toPixelWS, camRight) / eyeRadius;
                float eyeY = dot(toPixelWS, camUp) / eyeRadius;

                // Courbure naturelle des paupières (légère cambrure descendante)
                float archCurve = 1.0 - saturate(eyeX * eyeX * 0.25);
                float seamY = -0.05 * archCurve;

                // Trajectoires :
                // Au repos (_BlinkAmount = 0), les paupières sont complètement en dehors de la sphère (+1.5 et -1.5)
                // Aucun trait, aucune bordure ou masque autour de la sphère !
                // À la fermeture (_BlinkAmount = 1), elles se rejoignent au centre (seamY).
                float topEyelid = lerp(1.5, seamY - 0.03, _BlinkAmount);
                float bottomEyelid = lerp(-1.5, seamY + 0.03, _BlinkAmount);

                // Masque de couverture des paupières avec bordure douce anti-aliasing
                float isTopCovered = smoothstep(topEyelid - 0.02, topEyelid + 0.02, eyeY);
                float isBottomCovered = smoothstep(bottomEyelid + 0.02, bottomEyelid - 0.02, eyeY);
                float eyelidMask = max(isTopCovered, isBottomCovered);

                // Garantir un oeil 100% propre et ouvert au repos, et 100% fermé à l'arrêt
                if (_BlinkAmount >= 0.98)
                {
                    eyelidMask = 1.0;
                }
                if (_BlinkAmount <= 0.01)
                {
                    eyelidMask = 0.0;
                }

                // 2. Texture Solaire (Identique à SunCore du Soleil)
                float3 N = normalize(input.normalWS);
                float3 V = normalize(_WorldSpaceCameraPos - input.positionWS);
                float NdotV = saturate(dot(N, V));
                float rim = pow(1.0 - NdotV, _SunRimPower);
                float3 sunColor = lerp(_SunColor.rgb, _SunRimColor.rgb, rim);

                if (_SwirlIntensity > 0.001)
                {
                    float t = _Time.y * _SwirlSpeed;
                    float wave = sin(input.positionWS.y * 0.15 + t) * cos(input.positionWS.x * 0.15 + t * 0.8);
                    sunColor += _SunRimColor.rgb * (wave * _SwirlIntensity);
                }
                sunColor *= _SunIntensity;

                // Trait de pli optionnel (désactivé par défaut, actif UNIQUEMENT si _ShowCreaseLine > 0)
                if (_ShowCreaseLine > 0.001 && _BlinkAmount > 0.05)
                {
                    float distTop = abs(eyeY - topEyelid);
                    float distBottom = abs(eyeY - bottomEyelid);
                    float distSeam = abs(eyeY - seamY);
                    float openFold = (smoothstep(_EyelidFoldWidth, 0.0, distTop) + smoothstep(_EyelidFoldWidth, 0.0, distBottom));
                    float closedCrease = smoothstep(_EyelidFoldWidth * 0.75, 0.0, distSeam);
                    float creaseIntensity = lerp(openFold * 0.35, closedCrease * 0.55, _BlinkAmount) * _ShowCreaseLine;
                    sunColor = lerp(sunColor, _EyelidFoldColor.rgb * _SunIntensity, creaseIntensity);
                }

                // 3. Rendu de l'œil (Sclère ou Pupille)
                float2 uv = input.uv + _PupilLookOffset.xy;
                half4 eyeTex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);
                float3 eyeColor = eyeTex.rgb * _EyeColor.rgb * _EyeGlow;

                // 4. Fondu final entre l'œil et la paupière solaire
                float3 finalColor = lerp(eyeColor, sunColor, eyelidMask);

                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
}
