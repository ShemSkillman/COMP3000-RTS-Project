using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RTSEngine
{
    [System.Serializable]
    public class ProgressBarUI
    {
        [SerializeField]
        private RectTransform empty = null;
        [SerializeField]
        private RectTransform full = null;

        Image imageEmpty;
        Image imageFull;

        public void Init ()
        {
            imageEmpty = empty.GetComponent<Image>();
            imageFull = full.GetComponent<Image>();
        }

        public void Toggle (bool enable)
        {
            imageEmpty.enabled = enable;
            imageFull.enabled = enable;
        }

        public void Update (float progress)
        {
            progress = Mathf.Clamp(progress, 0.0f, 1.0f); //the progress value must be always between 0.0f and 1.0f

            //set the full progress bar size to showcase the progress value
            full.sizeDelta = new Vector2(progress * empty.sizeDelta.x , full.sizeDelta.y);
            full.localPosition = new Vector3(empty.localPosition.x - (empty.sizeDelta.x - full.sizeDelta.x) / 2.0f, empty.localPosition.y, empty.localPosition.z); 
        }
    }
}
