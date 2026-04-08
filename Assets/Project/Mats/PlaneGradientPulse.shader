Shader "Custom/PlaneGradientPulse"
{
    Properties
    {
        _BottomColor("Bottom Color", Color) = (0.1, 0.6, 1.0, 1)
        _GlowIntensity("Glow Intensity", Range(0, 20)) = 3.0

        _FadeStart("Fade Start", Range(0, 1)) = 0.0
        _FadeEnd("Fade End", Range(0, 1)) = 1.0

        _Pulse("Pulse", Range(0,1)) = 0
        _PulseGlowBoost("Pulse Glow Boost", Range(0,20)) = 4.0
        _PulseAlphaBoost("Pulse Alpha Boost", Range(0,1)) = 0.2
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "Forward"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BottomColor;
                float _GlowIntensity;
                float _FadeStart;
                float _FadeEnd;
                float _Pulse;
                float _PulseGlowBoost;
                float _PulseAlphaBoost;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float y = saturate(IN.uv.y);

                // 1 at bottom, 0 at top
                float fade = smoothstep(_FadeStart, _FadeEnd, y);

                float glow = _GlowIntensity + _Pulse * _PulseGlowBoost;

                // Fade both color and alpha
                float alpha = saturate(fade + _Pulse * _PulseAlphaBoost * fade);
                float3 color = _BottomColor.rgb * glow * fade;

                return float4(color, alpha);
            }

            ENDHLSL
        }
    }
}