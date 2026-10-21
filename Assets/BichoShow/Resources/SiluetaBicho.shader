// Bicho Show — silueta del bicho tapado (2 de 2): pinta el bicho en blanco translúcido solo donde
// algo se le pone delante (ZTest Greater) y donde el propio bicho no se ve (stencil de
// SiluetaMarca.shader). El borde brilla más, para que se lea la forma. Lo usa SiluetaBicho.cs.
Shader "BichoShow/SiluetaBicho"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 0.55)
        _Borde ("Brillo del borde", Range(0, 1)) = 0.4
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent-19" "RenderType" = "Transparent" "IgnoreProjector" = "True" }

        Pass
        {
            Name "Silueta"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest Greater
            Cull Back
            Stencil
            {
                Ref 128
                ReadMask 128
                Comp NotEqual
            }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Borde;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; float3 vistaWS : TEXCOORD1; };

            Varyings Vert(Attributes v)
            {
                Varyings o;
                float3 posWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(posWS);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.vistaWS = GetWorldSpaceViewDir(posWS);
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                half borde = 1.0h - saturate(dot(normalize(i.normalWS), normalize(i.vistaWS)));
                half4 c = _Color;
                c.a = saturate(c.a + borde * borde * _Borde);
                return c;
            }
            ENDHLSL
        }
    }
}
