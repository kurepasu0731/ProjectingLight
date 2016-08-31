using System;
using UnityEngine;

namespace UnityStandardAssets.ImageEffects
{
    //[ExecuteInEditMode]
    [RequireComponent(typeof(Camera))]
    //[AddComponentMenu("Image Effects/Displacement/CameraDistortion")]
    public class PinHoleEffect : PostEffectsBase
    {

        public Shader cameraShader = null;
        private Material cameraMaterial = null;

        public override bool CheckResources()
        {
            CheckSupport(false);
            cameraMaterial = CheckShaderAndCreateMaterial(cameraShader, cameraMaterial);

            if (!isSupported)
                ReportAutoDisable();
            return isSupported;
        }

        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (CheckResources() == false)
            {
                Graphics.Blit(source, destination);
                return;
            }
            cameraMaterial.SetVector("_Color", new Vector4(1.0f, 1.0f, 1.0f, 1.0f));
            Graphics.Blit(source, destination, cameraMaterial);
        }
    }
}
