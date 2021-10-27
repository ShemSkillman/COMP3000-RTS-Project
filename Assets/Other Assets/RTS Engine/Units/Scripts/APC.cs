using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using RTSEngine.EntityComponent;
using System.Linq;

/* APC script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    [RequireComponent(typeof(FactionEntity))]
	public class APC : MonoBehaviour, IAddableUnit
    {
        public FactionEntity FactionEntity { private set; get; } //the main unit/building component

        [Header("Adding Units"), SerializeField]
        private Transform[] interactionPosition = new Transform[0]; //position where units get in/off the APC

        [SerializeField]
        private bool forceGroundPosition = true; //when enabled, the interaction position will be transformed into a ground position always

        /// <summary>
        /// Position at which a unit can be added to the APC when it reaches it.
        /// </summary>
        public Vector3 AddablePosition
        {
            get { return !forceGroundPosition ? interactionPosition[0].position : gameMgr.TerrainMgr.GetGroundTerrainPosition(interactionPosition[0].position); }
        }

        [SerializeField, Tooltip("Only in the case where this component is attached to a free faction entity is it allowed to accept units from any faction.")]
        private bool acceptAllFactions = false;
        [SerializeField]
        private bool acceptAllUnits = true; //allow all units to get in the APC?
        [SerializeField]
        private bool acceptUnitsInList = true; //this determines how the APC will handle the below list if the above bool is set to false, accept units defined there or deny them?
        [SerializeField]
        private CodeCategoryField codesList = new CodeCategoryField(); //a list of the unit codes that are allowed/not allowed to get in the APC

        [SerializeField]
		private int capacity = 4; //max amount of units to be stored in the APC at the same time
        private int currCapacity = 0; //the current capacity of the APC
        public int GetCapacity () { return capacity; }
		private List<Unit> storedUnits = new List<Unit>(); //holds the current units stored in the APC unit.
        public bool IsEmpty () { return currCapacity == 0; }
        public bool IsFull () { return currCapacity >= capacity; }
        public int GetCount () { return storedUnits.Count;  } //how many units are currently stored in the APC?
        public Unit GetStoredUnit (int id) { //get a reference to a unit that's stored inside the APC
            if (id >= 0 && id < storedUnits.Count)
                return storedUnits[id];
            return null;
        }
        public IEnumerable<Unit> GetStoredUnits () { return storedUnits; }

        [SerializeField, Tooltip("What audio clip to play when a unit goes into the APC?")]
        private AudioClipFetcher addUnitAudio = new AudioClipFetcher(); //audio clip played when a unit gets in the APC

        [Header("Ejecting Units"), SerializeField]
        private bool canEjectSingleUnit = true; //can the player eject single units?
        [SerializeField]
        private int ejectSingleUnitTaskCategory = 0; //the category ID of ejecting a single unit task. 

        [SerializeField]
        private bool canEjectAllUnits = true; //true when the APC is allowed to eject units all at once
        [SerializeField]
        private int ejectAllUnitsTaskCategory = 1; //the category ID of ejecting all units at once
        [SerializeField]
        private Sprite ejectAllUnitsIcon = null; //The icon of the task of ejecting all units at once
        public Sprite GetEjectAllUnitsIcon () { return ejectAllUnitsIcon; }

        public bool CanEject (bool allUnits) { return allUnits == true ? canEjectAllUnits : canEjectSingleUnit; }
        public int GetEjectTaskCategory (bool allUnits) { return allUnits == true ? ejectAllUnitsTaskCategory : ejectSingleUnitTaskCategory;  }

        [SerializeField, Tooltip("What audio clip to play when a unit is ejected from the APC?")]
        private AudioClipFetcher ejectUnitAudio = new AudioClipFetcher(); //audio clip played when a unit is removed from the APC

        [SerializeField]
        private bool ejectOnDestroy = true; //if true, all units will be released on destroy, if false, all contained units will be destroyed.

        [Header("Calling Units"),SerializeField]
        private bool canCallUnits = true; //can the APC call units to get them inside?
        public bool CanCallUnits () { return canCallUnits; }
        [SerializeField]
        private int callUnitsTaskCategory = 0; //the category ID of calling all units task
        public int GetCallUnitsTaskCategory() { return callUnitsTaskCategory; }
        [SerializeField]
        private float callUnitsRange = 20.0f; //the range at which units will be called to get into the APC
        [SerializeField]
        private Sprite callUnitsIcon = null; //The task's icon that will eject all the contained units when launched.
        public Sprite GetCallUnitsIcon () { return callUnitsIcon; }
        [SerializeField]
        private bool callIdleOnly = false; //call units that are in idle mode only
        [SerializeField]
        private bool callAttackUnits = false; //call units that have an attack component?

        [SerializeField, Tooltip("What audio clip to play when the APC calls units in range?")]
        private AudioClipFetcher callUnitsAudio = new AudioClipFetcher(); //audio clip played when the APC is calling units

        //other components
        GameManager gameMgr;

        public void Init(GameManager gameMgr, FactionEntity source)
        {
            this.gameMgr = gameMgr;
            FactionEntity = source;

            if (interactionPosition.Length == 0) //no interaction position is assigned
                interactionPosition = new Transform[] { transform }; //assign the interaction position to the faction entity's position

            currCapacity = 0;
        }

        //a method to check whether a unit can be added to this APC or not
        public bool CanAddUnit (Unit unit)
        {
            if (FactionEntity.EntityHealthComp.IsDead() == true)
                return false;

            if (acceptAllUnits)
                return true;

            return acceptUnitsInList == codesList.Contains(unit.GetCode(), unit.GetCategory()); //if accept units in list is disabled and the codes list is empty then true
        }

        /// <summary>
        /// Move a unit towards the interaction position of the APC to be added.
        /// </summary>
        /// <param name="unit">Unit instance to add to the APC.</param>
        /// <param name="playerCommand">True if the method was called through a direct player command, otherwise false.</param>
        /// <returns>ErrorMessage.none if the unit can be moved to be added to the APC (fullfils certain conditions), otherwise failure error code.</returns>
        public ErrorMessage Move (Unit unit, bool playerCommand)
        {
            if ((FactionEntity.FactionID != unit.FactionID && !FactionEntity.IsFree()) || (FactionEntity.IsFree() && !acceptAllFactions)) //doesn't belong to the same faction
                return ErrorMessage.targetDifferentFaction;
            else if (FactionEntity.Type == EntityTypes.building && ((Building)FactionEntity).IsBuilt == false)
                return ErrorMessage.buildingNotBuilt;
            else if (FactionEntity.EntityHealthComp.IsDead() == true) //APC is dead
                return ErrorMessage.targetDead;
            else if (IsFull()) //max capacity reached
                return ErrorMessage.targetMaxCapacityReached;
            else if (unit.APCComp != null || !CanAddUnit(unit))
                return ErrorMessage.entityNotAllowed;

            gameMgr.MvtMgr.Move(unit, interactionPosition.OrderBy(t => Vector3.Distance(t.position, unit.transform.position)).FirstOrDefault().position, 0.0f, FactionEntity, InputMode.addUnit, playerCommand);
            return ErrorMessage.none;
        }

        /// <summary>
        /// Stores a unit inside the APC.
        /// </summary>
        /// <param name="unit">Unit instance to add to the APC.</param>
        /// <returns>ErrorMessage.none if the unit is successfully added to the APC, otherwise failure error code.</returns>
		public ErrorMessage Add (Unit unit)
		{
            if (IsFull()) //if the APC gets full, then do not add unit
                return ErrorMessage.targetMaxCapacityReached;

            unit.gameObject.SetActive(false); //hide unit object
            storedUnits.Add(unit); //add it to the stored units list
            unit.transform.SetParent(transform, true); //make it a child object of the APC object (so that it moves with the APC)
            unit.currAPC = this;

            unit.CancelJob(Unit.jobType.all); //cancel all the units jobs
            unit.Interactable = false; //unit is no longer interactable since it's inside the APC.

            gameMgr.SelectionMgr.Selected.Remove(unit); //deselect unit in case it was selected

            currCapacity += unit.GetAPCSlots(); //increase the APC's current capacity

            gameMgr.AudioMgr.PlaySFX(FactionEntity.AudioSourceComp, addUnitAudio.Fetch(), false);

            CustomEvents.OnAPCAddUnit(this, unit); //trigger custom event

            return ErrorMessage.none;
		}

        //a method called to eject all units
        public void EjectAll (bool destroyed)
        {
            if (GameManager.MultiplayerGame == false) //if this is a singleplayer game then go ahead directly
                EjectAllLocal(destroyed);
            else if (RTSHelper.IsLocalPlayer(FactionEntity)) //multiplayer game and this is the APC's owner
            {
                NetworkInput newInput = new NetworkInput()
                {
                    sourceMode = (byte)InputMode.APC,
                    targetMode = (byte)InputMode.APCEjectAll,
                    value = destroyed == true ? 1 : 0
                };

                InputManager.SendInput(newInput, FactionEntity, null); //send input to input manager
            }
        }

        //a method called to eject all units
        public void EjectAllLocal(bool destroyed)
        {
            while(storedUnits.Count > 0) //go through all stored units and remove them
                EjectLocal(storedUnits[0], destroyed);
        }

        //a method called to eject one unit
        public void Eject (Unit unit, bool destroyed)
        {
            if (unit == null || storedUnits.Contains(unit) == false) //invalid unit or unit that's not stored here?
                return;

            if (GameManager.MultiplayerGame == false) //if this is a singleplayer game then go ahead directly
                EjectLocal(unit, destroyed);
            else if (RTSHelper.IsLocalPlayer(FactionEntity)) //multiplayer game and this is the APC's owner
            {
                NetworkInput newInput = new NetworkInput()
                {
                    sourceMode = (byte)InputMode.APC,
                    targetMode = (byte)InputMode.APCEject,
                    value = destroyed == true ? 1 : 0
                };

                InputManager.SendInput(newInput, FactionEntity, unit); //send input to input manager
            }
        }

        //a method called to eject one unit locally
        public void EjectLocal(Unit unit, bool destroyed)
        {
            if (unit == null || storedUnits.Contains(unit) == false) //invalid unit or unit that's not stored here?
                return;

            //unit can not move in the ejection position!
            if (!gameMgr.MvtMgr.IsAreaMovable(AddablePosition, unit.MovementComp.Controller.Radius, unit.MovementComp.Controller.AreaMask))
                return;

            unit.transform.position = AddablePosition; //set the unit position to the interaction position
            unit.transform.SetParent(null, true); //APC is no longer the parent of the unit object
            storedUnits.Remove(unit); //remove unit from the list
            unit.gameObject.SetActive(true); //activate object
            unit.currAPC = null;

            unit.Interactable = true; //unit is no longer interactable since it's inside the APC.

            currCapacity -= unit.GetAPCSlots(); //decrease the APC's current capacity

            gameMgr.AudioMgr.PlaySFX(FactionEntity.AudioSourceComp, ejectUnitAudio.Fetch(), false);

            CustomEvents.OnAPCRemoveUnit(this, unit); //trigger custom event

            //if the APC is marked as destroyed and units are supposed to be destroyed as well
            if (destroyed == true && ejectOnDestroy == false)
                unit.HealthComp.DestroyFactionEntity(false); //destroy unit

        }

		//method called when the APC requests nearby units to enter.
		public void CallUnits ()
		{
			gameMgr.AudioMgr.PlaySFX(FactionEntity.AudioSourceComp, callUnitsAudio.Fetch(), false); //play the call for units audio.

			foreach (Unit u in FactionEntity.FactionMgr.GetUnits()) { //go through the faction's units

                if (currCapacity >= capacity) //if there are no longer free positions
                    break; //stop

                float distance = Vector3.Distance(transform.position, u.transform.position);

                //if the APC calls idle units only and the current unit is not idle or the APC doesn't call attack units and the current unit has an attack component
                if (CanAddUnit(u) == false || (callIdleOnly == true && u.IsIdle() == false) || (callAttackUnits == false && u.AttackComp != null))
                    continue; //move to next unit in loop

                //the target unit can't be another APC, it must be active, alive and inside the calling range
                if (u.APCComp == null && u.gameObject.activeInHierarchy == true && u.HealthComp.IsDead() == false && distance <= callUnitsRange)
                    gameMgr.MvtMgr.Move(u, interactionPosition.OrderBy(t => Vector3.Distance(t.position, u.transform.position)).FirstOrDefault().position, 0.0f, FactionEntity, InputMode.addUnit, false);

                //trigger custom event:
                CustomEvents.OnAPCCallUnit(this, u);
			}

		}
	}
}