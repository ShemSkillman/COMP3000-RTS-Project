using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FPSCounter : MonoBehaviour
{
    Text text;

    int framesPassed = 0;
    float time;

    private void Awake()
    {
        text = GetComponent<Text>();
    }

    // Update is called once per frame
    void Update()
    {
        framesPassed++;
        time += Time.deltaTime;

        if (time >= 1f)
        {
            Color col;

            if (framesPassed >= 60)
            {
                col = Color.green;
            }
            else if (framesPassed >= 30)
            {
                col = Color.yellow;
            }
            else
            {
                col = Color.red;
            }

            text.text = "FPS: " + framesPassed;

            text.color = col;

            framesPassed = 0;
            time = 0f;
        }
    }
}
