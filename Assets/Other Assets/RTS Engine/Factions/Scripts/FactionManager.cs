using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace RTSEngine 
{
    public class FactionManager : MonoBehaviour {

        public int FactionID { private set; get; } //the faction ID that this manager belongs to.
        public FactionSlot Slot {private set; get;}

        private List<FactionEntity> factionEntities = new List<FactionEntity>(); //contains all faction entities (units+buildings) that belong to this faction
        public IEnumerable<FactionEntity> GetFactionEntities() { return factionEntities; } //get the faction entities enumerator
        private List<FactionEntity> enemyFactionEntities = new List<FactionEntity>(); //contains all the enemy faction entities.
        public IEnumerable<FactionEntity> GetEnemyFactionEntities() { return enemyFactionEntities; }

		//The lists below hold all different types of units.
		private List<Unit> units = new List<Unit>(); //list containing all the units that this faction owns.
        public IEnumerable<Unit> GetUnits() { return units; }
		private List<Builder> builders = new List<Builder>(); //list containing all the builders that this faction owns.
		private List<ResourceCollector> collectors = new List<ResourceCollector>(); //list containing all the resource collectors that this faction owns.
		private List<Healer> healers = new List<Healer>(); //list containing all the healers that this faction owns.
		private List<Converter> converters = new List<Converter>(); //list containing all the converters that this faction owns.
		private List<Unit> attackUnits = new List<Unit>(); //list containing all the army units this faction owns.
        public IEnumerable<Unit> GetAttackUnits(float range = 1.0f) { return attackUnits.GetRange(0, (int)(attackUnits.Count * (range >= 0.0f && range <= 1.0f ? range : 1.0f))); }
        private List<Unit> nonAttackUnits = new List<Unit>(); //list containing units that don't have an Attack component in this faction
		private List<Unit> enemyUnits = new List<Unit>(); //list containing all the enemy units.
        public IEnumerable<Unit> GetEnemyUnits () { return enemyUnits; }

        //The lists below hold buildings with different needs:
		private List<Building> buildings = new List<Building>();//list containing all the buildings this faction owns.
        public IEnumerable<Building> GetBuildings () { return buildings; }
		private List<Building> buildingCenters = new List<Building>(); //list containing all the building centers this faction owns.
        public IEnumerable<Building> GetBuildingCenters () { return buildingCenters; }
		private List<Building> dropOffBuildings = new List<Building>(); //list containing all the resource drop off buildings that this faction owns.
        public IEnumerable<Building> GetDropOffBuildings () { return dropOffBuildings; }
		private List<Building> enemyBuildings = new List<Building>(); //list containing all the enemy buildings.
        public IEnumerable<Building> GetEnemyBuildings () { return enemyBuildings; }

        //Task Launchers:
        private List<TaskLauncher> taskLaunchers = new List<TaskLauncher>();
        public IEnumerable<TaskLauncher> GetTaskLaunchers () { return taskLaunchers; }

        //holds the building/unit for for the faction type that this faction belongs to
        private List<FactionLimit> limits = new List<FactionLimit>();

		private List<Resource> resourcesInRange = new List<Resource>(); //A list of all the resources that are inside the faction's terrirtory.
        public IEnumerable<Resource> GetResourcesInRange () { return resourcesInRange; }

        //other components
        private GameManager gameMgr;

		public void Init (GameManager gameMgr, int ID, IEnumerable<FactionLimit> factionLimits, FactionSlot slot) {

            this.gameMgr = gameMgr;
            this.FactionID = ID;
            this.Slot = slot;

            this.limits.Clear();
            //add the limits that come with the faction type
            if(factionLimits != null)
            {
                foreach(FactionLimit limit in factionLimits) //go through the faction limits
                {
                    this.limits.Add(new FactionLimit //and add them to this faction manager instance
                    {
                        code = limit.code,
                        maxAmount = limit.maxAmount
                    });
                }
            }

            //start listening to events:
            CustomEvents.UnitCreated += AddUnit;
            CustomEvents.UnitConversionStart += OnUnitConversionStart;
            CustomEvents.UnitConversionComplete += OnUnitConversionComplete;
            CustomEvents.UnitDead += RemoveUnit;
            CustomEvents.UnitInstanceUpgraded += RemoveUnit;

            CustomEvents.BuildingPlaced += AddBuilding;
            CustomEvents.BuildingDestroyed += RemoveBuilding;
            CustomEvents.BuildingInstanceUpgraded += RemoveBuilding;

            CustomEvents.TaskLauncherAdded += OnTaskLauncherAdded;
            CustomEvents.TaskLauncherRemoved += OnTaskLauncherRemoved;
		}

        private void OnDisable()
        {
            //stop listening to events:
            CustomEvents.UnitCreated -= AddUnit;
            CustomEvents.UnitDead -= RemoveUnit;
            CustomEvents.UnitConversionStart -= OnUnitConversionStart;
            CustomEvents.UnitConversionComplete -= OnUnitConversionComplete;
            CustomEvents.UnitInstanceUpgraded -= RemoveUnit;

            CustomEvents.BuildingPlaced -= AddBuilding;
            CustomEvents.BuildingDestroyed -= RemoveBuilding;
            CustomEvents.BuildingInstanceUpgraded -= RemoveBuilding;

            CustomEvents.TaskLauncherAdded -= OnTaskLauncherAdded;
            CustomEvents.TaskLauncherRemoved -= OnTaskLauncherRemoved;
        }

        //before the conversion starts, remove the unit from the faction lists...
        private void OnUnitConversionStart (Unit source, Unit target)
        {
            RemoveUnit(target);
        }
        //and when the conversion is complete and the target is assigned their new faction, add them back to the lists
        private void OnUnitConversionComplete (Unit source, Unit target)
        {
            AddUnit(target);
        }

        private void AddUnit (Unit unit)
        {
            if(unit.IsFree() || unit.FactionID != FactionID) //if the created unit is from another faction
            {
                enemyUnits.Add(unit); //add it to the enemy units list
                enemyFactionEntities.Add(unit);
                return; //do not proceed
            }

            //if the unit belongs to the faction managed by this component
			units.Add (unit);
            factionEntities.Add(unit);

            //depending on what component the unit has, add it to the correspondant list:
            if (unit.BuilderComp)
                builders.Add(unit.BuilderComp);
            if (unit.CollectorComp)
                collectors.Add(unit.CollectorComp);
            if (unit.HealerComp)
                healers.Add(unit.HealerComp);
            if (unit.ConverterComp)
                converters.Add(unit.ConverterComp);

            if (unit.AttackComp)
                attackUnits.Add(unit);
            else
                nonAttackUnits.Add(unit);
        }

		//a method that removes the unit from all the lists:
		private void RemoveUnit (Unit unit)
		{
            //if this is a free unit or does not belong to this faction
            if(unit.IsFree() || unit.FactionID != FactionID)
            {
                enemyUnits.Remove(unit);
                enemyFactionEntities.Remove(unit);
                return;
            }

			units.Remove (unit);
            factionEntities.Remove(unit);

            //remove units from the lists it may belong to
            if (unit.BuilderComp)
                builders.Remove(unit.BuilderComp);
            if (unit.CollectorComp)
                collectors.Remove(unit.CollectorComp);
            if (unit.HealerComp)
                healers.Remove(unit.HealerComp);
            if (unit.ConverterComp)
                converters.Remove(unit.ConverterComp);

            if (unit.AttackComp)
                attackUnits.Remove(unit);
            else
                nonAttackUnits.Remove(unit);

            //update the limits list:
            UpdateLimitsList(unit.GetCode(), unit.GetCategory(), false);

            CheckFactionDefeat(); //check if the faction doesn't have any buildings/units anymore and trigger the faction defeat in that case
        }

        //the method that registers the building:
        private void AddBuilding (Building building)
		{
            if (building.IsFree() || building.FactionID != FactionID) //if the building doesn't belong to this faction
            {
                enemyBuildings.Add(building);
                enemyFactionEntities.Add(building);
                return;
            }

			//building is registered:
			buildings.Add (building);
            factionEntities.Add(building);

            //if this building is a drop off building, add it to the list:
            if (building.DropOffComp)
                dropOffBuildings.Add(building);

            //if this has the Border component -> it's a building center
            if (building.BorderComp)
                buildingCenters.Add(building);

            //update the limits list:
            UpdateLimitsList(building.GetCode(), building.GetCategory(), true);
		}

		private void RemoveBuilding (Building building)
		{
            if (building.IsFree() || building.FactionID != FactionID) //if the building doesn't belong to this faction
            {
                enemyBuildings.Remove(building);
                enemyFactionEntities.Remove(building);
                return;
            }

			buildings.Remove (building);
            factionEntities.Remove(building);

            if (building.DropOffComp)
                dropOffBuildings.Remove(building);

            if (building.BorderComp)
                buildingCenters.Remove(building);
            
            //update the limits list:
            UpdateLimitsList(building.GetCode(), building.GetCategory(), false);

            CheckFactionDefeat(); //check if the faction doesn't have any buildings/units anymore and trigger the faction defeat in that case
        }

        /// <summary>
        /// Called each time a new TaskLauncher instance is initialized.
        /// </summary>
        /// <param name="taskLauncher">The new added TaskLauncher instance.</param>
        /// <param name="taskID">None.</param>
        /// <param name="taskQueueID">None.</param>
        private void OnTaskLauncherAdded (TaskLauncher taskLauncher, int taskID, int taskQueueID)
        {
            if(taskLauncher.FactionEntity.FactionID == FactionID) //make sure the new task launcher belongs to the faction managed by this component
                taskLaunchers.Add(taskLauncher); //add task launcher to list.
        }

        /// <summary>
        /// Called each time a TaskLauncher instance is removed/destroyed.
        /// </summary>
        /// <param name="taskLauncher">The removed/destroyed TaskLauncher instance.</param>
        /// <param name="taskID">None.</param>
        /// <param name="taskQueueID">None.</param>
        private void OnTaskLauncherRemoved (TaskLauncher taskLauncher, int taskID, int taskQueueID)
        {
            if(taskLauncher.FactionEntity.FactionID == FactionID) //make sure the new task launcher belongs to the faction managed by this component
                taskLaunchers.Remove(taskLauncher); //remove task launcher from list
        }

        //a method that checks if the faction doesn't have any more units/buildings and trigger a faction defeat in that case:
        private void CheckFactionDefeat ()
        {
            //if the defeat condition is set to eliminate all units and buildings and there are no more units and buildings for this faction
            if (gameMgr.GetDefeatCondition() == DefeatConditions.eliminateAll && units.Count == 0 && buildings.Count == 0)
                gameMgr.OnFactionDefeated(FactionID);
        }

        //When a new resource drop off building is spawned, all collectors check if this building can suit them or not.
        public void UpdateCollectorsDropOffBuilding ()
		{
            foreach (ResourceCollector collector in collectors)
                collector.UpdateDropOffBuilding();
        }

        //Faction limits:
        //check if the faction has hit its limit with placing a specific building/unit
        public bool HasReachedLimit(string code, string category)
        {
            foreach(FactionLimit limit in limits)
            {
                if (limit.code.Contains(code, category))
                    return limit.IsMaxReached();
            }

            //if the building/unit is not found in the list
            return false;
        }

        //when a unit/building is added, this is called to increment the limits list
        public void UpdateLimitsList(string code, string category, bool increment)
        {
            foreach(FactionLimit limit in limits)
            {
                if (limit.code.Contains(code, category))
                    limit.Update(increment ? 1 : -1);
            }
        }

        /// <summary>
        /// Searches for a building center that allows the given building type to be built inside its territory.
        /// </summary>
        /// <param name="building">Code of the building type to place/build.</param>
        /// <returns></returns>
        public Building GetFreeBuildingCenter (string code)
        {
            //go through the building centers of the faciton
            foreach(Building center in buildingCenters)
                //see if the building center can have the input building placed around it:
                if(center.BorderComp.AllowBuildingInBorder(code))
                    //if yes then return this center:
                    return center;

            //no center found? 
            return null;
        }
    }
}
