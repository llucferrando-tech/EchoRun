Shader "Custom/GuitarHeroBlackLines"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        _LineColor("Line Color", Color) = (0, 0, 0, 1)

        _LaneCount("Lane Count", Float) = 5
        _LineWidth("Line Width", Range(0.001, 0.2)) = 0.03

        _HalfExtents("Half Extents", Vector) = (0.5, 0.5, 0.5, 0)

        _UseZLines("Use Z Direction Lines", Range(0,1)) = 1
        _UseXLines("Use X Direction Lines", Range(0,1)) = 1

        _LineScroll("Line Scroll", Float) = 0
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
                half4 _LineColor;

                float _LaneCount;
                float _LineWidth;

                float4 _HalfExtents;

                float _UseZLines;
                float _UseXLines;

                float _LineScroll;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionOS = IN.positionOS.xyz;
                return OUT;
            }

            float GridLineMask(float value01, float count, float width)
            {
                float scaled = value01 * count;
                float cell = frac(scaled);

                float distToLine = min(cell, 1.0 - cell);

                return 1.0 - smoothstep(width, width * 1.5, distToLine);
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 pos = IN.positionOS;

                float3 halfExtents = max(_HalfExtents.xyz, float3(0.0001, 0.0001, 0.0001));

                float x01 = saturate((pos.x + halfExtents.x) / (halfExtents.x * 2.0));

                // Scroll the Z grid through object space.
                float zPosScrolled = pos.z + _LineScroll;
                float z01 = frac((zPosScrolled + halfExtents.z) / (halfExtents.z * 2.0));

                float xLines = GridLineMask(x01, _LaneCount, _LineWidth) * _UseXLines;
                float zLines = GridLineMask(z01, _LaneCount, _LineWidth) * _UseZLines;

                float lineMask = saturate(max(xLines, zLines));

                float3 finalColor = lerp(_BaseColor.rgb, _LineColor.rgb, lineMask);

                return half4(finalColor, 1.0);
            }

            ENDHLSL
        }
    }
}