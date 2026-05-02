using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

public class ScreenSpaceOutlineFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public Material outlineMaterial;
        public LayerMask outlineLayers = 0;
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    }

    public Settings settings = new Settings();

    ScreenSpaceOutlineNormalsPass m_NormalsPass;
    ScreenSpaceOutlinePass m_OutlinePass;

    public override void Create()
    {
        m_NormalsPass = new ScreenSpaceOutlineNormalsPass(settings);
        m_OutlinePass = new ScreenSpaceOutlinePass(settings);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.outlineMaterial == null) return;
        renderer.EnqueuePass(m_NormalsPass);
        renderer.EnqueuePass(m_OutlinePass);
    }

    // ---------------------------------------------------------------
    // Pass 1: Renders outlined objects normals into a dedicated texture
    // ---------------------------------------------------------------
    class ScreenSpaceOutlineNormalsPass : ScriptableRenderPass
    {
        Settings m_Settings;

        static readonly int s_OutlineNormalsId =
            Shader.PropertyToID("_OutlineNormalsTexture");

        // Shared handle so the outline pass can access the normals texture
        public static TextureHandle s_NormalsTexture;

        class NormalsPassData
        {
            public RendererListHandle rendererList;
        }

        class BindPassData
        {
            public TextureHandle normalsTexture;
        }

        public ScreenSpaceOutlineNormalsPass(Settings settings)
        {
            m_Settings = settings;
            renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

            RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;
            desc.msaaSamples = 1;
            desc.colorFormat = RenderTextureFormat.ARGB32;

            s_NormalsTexture = UniversalRenderer.CreateRenderGraphTexture(
                renderGraph, desc, "_OutlineNormalsTexture", true, FilterMode.Bilinear);

            var shaderTagIds = new System.Collections.Generic.List<ShaderTagId>
            {
                new ShaderTagId("DepthNormals"),    // Custom GhibliToon pass
                new ShaderTagId("DepthNormalOnly"), // URP built-in Lit pass tag
            };

            var drawSettings = RenderingUtils.CreateDrawingSettings(
                shaderTagIds,
                renderingData, cameraData, lightData,
                SortingCriteria.CommonOpaque);

            var filterSettings = new FilteringSettings(
                RenderQueueRange.opaque, m_Settings.outlineLayers);

            RendererListParams listParams = new RendererListParams(
                renderingData.cullResults, drawSettings, filterSettings);

            TextureHandle depthHandle = resourceData.activeDepthTexture;

            // Sub-pass A: render normals
            using (var builder = renderGraph.AddRasterRenderPass<NormalsPassData>(
                "ScreenSpaceOutlines_Normals", out var passData))
            {
                passData.rendererList = renderGraph.CreateRendererList(listParams);

                builder.SetRenderAttachment(s_NormalsTexture, 0, AccessFlags.Write);
                builder.SetRenderAttachmentDepth(depthHandle, AccessFlags.ReadWrite);
                builder.UseRendererList(passData.rendererList);
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((NormalsPassData data, RasterGraphContext context) =>
                {
                    context.cmd.ClearRenderTarget(false, true, Color.black);
                    context.cmd.DrawRendererList(data.rendererList);
                });
            }

            // Sub-pass B: bind normals texture as global AFTER it has been written
            using (var builder = renderGraph.AddRasterRenderPass<BindPassData>(
                "ScreenSpaceOutlines_BindNormals", out var passData))
            {
                passData.normalsTexture = s_NormalsTexture;

                // Declare as read so RenderGraph knows this pass depends on the normals pass
                builder.UseTexture(s_NormalsTexture, AccessFlags.Read);
                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);

                // Need a dummy render attachment to make this a valid raster pass
                builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.Read);

                builder.SetRenderFunc((BindPassData data, RasterGraphContext context) =>
                {
                    context.cmd.SetGlobalTexture(s_OutlineNormalsId, data.normalsTexture);
                });
            }
        }
    }

    // ---------------------------------------------------------------
    // Pass 2: Edge detection and outline compositing
    // ---------------------------------------------------------------
    class ScreenSpaceOutlinePass : ScriptableRenderPass
    {
        Settings m_Settings;

        class PassData
        {
            public Material material;
            public TextureHandle source;
            public TextureHandle destination;
            public TextureHandle depth;
        }

        public ScreenSpaceOutlinePass(Settings settings)
        {
            m_Settings = settings;
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
            ConfigureInput(ScriptableRenderPassInput.Depth |
                           ScriptableRenderPassInput.Color);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (m_Settings.outlineMaterial == null) return;

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            if (resourceData.isActiveTargetBackBuffer) return;

            RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;
            desc.msaaSamples = 1;

            TextureHandle source = resourceData.activeColorTexture;
            TextureHandle depth = resourceData.activeDepthTexture;
            TextureHandle destination = UniversalRenderer.CreateRenderGraphTexture(
                renderGraph, desc, "_OutlineTemp", false, FilterMode.Bilinear);

            using (var builder = renderGraph.AddRasterRenderPass<PassData>(
                "ScreenSpaceOutlines_Draw", out var passData))
            {
                passData.material = m_Settings.outlineMaterial;
                passData.source = source;
                passData.destination = destination;
                passData.depth = depth;

                builder.UseTexture(source, AccessFlags.Read);
                builder.UseTexture(depth, AccessFlags.Read);

                // Declare the normals texture as read so execution order is correct
                if (ScreenSpaceOutlineNormalsPass.s_NormalsTexture.IsValid())
                    builder.UseTexture(
                        ScreenSpaceOutlineNormalsPass.s_NormalsTexture, AccessFlags.Read);

                builder.SetRenderAttachment(destination, 0, AccessFlags.Write);
                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    context.cmd.SetGlobalTexture("_CameraDepthTexture", data.depth);

                    Blitter.BlitTexture(context.cmd, data.source,
                        new Vector4(1, 1, 0, 0), data.material, 0);
                });
            }

            using (var builder = renderGraph.AddRasterRenderPass<PassData>(
                "ScreenSpaceOutlines_CopyBack", out var passData))
            {
                passData.source = destination;
                passData.destination = source;

                builder.UseTexture(destination, AccessFlags.Read);
                builder.SetRenderAttachment(source, 0, AccessFlags.Write);
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    Blitter.BlitTexture(context.cmd, data.source,
                        new Vector4(1, 1, 0, 0), 0, false);
                });
            }
        }
    }
}