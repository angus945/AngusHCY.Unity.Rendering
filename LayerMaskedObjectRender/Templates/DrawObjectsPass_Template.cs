using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

// https://docs.unity3d.com/Manual/urp/render-graph-draw-objects-in-a-pass.html
class DrawObjectsPass_Template : ScriptableRenderPass
{
    private Material materialToUse;
    public int layerMask = -1; // Default to all layers

    public DrawObjectsPass_Template(Material overrideMaterial, int layerMask = -1)
    {
        // Set the pass's local copy of the override material 
        materialToUse = overrideMaterial;
        this.layerMask = layerMask;
    }

    class ObjectPassData
    {
        // Create a field to store the list of objects to draw
        public RendererListHandle rendererListHandle;
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameContext)
    {
        using (var builder = renderGraph.AddRasterRenderPass<ObjectPassData>("Redraw objects", out ObjectPassData passData))
        {
            // Get the data needed to create the list of objects to draw
            UniversalRenderingData renderingData = frameContext.Get<UniversalRenderingData>();
            UniversalCameraData cameraData = frameContext.Get<UniversalCameraData>();
            UniversalLightData lightData = frameContext.Get<UniversalLightData>();

            // Redraw only objects that have their LightMode tag set to UniversalForward 
            ShaderTagId shadersToOverride = new ShaderTagId("UniversalForward");

            // Create drawing settings
            SortingCriteria sortFlags = cameraData.defaultOpaqueSortFlags;
            DrawingSettings drawSettings = RenderingUtils.CreateDrawingSettings(shadersToOverride, renderingData, cameraData, lightData, sortFlags);

            // Add the override material to the drawing settings
            drawSettings.overrideMaterial = materialToUse;

            // Create the list of objects to draw
            RenderQueueRange renderQueueRange = RenderQueueRange.opaque;
            FilteringSettings filterSettings = new FilteringSettings(renderQueueRange, layerMask);
            RendererListParams rendererListParameters = new RendererListParams(renderingData.cullResults, drawSettings, filterSettings);

            // Convert the list to a list handle that the render graph system can use
            passData.rendererListHandle = renderGraph.CreateRendererList(rendererListParameters);

            // Set the render target as the color and depth textures of the active camera texture
            UniversalResourceData resourceData = frameContext.Get<UniversalResourceData>();
            builder.UseRendererList(passData.rendererListHandle);
            builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
            builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.Write);

            builder.SetRenderFunc(static (ObjectPassData data, RasterGraphContext context) => ExecutePass(data, context));
        }
    }

    static void ExecutePass(ObjectPassData data, RasterGraphContext context)
    {
        // Clear the render target to clear, clear color only
        context.cmd.ClearRenderTarget(false, true, Color.clear);

        // Draw the objects in the list
        context.cmd.DrawRendererList(data.rendererListHandle);
    }

}
