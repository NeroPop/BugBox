Shader "Custom/GhibliToon"
{
    Properties
    {
        // ---------------------------------------------------------------
        // Texture & Base Colour
        // ---------------------------------------------------------------
        [Header(Base)]
        _MainTex   ("Painted Texture", 2D)    = "white" {}
        _BaseColor ("Base Color Tint", Color) = (1, 1, 1, 1)

        // ---------------------------------------------------------------
        // Toon Shading
        // Controls the step between lit and shadow areas
        // ---------------------------------------------------------------
        [Header(Toon Shading)]
        _ShadowColor   ("Shadow Color",   Color)         = (0.4, 0.45, 0.65, 1)
        _ShadowStep    ("Shadow Step",    Range(0, 1))   = 0.4
        _ShadowFeather ("Shadow Feather", Range(0, 0.2)) = 0.04

        // ---------------------------------------------------------------
        // Rim Lighting
        // ---------------------------------------------------------------
        [Header(Rim Light)]
        _RimColor    ("Rim Color",    Color)         = (0.8, 0.85, 1.0, 1)
        _RimPower    ("Rim Power",    Range(0.1, 8)) = 2.0
        _RimStrength ("Rim Strength", Range(0, 1))   = 0.3
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue"          = "Geometry"
        }

        // ---------------------------------------------------------------
        // Pass 1: Forward Lit
        // Main shading pass — handles lighting, shadows, rim and fog
        // ---------------------------------------------------------------
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            // Shadow variants
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            // Additional light variants
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            // Fog
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            // -------------------------------------------------------
            // Vertex input from mesh
            // -------------------------------------------------------
            struct Attributes
            {
                float4 positionOS : POSITION;  // Object space position
                float3 normalOS   : NORMAL;    // Object space normal
                float2 uv         : TEXCOORD0; // Texture coordinates
            };

            // -------------------------------------------------------
            // Data passed from vertex to fragment shader
            // -------------------------------------------------------
            struct Varyings
            {
                float4 positionHCS : SV_POSITION; // Clip space position
                float2 uv          : TEXCOORD0;   // Texture UV
                float3 normalWS    : TEXCOORD1;   // World space normal
                float3 positionWS  : TEXCOORD2;   // World space position
                float3 viewDirWS   : TEXCOORD3;   // Direction to camera
                float4 shadowCoord : TEXCOORD4;   // Shadow map coordinates
                float  fogFactor   : TEXCOORD5;   // Fog blend factor
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
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                // URP helper functions for correct space transforms
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
                // Normalise interpolated vectors (they drift during rasterisation)
                float3 normal  = normalize(IN.normalWS);
                float3 viewDir = normalize(IN.viewDirWS);

                // -----------------------------------------------
                // Base colour from painted texture
                // -----------------------------------------------
                half4 texColor  = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                half4 baseColor = texColor * _BaseColor;

                // -----------------------------------------------
                // Main light — direction, colour and shadow
                // -----------------------------------------------
                Light  mainLight = GetMainLight(IN.shadowCoord);
                float3 lightDir  = normalize(mainLight.direction);

                // NdotL remapped to 0-1 and multiplied by shadow attenuation
                // so fully shadowed areas get the same treatment as unlit faces
                float NdotL    = dot(normal, lightDir);
                float shadow   = mainLight.shadowAttenuation;
                float lightVal = NdotL * shadow * 0.5 + 0.5;

                // -----------------------------------------------
                // Toon step — smoothstep gives a soft painted edge
                // instead of a hard binary light/shadow split
                // -----------------------------------------------
                float toon = smoothstep(
                    _ShadowStep - _ShadowFeather,
                    _ShadowStep + _ShadowFeather,
                    lightVal
                );

                // Blend between shadow colour and lit colour based on toon value
                half3 litColor    = baseColor.rgb * mainLight.color.rgb;
                half3 shadowColor = _ShadowColor.rgb * baseColor.rgb;
                half3 diffuse     = lerp(shadowColor, litColor, toon);

                // -----------------------------------------------
                // Rim light — brightens edges facing away from camera
                // Only applied on the lit side (multiplied by toon)
                // -----------------------------------------------
                float rim       = 1.0 - saturate(dot(viewDir, normal));
                float rimFactor = pow(rim, _RimPower) * _RimStrength * toon;
                half3 rimColor  = _RimColor.rgb * rimFactor;

                // -----------------------------------------------
                // Final composite and fog
                // -----------------------------------------------
                half3 finalColor = MixFog(diffuse + rimColor, IN.fogFactor);

                return half4(finalColor, baseColor.a);
            }
            ENDHLSL
        }

        // ---------------------------------------------------------------
        // Pass 2: Shadow Caster
        // Allows this object to cast shadows onto other objects
        // Reuses URP's built-in shadow caster pass
        // ---------------------------------------------------------------
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
    }
}