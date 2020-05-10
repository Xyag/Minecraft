using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SunFollower : MonoBehaviour
{
    public Transform Sun;
    public Transform Following;
    public float Distance;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Following == null)
            return;
        transform.position = Following.position;
        transform.rotation = Sun.rotation;
        transform.Translate(Vector3.back * Distance, Space.Self);
        transform.LookAt(Following);
    }
}
