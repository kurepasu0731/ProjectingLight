using UnityEngine;
using System.Collections;
using System;
using System.Runtime.InteropServices;

public class WebCamManager_native : MonoBehaviour {

    [DllImport("WebCamera_DLL")]
    private static extern IntPtr getCamera(int device_);
    [DllImport("WebCamera_DLL")]
    private static extern void setCameraProp(IntPtr camera, int width, int height, int fps);
    [DllImport("WebCamera_DLL")]
    private static extern void releaseCamera(IntPtr camera);
    [DllImport("WebCamera_DLL")]
    private static extern void getCameraTexture(IntPtr camera, IntPtr data);

    public int device = 0;
    public int width = 1920;
    public int height = 1080;
    public int fps = 30;

    private IntPtr camera_;
    private Texture2D texture_;
    private Color32[] pixels_;
    private GCHandle pixels_handle_;
    private IntPtr pixels_ptr_;


	// Use this for initialization
	void Start () {
        camera_ = getCamera(device);
        setCameraProp(camera_, width, height, fps);
        texture_ = new Texture2D(width, height, TextureFormat.ARGB32, false);
        pixels_ = texture_.GetPixels32();
        pixels_handle_ = GCHandle.Alloc(pixels_, GCHandleType.Pinned);
        pixels_ptr_ = pixels_handle_.AddrOfPinnedObject();
        GetComponent<Renderer>().material.mainTexture = texture_;
    }
	
	// Update is called once per frame
	void Update () {
        getCameraTexture(camera_, pixels_ptr_);
        texture_.SetPixels32(pixels_);
        texture_.Apply();
    }

    void OnApplicationQuit()
    {
        pixels_handle_.Free();
        releaseCamera(camera_);
    }
}
