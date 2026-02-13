using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal.Internal;
using UnityEngine.Experimental.Rendering;

// https://discussions.unity.com/t/how-to-blit-depth-into-rendertexture/817369/2
// https://docs.unity3d.com/Manual/urp/render-graph-draw-objects-in-a-pass.html
// Library\PackageCache\com.unity.render-pipelines.universal@002ea2b4fc19\Runtime\Passes
// Library\PackageCache\com.unity.render-pipelines.universal@002ea2b4fc19\Runtime\Passes\PostProcessPassRenderGraph.cs
// Library\PackageCache\com.unity.render-pipelines.universal@002ea2b4fc19\Runtime\Passes\FinalBlitPass.cs
// Library\PackageCache\com.unity.render-pipelines.universal@002ea2b4fc19\Runtime\Passes\CopyDepthPass.cs
// PostProcessPass
// FinalBlitPass 
// CopyDepthPass
class LayerMaskedObjectRenderPass : ScriptableRenderPass
{
    Material overrideMaterial;
    Material blitMaterial;
    int layerMask;
    Mesh fullscreenMesh;

    public LayerMaskedObjectRenderPass(Material overrideMaterial, Material blitMaterial, int layerMask = -1)
    {
        this.overrideMaterial = overrideMaterial;
        this.blitMaterial = blitMaterial;
        this.layerMask = layerMask;
#pragma warning disable CS0618
        fullscreenMesh = RenderingUtils.fullscreenMesh;
#pragma warning restore CS0618
    }
    private class RenderObjectPassData
    {
        public TextureHandle source;
        public RendererListHandle rendererListHandle;
    }
    private class BlitPassData
    {
        public TextureHandle sourceColor;
        public TextureHandle sourceDepth;
        public TextureHandle screenColor;
        public TextureHandle screenDepth;
        public Material blitMaterial;
        public Mesh fullscreenMesh;
        public UniversalCameraData cameraData;
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameContext)
    {
        TextureHandle sharedColorTex = CreateSharedColorTexture(renderGraph, frameContext.Get<UniversalCameraData>());
        TextureHandle sharedDepthTex = CreateSharedDepthTexture(renderGraph, frameContext.Get<UniversalCameraData>());

        RecordRasterPass(renderGraph, frameContext, sharedColorTex, sharedDepthTex);
        RecordRasterBlitPass(renderGraph, frameContext, sharedColorTex, sharedDepthTex);
    }
    TextureHandle CreateSharedColorTexture(RenderGraph renderGraph, UniversalCameraData cameraData)
    {
        RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
        TextureHandle sharedColorTex = renderGraph.CreateTexture(
        new TextureDesc(desc.width, desc.height)
        {
            colorFormat = desc.graphicsFormat,
            depthBufferBits = DepthBits.None,
            clearColor = Color.clear,
            clearBuffer = true,
            name = "SharedMaskedObjectColorRT"
        });
        return sharedColorTex;
    }
    TextureHandle CreateSharedDepthTexture(RenderGraph renderGraph, UniversalCameraData cameraData)
    {
        RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
        TextureHandle sharedDepthTex = renderGraph.CreateTexture(
        new TextureDesc(desc.width, desc.height)
        {
            colorFormat = desc.graphicsFormat,
            depthBufferBits = DepthBits.Depth32,
            clearColor = Color.clear,
            clearBuffer = true,
            name = "SharedMaskedObjectDepthRT"
        });
        return sharedDepthTex;
    }

