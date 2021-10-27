using UnityEngine;

using RTSEngine.Animation;

/* Converter script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
	public class Converter : UnitComponent<Unit> {

        [SerializeField]
        private float stoppingDistance = 5.0f; //when assigned a target unit, this is the stopping distance that the healer will have
        [SerializeField]
        private float maxDistance = 7.0f; //the maximum distance between the healer and the target unit to heal.

        [SerializeField]
        private float duration = 15.0f; //time (in seconds) in order to complete converting a target unit

        [SerializeField, Tooltip("What audio clip to play when a conversion is ongoing?")]
        private AudioClipFetcher conversionAudio = new AudioClipFetcher();

        [SerializeField]
		public EffectObj effect = null; //effect spawned at target unit when the conversion is done

        //a method that stops the unit from converting
        public override bool Stop()
        {
            Unit lastTarget = target;

            if (base.Stop() == false)
                return false;

            CustomEvents.OnUnitStopConverting(unit, lastTarget); //trigger custom event

            return true;
        }

        //update component if the converter has a target unit
        protected override bool OnActiveUpdate(float reloadTime, UnitAnimatorState activeAnimState, AudioClip inProgressAudio, bool breakCondition = false, bool inProgressEnableCondition = true, bool inProgressCondition = true)
        {
            if (base.OnActiveUpdate(
                duration,
                UnitAnimatorState.converting,
                conversionAudio.Fetch(),
                //breaking condition:
                //if the converter and the target have the same faction or the target is outside the max allowed range for conversion -> cancel job
                target.HealthComp.IsDead()
                    || target.FactionID == unit.FactionID
                    || (Vector3.Distance(transform.position, target.transform.position) > maxDistance && inProgress == true),
                //start converting as soon as we get into the stopping distance.
                Vector3.Distance(transform.position, target.transform.position) <= stoppingDistance + target.GetRadius()
                ) == false)
                return false;

            return true;
        }

        //a method that is called when the converter arrives at the target unit to convert
        protected override void OnInProgressEnabled(float reloadTime, UnitAnimatorState activeAnimState, AudioClip inProgressAudio)
        {
            base.OnInProgressEnabled(reloadTime, activeAnimState, inProgressAudio);

            CustomEvents.OnUnitStartConverting(unit, target); //trigger custom event
        }

        //a method that is called when the converter achieved progress in conversion
        protected override void OnInProgress()
        {
            base.OnInProgress();

            target.Convert(unit, unit.FactionID); //convert target unit
            Stop(); //cancel conversion job
        }

        //update component when the converter doesn't have a target unit
        protected override void OnInactiveUpdate ()
        {
            base.OnInactiveUpdate();
        }

        /// <summary>
        /// Determines whether a unit can be converted by the converter or not.
        /// </summary>
        /// <param name="target">Unit instance to test.</param>
        /// <returns>RTSEngine.ErrorMessage.none if the unit can be converted, otherwise the error that explains the invalidity of the target.</returns>
        public override ErrorMessage IsTargetValid (Unit target)
        {
            if (target == null)
                return ErrorMessage.invalid;
            else if (!target.gameObject.activeInHierarchy)
                return ErrorMessage.inactive;
            if (target.CanBeConverted() == false)
                return ErrorMessage.targetNoConversion;
            else if (unit.FactionID == target.FactionID)
                return ErrorMessage.targetSameFaction;
            else if (target.HealthComp.IsDead() == true)
                return ErrorMessage.targetDead;

            return ErrorMessage.none;
        }
        //a method that sets the target unit to convert
        public override ErrorMessage SetTarget(Unit newTarget, InputMode targetMode = InputMode.none)
        {
            ErrorMessage errorMsg = IsTargetValid(newTarget); 
            if (errorMsg != ErrorMessage.none) //if the new target is not a valid
                return errorMsg; //do not allow to set it as the target

            return base.SetTarget(newTarget, InputMode.convertOrder);
        }

        //a method that sets the conversion target locally
        public override void SetTargetLocal (Unit newTarget)
		{
			if (newTarget == null || newTarget == target)
				return;

            Stop(); //stop converting the current unit

            //set new target
            inProgress = false;
            target = newTarget;

            gameMgr.MvtMgr.Move(unit, target.transform.position, stoppingDistance, target, InputMode.unit, false); //move the unit towards the target unit
		}

        //a method that spawns the converter's effect when it has successfully converted a unit
        public void EnableConvertEffect ()
        {
            if (effect != null) //only if there's a valid effect object
                gameMgr.EffectPool.SpawnEffectObj(effect, target.transform.position, Quaternion.identity, target.transform); //spawn the conversion effect.
        }
    }
}