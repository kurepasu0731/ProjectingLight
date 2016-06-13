using UnityEngine;
using System.Collections;

public class materialChanger : MonoBehaviour {

    public Material doraMaterial;
    public Texture[] doraTextures;

    private float changeTime = 1;
    private float nowTime = 0;
    private int texNo = 0;

    // Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
    void Update()
    {
        nowTime += Time.deltaTime;
        if (nowTime > changeTime)
        {
            nowTime = 0;
            texNo++;
            if (texNo >= doraTextures.Length) texNo = 0;
            doraMaterial.mainTexture = doraTextures[texNo];
        }
    }
}
