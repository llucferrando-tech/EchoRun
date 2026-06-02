Shader "Custom/UIVoronoiWavy"
{
    Properties
    {
        _MainTex("Voronoi Texture", 2D) = "white" {}
        _Tint("Tint", Color) = (0.15, 0.18, 0.25, 1)

        _Tiling("Tiling", Vector) = (2, 4, 0, 0)
        _PanSpeed("Pan Speed", Vector) = (0.01, 0.015, 0, 0)

        _WaveStrength("Wave Strength", Range(0, 0.1)) = 0.006
        _WaveFrequency("Wave Frequency", Range(0, 30)) = 7
        _WaveSpeed("Wave Speed", Range(0, 5)) = 0.35

        _TextureStrength("Texture Strength", Range(0, 2)) = 0.45
        _Contrast("Contrast", Range(0.1, 4)) = 1.4
        _Brightness("Brightness", Range(0, 2)) = 0.8

        _Alpha("Alpha", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "RenderPipeline"="UniversalPipeline"
            "PreviewType"="Plane"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "Default"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
                float4 _Tiling;
                float4 _PanSpeed;

                float _WaveStrength;
                float _WaveFrequency;
                float _WaveSpeed;

                float _TextureStrength;
                float _Contrast;
                float _Brightness;
                float _Alpha;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float t = _Time.y;
                float2 uv = IN.uv;

                float waveX = sin(uv.y * _WaveFrequency + t * _WaveSpeed) * _WaveStrength;
                float waveY = cos(uv.x * (_WaveFrequency * 0.8) + t * (_WaveSpeed * 0.85)) * (_WaveStrength * 0.6);

                float2 finalUV = (uv + float2(waveX, waveY)) * _Tiling.xy + _PanSpeed.xy * t;

                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, finalUV);

                float n = tex.r;
                n = saturate((n - 0.5) * _Contrast + 0.5);
                n *= _Brightness;

                float3 color = _Tint.rgb + (n * _TextureStrength);

                return half4(color, _Alpha) * IN.color;
            }

            ENDHLSL
        }
    }
}