using UnityEngine;
using System.Collections;

public class FlyingObject : MonoBehaviour
{
    public float speed = 0.01f;
    public float max = 2.0f;

    // Use this for initialization
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if (this.transform.position.x >= -max && this.transform.position.x <= max)
            this.transform.Translate(speed, 0, 0);
        else
            this.transform.position = new Vector3(-max, this.transform.position.y, this.transform.position.z);
    }


}
