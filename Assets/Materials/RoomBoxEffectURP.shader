Shader "Meta/URP/RoomBoxEffectURP"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.15, 0.25, 0.9, 0.5)
        _EmissionColor ("Emission Color", Color) = (0.25, 0.5, 1.5, 1)
        _RimPower ("Edge Brightness", Range(0.1, 8)) = 2.5
        _Smoothness ("Smoothness", Range(0, 1)) = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS    : TEXCOORD0;
                float3 viewDirWS   : TEXCOORD1;
            };

            float4 _BaseColor;
            float4 _EmissionColor;
            float _RimPower;

            Varyings vert (Attributes v)
            {
                Varyings o;
                float3 positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionHCS = TransformWorldToHClip(positionWS);
                o.normalWS = normalize(TransformObjectToWorldNormal(v.normalOS));
                o.viewDirWS = normalize(GetWorldSpaceViewDir(positionWS));
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                float rim = pow(1.0 - saturate(dot(i.viewDirWS, i.normalWS)), _RimPower);
                float3 col = _BaseColor.rgb + _EmissionColor.rgb * rim;
                return half4(col, _BaseColor.a);
            }
            ENDHLSL
        }
    }
}
