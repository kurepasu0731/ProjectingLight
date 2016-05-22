using UnityEngine;
using System.Collections;

public class gui : MonoBehaviour {

    public ProjectionWindow window;
    public ProCamManager procamManager;
    public ProjectorPoseEstimation projectorposeestimationManager;
    public GameObject TargetObj;

    private string proWidth = "1280";
    private string proHeight = "800";
    private string camWidth = "1920";
    private string camHeight = "1080";
    private string num = "2";

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    void OnGUI()
    {

        if (GUI.Button(new Rect(20, 50, 150, 20), "パラメータ読み込み"))
        {
            procamManager.loadParam(int.Parse(camWidth), int.Parse(camHeight), int.Parse(proWidth), int.Parse(proHeight));
            projectorposeestimationManager.init(0, 30, int.Parse(camWidth), int.Parse(camHeight));
        }
        if (GUI.Button(new Rect(20, 70, 150, 20), "投影"))
        {
            window.callProjection(int.Parse(proWidth), int.Parse(proHeight), int.Parse(num));     // 投影の切り替え
        }
        if (GUI.Button(new Rect(170, 50, 150, 20), "ドラえもん 表示/非表示"))
        {
            TargetObj.SetActive(!TargetObj.activeInHierarchy);
        }
        if (GUI.Button(new Rect(170, 70, 150, 20), "tracking start/stop"))
        {
            //procamManager.callPCLViewer();
            projectorposeestimationManager.isTrack = !projectorposeestimationManager.isTrack;
        }


        GUI.TextField(new Rect(170, 110, 100, 20), "Camera width");
        camWidth = GUI.TextField(new Rect(270, 110, 50, 20), camWidth);
        GUI.TextField(new Rect(170, 130, 100, 20), "Camera height");
        camHeight = GUI.TextField(new Rect(270, 130, 50, 20), camHeight);

        GUI.TextField(new Rect(20, 110, 100, 20), "Projector width");
        proWidth = GUI.TextField(new Rect(120, 110, 50, 20), proWidth);
        GUI.TextField(new Rect(20, 130, 100, 20), "Projector height");
        proHeight = GUI.TextField(new Rect(120, 130, 50, 20), proHeight);
        GUI.TextField(new Rect(20, 150, 100, 20), "Projector Num");
        num = GUI.TextField(new Rect(120, 150, 50, 20), num);

        //投影背景
        //GUI.DrawTexture(new Rect(0, 0, float.Parse(camWidth), float.Parse(camHeight)), BackGround, ScaleMode.ScaleToFit);

    }
}
