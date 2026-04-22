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
    /*
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
            // Blit.hlsl provides Vert, Varyings, _BlitTexture automatically
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

            // Blit.hlsl Varyings has uv as 'texCoord' in older URP
            // and as 'uv' in newer - we use the position to derive UV instead
            half4 frag(Varyings IN) : SV_Target
            {
                // Derive UV from clip position to avoid version differences
                float2 uv = IN.texCoord;

                // Debug: output red to confirm shader is running
                return half4(1, 0, 0, 1);
            }
            ENDHLSL
        }
    }*/
}