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
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
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
            public TextureHandle destination;
        }

        public ScreenSpaceOutlinePass(Settings settings)
        {
            m_Settings = settings;
            renderPassEvent = settings.renderPassEvent;
            ConfigureInput(ScriptableRenderPassInput.Normal | ScriptableRenderPassInput.Depth);
            requiresIntermediateTexture = true;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (m_Settings.outlineMaterial == null) return;

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            if (resourceData.isActiveTargetBackBuffer) return;

            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;

            TextureHandle source = resourceData.activeColorTexture;
            TextureHandle destination = UniversalRenderer.CreateRenderGraphTexture(
                renderGraph, desc, "_TempOutlineTexture", false);

            // Pass 1 - render outlines into temp texture
            using (var builder = renderGraph.AddRasterRenderPass<PassData>(
                "ScreenSpaceOutlines_Draw", out var passData))
            {
                passData.material = m_Settings.outlineMaterial;
                passData.source = source;
                passData.destination = destination;

                builder.UseTexture(source);
                builder.SetRenderAttachment(destination, 0);
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    Blitter.BlitTexture(context.cmd, data.source,
                        new Vector4(1, 1, 0, 0), data.material, 0);
                });
            }

            // Pass 2 - copy temp texture back to camera colour
            using (var builder = renderGraph.AddRasterRenderPass<PassData>(
                "ScreenSpaceOutlines_CopyBack", out var passData))
            {
                passData.source = destination;
                passData.destination = source;

                builder.UseTexture(destination);
                builder.SetRenderAttachment(source, 0);
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    // Use RenderingUtils blit material for the copy - avoids null material error
                    Blitter.BlitTexture(context.cmd, data.source,
                        new Vector4(1, 1, 0, 0), 0, false);
                });
            }
        }
    }
}