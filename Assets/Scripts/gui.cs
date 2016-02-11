using UnityEngine;
using System.Collections;

public class gui : MonoBehaviour {

    public ProjectionWindow window;
    public ProCamManager procamManager;

    private string proWidth = "1280";
    private string proHeight = "800";
    private string camWidth = "1920";
    private string camHeight = "1080";
    private string num = "1";

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
        }
        if (GUI.Button(new Rect(20, 70, 150, 20), "投影"))
        {
            window.callProjection(int.Parse(proWidth), int.Parse(proHeight), int.Parse(num));     // 投影の切り替え
        }
        if (GUI.Button(new Rect(20, 90, 150, 20), "PCL Viewer"))
        {
            procamManager.callPCLViewer();
        }


        GUI.TextField(new Rect(170, 100, 100, 20), "Camera width");
        camWidth = GUI.TextField(new Rect(270, 100, 50, 20), camWidth);
        GUI.TextField(new Rect(170, 120, 100, 20), "Camera height");
        camHeight = GUI.TextField(new Rect(270, 120, 50, 20), camHeight);

        GUI.TextField(new Rect(20, 100, 100, 20), "Projector width");
        proWidth = GUI.TextField(new Rect(120, 100, 50, 20), proWidth);
        GUI.TextField(new Rect(20, 120, 100, 20), "Projector height");
        proHeight = GUI.TextField(new Rect(120, 120, 50, 20), proHeight);
        GUI.TextField(new Rect(20, 140, 100, 20), "Projector Num");
        num = GUI.TextField(new Rect(120, 140, 50, 20), num);

        //投影背景
        //GUI.DrawTexture(new Rect(0, 0, float.Parse(camWidth), float.Parse(camHeight)), BackGround, ScaleMode.ScaleToFit);

    }
}
