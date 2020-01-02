using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisableAfterDelay : MonoBehaviour
{
    public float TimeToDisable = 5;
    private float timer;
    private void OnEnable()
    {
        timer = 0;
    }
    private void Update()
    {
        timer += Time.deltaTime;
        if (timer >= TimeToDisable)
        {
            gameObject.SetActive(false);
        }
    }
}
