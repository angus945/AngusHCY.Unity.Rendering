using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using AngusHCY.Unity.Rendering.PostProcessing.Contract;

namespace AngusHCY.Unity.Rendering.PostProcessing
{
    public class PostProcessFeature : ScriptableRendererFeature
    {
        [SerializeField] bool logDetectedComponents = true;
        List<AngusHCYPostProcessPass> createdPasses = new List<AngusHCYPostProcessPass>();
        [SerializeField] List<Material> cachedMaterials = new List<Material>();

        public override void Create()
        {
            VolumeStack stack = VolumeManager.instance.stack;
            createdPasses.Clear();
            cachedMaterials.Clear();

            Type[] allComponents = PostProcessComponentRegistry.GetAllComponents();
            string debugMsg = string.Empty;
            foreach (Type type in allComponents)
            {
                PostProcessComponentBase component = stack.GetComponent(type) as PostProcessComponentBase;
                if (component != null)
                {
                    string shaderPath = component.shaderPath;
                    Shader shader = Shader.Find(shaderPath);
                    if (shader == null)
                    {
                        Debug.LogError($"Shader not found at path: {shaderPath}, required by PostProcessComponent {type.Name}.");
                        continue;
                    }
                    AlwaysIncludedShaderUtility.AddShaderToAlwaysIncluded(shader);
                    Material material = new Material(shader);
                    RenderPassEvent eventPoint = component.injectionPoint;
                    ScriptableRenderPassInput requiredInputs = component.requiredInputs;
                    AngusHCYPostProcessPass pass = new AngusHCYPostProcessPass(type, eventPoint, requiredInputs, material);

                    cachedMaterials.Add(material);
                    createdPasses.Add(pass);
                    debugMsg += $"- {type.Name}, Shader: {shaderPath}, Event: {eventPoint}\n";
                }
            }

            debugMsg = $"Create Angus HCY PostProcess Feature, found: {allComponents.Length}, created: {createdPasses.Count}\n" + debugMsg;
            if (logDetectedComponents)
                Debug.Log(debugMsg);

        }

        List<(int order, AngusHCYPostProcessPass pass)> activePasses = new List<(int, AngusHCYPostProcessPass)>();
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            activePasses.Clear();

            VolumeStack stack = VolumeManager.instance.stack;

            foreach (var pass in createdPasses)
            {
                var component = stack.GetComponent(pass.componentType) as PostProcessComponentBase;
                if (pass.IsValid() && component != null && component.IsActive() && component.active)
                {
                    int order = component.custom_order.value;
                    activePasses.Add((order, pass));
                }
            }

            // 依 custom_order 排序
            activePasses.Sort((a, b) => a.order.CompareTo(b.order));

            // 依序 enqueue
            foreach (var item in activePasses)
            {
                renderer.EnqueuePass(item.pass);
            }
        }

    }
}