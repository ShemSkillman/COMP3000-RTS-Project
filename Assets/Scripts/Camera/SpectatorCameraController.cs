using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpectatorCameraController : MonoBehaviour
{
    [SerializeField] float rotSpeed = 50;
    [SerializeField] float moveSpeed = 20;

    void Update()
    {
        transform.position += Time.deltaTime * moveSpeed * transform.forward * Input.GetAxis("Vertical");
        transform.position += Time.deltaTime * moveSpeed * transform.right * Input.GetAxis("Horizontal");

        transform.eulerAngles += Time.deltaTime * rotSpeed * new Vector3(-Input.GetAxis("Mouse Y"), Input.GetAxis("Mouse X"), 0);
    }
}
