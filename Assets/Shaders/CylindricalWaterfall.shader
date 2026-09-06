Shader "2.5D RPG/CylindricalWaterfall"
{
    Properties
    {
        [Header(Couleurs d Eau)]
        _DeepColor ("Eau Profonde (Deep Color)", Color) = (0.02, 0.25, 0.55, 0.88)
        _ShallowColor ("Eau de Surface (Shallow Color)", Color) = (0.1, 0.78, 0.95, 0.72)
        _FoamColor ("Ecume & Mousse (Foam Color)", Color) = (0.95, 0.99, 1.0, 0.95)
        _FresnelColor ("Reflet Bords (Fresnel Color)", Color) = (0.5, 0.92, 1.0, 1.0)

        [Header(Ecoulement et Streaks Liquides)]
        _FlowSpeed ("Vitesse Chute d Eau", Float) = 3.5
        _StreamSpeed ("Vitesse Trainees Verticales", Float) = 5.0
        _StreamScale ("Echelle Trainees", Float) = 16.0
        _DistortionSpeed ("Vitesse Turbulences", Float) = 1.8
        _DistortionStrength ("Intensite Turbulences", Range(0, 0.4)) = 0.12

        [Header(Mousse et Intersections)]
        _FoamThreshold ("Seuil d Ecume", Range(0.1, 0.9)) = 0.52
        _FoamSoftness ("Lissage Ecume", Range(0.01, 0.5)) = 0.15
        _DepthFoamDistance ("Distance Mousse Intersection", Range(0.01, 3.0)) = 0.8

        [Header(Ondulations et Silhouette 3D)]
        _FresnelPower ("Puissance Fresnel (Silhouette)", Range(0.5, 8.0)) = 2.0
        _DisplacementAmount ("Ondulation 3D Parois", Range(0, 0.4)) = 0.08
        _DisplacementSpeed ("Vitesse Ondulation 3D", Float) = 2.5
        _DisplacementFrequency ("Frequence Ondulation 3D", Float) = 4.0

        [Header(Fondus d Extremites)]
        _TopFade ("Fondu Sommet", Range(0, 0.4)) = 0.08
        _BottomFade ("Fondu Base", Range(0, 0.4)) = 0.08
    }

    SubShader
    {
        Tags 
        { 
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent+10"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off // Double face pour un cylindre creux avec interieur et exterieur

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
                float2 uv           : TEXCOORD3;
                float3 viewDirWS    : TEXCOORD4;
                float4 screenPos    : TEXCOORD5;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _DeepColor;
                float4 _ShallowColor;
                float4 _FoamColor;
                float4 _FresnelColor;
                float _FlowSpeed;
                float _StreamSpeed;
                float _StreamScale;
                float _DistortionSpeed;
                float _DistortionStrength;
                float _FoamThreshold;
                float _FoamSoftness;
                float _DepthFoamDistance;
                float _FresnelPower;
                float _DisplacementAmount;
                float _DisplacementSpeed;
                float _DisplacementFrequency;
                float _TopFade;
                float _BottomFade;
            CBUFFER_END

            // Bruit de Perlin / Simplex 2D de haute qualite pour mouvements d eau naturels
            float hash21(float2 p)
            {
                p = frac(p * float2(234.34, 435.12));
                p += dot(p, p + 56.23);
                return frac(p.x * p.y);
            }

            float noise2D(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                float a = hash21(i);
                float b = hash21(i + float2(1.0, 0.0));
                float c = hash21(i + float2(0.0, 1.0));
                float d = hash21(i + float2(1.0, 1.0));

                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            float fbm(float2 p)
            {
                float val = 0.0;
                float amp = 0.5;
                for (int i = 0; i < 3; i++)
                {
                    val += noise2D(p) * amp;
                    p *= 2.07;
                    amp *= 0.5;
                }
                return val;
            }

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float time = _Time.y * _DisplacementSpeed;
                float3 posOS = input.positionOS.xyz;

                // Ondulation organique 3D des parois du cylindre pour briser la rigidite du tube
                float angleUV = input.uv.x * 6.28318 * 2.0;
                float wave1 = sin(posOS.y * _DisplacementFrequency + time) * cos(angleUV + time * 0.7);
                float wave2 = cos(posOS.y * (_DisplacementFrequency * 1.5) - time * 1.3) * sin(angleUV * 2.0 + time);
                float totalDisplacement = (wave1 + wave2 * 0.5) * _DisplacementAmount;

                posOS += input.normalOS * totalDisplacement;

                VertexPositionInputs vertexInput = GetVertexPositionInputs(posOS);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);

                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = normalize(normalInput.normalWS);
                output.uv = input.uv;
                output.viewDirWS = GetWorldSpaceNormalizeViewDir(vertexInput.positionWS);
                output.screenPos = ComputeScreenPos(vertexInput.positionCS);

                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float time = _Time.y;
                float2 uv = input.uv;

                // 1. Distorsion UV liquide multi-octaves (turbulences fluides)
                float2 distUV1 = uv * float2(6.0, 2.0) + float2(time * 0.3, time * _DistortionSpeed);
                float2 distUV2 = uv * float2(10.0, 4.0) - float2(time * 0.4, time * _DistortionSpeed * 1.3);
                
                float n1 = fbm(distUV1);
                float n2 = fbm(distUV2);
                float noiseDist = (n1 - n2);

                float2 distortedUV = float2(
                    uv.x + noiseDist * _DistortionStrength,
                    uv.y - time * _FlowSpeed * 0.2 + noiseDist * 0.05
                );

                // 2. Lignes d ecoulement vertical et trainees d eau (Vertical Water Streaks)
                float2 streamUV = float2(distortedUV.x * _StreamScale, distortedUV.y * 3.0 - time * _StreamSpeed);
                float streamNoise = fbm(streamUV);
                float streamMask = pow(saturate(streamNoise * 1.4), 2.0);

                // 3. Bandes de mousse dynamique (Dynamic Foam Layers)
                float2 foamUV = distortedUV * float2(8.0, 4.0) - float2(0, time * _FlowSpeed * 0.4);
                float foamNoise = fbm(foamUV);
                float foamMask = smoothstep(_FoamThreshold - _FoamSoftness, _FoamThreshold + _FoamSoftness, foamNoise + streamMask * 0.4);

                // 4. Foam d intersection avec les objets 3D et le sol (Soft Depth Foam)
                float depthFoam = 0.0;
                #if defined(_MAIN_LIGHT_SHADOWS) || defined(_CAMERA_DEPTH_TEXTURE) || defined(REQUIRES_DEPTH_TEXTURE)
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                float rawDepth = SampleSceneDepth(screenUV);
                float sceneEyeDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
                float surfaceEyeDepth = input.screenPos.w;
                float depthDiff = sceneEyeDepth - surfaceEyeDepth;

                if (depthDiff > 0.0 && depthDiff < _DepthFoamDistance)
                {
                    depthFoam = smoothstep(_DepthFoamDistance, 0.0, depthDiff);
                }
                #endif

                // 5. Gradient de couleur d eau (Profondeur vs Surface)
                float waterMix = saturate(streamMask * 0.6 + noiseDist * 0.3 + 0.3);
                float4 waterColor = lerp(_DeepColor, _ShallowColor, waterMix);

                // Ajouter les trainees d ecoulement et la mousse d intersection
                float totalFoam = saturate(foamMask + depthFoam * 0.8);
                waterColor = lerp(waterColor, _FoamColor, totalFoam);

                // 6. Effet Fresnel lumineux sur la silhouette du cylindre (Bords Volumetriques)
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(input.viewDirWS);
                float NdotV = saturate(dot(normalWS, viewDirWS));
                float fresnel = pow(1.0 - NdotV, _FresnelPower);

                waterColor += _FresnelColor * fresnel * 0.9;

                // 7. Fondus progressifs au sommet et a la base pour une transition naturelle dans l air et le sol
                float topFadeMask = smoothstep(0.0, _TopFade, 1.0 - input.uv.y);
                float bottomFadeMask = smoothstep(0.0, _BottomFade, input.uv.y);
                float edgeFade = topFadeMask * bottomFadeMask;

                waterColor.a *= edgeFade;

                return waterColor;
            }
            ENDHLSL
        }
    }
    FallBack "Transparent/Cutout/VertexLit"
}
