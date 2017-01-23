using UnityEngine;
using System.Collections;
using System;
using System.Runtime.InteropServices;

using System.Text;
using System.IO;

public class ProjectorPoseEstimation : MonoBehaviour {

    //**WEBCAMERA**//
    [DllImport("WebCamera_DLL", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr getCamera(int device_);
    [DllImport("WebCamera_DLL", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern void setCameraProp(IntPtr camera, int device, int width, int height, int fps);
    [DllImport("WebCamera_DLL", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern void releaseCamera(IntPtr camera, int device);
    [DllImport("WebCamera_DLL", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern void getCameraTexture(IntPtr camera, IntPtr data, bool isCameraRecord, bool isShowWin);

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



    [DllImport("ProjectorPoseEstimation_DLL2", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr openProjectorEstimation(int camWidth, int camHeight, int proWidth, int proHeight, double trackingtime, string backgroundImgFile,
                                                         int checkerRow, int checkerCol, int blockSize, int x_offset, int y_offset);
    [DllImport("ProjectorPoseEstimation_DLL2", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern void callloadParam(IntPtr projectorestimation, double[] initR, double[] initT);
    //プロジェクタ画像更新なし
    [DllImport("ProjectorPoseEstimation_DLL2", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    private static extern bool callfindProjectorPose_Corner(IntPtr projectorestimation,
                                                                                 //IntPtr cam_data,
                                                                                 int dotsCount, int[] dots_data,
                                                                                 double[] initR, double[] initT,
                                                                                 double[] dstR, double[] dstT, double[] error,
                                                                                 //int camCornerNum, double camMinDist, int projCornerNum, double projMinDist, 
                                                                                 double thresh, int mode, bool isKalman, bool isPredict);
                                                                                 //double C, int dotsMin, int dotsMax, float resizeScale);

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


    [DllImport("multiWindow", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    static extern void closeWindow(string windowName);


    public RenderTexture projectorImage; //プロジェクタ投影画像
    public RenderTexture cameraMask; //カメラマスク画像

    public WebCameraManager webcamManager;
    public ProCamManager procamManager;

    //カメラ・プロジェクタの解像度
    public int camWidth = 1920;
    public int camHeight = 1200; // WEBCAMERAは1080
    public int proWidth = 1280;
    public int proHeight = 800;

    //カメラデバイス番号(-1なら再生モード)
    public int camdevice = 0;

    //コーナー検出で用いるパラメータ
    public int camCornerNum = 500;
    public int camMinDist = 5;
    public int projCornerNum = 500;
    public int projMinDist = 10;
    public double thresh = 50;
    public int mode = 2;

    //ドット検出用パラメータ
    public double C = -5;
    public int DOT_THRESH_VAL_MIN = 100; //ドットノイズ弾き
    public int DOT_THRESH_VAL_MAX = 500; //エッジノイズ弾き
    public int DOT_THRESH_VAL_BRIGHT = 100; //ドット色閾値
    public float RESIZESCALE = 0.5f;

    //検出されたドットのデータ
    private int dotsCount;
    private int[] dotsData;

    //背景画像ファイル
    public string backgroundImgFile;// Assets/Image/○○

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
    //1フレームでの対応点の再投影誤差
    private double[] error = new double[1];

    //推定できたかどうか
    private bool result = false;
    //プロジェクタ位置推定を開始するかどうか
    public bool isTrack = false;
    //カルマンフィルタ使うかどうか
    public bool isKalman = true;
    //動き予測するかどうか
    public bool isPredict = false;
    //遅延補償する時間(ms)
    public double trackingTime;
    //csv記録するかどうか
    [HideInInspector]
    public bool isRecord = false;
    private System.IO.StreamWriter sw;

    //録画するか
    [HideInInspector]
    public bool isCameraRecord = false;

    //フラグのフラグ(トラッキングボタン押す前に設定する)
    public bool CSVREC = false; //トラッキングしてるときにcsvに記録するかどうか
    public bool VIDEOREC = false;//トラッキングしてるときに録画するかどうか


    //録画した時のプロジェクタの初期値(recordedInitialValue.txtからコピペする)
    private bool isfirst = true; //最初のフレームだけ
    private double[] recordedInitialR = { 0.659133818639633, -0.0200499252027763, -0.751758345231297, -0.00118308540885864, 0.999615643030721, -0.0276977709787877, 0.752024739908502, 0.0191459318822852, 0.658856755188795 };
    private double[] recordedInitialT = { 503.653325643345, -265.302786440286, 519.213222703862 };


    //WebCamera関係
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
    //プロジェクタの画像のポインタ
    //private Texture2D proj_texture;
    //private Color32[] proj_texturePixels_;
    //private GCHandle proj_texturePixelsHandle_;
    //private IntPtr proj_texturePixelsPtr_;

    //処理時間計測用
    private float check_time;

	// Use this for initialization
	void Awake () {
        projectorestimation = openProjectorEstimation(camWidth, camHeight, proWidth, proHeight, trackingTime, backgroundImgFile, checkerRow, checkerCol, BlockSize, X_offset, Y_offset);
        camera_ = getPGR(camdevice);
        mask_texture = new Texture2D(cameraMask.width, cameraMask.height, TextureFormat.ARGB32, true);
    }


    // Update is called once per frame
	void Update () {

        if (isTrack == true)
        {
            //一旦初期化
            result = false;

            ////録画時
            ////録画開始時の初期値を記録しておく
            //if (isCameraRecord)
            //{
            //    if (isfirst)
            //    {
            //        ////録画時
            //        //if (result)
            //        //{
            //        //    Encoding sjisEnc = Encoding.GetEncoding("Shift_JIS");
            //        //    StreamWriter writer = new StreamWriter(@"recordedInitialValue.txt", false, sjisEnc);
            //        //    for (int i = 0; i < 9; i++)
            //        //    {
            //        //        writer.Write(dst_R[i] + ",");
            //        //    }
            //        //    writer.WriteLine();
            //        //    for (int i = 0; i < 3; i++)
            //        //    {
            //        //        writer.Write(dst_T[i] + ",");
            //        //    }
            //        //    writer.Close();
            //        //}
            //        ////再生時
            //        //initial_R = recordedInitialR;
            //        //initial_T = recordedInitialT;
            //        isfirst = false;
            //    }
            //}

            ////★処理時間計測
            //check_time = Time.realtimeSinceStartup * 1000;
            //カメラの画像取ってくる
            //**WEBCAMERA**//
            //getCameraTexture(camera_, pixels_ptr_, isCameraRecord, false);
            //**PGRCAMERA**//
//            getPGRTexture(camera_, camdevice, pixels_ptr_, isCameraRecord, false);

            //check_time = Time.realtimeSinceStartup * 1000 - check_time;
            //Debug.Log("getCameraTexture :" + check_time + "ms");

            //★処理時間計測
            //check_time = Time.realtimeSinceStartup * 1000;

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
                        //pixels_ptr_,
                        dotsCount, dotsData,
                        initial_R, initial_T, dst_R, dst_T, error,
                        //camCornerNum, camMinDist, projCornerNum, projMinDist, 
                        thresh, mode, isKalman, isPredict);
                        //C, DOT_THRESH_VAL_MIN, DOT_THRESH_VAL_MAX, RESIZESCALE);

                    if (double.IsNaN(dst_R[0]) || double.IsNaN(dst_R[1]) || double.IsNaN(dst_R[2]) ||
                       double.IsNaN(dst_R[3]) || double.IsNaN(dst_R[4]) || double.IsNaN(dst_R[5]) ||
                       double.IsNaN(dst_R[6]) || double.IsNaN(dst_R[7]) || double.IsNaN(dst_R[8]) ||
                       double.IsNaN(dst_T[0]) || double.IsNaN(dst_T[1]) || double.IsNaN(dst_T[2]))
                    {
                        result = false;
                    }

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
                if (!double.IsNaN(dst_R[0]) && !double.IsNaN(dst_R[1]) && !double.IsNaN(dst_R[2]) &&
                        !double.IsNaN(dst_R[3]) && !double.IsNaN(dst_R[4]) && !double.IsNaN(dst_R[5]) &&
                        !double.IsNaN(dst_R[6]) && !double.IsNaN(dst_R[7]) && !double.IsNaN(dst_R[8]) &&
                        !double.IsNaN(dst_T[0]) && !double.IsNaN(dst_T[1]) && !double.IsNaN(dst_T[2]))
                    {
                    //プロジェクタの外部パラメータ更新
                    procamManager.UpdateProjectorExternalParam(dst_R, dst_T);

                    //csvに記録
                    if (isRecord) Record_T(dst_T, error[0]);


                    initial_R = dst_R;
                    initial_T = dst_T;
                }
                else
                {
                    Debug.Log("Nan!!");
                }
            }
            else
            {
                 Debug.Log("failed");
            }
        }
        //WebCameraの初期化が終わっていたら、画像表示開始
        else if (camera_ != System.IntPtr.Zero && pixels_ptr_ != System.IntPtr.Zero && camdevice != -1)
        {
            //★処理時間計測
            //check_time = Time.realtimeSinceStartup * 1000;
            //**WEBCAMERA**//
            //getCameraTexture(camera_, pixels_ptr_, isCameraRecord, true);
            //**PGRCAMERA**//
            getPGRTexture(camera_, camdevice, pixels_ptr_, isCameraRecord, true);
            //check_time = Time.realtimeSinceStartup * 1000 - check_time;
            //Debug.Log("getCameraTexture :" + check_time + "ms");

            ////ドット検出テスト//
            //★処理時間計測
            //check_time = Time.realtimeSinceStartup * 1000;
            dotsCount = getDotsCount(camera_);
            //Debug.Log("dots detected :" + dotsCount);

            if (dotsCount > 0 && dotsCount < 10000)
            {
                dotsData = new int[dotsCount * 2];
                getDotsData(camera_, ref dotsData[0]);
                //Debug.Log(dotsData.Length);
                //for (int i = 0; i < dotsCount; i++)
                //{
                //    Debug.Log(dotsData[i]);
                //}
                //渡せるか確認
                checkDotsArray(camera_, pixels_ptr_, dotsCount, dotsData);
                //showPixelData(pixels_ptr_);
            }
            //showPixelData(pixels_ptr_);


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

    public void initWebCamera(int fps, int cameraWidth, int cameraHeight)
    {
        camera_ = getCamera(camdevice);
        setCameraProp(camera_, camdevice, cameraWidth, cameraHeight, fps);
        texture_ = new Texture2D(cameraWidth, cameraHeight, TextureFormat.ARGB32, false);
        pixels_ = texture_.GetPixels32();
        pixels_handle_ = GCHandle.Alloc(pixels_, GCHandleType.Pinned);
        pixels_ptr_ = pixels_handle_.AddrOfPinnedObject();

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
        //**WEBCAMERA**//
        //initWebCamera(fps, cameraWidth, cameraHeight);
        //**PGRCAMERA**//
        initPGR(cameraWidth, cameraHeight);
    }

    //終了処理
    void OnApplicationQuit()
    {
        pixels_handle_.Free();
        //**WEBCAMERA**//
        //releaseCamera(camera_, camdevice);
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
