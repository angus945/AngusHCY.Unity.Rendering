using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace AngusHCY.Unity.Rendering.PostProcessing
{
    [System.Serializable]
    [VolumeComponentMenu("AngusHCY/Edge Line")]
    public class EdgeLineVolumeComponent : PostProcessComponentBase
    {
        public override string materialPath => "AngusHCY_PostProcess_EdgeLine";
        public override RenderPassEvent injectionPoint => RenderPassEvent.BeforeRenderingPostProcessing;
        public override ScriptableRenderPassInput requiredInputs => ScriptableRenderPassInput.Color | ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal;

        public ClampedIntParameter _LineThickness = new ClampedIntParameter(0, 0, 10);
        public ColorParameter _LineColor = new ColorParameter(new Color(0f, 0f, 0f, 1f), false, true, true);
        public FloatParameter _NoiseScale = new FloatParameter(0.1f);
        public FloatParameter _NoiseIntensity = new FloatParameter(0);
        public FloatParameter _AnimationSpeed = new FloatParameter(0);

        readonly int _LineThicknessID = Shader.PropertyToID(nameof(_LineThickness));
        readonly int _LineColorID = Shader.PropertyToID(nameof(_LineColor));
        readonly int _NoiseScaleID = Shader.PropertyToID(nameof(_NoiseScale));
        readonly int _NoiseIntensityID = Shader.PropertyToID(nameof(_NoiseIntensity));
        readonly int _AnimationSpeedID = Shader.PropertyToID(nameof(_AnimationSpeed));

        public override bool IsActive()
        {
            bool isActive = true;
            isActive &= _LineThickness.value > 0;
            isActive &= _LineColor.value.a > 0f;
            return isActive;
        }

        public override void SetupMaterialProperties(Material material)
        {
            material.SetInt(_LineThicknessID, _LineThickness.value);
            material.SetColor(_LineColorID, _LineColor.value);
            material.SetFloat(_NoiseScaleID, _NoiseScale.value);
            material.SetFloat(_NoiseIntensityID, _NoiseIntensity.value);
            material.SetFloat(_AnimationSpeedID, _AnimationSpeed.value);
        }
    }

}