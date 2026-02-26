using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace AngusHCY.Unity.Rendering.PostProcessing
{
    [System.Serializable]
    [VolumeComponentMenu("AngusHCY/Water Distortion")]
    public class WaterDistortionVolumeComponent : PostProcessComponentBase
    {
        public override string materialPath => "AngusHCY_PostProcess_WaterDistortion";
        public override RenderPassEvent injectionPoint => RenderPassEvent.BeforeRenderingPostProcessing;
        public override ScriptableRenderPassInput requiredInputs => ScriptableRenderPassInput.Color;

        public TextureParameter _FlowMapTexture = new TextureParameter(null);
        public ClampedFloatParameter _FlowStrength = new ClampedFloatParameter(0, 0, 0.1f);
        public FloatParameter _FlowOffset = new FloatParameter(20f);
        public MinFloatParameter _FlowSpeed = new MinFloatParameter(1f, 0);

        readonly int _FlowMapTextureID = Shader.PropertyToID(nameof(_FlowMapTexture));
        readonly int _FlowStrengthID = Shader.PropertyToID(nameof(_FlowStrength));
        readonly int _FlowOffsetID = Shader.PropertyToID(nameof(_FlowOffset));
        readonly int _FlowSpeedID = Shader.PropertyToID(nameof(_FlowSpeed));

        public override bool IsActive()
        {
            bool active = true;
            active &= _FlowMapTexture.value != null;
            active &= _FlowStrength.value > 0;
            return active;
        }
        public override void SetupMaterialProperties(Material material)
        {
            material.SetTexture(_FlowMapTextureID, _FlowMapTexture.value);
            material.SetFloat(_FlowStrengthID, _FlowStrength.value);
            material.SetFloat(_FlowOffsetID, _FlowOffset.value);
            material.SetFloat(_FlowSpeedID, _FlowSpeed.value);
        }
    }

}