Shader "ELISE/SunHalo"
{
    Properties
    {
        _Color ("Couleur du Halo", Color) = (1.0, 0.75, 0.3, 1.0)
        _Intensity ("Intensite", Range(0.0, 5.0)) = 1.4
        _Falloff ("Attenuation Radiale", Range(0.5, 6.0)) = 2.2
    }

    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent+50" 
            "RenderType"="Transparent" 
            "IgnoreProjector"="True"
            "RenderPipeline"="UniversalPipeline"
        }

        Blend One One
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "SunHaloPass"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Intensity;
                float _Falloff;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv * 2.0 - 1.0; // Centre a (0,0), bords a 1.0
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float dist = length(input.uv);
                float alpha = pow(saturate(1.0 - dist), _Falloff) * _Intensity;
                return half4(_Color.rgb * alpha, 1.0);
            }
            ENDHLSL
        }
    }
}
