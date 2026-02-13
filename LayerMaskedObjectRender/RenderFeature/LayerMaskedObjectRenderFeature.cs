using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public class LayerMaskedObjectRenderFeature : ScriptableRendererFeature
{
    LayerMaskedObjectRenderPass drawObjectsToRTPass;

    [SerializeField] RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
    [SerializeField] Material overrideMaterial;
    [SerializeField] Material blitMaterial;
    [SerializeField] LayerMask layerMask = ~0;

    public override void Create()
    {
        drawObjectsToRTPass = new LayerMaskedObjectRenderPass(overrideMaterial, blitMaterial, layerMask.value);
        drawObjectsToRTPass.renderPassEvent = renderPassEvent;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(drawObjectsToRTPass);
    }



}
