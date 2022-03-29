using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    [SerializeField] GameObject gameView;
    [SerializeField] GameObject spectatorView;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            gameView.SetActive(!gameView.activeInHierarchy);
            spectatorView.SetActive(!spectatorView.activeInHierarchy);
        }
    }
}
