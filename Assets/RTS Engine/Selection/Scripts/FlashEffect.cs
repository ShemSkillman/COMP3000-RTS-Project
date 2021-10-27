using UnityEngine;
using System.Collections;

/* Flash Effect script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    [RequireComponent(typeof(EffectObj))]
	public class FlashEffect : MonoBehaviour {

        EffectObj effectObj;

        [SerializeField]
        private bool isActive = false;
        [SerializeField]
        public float cycleDuration = 0.2f; //every time this time in seconds passes, the effect object is hidden or activated.

        void Start()
        {
            effectObj = GetComponent<EffectObj>();

            if (isActive == true)
                InvokeRepeating("Flash", 0.0f, cycleDuration);
        }

        void Flash()
        {
            if(effectObj.CurrentState == EffectObj.State.running) //as long as the game object is active
                gameObject.SetActive(!gameObject.activeInHierarchy);
        }
    }
}