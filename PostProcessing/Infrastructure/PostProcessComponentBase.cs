using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace AngusHCY.Unity.Rendering.PostProcessing
{
    public abstract class PostProcessComponentBase : VolumeComponent, IPostProcessComponent
    {
        // [Tooltip("AngusHCY Custom Order for \"PostProcessComponentBase\" execution order.")]
        // public IntParameter custom_order = new IntParameter(0);

        /// <summary>
        /// The path to the material used by this post-process effect. This should be a path relative to the "Resources" folder, without the file extension. For example, if your material is located at "Assets/Resources/MyPostProcessMaterial.mat", you would return "MyPostProcessMaterial" here.
        /// </summary> <summary>
        public abstract string materialPath { get; }

        public abstract RenderPassEvent injectionPoint { get; }
        public abstract ScriptableRenderPassInput requiredInputs { get; }

        public abstract bool IsActive();
        public abstract void SetupMaterialProperties(Material material);
    }

}