using UnityEngine;
using System.Collections;

public class WebCameraManager : MonoBehaviour {

    public int Width = 1920;
    public int Height = 1080;
    public int FPS = 30;

    private WebCamTexture webcamTexture;
    //private Color32[] color32;
    //private Texture2D texture;

    void Start()
    {
        WebCamDevice[] devices = WebCamTexture.devices;
        // display all cameras
        for (var i = 0; i < devices.Length; i++)
        {
            Debug.Log(devices[i].name);
        }

        webcamTexture = new WebCamTexture(devices[0].name, Width, Height, FPS);
        GetComponent<Renderer>().material.mainTexture = webcamTexture;
        webcamTexture.Play();
    }	
	// Update is called once per frame
	void Update () {
	}

    public Texture2D getWebCamTexture() { 
        //テクスチャに変換
        Color32[] color32 = webcamTexture.GetPixels32();
        Texture2D texture = new Texture2D(webcamTexture.width, webcamTexture.height);
        texture.SetPixels32(color32);
        texture.Apply();

        return texture; 
    }
}
