using UnityEngine;

using RTSEngine.EntityComponent;

namespace RTSEngine
{
	public class CustomEvents : MonoBehaviour {

        public delegate void GameStateEventHandler();
        public static event GameStateEventHandler GameStateUpdated = delegate { };

        public delegate void EntityEventHandler(Entity entity);
        public static event EntityEventHandler EntitySelected = delegate { };
        public static event EntityEventHandler EntityDeselected = delegate { };

        public delegate void FactionEntityEventHandler (FactionEntity entity);
        public static event FactionEntityEventHandler FactionEntityDead = delegate { };
        public static event FactionEntityEventHandler FactionEntityMouseEnter = delegate { };
        public static event FactionEntityEventHandler FactionEntityMouseExit = delegate { };

        public delegate void FactionEntityHealthEventHandler(FactionEntity factionEntity, int value, FactionEntity source);
        public static event FactionEntityHealthEventHandler FactionEntityHealthUpdated = delegate { };

        public delegate void EntityComponentTaskEventHandler(IEntityComponent component, string taskCode);
        public static event EntityComponentTaskEventHandler EntityComponentTaskReloadRequest = delegate { };

		public delegate void UnitEventHandler(Unit Unit);
		public static event UnitEventHandler UnitCreated = delegate {};
		public static event UnitEventHandler UnitDead = delegate {};
		public static event UnitEventHandler UnitSelected = delegate {};
		public static event UnitEventHandler UnitDeselected = delegate {};
        public static event UnitEventHandler UnitStartMoving = delegate {};
        public static event UnitEventHandler UnitStopMoving = delegate { };
        public static event UnitEventHandler UnitWanderToggled = delegate { };
        public static event UnitEventHandler UnitInstanceUpgraded = delegate { };

        public delegate void UnitHealthEventHandler(Unit Unit, float HealthPoints, FactionEntity Source);
        public static event UnitHealthEventHandler UnitHealthUpdated = delegate { };

        public delegate void UnitResourceEventHandler(Unit UnitComp, Resource Resource);
		public static event UnitResourceEventHandler UnitStartCollecting = delegate {};
		public static event UnitResourceEventHandler UnitStopCollecting = delegate {};
        public static event UnitResourceEventHandler UnitCollectionOrder = delegate { };
        public static event UnitResourceEventHandler UnitDropOffStart = delegate { };
        public static event UnitResourceEventHandler UnitDropOffComplete = delegate { };

        public delegate void UnitBuildingEventHandler (Unit UnitComp, Building Building);
		public static event UnitBuildingEventHandler UnitStartBuilding = delegate {};
		public static event UnitBuildingEventHandler UnitStopBuilding = delegate {};
        public static event UnitBuildingEventHandler UnitBuildingOrder = delegate { };

        public delegate void UnitHealingEventHandler (Unit UnitComp, Unit TargetUnit);
		public static event UnitHealingEventHandler UnitStartHealing = delegate {};
		public static event UnitHealingEventHandler UnitStopHealing = delegate {};

		public delegate void UnitConvertingEventHandler (Unit source, Unit target);
		public static event UnitConvertingEventHandler UnitStartConverting = delegate {};
		public static event UnitConvertingEventHandler UnitStopConverting = delegate {};
        public static event UnitConvertingEventHandler UnitConversionStart = delegate {};
		public static event UnitConvertingEventHandler UnitConversionComplete = delegate {};

		public delegate void AttackEventHandler (AttackEntity attackComp, FactionEntity target, Vector3 targetPosition);
		public static event AttackEventHandler AttackSwitch = delegate {};
        public static event AttackEventHandler AttackTargetLocked = delegate { };
        public static event AttackEventHandler AttackPerformed = delegate { };
        public static event AttackEventHandler AttackerInRange = delegate { };
        public static event AttackEventHandler AttackCooldownUpdated = delegate { };

        public delegate void AttackDamageEventHandler(AttackEntity attackComp, FactionEntity target, int damage);
        public static event AttackDamageEventHandler AttackDamageDealt = delegate { };

