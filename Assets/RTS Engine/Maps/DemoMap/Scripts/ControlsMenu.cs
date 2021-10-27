using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngineDemo
{
    public class ControlsMenu : MonoBehaviour
    {
        [SerializeField]
        private GameObject controlsMenu = null;

        public void ToggleControlsMenu ()
        {
            controlsMenu.SetActive(!controlsMenu.gameObject.activeInHierarchy);
        }
    }
}
