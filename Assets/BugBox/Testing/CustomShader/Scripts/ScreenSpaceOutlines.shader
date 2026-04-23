Shader "Custom/ScreenSpaceOutlines"
{
    Properties
    {
        _OutlineColor     ("Outline Color",     Color)         = (0.1, 0.08, 0.08, 1)
        _OutlineThickness ("Outline Thickness", Range(1, 10))  = 1.0
        _DepthThreshold   ("Depth Threshold",   Range(0, 5))   = 1.0
        _NormalThreshold  ("Normal Threshold",  Range(0, 5))   = 0.3
        _DepthFadeStart   ("Depth Fade Start",  Range(0, 200)) = 10.0
        _DepthFadeEnd     ("Depth Fade End",    Range(0, 200)) = 40.0
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "ScreenSpaceOutlines"
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            // Our dedicated normals texture containing only outlined objects
            // Non-outlined objects will have zero normals here
            TEXTURE2D_X(_OutlineNormalsTexture);

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float  _OutlineThickness;
                float  _DepthThreshold;
                float  _NormalThreshold;
                float  _DepthFadeStart;
                float  _DepthFadeEnd;
            CBUFFER_END

            float SampleLinearDepth(float2 uv)
            {
                float rawDepth = SampleSceneDepth(uv);
                return LinearEyeDepth(rawDepth, _ZBufferParams);
            }

            // Sample from our dedicated outline normals texture
            // rather than the scene normals which include all objects
            float3 SampleOutlineNormals(float2 uv)
            {
                return SAMPLE_TEXTURE2D_X(
                    _OutlineNormalsTexture, sampler_LinearClamp, uv).rgb;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
                float2 uv        = IN.texcoord;
                float2 texelSize = float2(
                    _OutlineThickness / _ScreenParams.x,
                    _OutlineThickness / _ScreenParams.y
                );

                // Sample scene colour from the blit source texture
                half3 sceneColor = SAMPLE_TEXTURE2D_X_LOD(
                    _BlitTexture, sampler_LinearClamp, uv, 0).rgb;

                // If centre pixel has no normals it is sky or a non-outlined object
                // Return scene colour unchanged — no outline should appear here
                float3 normalC = SampleOutlineNormals(uv);
                if (dot(normalC, normalC) < 0.01)
                    return half4(sceneColor, 1);

                // Sample depth and normals at centre and four neighbours
                float depthC = SampleLinearDepth(uv);
                float depthN = SampleLinearDepth(uv + float2( 0,  1) * texelSize);
                float depthS = SampleLinearDepth(uv + float2( 0, -1) * texelSize);
                float depthE = SampleLinearDepth(uv + float2( 1,  0) * texelSize);
                float depthW = SampleLinearDepth(uv + float2(-1,  0) * texelSize);

                float3 normalN = SampleOutlineNormals(uv + float2( 0,  1) * texelSize);
                float3 normalS = SampleOutlineNormals(uv + float2( 0, -1) * texelSize);
                float3 normalE = SampleOutlineNormals(uv + float2( 1,  0) * texelSize);
                float3 normalW = SampleOutlineNormals(uv + float2(-1,  0) * texelSize);

                // If a neighbour has no normals it is a non-outlined object or sky
                // Clamp its normal to centre normal to avoid false edges at boundaries
                // Only clamp depth if the neighbour is in front — if it is behind
                // it is sky or background and we keep the depth for silhouette edges
                bool noNormalN = dot(normalN, normalN) < 0.01;
                bool noNormalS = dot(normalS, normalS) < 0.01;
                bool noNormalE = dot(normalE, normalE) < 0.01;
                bool noNormalW = dot(normalW, normalW) < 0.01;

                normalN = noNormalN ? normalC : normalN;
                normalS = noNormalS ? normalC : normalS;
                normalE = noNormalE ? normalC : normalE;
                normalW = noNormalW ? normalC : normalW;

                depthN = (noNormalN && depthN < depthC) ? depthC : depthN;
                depthS = (noNormalS && depthS < depthC) ? depthC : depthS;
                depthE = (noNormalE && depthE < depthC) ? depthC : depthE;
                depthW = (noNormalW && depthW < depthC) ? depthC : depthW;

                // Depth edge detection — large differences indicate silhouette edges
                float depthEdge = abs(depthC - depthN)
                                + abs(depthC - depthS)
                                + abs(depthC - depthE)
                                + abs(depthC - depthW);

                // Normal edge detection — catches surface angle changes
                // such as hard edges on curved surfaces
                float normalEdge = distance(normalC, normalN)
                                 + distance(normalC, normalS)
                                 + distance(normalC, normalE)
                                 + distance(normalC, normalW);

                // Combine depth and normal edges into a single 0-1 value
                float edge = step(_DepthThreshold,  depthEdge)
                           + step(_NormalThreshold, normalEdge);
                edge       = saturate(edge);

                // Fade outlines with distance
                float fade = 1.0 - smoothstep(_DepthFadeStart, _DepthFadeEnd, depthC);
                edge      *= fade;

                // Composite outline colour over scene colour
                half3 final = lerp(sceneColor, _OutlineColor.rgb, edge);
                return half4(final, 1);
            }
            ENDHLSL
        }
    }
}