using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using AngusHCY.Unity.Rendering.PostProcessing.Contract;

namespace AngusHCY.Unity.Rendering.PostProcessing.Implement
{
    [System.Serializable]
    [VolumeComponentMenu("AngusHCY/Oil Painting")]
    public class OilPaintingVolumeComponent : PostProcessComponentBase
    {
        public override string shaderPath => "AngusHCY/PostProcess/OilPaintingShader";
        public override RenderPassEvent injectionPoint => RenderPassEvent.BeforeRenderingPostProcessing;
        public override ScriptableRenderPassInput requiredInputs => ScriptableRenderPassInput.Color;

        public ClampedIntParameter _KernelSize = new ClampedIntParameter(0, 0, 10);

        readonly int _KernelSizeID = Shader.PropertyToID("_KernelSize");

        public override bool IsActive()
        {
            return _KernelSize.value > 0;
        }

        public override void SetupMaterialProperties(Material material)
        {
            material.SetInt(_KernelSizeID, _KernelSize.value);
        }
    }

}