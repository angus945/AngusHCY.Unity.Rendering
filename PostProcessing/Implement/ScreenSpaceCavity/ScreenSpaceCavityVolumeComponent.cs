using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace AngusHCY.Unity.Rendering.PostProcessing
{
    [System.Serializable]
    [VolumeComponentMenu("AngusHCY/ScreenSpaceCavity")]
    public class ScreenSpaceCavityVolumeComponent : PostProcessComponentBase
    {
        public override string materialPath => "cavifree-main-CavityMaterial";
        public override RenderPassEvent injectionPoint => RenderPassEvent.BeforeRenderingPostProcessing;
        public override ScriptableRenderPassInput requiredInputs => ScriptableRenderPassInput.Color | ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal;

        public ClampedFloatParameter _Intensity = new ClampedFloatParameter(0f, 0f, 10f);
        public ClampedFloatParameter _Radius = new ClampedFloatParameter(1f, 0f, 10f);
        public ClampedFloatParameter Angle_sensitivity = new ClampedFloatParameter(2.5f, 1f, 5f); //_Exponent
        public ClampedFloatParameter Edge_intensity_multiplier = new ClampedFloatParameter(0.6f, 0f, 10f); //_Multiplier
        public ClampedFloatParameter _Sharpness = new ClampedFloatParameter(0.9f, 0f, 10f);

        int _IntensityID = Shader.PropertyToID("_Intensity");
        int _RadiusID = Shader.PropertyToID("_Radius");
        int _ExponentID = Shader.PropertyToID("_Exponent");
        int _MultiplierID = Shader.PropertyToID("_Multiplier");
        int _SharpnessID = Shader.PropertyToID("_Sharpness");


        public override bool IsActive()
        {
            bool isActive = true;
            isActive &= _Intensity.value > 0f;
            isActive &= _Radius.value > 0f;
            return isActive;
        }

        public override void SetupMaterialProperties(Material material)
        {
            material.SetFloat(_IntensityID, _Intensity.value);
            material.SetFloat(_RadiusID, _Radius.value);
            material.SetFloat(_ExponentID, Angle_sensitivity.value);
            material.SetFloat(_MultiplierID, Edge_intensity_multiplier.value);
            material.SetFloat(_SharpnessID, _Sharpness.value);
        }
    }

}