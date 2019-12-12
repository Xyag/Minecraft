﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DayNightMover : MonoBehaviour
{
    public GameObject Sun;
    public GameObject Moon;
    [Range(0.0f, 1.0f)]
    public float Progress;
    public float Speed = 0.1f;
    public AnimationCurve SunBrightness;
    public AnimationCurve MoonBrightness;

    private Light sunLight;
    private Light moonLight;

    // Start is called before the first frame update
    void Start()
    {
        sunLight = Sun.GetComponent<Light>();
        moonLight = Moon.GetComponent<Light>();
    }

    // Update is called once per frame
    void Update()
    {
        Progress += Time.deltaTime * Speed;
        Progress = Progress % 1.0f;
        Sun.transform.rotation = Quaternion.Euler(180 + Progress * 360, -62, 0);
        Moon.transform.rotation = Quaternion.Euler(Progress * 360, -62, 0);

        moonLight.intensity = MoonBrightness.Evaluate(Progress);
        sunLight.intensity = SunBrightness.Evaluate(Progress);
    }
}
