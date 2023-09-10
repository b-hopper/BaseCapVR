using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextFacer : MonoBehaviour
{
    Camera mainCamera;
    private void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("No main camera found");
            Destroy(this);
        }
    }

    private void Update()
    {
        CheckTextFlip();
    }
    
    private void CheckTextFlip()
    { // This isn't perfect, but it's good enough for now
        if (Vector3.Dot(mainCamera.transform.forward, transform.parent.forward) < 0)
        {
            transform.localRotation = Quaternion.Euler(0, 180, 0);
        }
        else
        {
            transform.localRotation = Quaternion.Euler(0, 0, 0);
        }
    }
}
