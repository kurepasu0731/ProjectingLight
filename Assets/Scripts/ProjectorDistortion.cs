using System;
using UnityEngine;

namespace UnityStandardAssets.ImageEffects
{
    //[ExecuteInEditMode]
    [RequireComponent(typeof(Camera))]
    //[AddComponentMenu("Image Effects/Displacement/CameraDistortion")]
    public class ProjectorDistortion : PostEffectsBase
    {

        public Shader cameraDistortionShader = null;
        private Material cameraDistortionMaterial = null;

        //パラメータ取得用
        public ProCamManager procamManager;
        //カメラ解像度
        public int projWidth = 1280;
        public int projHeight = 800;

        public override bool CheckResources()
        {
            CheckSupport(false);
            cameraDistortionMaterial = CheckShaderAndCreateMaterial(cameraDistortionShader, cameraDistortionMaterial);

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

            cameraDistortionMaterial.SetVector("internalParam", new Vector4((float)procamManager.proj_K[0], (float)procamManager.proj_K[4], (float)procamManager.proj_K[2], (float)procamManager.proj_K[5]));
            cameraDistortionMaterial.SetVector("distortion", new Vector4((float)procamManager.proj_dist[0], (float)procamManager.proj_dist[1], (float)procamManager.proj_dist[2], (float)procamManager.proj_dist[3]));
            cameraDistortionMaterial.SetVector("resolution", new Vector4((float)projWidth, (float)projHeight, 0f, 0f));
            Graphics.Blit(source, destination, cameraDistortionMaterial);
        }
    }
}
