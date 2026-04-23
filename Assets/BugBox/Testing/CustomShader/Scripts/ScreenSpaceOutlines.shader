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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

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

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
                float2 uv        = IN.texcoord;
                float2 texelSize = float2(
                    _OutlineThickness / _ScreenParams.x,
                    _OutlineThickness / _ScreenParams.y
                );

                // Sample scene colour
                half3 sceneColor = SAMPLE_TEXTURE2D_X_LOD(
                    _BlitTexture, sampler_LinearClamp, uv, 0).rgb;

                // Depth edges
                float depthC = SampleLinearDepth(uv);
                float depthN = SampleLinearDepth(uv + float2( 0,  1) * texelSize);
                float depthS = SampleLinearDepth(uv + float2( 0, -1) * texelSize);
                float depthE = SampleLinearDepth(uv + float2( 1,  0) * texelSize);
                float depthW = SampleLinearDepth(uv + float2(-1,  0) * texelSize);

                float depthEdge = abs(depthC - depthN)
                                + abs(depthC - depthS)
                                + abs(depthC - depthE)
                                + abs(depthC - depthW);

                // Normal edges
                float3 normalC = SampleSceneNormals(uv);
                float3 normalN = SampleSceneNormals(uv + float2( 0,  1) * texelSize);
                float3 normalS = SampleSceneNormals(uv + float2( 0, -1) * texelSize);
                float3 normalE = SampleSceneNormals(uv + float2( 1,  0) * texelSize);
                float3 normalW = SampleSceneNormals(uv + float2(-1,  0) * texelSize);

                float normalEdge = distance(normalC, normalN)
                                 + distance(normalC, normalS)
                                 + distance(normalC, normalE)
                                 + distance(normalC, normalW);

                float edge = step(_DepthThreshold,  depthEdge)
                           + step(_NormalThreshold, normalEdge);
                edge       = saturate(edge);

                float fade = 1.0 - smoothstep(_DepthFadeStart, _DepthFadeEnd, depthC);
                edge      *= fade;

                half3 final = lerp(sceneColor, _OutlineColor.rgb, edge);
                return half4(final, 1);
            }
            ENDHLSL
        }
    }
}