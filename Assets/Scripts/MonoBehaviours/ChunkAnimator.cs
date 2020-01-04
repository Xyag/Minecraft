using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkAnimator : MonoBehaviour
{
    private const float LERP_SPEED = 5.0f;
    private const float LERP_FINISH_TOLERANCE = 0.99f;
    private void OnEnable()
    {
        transform.localScale = new Vector3(0, 0, 0);  
    }
    private void Update()
    {
        if (transform.localScale.x > LERP_FINISH_TOLERANCE)
        {
            transform.localScale = Vector3.one;
        }
        else
        {
            transform.localScale = Mathf.Lerp(transform.localScale.x, 1, LERP_SPEED * Time.deltaTime) * Vector3.one;
        }
    }
}
