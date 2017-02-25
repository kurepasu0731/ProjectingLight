using UnityEngine;
using System.Collections;
using System;
using System.Runtime.InteropServices;

using System.Text;
using System.IO;

public class ProjectorPoseEstimation : MonoBehaviour {

    //**PGRCAMERA**//
    [DllImport("PGR_DLL", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr getPGR(int device_);
    [DllImport("PGR_DLL", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern void initPGR(IntPtr camera, int device);
    [DllImport("PGR_DLL", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern void releasePGR(IntPtr camera, int device);
    [DllImport("PGR_DLL", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern void getPGRTexture(IntPtr camera, int device, IntPtr data, bool isCameraRecord, bool isShowWin);
    [DllImport("PGR_DLL", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern void showPixelData(IntPtr data);
    //**ドット検出関連**//
    [DllImport("PGR_DLL", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern void setDotsParameters(IntPtr camera, double AthreshVal, int DotThreshValMin, int DotThreshValMax, int DotThreshValBright, float resizeScale);
    [DllImport("PGR_DLL", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern int getDotsCount(IntPtr camera);
    [DllImport("PGR_DLL", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern void getDotsData(IntPtr camera, ref int data);
    //マスク生成
    [DllImport("PGR_DLL", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern void createCameraMask_pgr(IntPtr camera, IntPtr cam_data);


    //**ProjectorPoseEstimationコア**//
    [DllImport("ProjectorPoseEstimation_DLL2", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr openProjectorEstimation(int camWidth, int camHeight, int proWidth, int proHeight, double trackingtime, string backgroundImgFile);

    [DllImport("ProjectorPoseEstimation_DLL2", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern void callloadParam(IntPtr projectorestimation, double[] initR, double[] initT);

    [DllImport("ProjectorPoseEstimation_DLL2", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern bool callfindProjectorPose_Corner(IntPtr projectorestimation,
                                                                                 int dotsCount, int[] dots_data,
                                                                                 double[] initR, double[] initT,
                                                                                 double[] dstR, double[] dstT, 
                                                                                 double[] error,
                                                                                 double[] dstR_predict, double[] dstT_predict,
                                                                                 double thresh, bool isKalman, bool isPredict);

    [DllImport("ProjectorPoseEstimation_DLL2", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern void destroyAllWindows();
    [DllImport("ProjectorPoseEstimation_DLL2", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern void createCameraMask(IntPtr projectorestimation, IntPtr cam_data);
    [DllImport("ProjectorPoseEstimation_DLL2", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern void checkDotsArray(IntPtr projectorestimation, IntPtr cam_data, int dotsCount, int[] dots_data);

    public delegate void DebugLogDelegate(string str);
    DebugLogDelegate debugLogFunc = msg => Debug.Log(msg);

    [DllImport("ProjectorPoseEstimation_DLL2", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    public static extern void set_debug_log_func(DebugLogDelegate func);

    public RenderTexture projectorImage; //プロジェクタ投影画像
    public RenderTexture cameraMask; //カメラマスク画像

    public ProCamManager procamManager;

    //カメラ・プロジェクタの解像度
    public int camWidth = 1920;
    public int camHeight = 1200;
    public int proWidth = 1280;
    public int proHeight = 800;

    //カメラデバイス番号(-1なら再生モード)
    public int camdevice = 0;

    //コーナー検出で用いるパラメータ
    public int camCornerNum = 200;
    public int camMinDist = 50;
    public int projCornerNum = 500;
    public int projMinDist = 20;
    public double thresh = 20; //対応点間の閾値(加重平均撮った距離を閾値で切ってる)

    //ドット検出用パラメータ
    public double C = -5;
    public int DOT_THRESH_VAL_MIN = 50; //ドットノイズ弾き
    public int DOT_THRESH_VAL_MAX = 500; //エッジノイズ弾き
    public int DOT_THRESH_VAL_BRIGHT = 100; //ドット色閾値
    public float RESIZESCALE = 1.0f;

    //背景画像ファイル
    public string backgroundImgFile;// Assets/Image/○○

    //プロジェクタ位置推定を開始するかどうか
    public bool isTrack = false;
    //カルマンフィルタ使うかどうか
    public bool isKalman = true;
    //動き予測するかどうか
    public bool isPredict = false;
    //遅延補償する時間(ms)
    public double trackingTime = 113; //[ms]

    //フラグのフラグ(トラッキングボタン押す前に設定する)
    public bool CSVREC = false; //トラッキングしてるときにcsvに記録するかどうか
    public bool VIDEOREC = false;//トラッキングしてるときに録画するかどうか

    //録画するか
    [HideInInspector]
    public bool isCameraRecord = false;
   //csv記録するかどうか
    [HideInInspector]
    public bool isRecord = false;

    //csv記録用
    private System.IO.StreamWriter sw;

    //検出されたドットのデータ
    private int dotsCount;
    private int[] dotsData;

    //ネイティブへのクラスポインタ
    private IntPtr projectorestimation;

    //初期位置
    private double[] initial_R = new double[9];
    private double[] initial_T = new double[3];

    //現フレームでの推定結果
    private double[] dst_R = new double[9];
    private double[] dst_T = new double[3];
    //1フレームでの対応点の再投影誤差
    private double[] error = new double[1];

    //予測結果
    private double[] dst_R_predict = new double[9];
    private double[] dst_T_predict = new double[3];

    //推定できたかどうか
    private bool result = false;

    //Camera関係
    private IntPtr camera_;
    private Texture2D texture_;
    private Color32[] pixels_;
    private GCHandle pixels_handle_;
    private IntPtr pixels_ptr_;

    //マスク画像のポインタ
    private Texture2D mask_texture;
    private Color32[] texturePixels_;
    private GCHandle texturePixelsHandle_;
    private IntPtr texturePixelsPtr_;

    //処理時間計測用
    private float check_time;

	// Use this for initialization
	void Awake () {
        projectorestimation = openProjectorEstimation(camWidth, camHeight, proWidth, proHeight, trackingTime, backgroundImgFile);
        camera_ = getPGR(camdevice);
        mask_texture = new Texture2D(cameraMask.width, cameraMask.height, TextureFormat.ARGB32, true);
    }


    // Update is called once per frame
    void Update()
    {

        if (isTrack == true)
        {
            //一旦初期化
            result = false;

            //カメラの画像取ってくる
            //**PGRCAMERA**//
            //getPGRTexture(camera_, camdevice, pixels_ptr_, isCameraRecord, false);

            //if (pixels_ptr_ != System.IntPtr.Zero)
            //{//位置推定(プロジェクタ画像更新なし)

            ////★処理時間計測
            //check_time = Time.realtimeSinceStartup * 1000;
            //**ドット検出フェーズ**//
            dotsCount = getDotsCount(camera_);
            if (dotsCount > 0 && dotsCount < 10000)
            {

                dotsData = new int[dotsCount * 2];
                getDotsData(camera_, ref dotsData[0]);
                ////★処理時間計測
                //check_time = Time.realtimeSinceStartup * 1000 - check_time;
                //Debug.Log("getDotsData :" + check_time + "ms");

                ////★処理時間計測
                //check_time = Time.realtimeSinceStartup * 1000;
                result = callfindProjectorPose_Corner(projectorestimation,
                    dotsCount, dotsData,
                    initial_R, initial_T, dst_R, dst_T, error,
                    dst_R_predict, dst_T_predict,
                    thresh, isKalman, isPredict);
                ////★処理時間計測
                //check_time = Time.realtimeSinceStartup * 1000 - check_time;
                //Debug.Log("caluclate :" + check_time + "ms");
            }
            else
            {
                result = false;
            }
            //}
            //check_time = Time.realtimeSinceStartup * 1000 - check_time;
            //Debug.Log("all callfindProjectorPose_Corner :" + check_time + "ms");

            if (result)
            {
                //プロジェクタの外部パラメータ更新
                if (isPredict)//予測ありのときは予測値で更新
                {
                    procamManager.UpdateProjectorExternalParam(dst_R_predict, dst_T_predict);
                }
                else
                {
                    procamManager.UpdateProjectorExternalParam(dst_R, dst_T);
                }

                //csvに記録
                if (isRecord) Record_T(dst_T, error[0]);

                initial_R = dst_R;
                initial_T = dst_T;
            }
            else
            {
                Debug.Log("failed");
            }
        }
        //Cameraの初期化が終わっていたら、画像表示開始
        else if (camera_ != System.IntPtr.Zero && pixels_ptr_ != System.IntPtr.Zero && camdevice != -1)
        {
            //★処理時間計測
            //check_time = Time.realtimeSinceStartup * 1000;
            //**PGRCAMERA**//
            getPGRTexture(camera_, camdevice, pixels_ptr_, isCameraRecord, true);
            //check_time = Time.realtimeSinceStartup * 1000 - check_time;
            //Debug.Log("getCameraTexture :" + check_time + "ms");

            ////ドット検出のみ//
            //★処理時間計測
            //check_time = Time.realtimeSinceStartup * 1000;
            dotsCount = getDotsCount(camera_);
            //Debug.Log("dots detected :" + dotsCount);

            if (dotsCount > 0 && dotsCount < 10000)
            {
                dotsData = new int[dotsCount * 2];
                getDotsData(camera_, ref dotsData[0]);
                //渡せるか確認
                checkDotsArray(camera_, pixels_ptr_, dotsCount, dotsData);
                //showPixelData(pixels_ptr_);
            }
        }
    }

    public void OpenStream(string filename)
    {
        sw = new System.IO.StreamWriter(@filename, false);
    }

    public void CloseStream()
    {
        if(sw != null) sw.Close();
    }

    //csvにdstTを記録
    public void Record_T(double[] dstT, double error)
    {
        try
        {
            if(sw != null)
                sw.WriteLine("{0}, {1}, {2}, {3}", dst_T[0], dst_T[1], dst_T[2], error);
        }
        catch (System.Exception e)
        {
            Debug.Log(e.Message);
        }
    }

    public void initPGR(int cameraWidth, int cameraHeight)
    {
        initPGR(camera_, camdevice);
        //ドット検出用パラメータセット
        setDotsParameters(camera_, C, DOT_THRESH_VAL_MIN, DOT_THRESH_VAL_MAX, DOT_THRESH_VAL_BRIGHT, RESIZESCALE);
        texture_ = new Texture2D(cameraWidth, cameraHeight, TextureFormat.ARGB32, false);
        pixels_ = texture_.GetPixels32();
        pixels_handle_ = GCHandle.Alloc(pixels_, GCHandleType.Pinned);
        pixels_ptr_ = pixels_handle_.AddrOfPinnedObject();
    }


    //パラメータ読み込み、カメラ起動などの初期化処理
    public void init(int fps, int cameraWidth, int cameraHeight)
    {
        //初期キャリブレーションファイル、3次元復元ファイル読み込み
        callloadParam(projectorestimation, initial_R, initial_T);
        //カメラ起動 各種設定
        //**PGRCAMERA**//
        initPGR(cameraWidth, cameraHeight);
    }

    //終了処理
    void OnApplicationQuit()
    {
        pixels_handle_.Free();
        //**PGRCAMERA**//
        if (camera_ != System.IntPtr.Zero)
        {
            releasePGR(camera_, camdevice);
        }

        destroyAllWindows();
    }

    //カメラのマスク画像の生成
    public void createCameraMaskImage()
    {
        //maskのTexture2Dをポインタに変換
        RenderTexture.active = cameraMask;
        mask_texture.ReadPixels(new Rect(0.0f, 0.0f, cameraMask.width, cameraMask.height), 0, 0);
        mask_texture.Apply();
        // Convert texture to ptr
        texturePixels_ = mask_texture.GetPixels32();
        texturePixelsHandle_ = GCHandle.Alloc(texturePixels_, GCHandleType.Pinned);
        texturePixelsPtr_ = texturePixelsHandle_.AddrOfPinnedObject();

        createCameraMask(projectorestimation, texturePixelsPtr_); //マスク画像を保存、セット
        createCameraMask_pgr(camera_, texturePixelsPtr_);//マスク画像をセット

        RenderTexture.active = null;

    }

    public void OnEnable()
    {
        set_debug_log_func(debugLogFunc);
    }

    public void OnDisable()
    {
        set_debug_log_func(null);
    }

}
