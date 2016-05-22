using UnityEngine;
using System.Collections;
using System.IO;
using System;
using System.Runtime.InteropServices;


public class ProjectionWindow : MonoBehaviour {

    [DllImport("multiWindow", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    static extern void openWindow(string windowName);
    [DllImport("multiWindow", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    static extern void closeWindow(string windowName);
    [DllImport("multiWindow", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    static extern void showWindow(string windowName, System.IntPtr data, int width, int height);
    [DllImport("multiWindow", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    static extern void fullWindow(string windowName, int displayNum, System.IntPtr data, int width, int height);

    [DllImport("undistort", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    static extern void undistort(System.IntPtr src_data, System.IntPtr dst_data, int width, int height, double[] K, double[] dist);

    [DllImport("ProjectorPoseEstimation_DLL2", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern void destroyAllWindows();

    //[DllImport("ExternalWindow", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    //static extern IntPtr openWindow(string windowName, int displayNum, int width, int height);
    //[DllImport("ExternalWindow", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    //static extern void destroyWindow(IntPtr window);
    //[DllImport("ExternalWindow", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    //static extern void drawTextureFullWindow(IntPtr window, IntPtr data);
    //[DllImport("ExternalWindow", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    //static extern void showWindow(string windowName, System.IntPtr data, int width, int height);

    public Camera myProjector;
    public RenderTexture ProjectorImage;
    public int proWidth = 1280;
    public int proHeight = 800;
    private string windowName = "Projection";
    public int displayNum = 1;

    public ProCamManager procamManager;

    private Texture2D tex;

    private Color32[] texturePixels_;
    private GCHandle texturePixelsHandle_;
    private IntPtr texturePixelsPtr_;

    private bool projection_flag = false;

    //private IntPtr window_;


	// Use this for initialization
    void Start()
    {
        myProjector = GetComponent<Camera>();
        tex = new Texture2D(proWidth, proHeight, TextureFormat.ARGB32, false);
    }
	
	// Update is called once per frame
	void Update () {

        // 投影
        if (projection_flag)
        {

            // off-screen rendering
            //var camtex = RenderTexture.GetTemporary(proWidth, proHeight, 24, RenderTextureFormat.Default, RenderTextureReadWrite.Default, 1);
            //myProjector.targetTexture = camtex;
            //myProjector.Render();
            //RenderTexture.active = camtex;

            //tex.ReadPixels(new Rect(0, 0, camtex.width, camtex.height), 0, 0);
            //tex.Apply();

            RenderTexture.active = ProjectorImage;
            tex.ReadPixels(new Rect(0, 0, ProjectorImage.width, ProjectorImage.height), 0, 0);
            tex.Apply();

            // Convert texture to ptr
            texturePixels_ = tex.GetPixels32();
            texturePixelsHandle_ = GCHandle.Alloc(texturePixels_, GCHandleType.Pinned);
            texturePixelsPtr_ = texturePixelsHandle_.AddrOfPinnedObject();

            //投影するとゆがむので、(逆方向に)歪ませる
            undistort(texturePixelsPtr_, texturePixelsPtr_, proWidth, proHeight, procamManager.proj_K, procamManager.proj_dist);

            // Show a window
            fullWindow(windowName, displayNum, texturePixelsPtr_, proWidth, proHeight);
            //drawTextureFullWindow(window_, texturePixelsPtr_);

            texturePixelsHandle_.Free();

            //RenderTexture.active = null;
            //RenderTexture.ReleaseTemporary(camtex);
            //myProjector.targetTexture = null;

            RenderTexture.active = null;
        }
	}

    public void callProjection(int width, int height, int num)
    {
        proWidth = width;
        proHeight = height;
        displayNum = num;

        projection_flag = !projection_flag;

        if (projection_flag)
        {
            tex.Resize(proWidth, proHeight);

            closeWindow(windowName);
            openWindow(windowName);
            //destroyWindow(window_);
            //window_ = openWindow(windowName, displayNum, proWidth, proHeight);
        }
        else
        {
            closeWindow(windowName);
            //destroyWindow(window_);
        }
    }


    void OnApplicationQuit()
    {
        closeWindow(windowName);
        //destroyAllWindows();
        //destroyWindow(window_);
    }
}
