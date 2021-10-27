using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace RTSEngine
{
	public class UnitManager : MonoBehaviour {

        [SerializeField]
		private List<Unit> freeUnits = new List<Unit>(); //units that don't belong to any faction here.
        public IEnumerable<Unit> GetFreeUnits () { return freeUnits; }
        [SerializeField]
		private Color freeUnitColor = Color.black;
        public Color GetFreeUnitColor() { return freeUnitColor; }

        [SerializeField]
        private AnimatorOverrideController defaultAnimController = null; //default override animation controller: used when there's no animation override controller assigned to a unit
        public AnimatorOverrideController GetDefaultAnimController() { return defaultAnimController; }

        //a list of all units (alive):
        private List<Unit> allUnits = new List<Unit>();
        public IEnumerable<Unit> GetAllUnits () { return allUnits; }

        //other components
        private GameManager gameMgr;

		public void Init (GameManager gameMgr)
		{
            this.gameMgr = gameMgr;

            CustomEvents.UnitCreated += AddUnit;
            CustomEvents.UnitDead += RemoveUnit;
            CustomEvents.UnitInstanceUpgraded += RemoveUnit;
            CustomEvents.UnitConversionStart += OnUnitConversionStart;
		}

        //called by the game manager after initializing the faction slots
        public void OnFactionSlotsInitialized ()
        {
            //activate free units
            foreach (Unit u in freeUnits)
                u.Init(gameMgr, -1, true, null, u.transform.position);
        }

        private void OnDestroy()
        {
            CustomEvents.UnitCreated -= AddUnit;
            CustomEvents.UnitDead -= RemoveUnit;
            CustomEvents.UnitInstanceUpgraded -= RemoveUnit;
            CustomEvents.UnitConversionStart -= OnUnitConversionStart;
        }

        //add/remove units from all the units list:
        private void AddUnit (Unit unit) {
            allUnits.Add(unit);

            if (unit.IsFree() && !freeUnits.Contains(unit)) //if this is a free unit, add it
                freeUnits.Add(unit);
        }
        private void RemoveUnit (Unit unit) {
            allUnits.Remove(unit);

            if (unit.IsFree()) //if this is a free unit, remove it
                freeUnits.Remove(unit);
        }
        private void OnUnitConversionStart (Unit source, Unit target)
        {
            if (target.IsFree()) //if the source unit was free
                freeUnits.Remove(target); //remove it from the free units list
        }

        public Unit CreateUnit(Unit unitPrefab, Vector3 spawnPosition, Quaternion spawnRotation, Vector3 gotoPosition, int factionID, Building createdBy, bool free = false, bool updatePopluation = true)
        {
            if (GameManager.MultiplayerGame == false) //single player game 
                return CreateUnitLocal(unitPrefab, spawnPosition, spawnRotation, gotoPosition, factionID, createdBy, free, updatePopluation); //directly create new unit instance
            else //if this is a multiplayer
            {
                //if it's a MP game, then ask the server to spawn the unit.
                //send input action to the input manager
                NetworkInput NewInputAction = new NetworkInput
                {
                    sourceMode = (byte)InputMode.create,
                    targetMode = (byte)InputMode.unit,
                    initialPosition = spawnPosition,
                    targetPosition = gotoPosition,
                    value = free ? 0 : (updatePopluation ? 2 : 1) //free unit: 0, faction unit without population update: 1, faction unit with population update: 2
                };
                InputManager.SendInput(NewInputAction, unitPrefab, createdBy); //send to input manager

                return null;
            }
        }

        public Unit CreateUnitLocal (Unit unitPrefab, Vector3 spawnPosition, Quaternion spawnRotation, Vector3 gotoPosition, int factionID, Building createdBy, bool free = false, bool updatePopulation = true)
        {
            //only if the prefab is valid:
            if (unitPrefab == null)
                return null;

            // create the new unit:
            unitPrefab.gameObject.GetComponent<NavMeshAgent>().enabled = false; //disable this component before spawning the unit as it might place the unit in an unwanted position when spawned

            Unit newUnit = Instantiate(unitPrefab.gameObject, spawnPosition, spawnRotation).GetComponent<Unit>();

            newUnit.Init(gameMgr, factionID, free, createdBy, gotoPosition);

            if(!free) //if this is not a free unit
            {
                if (updatePopulation) //update faction population
                {
                    gameMgr.GetFaction(factionID).UpdateCurrentPopulation(newUnit.GetPopulationSlots()); //update population slots
                    newUnit.FactionMgr.UpdateLimitsList(newUnit.GetCode(), newUnit.GetCategory(), true);
                }
                //TO BE CHANGED
                /*else //do not update faction population 
                    newUnit.SetPopulationSlots(0); //reset population slots so it won't be removed when the unit is destroyed.*/ 
            }
            

            return newUnit;
        }
    }
}