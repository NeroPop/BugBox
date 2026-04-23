Shader "Custom/GhibliToon"
{
    Properties
    {
        _MainTex        ("Painted Texture",  2D)            = "white" {}
        _BaseColor      ("Base Color Tint",  Color)         = (1,1,1,1)

        [Header(Toon Shading)]
        _ShadowColor    ("Shadow Color",     Color)         = (0.4, 0.45, 0.65, 1)
        _ShadowStep     ("Shadow Step",      Range(0, 1))   = 0.4
        _ShadowFeather  ("Shadow Feather",   Range(0, 0.2)) = 0.04

        [Header(Rim Light)]
        _RimColor       ("Rim Color",        Color)         = (0.8, 0.85, 1.0, 1)
        _RimPower       ("Rim Power",        Range(0.1, 8)) = 2.0
        _RimStrength    ("Rim Strength",     Range(0, 1))   = 0.3

        [Header(Outline)]
        [Toggle] _EnableOutline ("Enable Outline", Float)  = 1
        _OutlineColor   ("Outline Color Override",  Color) = (0.1, 0.08, 0.06, 1)
        _OutlineThicknessOverride ("Outline Thickness Override", Range(1, 10)) = 1.0

        [HideInInspector] _StencilRef  ("Stencil Ref",  Int) = 1
        [HideInInspector] _StencilComp ("Stencil Comp", Int) = 8
        [HideInInspector] _StencilOp   ("Stencil Op",   Int) = 2
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue"          = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Stencil
            {
                Ref   [_StencilRef]
                Comp  [_StencilComp]
                Pass  [_StencilOp]
            }

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float3 positionWS  : TEXCOORD2;
                float3 viewDirWS   : TEXCOORD3;
                float4 shadowCoord : TEXCOORD4;
                float  fogFactor   : TEXCOORD5;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _BaseColor;
                float4 _ShadowColor;
                float  _ShadowStep;
                float  _ShadowFeather;
                float4 _RimColor;
                float  _RimPower;
                float  _RimStrength;
                float4 _OutlineColor;
                float  _OutlineThicknessOverride;
                float  _EnableOutline;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   norInputs = GetVertexNormalInputs(IN.normalOS);

                OUT.positionHCS = posInputs.positionCS;
                OUT.positionWS  = posInputs.positionWS;
                OUT.normalWS    = norInputs.normalWS;
                OUT.viewDirWS   = GetWorldSpaceViewDir(posInputs.positionWS);
                OUT.uv          = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.shadowCoord = GetShadowCoord(posInputs);
                OUT.fogFactor   = ComputeFogFactor(posInputs.positionCS.z);

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 normal  = normalize(IN.normalWS);
                float3 viewDir = normalize(IN.viewDirWS);

                half4 texColor  = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                half4 baseColor = texColor * _BaseColor;

                Light  mainLight = GetMainLight(IN.shadowCoord);
                float3 lightDir  = normalize(mainLight.direction);

                float NdotL    = dot(normal, lightDir);
                float shadow   = mainLight.shadowAttenuation;
                float lightVal = NdotL * shadow * 0.5 + 0.5;

                float toon = smoothstep(
                    _ShadowStep - _ShadowFeather,
                    _ShadowStep + _ShadowFeather,
                    lightVal
                );

                half3 litColor    = baseColor.rgb * mainLight.color.rgb;
                half3 shadowColor = _ShadowColor.rgb * baseColor.rgb;
                half3 diffuse     = lerp(shadowColor, litColor, toon);

                float rim       = 1.0 - saturate(dot(viewDir, normal));
                float rimFactor = pow(rim, _RimPower) * _RimStrength * toon;
                half3 rimColor  = _RimColor.rgb * rimFactor;

                half3 finalColor = MixFog(diffuse + rimColor, IN.fogFactor);

                return half4(finalColor, baseColor.a);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex   vertDepthNormals
            #pragma fragment fragDepthNormals

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct AttributesDN
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct VaryingsDN
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS    : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _BaseColor;
                float4 _ShadowColor;
                float  _ShadowStep;
                float  _ShadowFeather;
                float4 _RimColor;
                float  _RimPower;
                float  _RimStrength;
                float4 _OutlineColor;
                float  _OutlineThicknessOverride;
                float  _EnableOutline;
            CBUFFER_END

            VaryingsDN vertDepthNormals(AttributesDN IN)
            {
                VaryingsDN OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.normalWS    = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            float4 fragDepthNormals(VaryingsDN IN) : SV_Target
            {
                return float4(normalize(IN.normalWS) * 0.5 + 0.5, 1);
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
    }
}