using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/* Hover Health Bar script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    /// <summary>
    /// Attached to the UI canvas that holds the hover health bar, handles displaying the hover health bar at the main camera.
    /// </summary>
    public class HoverHealthBar : MonoBehaviour
    {
        //make sure the hover health bar is always looking at the camera
        [SerializeField, Tooltip("Main camera in the game.")]
        private Transform mainCamTransform = null;

        void Update()
        {
            //move the canvas in order to face the camera and look at it
            transform.LookAt(transform.position + mainCamTransform.rotation * Vector3.forward,
                mainCamTransform.rotation * Vector3.up);
        }
    }
}
