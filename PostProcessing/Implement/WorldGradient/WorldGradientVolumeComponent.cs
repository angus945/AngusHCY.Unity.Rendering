using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace AngusHCY.Unity.Rendering.PostProcessing
{
    [System.Serializable]
    [VolumeComponentMenu("AngusHCY/World Gradient")]
    public class WorldGradientVolumeComponent : PostProcessComponentBase
    {
        public override string materialPath => "AngusHCY_PostProcess_WorldSpaceGradient";
        public override RenderPassEvent injectionPoint => RenderPassEvent.BeforeRenderingPostProcessing;
        public override ScriptableRenderPassInput requiredInputs => ScriptableRenderPassInput.Color | ScriptableRenderPassInput.Depth;

        public BoolParameter _Active = new BoolParameter(false);
        public ColorParameter _TopColor = new ColorParameter(new Color(1f, 1f, 1f, 0f), false, false, true);
        public ColorParameter _BottomColor = new ColorParameter(new Color(0, 0, 0, 0f), false, false, true);
        public ColorParameter _TopOverlayColor = new ColorParameter(new Color(1f, 1f, 1f, 0f), false, true, true);
        public ColorParameter _BottomOverlayColor = new ColorParameter(new Color(1f, 1f, 1f, 0f), false, true, true);
        public FloatParameter _GradientStartHeight = new FloatParameter(0f);
        public FloatParameter _GradientRange = new FloatParameter(5f);
        public FloatParameter _Power = new FloatParameter(1f);

        readonly int _TopColorID = Shader.PropertyToID(nameof(_TopColor));
        readonly int _BottomColorID = Shader.PropertyToID(nameof(_BottomColor));
        readonly int _TopOverlayColorID = Shader.PropertyToID(nameof(_TopOverlayColor));
        readonly int _BottomOverlayColorID = Shader.PropertyToID(nameof(_BottomOverlayColor));
        readonly int _GradientStartHeightID = Shader.PropertyToID(nameof(_GradientStartHeight));
        readonly int _GradientRangeID = Shader.PropertyToID(nameof(_GradientRange));
        readonly int _PowerID = Shader.PropertyToID(nameof(_Power));

        public override bool IsActive()
        {
            bool isActive = true;
            isActive &= _Active.value;
            return isActive;
        }
        public override void SetupMaterialProperties(Material material)
        {
            material.SetColor(_TopColorID, _TopColor.value);
            material.SetColor(_BottomColorID, _BottomColor.value);
            material.SetColor(_TopOverlayColorID, _TopOverlayColor.value);
            material.SetColor(_BottomOverlayColorID, _BottomOverlayColor.value);
            material.SetFloat(_GradientStartHeightID, _GradientStartHeight.value);
            material.SetFloat(_GradientRangeID, _GradientRange.value);
            material.SetFloat(_PowerID, _Power.value);
        }
    }

}