        public delegate void BuildingEventHandler (Building building);
		public static event BuildingEventHandler BuildingPlaced = delegate {};
		public static event BuildingEventHandler BuildingBuilt = delegate {};
		public static event BuildingEventHandler BuildingDestroyed = delegate {};
		public static event BuildingEventHandler BuildingSelected = delegate {};
		public static event BuildingEventHandler BuildingDeselected = delegate {};
        public static event BuildingEventHandler BuildingStartPlacement = delegate {};
        public static event BuildingEventHandler BuildingStopPlacement = delegate {};
        public static event BuildingEventHandler BuildingInstanceUpgraded = delegate { };

        public delegate void BuildingHealthEventHandler(Building Building, int Value, FactionEntity Source);
        public static event BuildingHealthEventHandler BuildingHealthUpdated = delegate {};

        public delegate void UpgradeEventHandler(Upgrade upgrade, int targetID);
		public static event UpgradeEventHandler UnitUpgraded = delegate {};
        public static event UpgradeEventHandler BuildingUpgraded = delegate { };

        //Border component related:
        public delegate void BorderEventHandler(Border border);
        public static event BorderEventHandler BorderActivated = delegate {};
        public static event BorderEventHandler BorderDeactivated = delegate { };

        //Task Launcher related:
        public delegate void TaskEventHandler (TaskLauncher taskLauncher, int taskID = -1, int taskQueueID = -1);
        public static event TaskEventHandler TaskLauncherAdded = delegate {};
        public static event TaskEventHandler TaskLauncherRemoved = delegate {};
        public static event TaskEventHandler TaskLaunched = delegate {};
        public static event TaskEventHandler TaskStarted = delegate {};
		public static event TaskEventHandler TaskCanceled = delegate {};
		public static event TaskEventHandler TaskCompleted = delegate {};

        //Population related:
        public delegate void PopulationEventHandler(FactionSlot factionSlot, int value);
        public static event PopulationEventHandler CurrentPopulationUpdated = delegate { };
        public static event PopulationEventHandler MaxPopulationUpdated = delegate { };

        //Resource related:
        public delegate void ResourceEventHandler (Resource Resource);
        public static event ResourceEventHandler ResourceAdded = delegate { };
		public static event ResourceEventHandler ResourceEmpty = delegate {};
		public static event ResourceEventHandler ResourceDestroyed = delegate {};
        public static event ResourceEventHandler ResourceAmountUpdated = delegate {};
		public static event ResourceEventHandler ResourceSelected = delegate {};
		public static event ResourceEventHandler ResourceDeselected = delegate {};

        //Faction-resource related:
        public delegate void FactionResourceHandler(ResourceTypeInfo resourceType, int factionID, int amount);
        public static event FactionResourceHandler FactionResourceUpdated = delegate { };

		public delegate void APCEventHandler (APC APC, Unit Unit);
		public static event APCEventHandler APCAddUnit = delegate {};
		public static event APCEventHandler APCRemoveUnit = delegate {}; 
		public static event APCEventHandler APCCallUnit = delegate {};

		public delegate void PortalEventHandler (Portal From, Portal To, Unit Unit);
		public static event PortalEventHandler UnitTeleport = delegate {};
		public static event PortalEventHandler PortalDoubleClick = delegate {}; 

		public delegate void FactionSlotHandler (FactionSlot factionSlot);
		public static event FactionSlotHandler FactionEliminated = delegate {};
		public static event FactionSlotHandler FactionWin = delegate {};
        public static event FactionSlotHandler FactionInit = delegate { };
        public static event FactionSlotHandler FactionDefaultEntitiesInit = delegate { };
        public static event FactionSlotHandler NPCFactionInit = delegate { };

        //Mission related events:
        public delegate void ScenarioEventHandler(Scenario scenario);
        public static event ScenarioEventHandler ScenarioStart = delegate { };
        public static event ScenarioEventHandler ScenarioSuccess = delegate { };
        public static event ScenarioEventHandler ScenarioFail = delegate { };

        public delegate void MissionEventHandler(Mission mission);
        public static event MissionEventHandler MissionStart = delegate { };
        public static event MissionEventHandler MissionComplete = delegate { };
        public static event MissionEventHandler MissionFail = delegate { };

		public delegate void CustomCommandEventHandler (NetworkInput command);
		public static event CustomCommandEventHandler CustomCommand = delegate {};

        //Game state related events:
        public static void OnGameStateUpdated ()
        {
            GameStateUpdated();
        }

