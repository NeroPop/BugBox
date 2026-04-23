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
    ScreenSpaceOutlinePass m_Pass;

    public override void Create()
    {
        m_Pass = new ScreenSpaceOutlinePass(settings);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.outlineMaterial == null) return;

        // Skip if the camera's culling mask doesn't include any outline layers
        Camera cam = renderingData.cameraData.camera;
        if ((cam.cullingMask & settings.outlineLayers) == 0) return;

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
            public int layerMask;
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

            using (var builder = renderGraph.AddRasterRenderPass<PassData>(
                "ScreenSpaceOutlines_Draw", out var passData))
            {
                passData.material = m_Settings.outlineMaterial;
                passData.source = source;
                passData.depth = resourceData.activeDepthTexture;
                passData.normals = resourceData.cameraNormalsTexture;
                passData.destination = destination;
                passData.layerMask = m_Settings.outlineLayers;

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
                    context.cmd.SetGlobalInt("_OutlineLayerMask", data.layerMask);

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