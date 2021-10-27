using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RTSEngine;

/* Building Construction Effect created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngineDemo
{
    [RequireComponent(typeof(BuildingHealth))]
    public class BuildingConstructionEffect : MonoBehaviour
    {
        [SerializeField]
        private bool destroyOnDisable = true; //destroy this component when it's disabled?

        [SerializeField]
        private Transform constructionTransform = null; //the main construction object of the building goes here.

        [SerializeField]
        private float maxHeight = 2.0f; //the maximum height (position on the y axis) that the construction object can reach

        [SerializeField]
        private float initialHeight = -1.0f; 
        private float targetHeight; //the current height its trying to reach
        public void UpdateTargetHeight () {
            targetHeight = ((float)building.HealthComp.CurrHealth / (float)building.HealthComp.MaxHealth) * maxHeight + initialHeight;
        }

        [SerializeField]
        private float smoothTime = 0.5f; //how smooth is the height transition
        private float currentVelocity = 0.0f; //required for the smooth damp

        Building building;

        private void FixedUpdate()
        {
            Vector3 nextPosition = constructionTransform.localPosition;
            nextPosition.y = Mathf.SmoothDamp(nextPosition.y, targetHeight,ref currentVelocity, smoothTime); //smoothly update the height
            constructionTransform.localPosition = nextPosition; //update the construction object's position
        }

        public void Toggle(GameManager gameMgr, bool enable)
        {
            enabled = enable;

            if (enabled == false)
            {
                constructionTransform.localPosition = new Vector3(constructionTransform.localPosition.x, maxHeight + initialHeight, constructionTransform.localPosition.z); //set the construction object height to the max
                if (destroyOnDisable)
                    Destroy(this); //disable the component by destroying it
            }
            else
            {
                if (constructionTransform == null) //if no construction object was assigned, disable this component
                    Toggle(null, false);

                building = GetComponent<Building>();

                smoothTime /= gameMgr.GetSpeedModifier();

                //initial values for the following height attributes
                constructionTransform.localPosition = new Vector3(constructionTransform.localPosition.x, initialHeight, constructionTransform.localPosition.z);
                targetHeight = initialHeight;
                maxHeight -= initialHeight;
            }
        }
    }
}
