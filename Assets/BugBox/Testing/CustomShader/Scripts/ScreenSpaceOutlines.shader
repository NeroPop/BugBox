Shader "Custom/ScreenSpaceOutlines"
{
    Properties
    {
        _OutlineColor       ("Outline Color",           Color)        = (0.1, 0.08, 0.08, 1)
        _OutlineThickness   ("Outline Thickness",       Range(0, 5))  = 1.0
        _DepthThreshold     ("Depth Threshold",         Range(0, 1))  = 0.01
        _NormalThreshold    ("Normal Threshold",        Range(0, 1))  = 0.3
        _DepthFadeStart     ("Depth Fade Start",        Range(0, 200)) = 10.0
        _DepthFadeEnd       ("Depth Fade End",          Range(0, 200)) = 40.0
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline" 
        }

        Pass
        {
            Name "ScreenSpaceOutlines"
            ZWrite Off
            ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float  _OutlineThickness;
                float  _DepthThreshold;
                float  _NormalThreshold;
                float  _DepthFadeStart;
                float  _DepthFadeEnd;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv          = IN.uv;
                return OUT;
            }

            // Sample depth and reconstruct linear depth
            float SampleLinearDepth(float2 uv)
            {
                float rawDepth = SampleSceneDepth(uv);
                return LinearEyeDepth(rawDepth, _ZBufferParams);
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv       = IN.uv;
                float2 texelSize = float2(
                    _OutlineThickness / _ScreenParams.x,
                    _OutlineThickness / _ScreenParams.y
                );

                // Sample depth at centre and 4 neighbours
                float depthC  = SampleLinearDepth(uv);
                float depthN  = SampleLinearDepth(uv + float2( 0,  1) * texelSize);
                float depthS  = SampleLinearDepth(uv + float2( 0, -1) * texelSize);
                float depthE  = SampleLinearDepth(uv + float2( 1,  0) * texelSize);
                float depthW  = SampleLinearDepth(uv + float2(-1,  0) * texelSize);

                // Roberts cross for depth edge detection
                float depthEdge = sqrt(
                    pow(depthC - depthN, 2) +
                    pow(depthC - depthS, 2) +
                    pow(depthC - depthE, 2) +
                    pow(depthC - depthW, 2)
                );

                // Sample normals at centre and neighbours
                float3 normalC = SampleSceneNormals(uv);
                float3 normalN = SampleSceneNormals(uv + float2( 0,  1) * texelSize);
                float3 normalS = SampleSceneNormals(uv + float2( 0, -1) * texelSize);
                float3 normalE = SampleSceneNormals(uv + float2( 1,  0) * texelSize);
                float3 normalW = SampleSceneNormals(uv + float2(-1,  0) * texelSize);

                // Normal edge detection
                float normalEdge = 0;
                normalEdge += distance(normalC, normalN);
                normalEdge += distance(normalC, normalS);
                normalEdge += distance(normalC, normalE);
                normalEdge += distance(normalC, normalW);

                // Combine depth and normal edges
                float edge = 0;
                edge += step(_DepthThreshold,  depthEdge);
                edge += step(_NormalThreshold, normalEdge);
                edge  = saturate(edge);

                // Fade outline with distance
                float fade = 1.0 - smoothstep(_DepthFadeStart, _DepthFadeEnd, depthC);
                edge      *= fade;

                return half4(_OutlineColor.rgb, edge * _OutlineColor.a);
            }
            ENDHLSL
        }
    }
}