        //Entity related events
		public static void OnEntitySelected (Entity entity) //called when an entity is selected
		{
			EntitySelected (entity);
		}
		public static void OnEntityDeselected (Entity entity) //called when a unit is deselected
		{
			EntityDeselected (entity);
		}

        //Faction entity related events:
        public static void OnFactionEntityDead (FactionEntity factionEntity) //called when the faction entity is dead
        {
            FactionEntityDead(factionEntity);
        }
        public static void OnFactionEntityMouseEnter (FactionEntity factionEntity) //called when the mouse is over the faction entity
        {
            FactionEntityMouseEnter(factionEntity);
        }
        public static void OnFactionEntityMouseExit (FactionEntity factionEntity) //called when the mouse is no longer over the faction entity
        {
            FactionEntityMouseExit(factionEntity);
        }

        public static void OnFactionEntityHealthUpdated (FactionEntity factionEntity, int value, FactionEntity source)
        {
            FactionEntityHealthUpdated(factionEntity, value, source);
        }

        public static void OnEntityComponentTaskReloadRequest (IEntityComponent component, string taskCode)
        {
            EntityComponentTaskReloadRequest(component, taskCode);
        }

        //Unit custom events:
        public static void OnUnitCreated (Unit Unit) //called when a unit is created.
		{
			UnitCreated (Unit);
		}
		public static void OnUnitDead (Unit unit) //called when a unit is dead
		{
			UnitDead (unit);
		}
		public static void OnUnitSelected (Unit Unit) //called when a unit is selected
		{
			UnitSelected (Unit);
		}
		public static void OnUnitDeselected (Unit Unit) //called when a unit is deselected
		{
			UnitDeselected (Unit);
		}
        public static void OnUnitStartMoving (Unit Unit) //when a unit starts moving
        {
            UnitStartMoving(Unit);
        }
        public static void OnUnitStopMoving (Unit Unit) //when a unit stops moving
        {
            UnitStopMoving(Unit);
        }
		public static void OnUnitWanderToggled (Unit unit) //called when a unit's wandering component is toggled
		{
			UnitWanderToggled (unit);
		}


        //Unit-Health events:

        public static void OnUnitHealthUpdated (Unit unit, float healthPoints, FactionEntity source)
        {
            UnitHealthUpdated(unit, healthPoints, source);
        }

        //Unit-Resource events:
        public static void OnUnitCollectionOrder(Unit unit, Resource resource) //called when a unit starts collecting a resource 
        {
            UnitCollectionOrder(unit, resource);
        }
        public static void OnUnitStartCollecting (Unit unit, Resource resource) //called when a unit starts collecting a resource 
		{
			UnitStartCollecting (unit, resource);
		}
		public static void OnUnitStopCollecting (Unit unit, Resource resource) //called when a unit stops collecting a resource
		{
			UnitStopCollecting (unit, resource);
		}
		public static void OnUnitDropOffStart (Unit unit, Resource resource) //called when a unit starts dropping off resources
		{
			UnitDropOffStart (unit, resource);
		}
		public static void OnUnitDropOffComplete (Unit unit, Resource resource) //called when a unit completes dropping off resources
		{
			UnitDropOffComplete (unit, resource);
		}

        //Unit-Building events:
        public static void OnUnitBuildingOrder(Unit Unit, Building Building) //called when a unit starts constructing a building
        {
            UnitBuildingOrder(Unit, Building);
        }
        public static void OnUnitStartBuilding (Unit unit, Building building) //called when a unit starts constructing a building
		{
			UnitStartBuilding (unit, building);
		}
		public static void OnUnitStopBuilding (Unit unit, Building building) //called when a unit stops constructing a building
		{
			UnitStopBuilding (unit, building);
		}

		//Portal:
		public static void OnUnitTeleport (Portal from, Portal to, Unit unit) //called when a unit teleports in a portal
		{
			UnitTeleport (from,to,unit);
		}
		public static void OnPortalDoubleClick (Portal from, Portal to, Unit unit) //called when a unit teleports in a portal
		{
			PortalDoubleClick (from,to,unit);
		}

		//Attack Switch:
		public static void OnAttackSwitch (AttackEntity attackComp) //called when a unit switchs attack type:
		{
			AttackSwitch (attackComp, null, Vector3.zero);
		}

