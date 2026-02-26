using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace AngusHCY.Unity.Rendering.PostProcessing
{
    public class AngusHCYPostProcessFeature : ScriptableRendererFeature
    {
        [SerializeField] bool logDetectedComponents = true;
        List<AngusHCYPostProcessPass> createdPasses = new List<AngusHCYPostProcessPass>();
        [SerializeField] List<Material> instancedMaterials = new List<Material>();

        Dictionary<string, bool> passActiveStatus = new Dictionary<string, bool>();

        public override void Create()
        {
            VolumeStack stack = VolumeManager.instance.stack;
            createdPasses.Clear();
            instancedMaterials.Clear();

            Type[] allComponents = PostProcessComponentRegistry.GetAllComponents();
            string debugMsg = string.Empty;
            foreach (Type type in allComponents)
            {
                PostProcessComponentBase component = stack.GetComponent(type) as PostProcessComponentBase;
                if (component != null)
                {
                    Material loadMaterial = Resources.Load<Material>(component.materialPath);
                    if (loadMaterial == null)
                    {
                        Debug.LogError($"Material not found at path: {component.materialPath}, required by PostProcessComponent {type.Name}.");
                        continue;
                    }

                    Material cloneMaterial = new Material(loadMaterial);
                    RenderPassEvent eventPoint = component.injectionPoint;
                    ScriptableRenderPassInput requiredInputs = component.requiredInputs;
                    AngusHCYPostProcessPass pass = new AngusHCYPostProcessPass(type, eventPoint, requiredInputs, cloneMaterial);

                    instancedMaterials.Add(cloneMaterial);
                    createdPasses.Add(pass);
                    debugMsg += $"- {type.Name}, Material: {component.materialPath}, Event: {eventPoint}\n";
                }
            }

            debugMsg = $"Create Angus HCY PostProcess Feature, found: {allComponents.Length}, created: {createdPasses.Count}\n" + debugMsg;
            if (logDetectedComponents)
            {
                // Debug.Log(debugMsg);
                Debug.LogError(debugMsg);
            }
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            VolumeStack stack = VolumeManager.instance.stack;

            foreach (AngusHCYPostProcessPass pass in createdPasses)
            {
                if (passActiveStatus.TryGetValue(pass.componentType.Name, out bool isActive) && !isActive)
                {
                    continue; // Skip this pass if it's marked as inactive
                }

                PostProcessComponentBase component = stack.GetComponent(pass.componentType) as PostProcessComponentBase;
                if (pass.IsValid() && component != null && component.IsActive() && component.active)
                {
                    // int order = component.custom_order.value;
                    renderer.EnqueuePass(pass);
                }
            }
        }

        public string[] GetCreatedPassNames()
        {
            string[] names = new string[createdPasses.Count];
            for (int i = 0; i < createdPasses.Count; i++)
            {
                names[i] = createdPasses[i].componentType.Name;
            }
            return names;
        }
        public void TogglePassActive(string passName, bool isActive)
        {
            foreach (var pass in createdPasses)
            {
                if (pass.componentType.Name == passName)
                {
                    passActiveStatus[passName] = isActive;
                    return;
                }
            }
        }

    }
}
