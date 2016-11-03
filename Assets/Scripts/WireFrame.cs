using UnityEngine;
using System.Collections;

public class WireFrame : MonoBehaviour
{

    // Use this for initialization
    void Start()
    {
    }

    public void setWireFrame()
    {
        MeshFilter mf = GetComponent<MeshFilter>();
        mf.mesh.SetIndices(mf.mesh.GetIndices(0), MeshTopology.Lines, 0);

        BlendModeUtils.SetBlendMode(this.GetComponent<Renderer>().material, BlendModeUtils.Mode.Fade);
    }
}