using System;
using AngusHCY.Unity.Rendering.PostProcessing.Contract;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace AngusHCY.Unity.Rendering.PostProcessing
{
    public class AngusHCYPostProcessPass : ScriptableRenderPass
    {
        public Type componentType { get; private set; }
        Material material;

        new string passName => componentType.Name;

        public AngusHCYPostProcessPass(Type type, RenderPassEvent renderPassEvent, ScriptableRenderPassInput requiredInputs, Material material)
        {
            this.componentType = type;
            this.renderPassEvent = renderPassEvent;
            this.material = material;

            requiresIntermediateTexture = true;

            ConfigureInput(requiredInputs);
        }
        public bool IsValid()
        {
            return material != null;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            VolumeStack stack = VolumeManager.instance.stack;
            PostProcessComponentBase component = stack.GetComponent(componentType) as PostProcessComponentBase;

            if (component.IsActive())
            {
                component.SetupMaterialProperties(material);
            }

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            if (resourceData.isActiveTargetBackBuffer)
            {
                Debug.LogError($"Skipping render pass. Target is backbuffer.");
                return;
            }

            TextureHandle source = resourceData.activeColorTexture;
            TextureDesc destinationDesc = renderGraph.GetTextureDesc(source);
            destinationDesc.name = passName;
            destinationDesc.clearBuffer = false;

            TextureHandle destination = renderGraph.CreateTexture(destinationDesc);

            if (component.IsActive())
            {
                RenderGraphUtils.BlitMaterialParameters para = new(source, destination, material, 0);
                renderGraph.AddBlitPass(para, passName: passName);
            }

            resourceData.cameraColor = destination;
        }

    }

}