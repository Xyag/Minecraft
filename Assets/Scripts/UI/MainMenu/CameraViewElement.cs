using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraViewElement : MonoBehaviour
{
    public float speed = 5;
    public Transform startTransform;
    public Vector3 offset = new Vector3(0, 0, -10);
    private Transform target;
    public void SetTarget(Transform target)
    {
        this.target = target;
    }
    // Start is called before the first frame update
    void Start()
    {
        target = startTransform;
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = Vector3.Lerp(transform.position, target.position + offset, speed * Time.deltaTime);
    }
}
