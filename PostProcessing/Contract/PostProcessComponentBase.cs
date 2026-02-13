using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace AngusHCY.Unity.Rendering.PostProcessing.Contract
{
    public abstract class PostProcessComponentBase : VolumeComponent, IPostProcessComponent
    {
        [Tooltip("AngusHCY Custom Order for \"PostProcessComponentBase\" execution order.")]
        public IntParameter custom_order = new IntParameter(0);

        public abstract string shaderPath { get; }
        public abstract RenderPassEvent injectionPoint { get; }
        public abstract ScriptableRenderPassInput requiredInputs { get; }

        public abstract bool IsActive();
        public abstract void SetupMaterialProperties(Material material);
    }

}