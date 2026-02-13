using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using AngusHCY.Unity.Rendering.PostProcessing.Contract;

namespace AngusHCY.Unity.Rendering.PostProcessing.Implement
{
    [System.Serializable]
    [VolumeComponentMenu("AngusHCY/World Height Volumetric Fog")]
    public class WorldHeightVolumetricFogVolumeComponent : PostProcessComponentBase
    {
        public override string shaderPath => "AngusHCY/PostProcess/WorldHeightVolumetricFog";
        public override RenderPassEvent injectionPoint => RenderPassEvent.BeforeRenderingPostProcessing;
        public override ScriptableRenderPassInput requiredInputs => ScriptableRenderPassInput.Color | ScriptableRenderPassInput.Depth;

        public ClampedFloatParameter _FogDensity = new ClampedFloatParameter(0f, 0f, 1f);
        public ColorParameter _FogColor = new ColorParameter(new Color(1f, 1f, 1f, 1f), false, false, true);
        public FloatParameter _FogBottomHeight = new FloatParameter(0f);
        public FloatParameter _FogFadeoutRange = new FloatParameter(5f);
        public ClampedFloatParameter _HeightExponent = new ClampedFloatParameter(1f, 0.1f, 20f);

        readonly int _FogColorID = Shader.PropertyToID(nameof(_FogColor));
        readonly int _FogBottomHeightID = Shader.PropertyToID(nameof(_FogBottomHeight));
        readonly int _FogFadeoutRangeID = Shader.PropertyToID(nameof(_FogFadeoutRange));
        readonly int _FogDensityID = Shader.PropertyToID(nameof(_FogDensity));
        readonly int _HeightExponentID = Shader.PropertyToID(nameof(_HeightExponent));

        public override bool IsActive()
        {
            bool isActive = false;
            isActive |= _FogColor.value.a > 0f;
            isActive |= _FogDensity.value > 0f;
            return isActive;
        }

        public override void SetupMaterialProperties(Material material)
        {
            material.SetColor(_FogColorID, _FogColor.value);
            material.SetFloat(_FogBottomHeightID, _FogBottomHeight.value);
            material.SetFloat(_FogFadeoutRangeID, _FogFadeoutRange.value);
            material.SetFloat(_FogDensityID, _FogDensity.value);
            material.SetFloat(_HeightExponentID, _HeightExponent.value);
        }
    }

}