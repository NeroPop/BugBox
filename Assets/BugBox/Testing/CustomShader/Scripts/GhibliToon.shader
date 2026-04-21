Shader "Custom/GhibliToon"
{
    Properties
    {
        _MainTex        ("Painted Texture",     2D)    = "white" {}
        _BaseColor      ("Base Color Tint",     Color) = (1,1,1,1)
        
        [Header(Toon Shading)]
        _ShadowColor    ("Shadow Color",        Color) = (0.4, 0.45, 0.65, 1)
        _ShadowStep     ("Shadow Step",         Range(0, 1))   = 0.4
        _ShadowFeather  ("Shadow Feather",      Range(0, 0.2)) = 0.04

        [Header(Rim Light)]
        _RimColor       ("Rim Color",           Color) = (0.8, 0.85, 1.0, 1)
        _RimPower       ("Rim Power",           Range(0.1, 8)) = 2.0
        _RimStrength    ("Rim Strength",        Range(0, 1))   = 0.3

        [Header(Painterly)]
        _TextureInfluence("Texture Influence",  Range(0, 1))   = 0.9

        [Header(Outline)]
        _OutlineColor   ("Outline Color",   Color)        = (0.1, 0.08, 0.08, 1)
        _OutlineWidth   ("Outline Width",   Range(0, 5))  = 1.0
    }

    SubShader
    {
        Tags 
        { 
            "RenderType"      = "Opaque" 
            "RenderPipeline"  = "UniversalPipeline"
            "LightMode"       = "UniversalForward"
        }

        // Pass 1 - Main toon lit pass
        Pass
        {
            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS    : TEXCOORD0;
                float3 positionWS  : TEXCOORD1;
                float2 uv          : TEXCOORD2;
                float3 viewDirWS   : TEXCOORD3;
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
                float  _TextureInfluence;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionWS  = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.normalWS    = TransformObjectToWorldNormal(IN.normalOS);
                OUT.viewDirWS   = GetWorldSpaceViewDir(OUT.positionWS);
                OUT.uv          = TRANSFORM_TEX(IN.uv, _MainTex);

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Normalise interpolated vectors
                float3 normal  = normalize(IN.normalWS);
                float3 viewDir = normalize(IN.viewDirWS);

                // Sample painted texture
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                half4 baseColor = texColor * _BaseColor;

                // Get main light
                float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                Light  mainLight   = GetMainLight(shadowCoord);

                float3 lightDir    = normalize(mainLight.direction);
                float  lightAtten  = mainLight.shadowAttenuation;

                // NdotL with shadow attenuation folded in
                float NdotL = dot(normal, lightDir) * 0.5 + 0.5; // remap to 0-1
                NdotL       = NdotL * lightAtten;

                // Stepped toon shading with feathered edge
                float toonStep = smoothstep(
                    _ShadowStep - _ShadowFeather,
                    _ShadowStep + _ShadowFeather,
                    NdotL
                );

                // Blend between shadow colour and lit colour
                // Shadow colour is independent so it can shift warm/cool
                half3 litColor    = baseColor.rgb * mainLight.color;
                half3 shadowColor = _ShadowColor.rgb * baseColor.rgb;
                half3 diffuse     = lerp(shadowColor, litColor, toonStep);

                // Blend texture influence — lets painted detail show through
                diffuse = lerp(baseColor.rgb, diffuse, _TextureInfluence);

                // Rim light
                float rim       = 1.0 - saturate(dot(viewDir, normal));
                float rimFactor = pow(rim, _RimPower) * _RimStrength;
                half3 rimColor  = _RimColor.rgb * rimFactor * (toonStep); // only on lit side
                
                half3 finalColor = diffuse + rimColor;

                return half4(finalColor, baseColor.a);
            }
            ENDHLSL
        }

        // Pass 2 - Outline pass 
        Pass
        {
            Name "Outline"
            Cull Front  // Render back faces only

            HLSLPROGRAM
            #pragma vertex   vertOutline
            #pragma fragment fragOutline

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _BaseColor;
                float4 _ShadowColor;
                float  _ShadowStep;
                float  _ShadowFeather;
                float4 _RimColor;
                float  _RimPower;
                float  _RimStrength;
                float  _TextureInfluence;
                float4 _OutlineColor;
                float  _OutlineWidth;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            Varyings vertOutline(Attributes IN)
            {
                Varyings OUT;

                // Push vertices out along their normals in clip space
                // This keeps outline width consistent regardless of distance
                float4 clipPos    = TransformObjectToHClip(IN.positionOS.xyz);
                float3 normalCS   = TransformObjectToHClip(IN.normalOS).xyz;
                float2 offset     = normalize(normalCS.xy);

                // Scale outline by clip space W to keep it screen-space consistent
                clipPos.xy       += offset * _OutlineWidth * 0.01 * clipPos.w;

                OUT.positionHCS   = clipPos;
                return OUT;
            }

            half4 fragOutline(Varyings IN) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }

        // Pass 3 - Shadow caster so objects cast correct shadows
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
    }
}