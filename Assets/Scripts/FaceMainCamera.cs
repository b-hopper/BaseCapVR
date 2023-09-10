using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FaceMainCamera : MonoBehaviour
{
    Camera mainCamera;
    [SerializeField] 
    bool onlyFaceY = false;
    
    void Start()
    {
        mainCamera = Camera.main;
    }
    
    void Update()
    {
        if (onlyFaceY)
        {
            transform.rotation = Quaternion.LookRotation(new Vector3(transform.position.x, mainCamera.transform.position.y, transform.position.z) - mainCamera.transform.position);
            return;
        }
        
        transform.rotation = Quaternion.LookRotation(transform.position - mainCamera.transform.position);
    }
}
