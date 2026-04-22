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
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    }

    public Settings settings = new Settings();
    ScreenSpaceOutlinePass m_Pass;

    public override void Create()
    {
        m_Pass = new ScreenSpaceOutlinePass(settings);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.outlineMaterial == null) return;
        renderer.EnqueuePass(m_Pass);
    }

    class ScreenSpaceOutlinePass : ScriptableRenderPass
    {
        Settings m_Settings;

        class PassData
        {
            public Material material;
            public TextureHandle source;
            public TextureHandle depth;
            public TextureHandle normals;
            public TextureHandle destination;
        }

        public ScreenSpaceOutlinePass(Settings settings)
        {
            m_Settings = settings;
            renderPassEvent = settings.renderPassEvent;
            ConfigureInput(ScriptableRenderPassInput.Normal |
                           ScriptableRenderPassInput.Depth |
                           ScriptableRenderPassInput.Color);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (m_Settings.outlineMaterial == null) return;

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            if (resourceData.isActiveTargetBackBuffer) return;
            if (!resourceData.cameraNormalsTexture.IsValid()) return;

            RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;
            desc.msaaSamples = 1;

            TextureHandle source = resourceData.activeColorTexture;
            TextureHandle destination = UniversalRenderer.CreateRenderGraphTexture(
                renderGraph, desc, "_OutlineTemp", false, FilterMode.Bilinear);

            // Pass 1 — outline shader into temp
            using (var builder = renderGraph.AddRasterRenderPass<PassData>(
                "ScreenSpaceOutlines_Draw", out var passData))
            {
                passData.material = m_Settings.outlineMaterial;
                passData.source = source;
                passData.depth = resourceData.activeDepthTexture;
                passData.normals = resourceData.cameraNormalsTexture;
                passData.destination = destination;

                builder.UseTexture(source, AccessFlags.Read);
                builder.UseTexture(passData.depth, AccessFlags.Read);
                builder.UseTexture(passData.normals, AccessFlags.Read);
                builder.SetRenderAttachment(destination, 0, AccessFlags.Write);
                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    context.cmd.SetGlobalTexture("_CameraDepthTexture", data.depth);
                    context.cmd.SetGlobalTexture("_CameraNormalsTexture", data.normals);

                    Blitter.BlitTexture(context.cmd, data.source,
                        new Vector4(1, 1, 0, 0), data.material, 0);
                });
            }

            // Pass 2 — copy temp back to camera target
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