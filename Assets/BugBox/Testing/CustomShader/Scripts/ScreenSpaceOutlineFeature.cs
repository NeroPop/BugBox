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

        // RenderGraph pass data
        class PassData
        {
            public Material material;
            public TextureHandle source;
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

            TextureHandle source = resourceData.activeColorTexture;

            // Get camera texture descriptor for temp texture
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;

            TextureHandle destination = UniversalRenderer.CreateRenderGraphTexture(
                renderGraph, desc, "_TempOutlineTexture", false);

            using (var builder = renderGraph.AddRasterRenderPass<PassData>(
                "ScreenSpaceOutlines", out var passData))
            {
                passData.material = m_Settings.outlineMaterial;
                passData.source = source;

                builder.UseTexture(source);
                builder.SetRenderAttachment(destination, 0);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    Blitter.BlitTexture(context.cmd, data.source,
                        new Vector4(1, 1, 0, 0), data.material, 0);
                });
            }

            // Copy result back to active color texture
            using (var builder = renderGraph.AddRasterRenderPass<PassData>(
                "ScreenSpaceOutlines_CopyBack", out var passData))
            {
                passData.material = null;
                passData.source = destination;

                builder.UseTexture(destination);
                builder.SetRenderAttachment(source, 0);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    Blitter.BlitTexture(context.cmd, data.source,
                        new Vector4(1, 1, 0, 0), null, 0);
                });
            }
        }
    }
}