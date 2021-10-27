using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RTSEngine
{
    [System.Serializable]
    public class PeaceTimeUI
    {
        [SerializeField]
        private GameObject panel = null;
        [SerializeField]
        private Text timeText = null;

        //enable/disable the peace time UI
        public void Toggle (bool enable)
        {
            panel.SetActive(enable);
        }

        //update the peace time text to display the current peace time:
        public void Update (float currTime)
		{
            if (timeText == null || currTime <= 0.0f)
                return; //do not proceed if the peace time text is not assigned or the peace time is invalid

            timeText.text = RTSHelper.TimeToString(currTime);
		}
    }
}