		//Unit-Healing events:
		public static void OnUnitStartHealing (Unit unit, Unit targetUnit) //called when a unit starts healing another unit
		{
			UnitStartHealing (unit, targetUnit);
		}
		public static void OnUnitStopHealing (Unit unit, Unit targetUnit) //called when a unit stops healing another unit
		{
			UnitStopHealing (unit, targetUnit);
		}

		//Unit-Converting events:
		public static void OnUnitStartConverting (Unit source, Unit target) //called when a unit starts converting another unit
		{
			UnitStartConverting (source, target);
		}
		public static void OnUnitStopConverting (Unit source, Unit target) //called when a unit stops converting another unit
		{
			UnitStopConverting (source, target);
		}
		public static void OnUnitConversionStart (Unit source, Unit target) //called just before the target unit is removed from its faction
		{
			UnitConversionStart (source, target);
		}
		public static void OnUnitConversionComplete (Unit source, Unit target) //called just before the target unit is added to its new faction
		{
			UnitConversionComplete (source, target);
		}

		//Building custom events:
		public static void OnBuildingPlaced (Building building) //called when a building is placed:
		{
			BuildingPlaced (building);
		}
		public static void OnBuildingBuilt (Building building) //called when a building is built:
		{
			BuildingBuilt (building);
		}
		public static void OnBuildingDestroyed (Building building) //called when a building is placed:
		{
			BuildingDestroyed (building);
		}
		public static void OnBuildingSelected (Building building) //called when a building is placed:
		{
			BuildingSelected (building);
		}
		public static void OnBuildingDeselected (Building building) //called when a building is placed:
		{
			BuildingDeselected (building);
		}
        public static void OnBuildingStartPlacement(Building building) //called when a building started getting placed.
        {
            BuildingStartPlacement(building);
        }
        public static void OnBuildingStopPlacement(Building building) //called when a building stopped getting placed.
        {
            BuildingStopPlacement(building);
        }

        //Building's Health related event:
        public static void OnBuildingHealthUpdated (Building building, int value, FactionEntity source)
        {
            BuildingHealthUpdated(building, value, source);
        }

        //Border component's related events:
        public static void OnBorderActivated (Border border)
        {
            BorderActivated(border);
        }
        public static void OnBorderDeactivated(Border border)
        {
            BorderDeactivated(border);
        }

        //APC:
        public static void OnAPCAddUnit (APC apc, Unit unit) //called when an APC adds a unit.
		{
			APCAddUnit(apc, unit);
		}

		public static void OnAPCRemoveUnit (APC apc, Unit unit) //called when an APC removes a unit.
		{
			APCRemoveUnit(apc, unit);
		}

		public static void OnAPCCallUnit (APC APC, Unit Unit) //called when an APC removes a unit (Unit here is irrelevant)
		{
			APCCallUnit(APC, Unit);
		}

		//Task Events:
        public static void OnTaskLauncherAdded (TaskLauncher taskLauncher) //called when a new task launcher has been added
        {
            TaskLauncherAdded(taskLauncher);
        }
        public static void OnTaskLauncherRemoved(TaskLauncher taskLauncher) //called when a task launcher has been removed
        {
            TaskLauncherRemoved(taskLauncher);
        }

        public static void OnTaskLaunched (TaskLauncher taskLauncher, int taskID, int taskQueueID) //called when a task launcher launches a task
		{
			TaskLaunched (taskLauncher, taskID, taskQueueID);
		}
        public static void OnTaskStarted (TaskLauncher taskLauncher, int taskID, int taskQueueID) //called when a task launcher launches a task
		{
			TaskStarted (taskLauncher, taskID, taskQueueID);
		}
		public static void OnTaskCanceled(TaskLauncher taskLauncher, int taskID, int taskQueueID) //called when a task launcher cancels a task
        {
			TaskCanceled(taskLauncher, taskID, taskQueueID);
        }
		public static void OnTaskCompleted(TaskLauncher taskLauncher, int taskID, int taskQueueID) //called when a task launcher completes a task
        {
			TaskCompleted(taskLauncher, taskID, taskQueueID);
        }

