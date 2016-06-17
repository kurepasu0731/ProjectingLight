using UnityEngine;
using System.Collections;
using System;
using System.Runtime.InteropServices;

public class ProjectorPoseEstimation : MonoBehaviour {

    [DllImport("WebCamera_DLL", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr getCamera(int device_);
    [DllImport("WebCamera_DLL", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern void setCameraProp(IntPtr camera, int width, int height, int fps);
    [DllImport("WebCamera_DLL", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern void releaseCamera(IntPtr camera);
    [DllImport("WebCamera_DLL", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern void getCameraTexture(IntPtr camera, IntPtr data);

    [DllImport("ProjectorPoseEstimation_DLL2", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr openProjectorEstimation(int camWidth, int camHeight, int proWidth, int proHeight, string backgroundImgFile,
                                                         int checkerRow, int checkerCol, int blockSize, int x_offset, int y_offset);
    [DllImport("ProjectorPoseEstimation_DLL2", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern void callloadParam(IntPtr projectorestimation, double[] initR, double[] initT);
    [DllImport("ProjectorPoseEstimation_DLL2", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern bool callfindProjectorPose_Corner(IntPtr projectorestimation,
                                                                                 IntPtr cam_data, IntPtr prj_data,
                                                                                 double[] initR, double[] initT,
                                                                                 double[] dstR, double[] dstT,
                                                                                 int camCornerNum, double camMinDist, int projCornerNum, double projMinDist, int mode);
    [DllImport("ProjectorPoseEstimation_DLL2", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern void destroyAllWindows();

    [DllImport("multiWindow", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    static extern void closeWindow(string windowName);


    public RenderTexture projectorImage;
    public WebCameraManager webcamManager;
    public ProCamManager procamManager;

    //カメラ・プロジェクタの解像度
    public int camWidth = 1920;
    public int camHeight = 1080;
    public int proWidth = 1280;
    public int proHeight = 800;

    //コーナー検出で用いるパラメータ
    public int camCornerNum = 500;
    public int camMinDist = 5;
    public int projCornerNum = 500;
    public int projMinDist = 10;
    public int mode = 2;

    //背景画像ファイル
    public string backgroundImgFile = "Assets/Image/bedsidemusic_1280_800.jpg";// Assets/Image/○○

    //チェッカパターン情報
    public int checkerRow = 10;
    public int checkerCol = 17;
    public int BlockSize = 64;
    public int X_offset = 128;
    public int Y_offset = 112;

    //ネイティブへのクラスポインタ
    private IntPtr projectorestimation;

    //初期位置
    private double[] initial_R = new double[9];
    private double[] initial_T = new double[3];

    //現フレームでの推定結果
    private double[] dst_R = new double[9];
    private double[] dst_T = new double[3];

    //推定できたかどうか
    private bool result = false;
    //プロジェクタ位置推定を開始するかどうか
    public bool isTrack = false;

    //WebCamera関係
    private IntPtr camera_;
    private Texture2D texture_;
    private Color32[] pixels_;
    private GCHandle pixels_handle_;
    private IntPtr pixels_ptr_;

    ////Webカメラの画像のポインタ
    //private Color32[] texturePixels_;
    //private GCHandle texturePixelsHandle_;
    //private IntPtr texturePixelsPtr_;
    //プロジェクタの画像のポインタ
    private Texture2D proj_texture;
    private Color32[] proj_texturePixels_;
    private GCHandle proj_texturePixelsHandle_;
    private IntPtr proj_texturePixelsPtr_;

	// Use this for initialization
	void Awake () {
        projectorestimation = openProjectorEstimation(camWidth, camHeight, proWidth, proHeight, backgroundImgFile, checkerRow, checkerCol, BlockSize, X_offset, Y_offset);
        proj_texture = new Texture2D(projectorImage.width, projectorImage.height, TextureFormat.ARGB32, false);
	}
	
	// Update is called once per frame
	void Update () {

        ////webcameraのTexture2Dをポインタに変換
        //// Convert texture to ptr
        //texturePixels_ = webcamManager.getWebCamTexture().GetPixels32();
        //texturePixelsHandle_ = GCHandle.Alloc(texturePixels_, GCHandleType.Pinned);
        //texturePixelsPtr_ = texturePixelsHandle_.AddrOfPinnedObject();
        //プロジェクタのRenderTexture をTexture2Dに変換
        RenderTexture.active = projectorImage;
        proj_texture.ReadPixels(new Rect(0.0f, 0.0f, projectorImage.width, projectorImage.height), 0, 0);
        proj_texture.Apply();
        //ポインタに変換
        proj_texturePixels_ = proj_texture.GetPixels32();
        proj_texturePixelsHandle_ = GCHandle.Alloc(proj_texturePixels_, GCHandleType.Pinned);
        proj_texturePixelsPtr_ = proj_texturePixelsHandle_.AddrOfPinnedObject();

        if (isTrack == true)
        {
            //カメラの画像取ってくる
            getCameraTexture(camera_, pixels_ptr_);

            //位置推定
            //result = callfindProjectorPose_Corner(projectorestimation,
            //    pixels_ptr_,
            //    initial_R, initial_T, dst_R, dst_T,
            //    camCornerNum, camMinDist, projCornerNum, projMinDist, mode);

            //位置推定
            result = callfindProjectorPose_Corner(projectorestimation,
                pixels_ptr_, proj_texturePixelsPtr_,  
                initial_R, initial_T, dst_R, dst_T,
                camCornerNum, camMinDist, projCornerNum, projMinDist, mode);

            if (result)
            {
                //プロジェクタの外部パラメータ更新
                procamManager.UpdateProjectorExternalParam(dst_R, dst_T);

                initial_R = dst_R;
                initial_T = dst_T;
            }
        }
        //WebCameraの初期化が終わっていたら、画像表示開始
        else if (camera_ != System.IntPtr.Zero && pixels_ptr_ != System.IntPtr.Zero)
            getCameraTexture(camera_, pixels_ptr_);
	}

    public void initWebCamera(int camdevice, int fps, int cameraWidth, int cameraHeight)
    {
        camera_ = getCamera(camdevice);
        setCameraProp(camera_,cameraWidth, cameraHeight, fps);
        texture_ = new Texture2D(cameraWidth, cameraHeight, TextureFormat.ARGB32, false);
        pixels_ = texture_.GetPixels32();
        pixels_handle_ = GCHandle.Alloc(pixels_, GCHandleType.Pinned);
        pixels_ptr_ = pixels_handle_.AddrOfPinnedObject();

    }

    //パラメータ読み込み、カメラ起動などの初期化処理
    public void init(int camdevice, int fps, int cameraWidth, int cameraHeight)
    {
        //初期キャリブレーションファイル、3次元復元ファイル読み込み
        callloadParam(projectorestimation, initial_R, initial_T);
        //カメラ起動 各種設定
        initWebCamera(camdevice, fps, cameraWidth, cameraHeight);
    }

    //終了処理
    void OnApplicationQuit()
    {
        pixels_handle_.Free();
        releaseCamera(camera_);

        //closeWindow("Camera detected corners");
        //closeWindow("Projector detected corners");
        //closeWindow("web camera");

        destroyAllWindows();
    }

}
