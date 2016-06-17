using UnityEngine;
using System.Collections;

public class RenderCameraMask : MonoBehaviour {

    public RenderTexture cameraDepthImage;
    public RenderTexture maskImage;
    public Material maskMat;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
        //影画像のレンダリング
        Graphics.Blit(cameraDepthImage, maskImage, maskMat);
    }
}
