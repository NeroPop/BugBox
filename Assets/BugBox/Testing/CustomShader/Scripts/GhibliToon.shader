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
        // Adds a soft halo around edges facing away from the light
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

            // Shadow variants — determines which shadow mode is compiled
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            // Additional lights — point lights, spot lights etc
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            // Fog — scene fog blending
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            // -------------------------------------------------------
            // Vertex input — data read directly from the mesh
            // -------------------------------------------------------
            struct Attributes
            {
                float4 positionOS : POSITION;  // Object space position
                float3 normalOS   : NORMAL;    // Object space normal
                float2 uv         : TEXCOORD0; // Texture coordinates
            };

            // -------------------------------------------------------
            // Varyings — data interpolated across the triangle
            // and passed from vertex shader to fragment shader
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

            // Constant buffer — all material properties declared here
            // so the GPU can batch them efficiently
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;    // Texture tiling and offset
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

                // GetVertexPositionInputs provides positions in all
                // spaces (object, world, clip) in one call
                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                // GetVertexNormalInputs handles the inverse transpose
                // needed for correct normal transformation
                VertexNormalInputs   norInputs = GetVertexNormalInputs(IN.normalOS);

                OUT.positionHCS = posInputs.positionCS;
                OUT.positionWS  = posInputs.positionWS;
                OUT.normalWS    = norInputs.normalWS;
                OUT.viewDirWS   = GetWorldSpaceViewDir(posInputs.positionWS);
                // TRANSFORM_TEX applies the tiling and offset set in the Inspector
                OUT.uv          = TRANSFORM_TEX(IN.uv, _MainTex);
                // GetShadowCoord selects the correct shadow cascade
                OUT.shadowCoord = GetShadowCoord(posInputs);
                OUT.fogFactor   = ComputeFogFactor(posInputs.positionCS.z);

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Normalise interpolated vectors — they can drift from
                // unit length during rasterisation across a triangle
                float3 normal  = normalize(IN.normalWS);
                float3 viewDir = normalize(IN.viewDirWS);

                // -----------------------------------------------
                // Base colour — painted texture tinted by BaseColor
                // -----------------------------------------------
                half4 texColor  = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                half4 baseColor = texColor * _BaseColor;

                // -----------------------------------------------
                // Main light — fetch direction, colour and shadow
                // -----------------------------------------------
                Light  mainLight = GetMainLight(IN.shadowCoord);
                float3 lightDir  = normalize(mainLight.direction);

                // NdotL remapped 0-1 then multiplied by shadow attenuation
                // Remapping means back faces sit at 0 rather than going negative
                // which avoids shadow artefacts on the dark side of objects
                float NdotL    = dot(normal, lightDir);
                float shadow   = mainLight.shadowAttenuation;
                float lightVal = NdotL * shadow * 0.5 + 0.5;

                // -----------------------------------------------
                // Toon step — smoothstep creates a soft painted edge
                // ShadowFeather controls how wide the transition band is
                // ShadowStep controls where the transition sits
                // -----------------------------------------------
                float toon = smoothstep(
                    _ShadowStep - _ShadowFeather,
                    _ShadowStep + _ShadowFeather,
                    lightVal
                );

                // Lerp between shadow and lit colour using the toon value
                // Shadow colour is independent so it can shift warm or cool
                half3 litColor    = baseColor.rgb * mainLight.color.rgb;
                half3 shadowColor = _ShadowColor.rgb * baseColor.rgb;
                half3 diffuse     = lerp(shadowColor, litColor, toon);

                // -----------------------------------------------
                // Rim light — highlights silhouette edges
                // 1 - NdotV gives maximum value where surface faces away
                // from the camera, which is where the rim appears
                // Multiplying by toon keeps it off the shadow side
                // -----------------------------------------------
                float rim       = 1.0 - saturate(dot(viewDir, normal));
                float rimFactor = pow(rim, _RimPower) * _RimStrength * toon;
                half3 rimColor  = _RimColor.rgb * rimFactor;

                // -----------------------------------------------
                // Final colour — add rim to diffuse then apply fog
                // MixFog blends toward the scene fog colour based
                // on the fogFactor calculated in the vertex shader
                // -----------------------------------------------
                half3 finalColor = MixFog(diffuse + rimColor, IN.fogFactor);

                return half4(finalColor, baseColor.a);
            }
            ENDHLSL
        }

        // ---------------------------------------------------------------
        // Pass 2: Depth Normals
        // Writes world space normals to the _CameraNormalsTexture
        // Required for the screen space outline feature to detect edges
        // Only runs for objects on the Outline layer due to the
        // Prepass Layer Mask setting on the URP Renderer
        // ---------------------------------------------------------------
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

            // Minimal vertex input — only position and normal needed
            struct AttributesDN
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct VaryingsDN
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS    : TEXCOORD0; // World space normal to write to buffer
            };

            // Must match the main pass CBUFFER exactly or Unity
            // will fail to batch the draw calls correctly
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

            VaryingsDN vertDepthNormals(AttributesDN IN)
            {
                VaryingsDN OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                // TransformObjectToWorldNormal handles the inverse transpose
                // so non-uniform scaling doesn't distort the normals
                OUT.normalWS    = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            // Pack normal into 0-1 range for storage in the RGBA texture
            // The outline shader unpacks by multiplying by 2 and subtracting 1
            float4 fragDepthNormals(VaryingsDN IN) : SV_Target
            {
                return float4(normalize(IN.normalWS) * 0.5 + 0.5, 1);
            }
            ENDHLSL
        }

        // ---------------------------------------------------------------
        // Pass 3: Shadow Caster
        // Allows this object to cast shadows onto other objects
        // Reuses URP's built-in shadow caster pass directly
        // ---------------------------------------------------------------
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
    }
}