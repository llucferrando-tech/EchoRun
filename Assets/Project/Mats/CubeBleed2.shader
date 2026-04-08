Shader "Custom/CubeBleedTransparentPulse"
{
    Properties
    {
        _BottomGlowColor("Bottom Glow Color", Color) = (0.1, 0.6, 1.0, 1)
        _TopGlowColor("Top Glow Color", Color) = (1.0, 0.2, 0.8, 1)

        _EdgeWidth("Edge Width", Range(0.001, 0.25)) = 0.06
        _GlowIntensity("Glow Intensity", Range(0, 20)) = 5.0
        _CornerBoost("Corner Boost", Range(0, 5)) = 1.0

        _BleedStart("Bleed Start", Range(0, 1)) = 0.0
        _BleedEnd("Bleed End", Range(0, 1)) = 1.0

        _HalfExtents("Half Extents", Vector) = (0.5, 0.5, 0.5, 0)

        _Reveal("Reveal", Range(0,1)) = 0.5
        _RevealFeather("Reveal Feather", Range(0.001,0.3)) = 0.05
        _RevealTop("Reveal Top (1=top, 0=bottom)", Range(0,1)) = 1

        _Pulse("Pulse", Range(0,1)) = 0
        _PulseGlowBoost("Pulse Glow Boost", Range(0,20)) = 4
        _PulseWidthBoost("Pulse Width Boost", Range(0,0.1)) = 0.02
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
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionOS  : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BottomGlowColor;
                half4 _TopGlowColor;

                float _EdgeWidth;
                float _GlowIntensity;
                float _CornerBoost;
                float _BleedStart;
                float _BleedEnd;
                float4 _HalfExtents;

                float _Reveal;
                float _RevealFeather;
                float _RevealTop;

                float _Pulse;
                float _PulseGlowBoost;
                float _PulseWidthBoost;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionOS = IN.positionOS.xyz;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 pos = IN.positionOS;
                float3 absPos = abs(pos);

                float3 halfExtents = max(_HalfExtents.xyz, float3(0.0001, 0.0001, 0.0001));

                float pulseWidth = _EdgeWidth + _Pulse * _PulseWidthBoost;
                float pulseGlow = _GlowIntensity + _Pulse * _PulseGlowBoost;

                float mx = smoothstep(halfExtents.x - pulseWidth, halfExtents.x, absPos.x);
                float my = smoothstep(halfExtents.y - pulseWidth, halfExtents.y, absPos.y);
                float mz = smoothstep(halfExtents.z - pulseWidth, halfExtents.z, absPos.z);

                float edgeMask = max(mx * my, max(mx * mz, my * mz));
                float cornerMask = mx * my * mz;
                float mask = saturate(edgeMask + cornerMask * _CornerBoost);

                float y01 = saturate((pos.y + halfExtents.y) / max(halfExtents.y * 2.0, 0.0001));

                float bleed = smoothstep(_BleedStart, _BleedEnd, y01);
                float3 color = lerp(_BottomGlowColor.rgb, _TopGlowColor.rgb, bleed);

                float topReveal = smoothstep(_Reveal - _RevealFeather, _Reveal + _RevealFeather, y01);
                float bottomReveal = 1.0 - topReveal;
                float revealMask = lerp(bottomReveal, topReveal, _RevealTop);

                float finalMask = mask * revealMask;
                float3 glow = color * finalMask * pulseGlow;

                return float4(glow, finalMask);
            }

            ENDHLSL
        }
    }
}