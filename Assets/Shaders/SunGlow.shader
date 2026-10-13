Shader "ELISE/SunCore"
{
    Properties
    {
        _Color ("Couleur Centrale (Incandescence)", Color) = (1.0, 0.88, 0.35, 1.0)
        _RimColor ("Couleur du Bord (Couronne Solaire)", Color) = (1.0, 0.52, 0.12, 1.0)
        _Intensity ("Intensite Globale", Range(0.5, 5.0)) = 1.6
        _RimPower ("Douceur du Bord", Range(0.5, 8.0)) = 2.4
        _SwirlIntensity ("Ondulation Solaire", Range(0.0, 1.0)) = 0.12
        _SwirlSpeed ("Vitesse Ondulation", Range(0.0, 3.0)) = 0.8
    }

    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent-10"
            "RenderPipeline"="UniversalPipeline"
            "IgnoreProjector"="True"
        }

        Cull Back
        ZWrite Off
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "SunCoreForward"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _RimColor;
                float _Intensity;
                float _RimPower;
                float _SwirlIntensity;
                float _SwirlSpeed;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 N = normalize(input.normalWS);
                float3 V = normalize(_WorldSpaceCameraPos - input.positionWS);

                // Calcul du limbe solaire (rim) parfaitement spherique et regulier a toute distance
                float NdotV = saturate(dot(N, V));
                float rim = pow(1.0 - NdotV, _RimPower);

                // Degrade du coeur solaire vers la couronne
                float3 color = lerp(_Color.rgb, _RimColor.rgb, rim);

                // Ondulation discrete vivante a la surface du soleil
                if (_SwirlIntensity > 0.001)
                {
                    float t = _Time.y * _SwirlSpeed;
                    float wave = sin(input.positionWS.y * 0.15 + t) * cos(input.positionWS.x * 0.15 + t * 0.8);
                    color += _RimColor.rgb * (wave * _SwirlIntensity);
                }

                color *= _Intensity;

                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
}
