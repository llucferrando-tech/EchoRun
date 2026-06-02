Shader "Custom/CubeBaseEdgeGlowPulse"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (0.18, 0.15, 0.32, 1)
        _TopBaseColor("Top Base Color", Color) = (0.32, 0.28, 0.5, 1)

        _BottomGlowColor("Bottom Glow Color", Color) = (0.1, 0.6, 1.0, 1)
        _TopGlowColor("Top Glow Color", Color) = (1.0, 0.2, 0.8, 1)

        _EdgeWidth("Edge Width", Range(0.001, 0.25)) = 0.06
        _GlowIntensity("Glow Intensity", Range(0, 20)) = 4.0
        _CornerBoost("Corner Boost", Range(0, 5)) = 1.0

        _BaseGradientStrength("Base Gradient Strength", Range(0, 1)) = 0.5
        _BleedStart("Glow Bleed Start", Range(0, 1)) = 0.0
        _BleedEnd("Glow Bleed End", Range(0, 1)) = 1.0

        _HalfExtents("Half Extents", Vector) = (0.5, 0.5, 0.5, 0)

        _Pulse("Pulse", Range(0,1)) = 0
        _PulseGlowBoost("Pulse Glow Boost", Range(0,20)) = 4
        _PulseWidthBoost("Pulse Width Boost", Range(0,0.1)) = 0.02
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }

            ZWrite On
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
                half4 _BaseColor;
                half4 _TopBaseColor;

                half4 _BottomGlowColor;
                half4 _TopGlowColor;

                float _EdgeWidth;
                float _GlowIntensity;
                float _CornerBoost;

                float _BaseGradientStrength;
                float _BleedStart;
                float _BleedEnd;

                float4 _HalfExtents;

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

                float y01 = saturate((pos.y + halfExtents.y) / max(halfExtents.y * 2.0, 0.0001));

                // Solid cube body.
                float3 baseGradient = lerp(_BaseColor.rgb, _TopBaseColor.rgb, y01);
                float3 baseColor = lerp(_BaseColor.rgb, baseGradient, _BaseGradientStrength);

                // Edge/corner mask.
                float pulseWidth = _EdgeWidth + _Pulse * _PulseWidthBoost;
                float pulseGlow = _GlowIntensity + _Pulse * _PulseGlowBoost;

                float mx = smoothstep(halfExtents.x - pulseWidth, halfExtents.x, absPos.x);
                float my = smoothstep(halfExtents.y - pulseWidth, halfExtents.y, absPos.y);
                float mz = smoothstep(halfExtents.z - pulseWidth, halfExtents.z, absPos.z);

                // Edges = at least two axes are near their outer surface.
                float edgeMask = max(mx * my, max(mx * mz, my * mz));

                // Corners = all three axes are near their outer surface.
                float cornerMask = mx * my * mz;

                float mask = saturate(edgeMask + cornerMask * _CornerBoost);

                // Vertical glow color blend.
                float bleed = smoothstep(_BleedStart, _BleedEnd, y01);
                float3 glowColor = lerp(_BottomGlowColor.rgb, _TopGlowColor.rgb, bleed);

                float3 glow = glowColor * mask * pulseGlow;

                float3 finalColor = baseColor + glow;

                return half4(finalColor, 1.0);
            }

            ENDHLSL
        }
    }
}