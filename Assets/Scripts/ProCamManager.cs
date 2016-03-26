using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;


public class ProCamManager : MonoBehaviour {

    [DllImport("ProjectorPoseEstimation_DLL", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    static extern void loadCameraParam(double[] projectionMatrix, double[] dist);
    [DllImport("ProjectorPoseEstimation_DLL", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    static extern void loadProjectorParam(double[] projectionMatrix, double[] dist);
    [DllImport("ProjectorPoseEstimation_DLL", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    static extern void loadExternalParam(double[] R, double[] T);
    [DllImport("ProjectorPoseEstimation_DLL", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    static extern void PCL_viewer();

    public Camera mainCamera;
    public Camera mainProjector;

    public int maxDisplayCount = 2;

	// Use this for initialization
	void Start () {
        //マルチディスプレイを有効にする
        for (int i = 0; i < maxDisplayCount && i < Display.displays.Length; i++)
        {
            Display.displays[i].Activate();
        }
	
        //FPS設定(Dont sync にしてないと効かない)
        Application.targetFrameRate = 60;

	}
	
	// Update is called once per frame
	void Update () {	
	}

    public void callPCLViewer()
    {
        PCL_viewer();
    }

    public void loadParam(int camWidth, int camHeight, int proWidth, int proHeight)
    {
        setCameraMatrix(camWidth, camHeight);
        setProjectorMatrix(proWidth, proHeight);
    }

    // パラメータを読み込み,カメラに設定
    public void setCameraMatrix(int width, int height)
    {
        // 内部パラメータの設定
        double[] cameraMat = new double[9];
        double[] dist = new double[5];

        loadCameraParam(cameraMat, dist);

        double far = mainCamera.farClipPlane;
        double near = mainCamera.nearClipPlane;

        Matrix4x4 cameraMatrix = Matrix4x4.zero;

        cameraMatrix.m00 = (float)((-2.0f * cameraMat[0]) / width);
        cameraMatrix.m01 = 0.0f;
        cameraMatrix.m02 = (float)(1.0f - ((2.0f * cameraMat[2]) / width));
        cameraMatrix.m03 = 0.0f;
        cameraMatrix.m10 = 0.0f;
        cameraMatrix.m11 = (float)((2.0f * cameraMat[4]) / height);
        cameraMatrix.m12 = (float)(-1.0f + ((2.0f * cameraMat[5]) / height));
        cameraMatrix.m13 = 0.0f;
        cameraMatrix.m20 = 0.0f;
        cameraMatrix.m21 = 0.0f;
        cameraMatrix.m22 = (float)(-(far + near) / (far - near));
        cameraMatrix.m23 = (float)(-2 * far * near / (far - near));
        cameraMatrix.m30 = 0.0f;
        cameraMatrix.m31 = 0.0f;
        cameraMatrix.m32 = -1.0f;
        cameraMatrix.m33 = 0.0f;

        // set projection matrix
        mainCamera.projectionMatrix = cameraMatrix;

        //外部パラメータの設定(カメラ)は常に原点なので単位行列
        // set external matrix
        ////z軸方向に逆を向いているので、回転
        mainCamera.worldToCameraMatrix = Matrix4x4.identity * Matrix4x4.TRS(Vector3.zero, Quaternion.AngleAxis(180, Vector3.up), Vector3.one);
        //mainCamera.worldToCameraMatrix = Matrix4x4.identity;
    }

    // パラメータを読み込み,カメラに設定
    public void setProjectorMatrix(int width, int height)
    {
        // 内部パラメータの設定
        double[] cameraMat = new double[9];
        double[] dist = new double[5];

        loadProjectorParam(cameraMat, dist);

        double far = mainCamera.farClipPlane;
        double near = mainCamera.nearClipPlane;

        Matrix4x4 cameraMatrix = Matrix4x4.zero;

        cameraMatrix.m00 = (float)((-2.0f * cameraMat[0]) / width);
        cameraMatrix.m01 = 0.0f;
        cameraMatrix.m02 = (float)(1.0f - ((2.0f * cameraMat[2]) / width));
        cameraMatrix.m03 = 0.0f;
        cameraMatrix.m10 = 0.0f;
        cameraMatrix.m11 = (float)((2.0f * cameraMat[4]) / height);
        cameraMatrix.m12 = (float)(-1.0f + ((2.0f * cameraMat[5]) / height));
        cameraMatrix.m13 = 0.0f;
        cameraMatrix.m20 = 0.0f;
        cameraMatrix.m21 = 0.0f;
        cameraMatrix.m22 = (float)(-(far + near) / (far - near));
        cameraMatrix.m23 = (float)(-2 * far * near / (far - near));
        cameraMatrix.m30 = 0.0f;
        cameraMatrix.m31 = 0.0f;
        cameraMatrix.m32 = -1.0f;
        cameraMatrix.m33 = 0.0f;

        // set projection matrix
        mainProjector.projectionMatrix = cameraMatrix;


        //外部パラメータの設定
        double[] RMat = new double[9];
        double[] TMat = new double[3];

        loadExternalParam(RMat, TMat);

        Matrix4x4 ExternalMatrix = Matrix4x4.identity;

        ExternalMatrix.m00 = (float)RMat[0];
        ExternalMatrix.m01 = (float)RMat[1];
        ExternalMatrix.m02 = (float)RMat[2];
        ExternalMatrix.m10 = (float)RMat[3];
        ExternalMatrix.m11 = (float)RMat[4];
        ExternalMatrix.m12 = (float)RMat[5];
        ExternalMatrix.m20 = (float)RMat[6];
        ExternalMatrix.m21 = (float)RMat[7];
        ExternalMatrix.m22 = (float)RMat[8];

        //tをセット
        ExternalMatrix.m03 = (float)(-TMat[0] / 1000);
        ExternalMatrix.m13 = (float)(-TMat[1] / 1000);
        ExternalMatrix.m23 = (float)(-TMat[2] / 1000);

        ////z軸方向に逆を向いているので、回転
        ExternalMatrix =ExternalMatrix * Matrix4x4.TRS(Vector3.zero, Quaternion.AngleAxis(180, Vector3.up), Vector3.one) ;

        //転置(逆行列)
        //ExternalMatrix = ExternalMatrix.inverse;
        ////並進にRをかける
        //double t00 = ExternalMatrix.m00 * (float)TMat[0] + ExternalMatrix.m01 * (float)TMat[1] + ExternalMatrix.m02 * (float)TMat[2] + ExternalMatrix.m03;
        //double t10 = ExternalMatrix.m10 * (float)TMat[0] + ExternalMatrix.m11 * (float)TMat[1] + ExternalMatrix.m12 * (float)TMat[2] + ExternalMatrix.m13;
        //double t20 = ExternalMatrix.m20 * (float)TMat[0] + ExternalMatrix.m21 * (float)TMat[1] + ExternalMatrix.m22 * (float)TMat[2] + ExternalMatrix.m23;
        //double t30 = ExternalMatrix.m30 * (float)TMat[0] + ExternalMatrix.m31 * (float)TMat[1] + ExternalMatrix.m32 * (float)TMat[2] + ExternalMatrix.m33;

        ////wで割って向きを逆にする(z, yはそのまま)
        //t00 = -t00 / t30;
        //t10 = -t10 / t30;
        //t20 = -t20 / t30;

        ////tをセット
        //ExternalMatrix.m03 = (float)(t00 / 1000);
        //ExternalMatrix.m13 = (float)(t10 / 1000);
        //ExternalMatrix.m23 = (float)(t20 / 1000);

        //なんか逆にする
        //ExternalMatrix.m10 = -ExternalMatrix.m10;
        //ExternalMatrix.m11 = -ExternalMatrix.m11;
        //ExternalMatrix.m12 = -ExternalMatrix.m12;

        // set external matrix
        mainProjector.worldToCameraMatrix = ExternalMatrix;
        //mainProjector.worldToCameraMatrix = Matrix4x4.TRS(Vector3.right, Quaternion.identity, Vector3.one);
        //mainProjector.worldToCameraMatrix = Matrix4x4.identity;

    }

    //プロジェクタの外部パラメータの更新
    public void UpdateProjectorExternalParam(double[] RMat, double[] TMat)
    {
        Matrix4x4 ExternalMatrix = Matrix4x4.identity;

        ExternalMatrix.m00 = (float)RMat[0];
        ExternalMatrix.m01 = (float)RMat[1];
        ExternalMatrix.m02 = (float)RMat[2];
        ExternalMatrix.m10 = (float)RMat[3];
        ExternalMatrix.m11 = (float)RMat[4];
        ExternalMatrix.m12 = (float)RMat[5];
        ExternalMatrix.m20 = (float)RMat[6];
        ExternalMatrix.m21 = (float)RMat[7];
        ExternalMatrix.m22 = (float)RMat[8];

        //tをセット
        ExternalMatrix.m03 = (float)(-TMat[0] / 1000);
        ExternalMatrix.m13 = (float)(-TMat[1] / 1000);
        ExternalMatrix.m23 = (float)(-TMat[2] / 1000);

        ////z軸方向に逆を向いているので、回転
        ExternalMatrix = ExternalMatrix * Matrix4x4.TRS(Vector3.zero, Quaternion.AngleAxis(180, Vector3.up), Vector3.one);

        // set external matrix
        mainProjector.worldToCameraMatrix = ExternalMatrix;

    }
}
