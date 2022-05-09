using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChoppedTree : MonoBehaviour
{
    [SerializeField] float fallForce = 50f;

    void Start()
    {
        Rigidbody rb = GetComponent<Rigidbody>();

        Vector3 dir = new Vector3((Random.value * 2) - 1, 0f, (Random.value * 2) - 1);
        dir.Normalize();
        dir.y = 1f;

        rb.AddForce(dir * fallForce);
    }
}
