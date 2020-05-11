using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CloseableUIPanel : MonoBehaviour
{
    public bool StartOpen = false;
    public bool Open { get { return opening; } }
    public float openSpeed = 5;
    private bool opening;
    private const float inactiveCloseTolerance = 0.01f;
    void Start()
    {
        if (StartOpen)
        {
            open();
        }
        else
        {
            close();
            gameObject.SetActive(false);
            transform.localScale = new Vector3(1, 0, 1);
        }
    }

    public void Update()
    {
        if (opening)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, new Vector3(1, 1, 1), Time.fixedDeltaTime * openSpeed);
        }
        else
        {
            transform.localScale = Vector3.Lerp(transform.localScale, new Vector3(1, 0, 1), Time.fixedDeltaTime * openSpeed);
            if (transform.localScale.y < inactiveCloseTolerance)
            {
                gameObject.SetActive(false);
            }
        }
    }
    public void toggle()
    {
        if (Open)
        {
            close();
        }
        else
        {
            open();
        }
    }
    public virtual void open()
    {
        opening = true;
        gameObject.SetActive(true);
    }
    public virtual void close()
    {
        opening = false;
    }
}
