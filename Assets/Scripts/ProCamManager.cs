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
    
    //パラメータを読み込んだかどうか
    public bool isloadParam = false;

    //プロジェクタの内部、歪み係数
    public double[] proj_K;
    public double[] proj_dist;
    
    //初期位置(表示用)
    public double[] initialT;

    //プロジェクタの外部(確認表示用)
    public double[] proj_R;
    public double[] proj_T;

    //プロジェクタとカメラの距離(tのノルム)
    public double nolm = 0.0;


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
        isloadParam = true;
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

        cameraMatrix.m00 = (float)((2.0f * cameraMat[0]) / width);
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
        //cameraMatrix.m00 = (float)((-2.0f * cameraMat[0]) / width);
        //cameraMatrix.m01 = 0.0f;
        //cameraMatrix.m02 = (float)(1.0f - ((2.0f * cameraMat[2]) / width));
        //cameraMatrix.m03 = 0.0f;
        //cameraMatrix.m10 = 0.0f;
        //cameraMatrix.m11 = (float)((2.0f * cameraMat[4]) / height);
        //cameraMatrix.m12 = (float)(-1.0f + ((2.0f * cameraMat[5]) / height));
        //cameraMatrix.m13 = 0.0f;
        //cameraMatrix.m20 = 0.0f;
        //cameraMatrix.m21 = 0.0f;
        //cameraMatrix.m22 = (float)(-(far + near) / (far - near));
        //cameraMatrix.m23 = (float)(-2 * far * near / (far - near));
        //cameraMatrix.m30 = 0.0f;
        //cameraMatrix.m31 = 0.0f;
        //cameraMatrix.m32 = -1.0f;
        //cameraMatrix.m33 = 0.0f;

        // set projection matrix
        mainCamera.projectionMatrix = cameraMatrix;

        //外部パラメータの設定(カメラ)は常に原点なので単位行列
        // set external matrix
        ////z軸方向に逆を向いているので、回転
        //mainCamera.worldToCameraMatrix = Matrix4x4.identity * Matrix4x4.TRS(Vector3.zero, Quaternion.AngleAxis(180, Vector3.up), Vector3.one);
        //mainCamera.worldToCameraMatrix = Matrix4x4.identity;
        Matrix4x4 m = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1, 1, -1));
        mainCamera.worldToCameraMatrix = m * Matrix4x4.identity;
    }

    // パラメータを読み込み,カメラに設定
    public void setProjectorMatrix(int width, int height)
    {
        // 内部パラメータの設定
        proj_K = new double[9];
        proj_dist = new double[5];

        loadProjectorParam(proj_K, proj_dist);

        double far = mainProjector.farClipPlane;
        double near = mainProjector.nearClipPlane;

        Matrix4x4 cameraMatrix = Matrix4x4.zero;

        cameraMatrix.m00 = (float)((2.0f * proj_K[0]) / width);
//        cameraMatrix.m00 = (float)((-2.0f * proj_K[0]) / width);
        cameraMatrix.m01 = 0.0f;
        cameraMatrix.m02 = (float)(1.0f - ((2.0f * proj_K[2]) / width));
        cameraMatrix.m03 = 0.0f;
        cameraMatrix.m10 = 0.0f;
        cameraMatrix.m11 = (float)((2.0f * proj_K[4]) / height);
        cameraMatrix.m12 = (float)(-1.0f + ((2.0f * proj_K[5]) / height));
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

        ////y,z軸を反転
        //Matrix4x4 calibMatrix = Matrix4x4.identity;
        //calibMatrix.m00 = (float)RMat[0];
        //calibMatrix.m01 = (float)RMat[1];
        //calibMatrix.m02 = (float)RMat[2];
        //calibMatrix.m10 = (float)RMat[3];
        //calibMatrix.m11 = (float)RMat[4];
        //calibMatrix.m12 = (float)RMat[5];
        //calibMatrix.m20 = (float)RMat[6];
        //calibMatrix.m21 = (float)RMat[7];
        //calibMatrix.m22 = (float)RMat[8];
        //calibMatrix.m03 = (float)(TMat[0] * 0.001);
        //calibMatrix.m13 = (float)(TMat[1] * 0.001);
        //calibMatrix.m23 = (float)(TMat[2] * 0.001);
        //Matrix4x4 m = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1, -1, -1));
        //calibMatrix = m * calibMatrix;

        //float[] camR = new float[9];
        //float[] camT = new float[3];
        //ChangeCameraCoordinate(calibMatrix, camR, camT);

        Matrix4x4 ExternalMatrix = Matrix4x4.identity;

        //ExternalMatrix.m00 = camR[0];
        //ExternalMatrix.m01 = camR[1];
        //ExternalMatrix.m02 = camR[2];
        //ExternalMatrix.m10 = camR[3];
        //ExternalMatrix.m11 = camR[4];
        //ExternalMatrix.m12 = camR[5];
        //ExternalMatrix.m20 = camR[6];
        //ExternalMatrix.m21 = camR[7];
        //ExternalMatrix.m22 = camR[8];
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
        //ExternalMatrix.m03 = camT[0];
        //ExternalMatrix.m13 = camT[1];
        //ExternalMatrix.m23 = camT[2];
//        ExternalMatrix.m03 = (float)(-TMat[0] * 0.001);
//        ExternalMatrix.m13 = (float)(-TMat[1] * 0.001);
//        ExternalMatrix.m23 = (float)(-TMat[2] * 0.001);

        ExternalMatrix.m03 = (float)(TMat[0] * 0.001);
        ExternalMatrix.m13 = (float)(TMat[1] * 0.001);
        ExternalMatrix.m23 = (float)(TMat[2] * 0.001);
        setProjectorTransoform(ExternalMatrix);


        ////z軸方向に逆を向いているので、回転
//        ExternalMatrix = ExternalMatrix * Matrix4x4.TRS(Vector3.zero, Quaternion.AngleAxis(180, Vector3.up), Vector3.one);
        //Matrix4x4 m = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1, 1, -1));
        //ExternalMatrix = m * ExternalMatrix;

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
//        mainProjector.worldToCameraMatrix = ExternalMatrix;
        //mainProjector.worldToCameraMatrix = Matrix4x4.TRS(Vector3.right, Quaternion.identity, Vector3.one);
        //mainProjector.worldToCameraMatrix = Matrix4x4.identity;

        ////mainProjectorのTramsformを変えるパターン
        //Matrix4x4 RotateMatrix = Matrix4x4.identity;
        //RotateMatrix.m00 = ExternalMatrix.m00;
        //RotateMatrix.m01 = ExternalMatrix.m01;
        //RotateMatrix.m02 = ExternalMatrix.m02;
        //RotateMatrix.m10 = ExternalMatrix.m10;
        //RotateMatrix.m11 = ExternalMatrix.m11;
        //RotateMatrix.m12 = ExternalMatrix.m12;
        //RotateMatrix.m20 = ExternalMatrix.m20;
        //RotateMatrix.m21 = ExternalMatrix.m21;
        //RotateMatrix.m22 = ExternalMatrix.m22;
        //mainProjector.transform.rotation = QuaternionFromMatrix(RotateMatrix);
        //mainProjector.transform.position = new Vector3(ExternalMatrix.m03, ExternalMatrix.m13, ExternalMatrix.m23);


        //debug用
        proj_R = new double[9];
        proj_T = new double[3];

        proj_R[0] = ExternalMatrix.m00;
        proj_R[1] = ExternalMatrix.m01;
        proj_R[2] = ExternalMatrix.m02;
        proj_R[3] = ExternalMatrix.m10;
        proj_R[4] = ExternalMatrix.m11;
        proj_R[5] = ExternalMatrix.m12;
        proj_R[6] = ExternalMatrix.m20;
        proj_R[7] = ExternalMatrix.m21;
        proj_R[8] = ExternalMatrix.m22;

        proj_T[0] = ExternalMatrix.m03;
        proj_T[1] = ExternalMatrix.m13;
        proj_T[2] = ExternalMatrix.m23;

        nolm = Mathf.Sqrt(ExternalMatrix.m03 * ExternalMatrix.m03 + ExternalMatrix.m13 * ExternalMatrix.m13 + ExternalMatrix.m23 * ExternalMatrix.m23);

        //初期位置(表示用)
        initialT = new double[3];
        initialT[0] = ExternalMatrix.m03;
        initialT[1] = ExternalMatrix.m13;
        initialT[2] = ExternalMatrix.m23;

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

        ExternalMatrix.m03 = (float)(TMat[0] * 0.001);
        ExternalMatrix.m13 = (float)(TMat[1] * 0.001);
        ExternalMatrix.m23 = (float)(TMat[2] * 0.001);
        setProjectorTransoform(ExternalMatrix);

        //tをセット
        //ExternalMatrix.m03 = (float)(TMat[0] * 0.001);
        //ExternalMatrix.m13 = (float)(-TMat[1] * 0.001);
        //ExternalMatrix.m23 = (float)(TMat[2] * 0.001);
//        ExternalMatrix.m03 = (float)(-TMat[0] * 0.001);
//        ExternalMatrix.m13 = (float)(-TMat[1] * 0.001);
//        ExternalMatrix.m23 = (float)(-TMat[2] * 0.001);

        ////z軸方向に逆を向いているので、回転
//        ExternalMatrix = ExternalMatrix * Matrix4x4.TRS(Vector3.zero, Quaternion.AngleAxis(180, Vector3.up), Vector3.one);
        //Matrix4x4 m = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1, 1, -1));
        //ExternalMatrix = m * ExternalMatrix;

        // set external matrix
 //       mainProjector.worldToCameraMatrix = ExternalMatrix;

        //debug用
        proj_R[0] = ExternalMatrix.m00;
        proj_R[1] = ExternalMatrix.m01;
        proj_R[2] = ExternalMatrix.m02;
        proj_R[3] = ExternalMatrix.m10;
        proj_R[4] = ExternalMatrix.m11;
        proj_R[5] = ExternalMatrix.m12;
        proj_R[6] = ExternalMatrix.m20;
        proj_R[7] = ExternalMatrix.m21;
        proj_R[8] = ExternalMatrix.m22;

        proj_T[0] = ExternalMatrix.m03;
        proj_T[1] = ExternalMatrix.m13;
        proj_T[2] = ExternalMatrix.m23;

        nolm = Mathf.Sqrt(ExternalMatrix.m03 * ExternalMatrix.m03 + ExternalMatrix.m13 * ExternalMatrix.m13 + ExternalMatrix.m23 * ExternalMatrix.m23);

    }

    //プロジェクタからみたカメラの座標→カメラからみたプロジェクタの座標に変換
    public void setProjectorTransoform(Matrix4x4 calibMatrix)
    {
        Matrix4x4 RotateMatrix = Matrix4x4.identity;
        RotateMatrix.m00 = calibMatrix.m00;
        RotateMatrix.m01 = calibMatrix.m01;
        RotateMatrix.m02 = calibMatrix.m02;
        RotateMatrix.m10 = calibMatrix.m10;
        RotateMatrix.m11 = calibMatrix.m11;
        RotateMatrix.m12 = calibMatrix.m12;
        RotateMatrix.m20 = calibMatrix.m20;
        RotateMatrix.m21 = calibMatrix.m21;
        RotateMatrix.m22 = calibMatrix.m22;

        Matrix4x4 t_RotateMatrix = RotateMatrix.transpose;//転置

        Vector3 TramslateVector = new Vector3(calibMatrix.m03, calibMatrix.m13, calibMatrix.m23);
        Vector3 camTvec = -(t_RotateMatrix * TramslateVector);

        //Debug.Log("(x, y, z): (" + camTvec.x + ", " + camTvec.y + ", " + camTvec.z + ")");

        //transform projector
        //y軸逆にする
        //Matrix4x4 m = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1, 1, 1));
        //t_RotateMatrix = m * t_RotateMatrix;
        camTvec.y = -camTvec.y;

        mainProjector.transform.position = camTvec;

        //Matrix4x4 -> Quarternionへ
        double[] quart = new double[4];
        transformRotMatToQuaternion(t_RotateMatrix.transpose, quart);
        Quaternion q = new Quaternion((float)quart[0], (float)quart[1], (float)quart[2], (float)quart[3]);
        //Quaternion q = QuaternionFromMatrix(t_RotateMatrix);
        //Quaternion q = MatrixToQuaternion(t_RotateMatrix);
        //mainProjector.transform.eulerAngles = new Vector3(-q.eulerAngles.x,q.eulerAngles.y, -q.eulerAngles.z);
        mainProjector.transform.rotation = new Quaternion(-q.x, q.y, -q.z, q.w); //Unityの座標軸と全軸逆向き

    }

    //こっちはそれっぽく動くぞ
    public static Quaternion QuaternionFromMatrix(Matrix4x4 m)
    {
        // Adapted from: http://www.euclideanspace.com/maths/geometry/rotations/conversions/matrixToQuaternion/index.htm
        Quaternion q = new Quaternion();
        q.w = Mathf.Sqrt(Mathf.Max(0, 1 + m[0, 0] + m[1, 1] + m[2, 2])) / 2;
        q.x = Mathf.Sqrt(Mathf.Max(0, 1 + m[0, 0] - m[1, 1] - m[2, 2])) / 2;
        q.y = Mathf.Sqrt(Mathf.Max(0, 1 - m[0, 0] + m[1, 1] - m[2, 2])) / 2;
        q.z = Mathf.Sqrt(Mathf.Max(0, 1 - m[0, 0] - m[1, 1] + m[2, 2])) / 2;
        q.x *= Mathf.Sign(q.x * (m[2, 1] - m[1, 2]));
        q.y *= Mathf.Sign(q.y * (m[0, 2] - m[2, 0]));
        q.z *= Mathf.Sign(q.z * (m[1, 0] - m[0, 1]));
        return q;
    }

    //これも動く
    public static Quaternion MatrixToQuaternion(Matrix4x4 m)
    {
        float tr = m.m00 + m.m11 + m.m22;
        float w, x, y, z;
        if (tr > 0f)
        {
            float s = Mathf.Sqrt(1f + tr) * 2f;
            w = 0.25f * s;
            x = (m.m21 - m.m12) / s;
            y = (m.m02 - m.m20) / s;
            z = (m.m10 - m.m01) / s;
        }
        else if ((m.m00 > m.m11) && (m.m00 > m.m22))
        {
            float s = Mathf.Sqrt(1f + m.m00 - m.m11 - m.m22) * 2f;
            w = (m.m21 - m.m12) / s;
            x = 0.25f * s;
            y = (m.m01 + m.m10) / s;
            z = (m.m02 + m.m20) / s;
        }
        else if (m.m11 > m.m22)
        {
            float s = Mathf.Sqrt(1f + m.m11 - m.m00 - m.m22) * 2f;
            w = (m.m02 - m.m20) / s;
            x = (m.m01 + m.m10) / s;
            y = 0.25f * s;
            z = (m.m12 + m.m21) / s;
        }
        else
        {
            float s = Mathf.Sqrt(1f + m.m22 - m.m00 - m.m11) * 2f;
            w = (m.m10 - m.m01) / s;
            x = (m.m02 + m.m20) / s;
            y = (m.m12 + m.m21) / s;
            z = 0.25f * s;
        }

        Quaternion quat = new Quaternion(x, y, z, w);
        //Debug.Log("Quat is " + quat.ToString() );
        return quat;
    }

    //これも動く
    //mは転置してから！
    public static bool transformRotMatToQuaternion(Matrix4x4 m, double[] q)
    {
        float m11 = m.m00;
        float m12 = m.m01;
        float m13 = m.m02;
        float m21 = m.m10;
        float m22 = m.m11;
        float m23 = m.m12;
        float m31 = m.m20;
        float m32 = m.m21;
        float m33 = m.m22;

        // 最大成分を検索
        float[] elem = new float[4]; // 0:x, 1:y, 2:z, 3:w
        elem[0] = m11 - m22 - m33 + 1.0f;
        elem[1] = -m11 + m22 - m33 + 1.0f;
        elem[2] = -m11 - m22 + m33 + 1.0f;
        elem[3] = m11 + m22 + m33 + 1.0f;

        int biggestIndex = 0;
        for (int i = 1; i < 4; i++)
        {
            if (elem[i] > elem[biggestIndex])
                biggestIndex = i;
        }

        if (elem[biggestIndex] < 0.0f)
            return false; // 引数の行列に間違いあり！

        // 最大要素の値を算出
        double v = Mathf.Sqrt(elem[biggestIndex]) * 0.5f;
        q[biggestIndex] = v;
        double mult = 0.25f / v;

        switch (biggestIndex)
        {
            case 0: // x
                q[1] = (m12 + m21) * mult;
                q[2] = (m31 + m13) * mult;
                q[3] = (m23 - m32) * mult;
                break;
            case 1: // y
                q[0] = (m12 + m21) * mult;
                q[2] = (m23 + m32) * mult;
                q[3] = (m31 - m13) * mult;
                break;
            case 2: // z
                q[0] = (m31 + m13) * mult;
                q[1] = (m23 + m32) * mult;
                q[3] = (m12 - m21) * mult;
                break;
            case 3: // w
                q[0] = (m23 - m32) * mult;
                q[1] = (m31 - m13) * mult;
                q[2] = (m12 - m21) * mult;
                break;
        }


        return true;
    }

}
