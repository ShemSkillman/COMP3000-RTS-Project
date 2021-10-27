using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* Damage Over Time script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    //holds the attributes that can be chosen from the inspector
    [System.Serializable]
    public struct DoTAttributes
    {
        public bool infinite; //does the dot stop or does it keep going until the target is destroyed?
        public float duration; //if the above option is not disabled, this is how long will the DoT last for
        public float cycleDuration; //each cycle, the damage will be applied
    }

    //when damage over time is enabled, the damage values from the above fields will be applied over time to the faction entity attached to the same game object
    [RequireComponent(typeof(FactionEntityHealth))] 
    public class DamageOverTime : MonoBehaviour
    {
        public bool IsActive { private set; get; }

        private int damage; //the damage value will be copied because DoT even if the attacker is destroyed (where accessing the damage value will not be possible in that case).

        private FactionEntity source;
        private FactionEntityHealth target;

        DoTAttributes attributes;

        private float cycleTimer = 0.0f;

        //activates the damage over time attributes
        public void Init(int damage, DoTAttributes attributes,  FactionEntity source, FactionEntityHealth target)
        {
            this.damage = damage;
            this.attributes = attributes;
            this.source = source;
            this.target = target;

            cycleTimer = 0.0f;

            //activate component
            IsActive = true;
            enabled = true;
        }

        //update the damage over time until it is done
        private void Update()
        {
            if (IsActive == false || target == null || target.IsDead()) //if we have an invalid target or a dead one
            {
                Disable();
                return;
            }

            if (attributes.infinite == false) //if the dot is not supposed to be infinite
            {
                if (attributes.duration > 0) //DoT effect timer
                    attributes.duration -= Time.deltaTime;
                else
                {
                    Disable();
                    return;
                }
            }

            //within each cycle, the target (the faction entity attached to the same game object) receives damage.
            if (cycleTimer > 0)
                cycleTimer -= Time.deltaTime;
            else
            {
                target.AddHealth(-damage, source);
                cycleTimer = attributes.cycleDuration;
            }
        }

        //disable the DoT effect
        public void Disable ()
        {
            //deactivate component
            IsActive = false;
            enabled = false;
        }
    }
}
