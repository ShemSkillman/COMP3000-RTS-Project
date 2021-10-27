using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* Attack LOS (Line of Sight) script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    [System.Serializable]
    public class AttackLOS
    {
        [SerializeField]
        private bool enable = true; //can the attack entity attack only if the target is in line of sight? 

        [SerializeField]
        private bool useWeaponObject = false; //use the weapon object instead of the attack entity's main object as the reference for the line of sight

        [SerializeField]
        private float LOSAngle = 40.0f; //the close this value to 0.0f, the closer must the attack entity face its target

        [SerializeField, Tooltip("Define layers for obstacles that block the line of sight.")]
        private LayerMask obstacleLayerMask;

        //Ignore one or more axis while considering LOS?
        [SerializeField]
        private bool ignoreRotationX = false;
        [SerializeField]
        private bool ignoreRotationY = false;
        [SerializeField]
        private bool ignoreRotationZ = false;

        public bool IsInSight (Vector3 targetPosition, Transform weaponTransform, Transform attackerTransform)
        {
            if (enable == false) //if this is not enabled or the attacker already entered in sight of the target, true!
                return true;

            //use the weapon object or the attacker's object as the reference for the line of sight:
            Transform transformRef = (useWeaponObject == true) ? weaponTransform : attackerTransform;
            Vector3 lookAt = targetPosition - transformRef.position;

            //Which axis to ignore when checking for LOS?
            if (ignoreRotationX == true)
                lookAt.x = 0.0f;
            if (ignoreRotationY == true)
                lookAt.y = 0.0f;
            if (ignoreRotationZ == true)
                lookAt.z = 0.0f;

            //if the angle is below the allowed LOS Angle then the attacker is in line of sight of the target
            //and make sure there are no obstacles in the way of the attacker
            return Vector3.Angle(transformRef.forward, lookAt) <= LOSAngle && !Physics.Linecast(transformRef.position, targetPosition, obstacleLayerMask);
        }
    }
}
