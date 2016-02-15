using UnityEngine;
using System.Collections;
using System;
using System.Runtime.InteropServices;

public class ProjectorPoseEstimation : MonoBehaviour {

    [DllImport("ProjectorPoseEstimation")]
    private static extern IntPtr openProjectorEstimation(int camWidth, int camHeight, int proWidth, int proHeight);
    [DllImport("ProjectorPoseEstimation")]
    private static extern IntPtr callloadParam(IntPtr projectorestimation, double[] initR, double[] initT);
    [DllImport("ProjectorPoseEstimation")]
    private static extern bool callfindProjectorPose_Corner(IntPtr projectorestimation, 
                                                                                 IntPtr cam_data, IntPtr proj_data, 
                                                                                 double[] initR, double[] initT,
                                                                                 double[] dstR, double[] dstT,
                                                                                 int camCornerNum, double camMinDist, int projCornerNum, double projMinDist, int mode);


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

    //Webカメラの画像のポインタ
    private Color32[] texturePixels_;
    private GCHandle texturePixelsHandle_;
    private IntPtr texturePixelsPtr_;
    //プロジェクタの画像のポインタ
    private Texture2D proj_texture;
    private Color32[] proj_texturePixels_;
    private GCHandle proj_texturePixelsHandle_;
    private IntPtr proj_texturePixelsPtr_;

	// Use this for initialization
	void Awake () {
        projectorestimation = openProjectorEstimation(camWidth, camHeight, proWidth, proHeight);
        proj_texture = new Texture2D(projectorImage.width, projectorImage.height, TextureFormat.ARGB32, false);
	}
	
	// Update is called once per frame
	void Update () {

        //webcameraのTexture2Dをポインタに変換
        // Convert texture to ptr
        texturePixels_ = webcamManager.getWebCamTexture().GetPixels32();
        texturePixelsHandle_ = GCHandle.Alloc(texturePixels_, GCHandleType.Pinned);
        texturePixelsPtr_ = texturePixelsHandle_.AddrOfPinnedObject();
        //プロジェクタのRenderTexture をTexture2Dに変換
        RenderTexture.active = projectorImage;
        proj_texture.ReadPixels(new Rect(0.0f, 0.0f, projectorImage.width, projectorImage.height), 0, 0);
        proj_texture.Apply();
        //ポインタに変換
        proj_texturePixels_ = proj_texture.GetPixels32();
        proj_texturePixelsHandle_ = GCHandle.Alloc(proj_texturePixels_, GCHandleType.Pinned);
        proj_texturePixelsPtr_ = proj_texturePixelsHandle_.AddrOfPinnedObject();

        //位置推定
        result = callfindProjectorPose_Corner(projectorestimation,
            texturePixelsPtr_, proj_texturePixelsPtr_,  //->OpenCV で扱うテクスチャ画と Unity 側のテクスチャ画の x 軸反転と、RGBA <-> BGRA 変換を忘れずに
            initial_R, initial_T, dst_R, dst_T, 
            camCornerNum, camMinDist, projCornerNum, projMinDist, 2);

        if (result)
        {
            //プロジェクタの外部パラメータ更新
            procamManager.UpdateProjectorExternalParam(dst_R, dst_T);

            initial_R = dst_R;
            initial_T = dst_T;
        }
	}

    //初期キャリブレーションファイル、3次元復元ファイル読み込み
    public void callLoadParam()
    {
        callloadParam(projectorestimation, initial_R, initial_T);
    }

}
