using UnityEngine;
using System.Collections;

public class WireFrame : MonoBehaviour
{

    // Use this for initialization
    void Start()
    {
        MeshFilter mf = GetComponent<MeshFilter>();
        mf.mesh.SetIndices(mf.mesh.GetIndices(0), MeshTopology.Lines, 0);

    }
}