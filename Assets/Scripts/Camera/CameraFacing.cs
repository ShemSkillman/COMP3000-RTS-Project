using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFacing : MonoBehaviour
{
    Camera cam;

    private void Awake()
    {
        cam = Camera.main;
    }

    private void Start()
    {
        transform.forward = cam.transform.forward;
    }

    void LateUpdate()
    {
        transform.forward = cam.transform.forward;
    }
}
