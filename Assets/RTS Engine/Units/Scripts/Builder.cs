using UnityEngine;

using RTSEngine.Animation;

/* Builder script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
	public class Builder : UnitComponent<Building> {

        [SerializeField]
        private bool constructFreeBuildings = false; //can the builder construct free buildings that do not belong to any faction?
        public bool CanConstructFreeBuilding () { return constructFreeBuildings; }

        [SerializeField]
		private int healthPerSecond = 5; //amount of health that the building will receive per second

        [SerializeField, Tooltip("What audio clip to play when constructing a building?")]
		private AudioClipFetcher constructionAudio = new AudioClipFetcher(); //played when the unit is constructing a building

        [SerializeField]
        private CodeCategoryField restrictions = new CodeCategoryField(); //if a building is in this list, then the builder won't be able to place it/construct it
        [SerializeField, Tooltip("When enabled, the restrictions will only apply to placing the buildings but the builder will still be able to construct buildings in the ")]
        private bool restrictBuildingPlacementOnly = true;

        public bool CanPlaceBuilding (Building building) //checks whether the input building can be constructed by this builder
        {
            return building != null ? !restrictions.Contains(building.GetCode(), building.GetCategory()) : false;
        }

        public override void Init(GameManager gameMgr, Unit unit)
        {
            base.Init(gameMgr, unit);

            healthPerSecond = (int)(healthPerSecond * gameMgr.GetSpeedModifier()); //get the speed modifier and set the health per second accordinly
        }

        //update component if the builder has a target
        protected override bool OnActiveUpdate(float reloadTime, UnitAnimatorState activeAnimState, AudioClip inProgressAudio, bool breakCondition = false, bool inProgressEnableCondition = true, bool inProgressCondition = true)
        {
            return base.OnActiveUpdate(
                1.0f,
                UnitAnimatorState.building,
                constructionAudio.Fetch(),
                target.HealthComp.IsDead() || target.HealthComp.CurrHealth >= target.HealthComp.MaxHealth,
                unit.MovementComp.DestinationReached);
        }

        //a method that is called when the builder arrives at the target building to construct
        protected override void OnInProgressEnabled(float reloadTime, UnitAnimatorState activeAnimState, AudioClip inProgressAudio)
        {
            base.OnInProgressEnabled(reloadTime, activeAnimState, inProgressAudio);

            CustomEvents.OnUnitStartBuilding(unit, target); //trigger custom event.
        }

        //a method that is called when the builder achieved progress in construction
        protected override void OnInProgress()
        {
            base.OnInProgress();

            target.HealthComp.AddHealth(healthPerSecond, unit); //add health points to the building.
        }

        //update component if the builder doesn't have a target
        protected override void OnInactiveUpdate()
        {
            base.OnInactiveUpdate();
        }

        //a method that stops the builder from constructing
        public override bool Stop ()
        {
            Building lastTarget = target;

            if (base.Stop() == false)
                return false;

            if(lastTarget) //if there was a target building
            {
                lastTarget.WorkerMgr.Remove(unit);//remove the unit from the worker manager

                CustomEvents.OnUnitStopBuilding(unit, lastTarget); //trigger custom event
            }
            return true;
        }

        /// <summary>
        /// Determines whether a building can be constructed by the builder or not.
        /// </summary>
        /// <param name="building">Building instance to test.</param>
        /// <returns>RTSEngine.ErrorMessage.none if the building can be constructed, otherwise the error that explains the invalidity of the target.</returns>
        public override ErrorMessage IsTargetValid (Building building)
        {
            if (building == null)
                return ErrorMessage.invalid;
            else if (!building.gameObject.activeInHierarchy)
                return ErrorMessage.inactive;
            else if (!building.Placed) //can't construct a building that's not placed yet
                return ErrorMessage.buildingNotPlaced;
            else if (!restrictBuildingPlacementOnly
                && restrictions.Contains(building.GetCode(), building.GetCategory())) //if the builder is not allowed to construct the input building
                return ErrorMessage.entityNotAllowed;
            else if (building.FactionID != unit.FactionID) //different faction
                return ErrorMessage.targetDifferentFaction;
            else if (building.HealthComp.CurrHealth >= building.HealthComp.MaxHealth) //max health already
                return ErrorMessage.targetMaxHealth;
            else if (building.WorkerMgr.currWorkers >= building.WorkerMgr.GetAvailableSlots()) //max workers reached
                return ErrorMessage.targetMaxWorkers;

            return ErrorMessage.none;
        }

        //a method that sets the target building to construct
        public override ErrorMessage SetTarget (Building newTarget, InputMode targetMode = InputMode.none)
        {
            ErrorMessage errorMsg; 
            if ((errorMsg = IsTargetValid(newTarget)) != ErrorMessage.none) //if the new target is not a valid
                return errorMsg; //do not allow to set it as the target

            return base.SetTarget(newTarget, InputMode.builder);
        }

        //a method that sets the target building to construct locally
        public override void SetTargetLocal (Building newTarget)
		{
			if (newTarget == null || target == newTarget || newTarget.WorkerMgr.currWorkers >= newTarget.WorkerMgr.GetAvailableSlots()) //if the new target is invalid or it's already the builder's target
				return; //do not proceed

            Stop(); //stop constructing the current building

            //set new target
            inProgress = false;
            target = newTarget;

            gameMgr.MvtMgr.MoveLocal(unit, target.WorkerMgr.Add(unit), target.GetRadius(), target, InputMode.building, false); //move the unit towards the target building

            CustomEvents.OnUnitBuildingOrder(unit, target); //trigger custom event

		}
	}
}