    void RecordRasterPass(RenderGraph renderGraph, ContextContainer frameContext, TextureHandle sharedColorTex, TextureHandle sharedDepthTex)
    {
        using (IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass<RenderObjectPassData>("LayerMaskPostProcessingPass - RenderObject", out RenderObjectPassData passData))
        {
            // Get necessary data
            UniversalRenderingData renderingData = frameContext.Get<UniversalRenderingData>();
            UniversalCameraData cameraData = frameContext.Get<UniversalCameraData>();
            UniversalLightData lightData = frameContext.Get<UniversalLightData>();

            // Redraw only objects that have their LightMode tag set to UniversalForward 
            ShaderTagId shadersToOverride = new ShaderTagId("UniversalForward");

            // Create drawing settings
            SortingCriteria sortFlags = cameraData.defaultOpaqueSortFlags;
            DrawingSettings drawSettings = RenderingUtils.CreateDrawingSettings(shadersToOverride, renderingData, cameraData, lightData, sortFlags);
            drawSettings.overrideMaterial = overrideMaterial ?? null;

            // Create the list of objects to draw
            RenderQueueRange renderQueueRange = RenderQueueRange.opaque;
            FilteringSettings filterSettings = new FilteringSettings(renderQueueRange, layerMask);
            RendererListParams rendererListParameters = new RendererListParams(renderingData.cullResults, drawSettings, filterSettings);

            // Convert the list to a list handle that the render graph system can use
            passData.rendererListHandle = renderGraph.CreateRendererList(rendererListParameters);
            passData.source = sharedColorTex;

            // Set the render target as the color and depth textures of the active camera texture
            UniversalResourceData resourceData = frameContext.Get<UniversalResourceData>();
            builder.UseRendererList(passData.rendererListHandle);
            builder.SetRenderAttachment(sharedColorTex, 0, AccessFlags.ReadWrite);
            builder.SetRenderAttachmentDepth(sharedDepthTex, AccessFlags.ReadWrite);

            builder.SetRenderFunc(static (RenderObjectPassData data, RasterGraphContext context) => ExecuteRasterPass(data, context));
        }
    }
    static void ExecuteRasterPass(RenderObjectPassData data, RasterGraphContext context)
    {
        // Clear the render target to black
        context.cmd.ClearRenderTarget(true, true, Color.clear);

        // Draw the objects in the list
        context.cmd.DrawRendererList(data.rendererListHandle);
    }

    void RecordRasterBlitPass(RenderGraph renderGraph, ContextContainer frameContext, TextureHandle sharedColorTex, TextureHandle sharedDepthTex)
    {
        using (IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass<BlitPassData>("LayerMaskPostProcessingPass - Blit", out BlitPassData passData))
        {
            // Get necessary data
            UniversalCameraData cameraData = frameContext.Get<UniversalCameraData>();
            UniversalResourceData resourceData = frameContext.Get<UniversalResourceData>();

            passData.sourceColor = sharedColorTex;
            passData.sourceDepth = sharedDepthTex;
            passData.screenColor = resourceData.activeColorTexture;
            passData.screenDepth = resourceData.activeDepthTexture;
            passData.blitMaterial = blitMaterial;
            passData.fullscreenMesh = fullscreenMesh;
            passData.cameraData = cameraData;

            // 用 UseTexture 告訴 RenderGraph 這個 Pass 還會用到 Texture
            builder.UseTexture(passData.sourceColor, AccessFlags.Read);
            builder.UseTexture(passData.sourceDepth, AccessFlags.Read);

            // 設定 render target：Color + Depth
            builder.SetRenderAttachment(passData.screenColor, 0, AccessFlags.ReadWrite);
            builder.SetRenderAttachmentDepth(passData.screenDepth, AccessFlags.ReadWrite);
            builder.SetRenderFunc(static (BlitPassData data, RasterGraphContext ctx) => ExecuteBlitPass(data, ctx));
        }

    }
    static void ExecuteBlitPass(BlitPassData data, RasterGraphContext ctx)
    {
        if (data.blitMaterial == null)
        {
            Debug.LogError("DepthTestFullscreenPass: Material is null, skipping pass.");
            return;
        }

        Mesh fullscreenMesh = data.fullscreenMesh;

        Material blitMaterial = data.blitMaterial;
        blitMaterial.SetTexture("_SourceColor", data.sourceColor);
        blitMaterial.SetTexture("_SourceDepth", data.sourceDepth);
        blitMaterial.SetTexture("_ScreenColor", data.screenColor);
        blitMaterial.SetTexture("_ScreenDepth", data.screenDepth);

        UniversalCameraData cameraData = data.cameraData;
        Camera camera = cameraData.camera;

        ctx.cmd.SetViewport(camera.pixelRect);
        ctx.cmd.DrawMesh(fullscreenMesh, Matrix4x4.identity, blitMaterial, 0, 0);
        //TODO 可能要用自製的 sv depth?
    }
}