        //Population Events:
        public static void OnCurrentPopulationUpdated (FactionSlot factionSlot, int value) //called when the current population of a faction is updated
        {
            CurrentPopulationUpdated(factionSlot, value);
        }

        public static void OnMaxPopulationUpdated(FactionSlot factionSlot, int value)
        {
            MaxPopulationUpdated(factionSlot, value);
        }

        //Resource events:
        public static void OnResourceAdded (Resource resource) //called when a resource is initialised
		{
			ResourceAdded (resource);
		}
        public static void OnResourceEmpty (Resource resource) //called when a resource is empty
		{
			ResourceEmpty (resource);
		}
        /// <summary>
        /// Called when a Resource instance is destroyed.
        /// </summary>
        /// <param name="resource">Resource instance that is destroyed.</param>
        public static void OnResourceDestroyed (Resource resource) //called when a resource is empty
		{
			ResourceDestroyed (resource);
		}
        public static void OnResourceAmountUpdated (Resource resource) //called when a resource's amount changes
        {
            ResourceAmountUpdated(resource);
        }
		public static void OnResourceSelected (Resource resource) //called when a resource is selected
		{
			ResourceSelected (resource);
		}
		public static void OnResourceDeselected (Resource resource) //called when a resource is desselected
		{
			ResourceDeselected (resource);
		}

        //Faction-resource events:
        public static void OnFactionResourceUpdate (ResourceTypeInfo resourceType, int factionID, int amount)
        {
            FactionResourceUpdated(resourceType, factionID, amount);
        }

        //Mission related events:
        public static void OnScenarioStart (Scenario scenario) { ScenarioStart(scenario); }
        public static void OnScenarioSuccess (Scenario scenario) { ScenarioSuccess(scenario); }
        public static void OnScenarioFail (Scenario scenario) { ScenarioFail(scenario); }

        public static void OnMissionStart (Mission mission) { MissionStart(mission); }
        public static void OnMissionComplete (Mission mission) { MissionComplete(mission); }
        public static void OnMissionFail (Mission mission) { MissionFail(mission); }

		//Game events:
		public static void OnFactionEliminated (FactionSlot factionSlot)
		{
			FactionEliminated (factionSlot);
		}
		public static void OnFactionWin (FactionSlot factionSlot)
		{
			FactionWin (factionSlot);
		}
		public static void OnFactionInit (FactionSlot factionSlot)
		{
			FactionInit (factionSlot);
		}
		public static void OnFactionDefaultEntitiesInit (FactionSlot factionSlot)
		{
			FactionDefaultEntitiesInit (factionSlot);
		}
		public static void OnNPCFactionInit (FactionSlot factionSlot)
		{
			NPCFactionInit (factionSlot);
		}

        public static void OnAttackTargetLocked(AttackEntity attackComp, FactionEntity target, Vector3 targetPosition)
        {
            AttackTargetLocked(attackComp, target, targetPosition);
        }

        public static void OnAttackPerformed(AttackEntity attackComp, FactionEntity target, Vector3 targetPosition)
        {
            AttackPerformed(attackComp, target, targetPosition);
        }

        public static void OnAttackerInRange(AttackEntity attackComp, FactionEntity target, Vector3 targetPosition)
        {
            AttackerInRange(attackComp, target, targetPosition);
        }

        public static void OnAttackCooldownUpdated (AttackEntity attackComp, FactionEntity target)
        {
            AttackCooldownUpdated(attackComp, target, Vector3.zero);
        }

        public static void OnAttackDamageDealt(AttackEntity attackComp, FactionEntity target, int damage)
        {
            AttackDamageDealt(attackComp, target, damage);
        }

        //Upgrades:
        public static void OnUnitUpgraded (Upgrade upgrade, int targetID)
        {
            UnitUpgraded(upgrade, targetID);
        }
        public static void OnUnitInstanceUpgraded (Unit unit)
        {
            UnitInstanceUpgraded(unit);
        }

        public static void OnBuildingUpgraded (Upgrade upgrade, int targetID)
        {
            BuildingUpgraded(upgrade, targetID);
        }
        public static void OnBuildingInstanceUpgraded (Building building)
        {
            BuildingInstanceUpgraded(building);
        }

        //custom action events:
        public static void OnCustomCommand(NetworkInput command)
        {
            CustomCommand(command);
		}
	}
}