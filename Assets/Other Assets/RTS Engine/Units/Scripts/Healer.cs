using UnityEngine;

using RTSEngine.Animation;

/* Healer script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    public class Healer : UnitComponent<Unit>
    {
        [SerializeField]
        private float stoppingDistance = 5.0f; //when assigned a target unit, this is the stopping distance that the healer will have
        [SerializeField]
        private float maxDistance = 7.0f; //the maximum distance between the healer and the target unit to heal.

        [SerializeField]
        public int healthPerSecond = 5; //amount of health to give the target unit per second.

        [SerializeField, Tooltip("What audio clip to play when a healing is in progress?")]
        private AudioClipFetcher healingAudio = new AudioClipFetcher();

        //a method that stops the unit from healing
        public override bool Stop()
        {
            Unit lastTarget = target;

            if (base.Stop() == false)
                return false;

            CustomEvents.OnUnitStopHealing(unit, lastTarget); //trigger custom event

            return true;
        }

        //update component if the healer has a target unit
        protected override bool OnActiveUpdate(float reloadTime, UnitAnimatorState activeAnimState, AudioClip inProgressAudio, bool breakCondition = false, bool inProgressEnableCondition = true, bool inProgressCondition = true)
        {
            if (base.OnActiveUpdate(
                1.0f,
                UnitAnimatorState.healing,
                healingAudio.Fetch(),
                //if target has max health the healer and the target don't have the same faction or the target is outside the max allowed range for healing -> cancel job
                target.HealthComp.IsDead()
                    || target.HealthComp.CurrHealth >= target.HealthComp.MaxHealth
                    || target.FactionID != unit.FactionID
                    || (Vector3.Distance(transform.position, target.transform.position) > maxDistance && inProgress == true),
                //start converting as soon as we get into the stopping distance.
                Vector3.Distance(transform.position, target.transform.position) <= stoppingDistance + target.GetRadius() 
                ) == false)
                return false;

            return true;
        }

        //a method that is called when the healer arrives at the target unit to heal
        protected override void OnInProgressEnabled(float reloadTime, UnitAnimatorState activeAnimState, AudioClip inProgressAudio)
        {
            base.OnInProgressEnabled(reloadTime, activeAnimState, inProgressAudio);

            CustomEvents.OnUnitStartHealing(unit, target); //trigger custom event.
        }

        //a method that is called when the healer achieved progress in healing
        protected override void OnInProgress()
        {
            base.OnInProgress();

            target.HealthComp.AddHealth(healthPerSecond, unit); //add health points to the target unit
        }

        //update component when the healer doesn't have a target unit
        protected override void OnInactiveUpdate()
        {
            base.OnInactiveUpdate();
        }

        /// <summary>
        /// Determines whether a unit can be healed by the healer or not.
        /// </summary>
        /// <param name="target">Unit instance to test.</param>
        /// <returns>ErrorMessage.none if the unit can be healed, otherwise the error that explains the invalidity of the target.</returns>
        public override ErrorMessage IsTargetValid (Unit target)
        {
            if (target == null)
                return ErrorMessage.invalid;
            else if (!target.gameObject.activeInHierarchy)
                return ErrorMessage.inactive;
            else if (unit.FactionID != target.FactionID)
                return ErrorMessage.targetDifferentFaction;
            else if (target.HealthComp.IsDead() == true)
                return ErrorMessage.targetDead;
            else if (target.HealthComp.CurrHealth >= target.HealthComp.MaxHealth)
                return ErrorMessage.targetMaxHealth;

            return ErrorMessage.none;
        }

        //a method that sets the target unit to heal
        public override ErrorMessage SetTarget(Unit newTarget, InputMode targetMode = InputMode.none)
        {
            ErrorMessage errorMsg = IsTargetValid(newTarget); 
            if (errorMsg != ErrorMessage.none) //if the new target is not a valid
                return errorMsg; //do not allow to set it as the target

            return base.SetTarget(newTarget, InputMode.heal);
        }

        //a method that sets the healing target locally
        public override void SetTargetLocal(Unit newTarget)
        {
            if (newTarget == null || newTarget == target)
                return;

            if (newTarget.FactionID == unit.FactionID && newTarget.HealthComp.CurrHealth < newTarget.HealthComp.MaxHealth) //Make sure the new target has the same faction ID as the healer and it doesn't have max health
            {
                Stop(); //stop healing the current unit

                //set new target
                inProgress = false;
                target = newTarget;

                gameMgr.MvtMgr.MoveLocal(unit, target.transform.position, stoppingDistance, target, InputMode.unit, false); //move the unit towards the target unit
            }
        }
    }
}