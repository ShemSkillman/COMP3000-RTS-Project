using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Assertions;
using UnityEngine;
using RTSEngine;
using RTSEngine.EntityComponent;

namespace ColdAlliances.AI
{
    public class AIBrain : MonoBehaviour
    {
        GameManager gameMgr;
        FactionManager factionMgr;

        [SerializeField] int actionsPerMinute = 60;

        [Header("Units")]
        [SerializeField] Unit villager;
        [SerializeField] Unit swordsman;

        [Header("Buildings")]
        [SerializeField] Building house;
        [SerializeField] Building barracks;
        [SerializeField] Building tower;

        [Header("Resources")]
        [SerializeField] Resource coinSource;
        [SerializeField] Resource woodSource;
        [SerializeField] ResourceTypeInfo defensePower;
        [SerializeField] ResourceTypeInfo attackPower;
     
        float timeSinceLastAction = 0f;
        float timeBetweenActions;

        List<ConstructionTask> constructionTasks = new List<ConstructionTask>();
        Dictionary<string, List<CollectionTask>> collectionTasksDic = new Dictionary<string, List<CollectionTask>>();

        public NPCBuildingPlacer BuildingPlacer { private set; get; }

        bool intiated = false;

        bool isBarracksNeeded = false;

        ArmyGroup armyGroup;

        private void OnEnable()
        {
            CustomEvents.UnitCreated += UnitCreated;
        }

        private void OnDisable()
        {
            CustomEvents.UnitCreated -= UnitCreated;
        }

        private void UnitCreated(Unit unit)
        {
            if (unit.IsFree() || unit.FactionID != factionMgr.FactionID)
            {
                return;
            }

            if (unit.AttackComp != null)
            {
                if (armyGroup == null)
                {
                    armyGroup = new ArmyGroup(unit);
                }
                else
                {
                    armyGroup.AttackUnits.Add(unit);
                }
            }
        }

        public void Init(GameManager gameMgr, FactionManager factionMgr)
        {


            this.gameMgr = gameMgr;
            this.factionMgr = factionMgr;

            timeBetweenActions = 60f / actionsPerMinute;

            BuildingPlacer = GetComponentInChildren<NPCBuildingPlacer>();
            BuildingPlacer.Init(gameMgr, this, factionMgr);

            collectionTasksDic[woodSource.GetResourceType().Key] = new List<CollectionTask>();
            collectionTasksDic[coinSource.GetResourceType().Key] = new List<CollectionTask>();

            intiated = true;
        }

        private void Update()
        {
            if (!intiated)
            {
                return;
            }

            timeSinceLastAction += Time.deltaTime;

            if (timeSinceLastAction >= timeBetweenActions)
            {
                timeSinceLastAction = 0f;
                constructionTasks.RemoveAll(ConstructionTask.IsConstructionTaskInvalid);

                foreach (List<CollectionTask> collectionTasks in collectionTasksDic.Values)
                {
                    collectionTasks.RemoveAll(CollectionTask.IsCollectionTaskInvalid);
                }

                PerformAction();
            }
        }

        private void PerformAction()
        {
            List<Unit> idleVillagers = factionMgr.GetIdleVillagers();

            int villagerCountGoal = factionMgr.Slot.MaxPopulation / 2;

            int villagerCount = factionMgr.Villagers.Count + factionMgr.Slot.CapitalBuilding.TaskLauncherComp.GetTaskQueueCount();

            List<Unit> woodCollectors = factionMgr.GetVillagersCollectingResource(woodSource.GetResourceType().Key);
            List<Unit> coinCollectors = factionMgr.GetVillagersCollectingResource(coinSource.GetResourceType().Key);

            int woodCollectorCount = woodCollectors.Count + collectionTasksDic[woodSource.GetResourceType().Key].Count;
            int coinCollectorCount = coinCollectors.Count + collectionTasksDic[coinSource.GetResourceType().Key].Count;

            if (factionMgr.Villagers.Count > 0 && 
                (idleVillagers.Count > 0 || Mathf.Abs(coinCollectorCount - woodCollectorCount) > 1))
            {
                Unit villager;
                if (idleVillagers.Count > 0)
                {
                    villager = idleVillagers[0];
                }
                else
                {
                    if (coinCollectorCount > woodCollectorCount)
                    {
                        villager = coinCollectors[0];
                    }
                    else
                    {
                        villager = woodCollectors[0];
                    }
                }

                if (villager == null)
                {
                    print("NULL VILLAGER - THIS SHOULD NEVER HAPPEN!");
                }

                if (coinCollectorCount > woodCollectorCount)
                {
                    AssignVillagerToResource(villager, woodSource);
                }
                else
                {
                    AssignVillagerToResource(villager, coinSource);
                }
            }
            else if (IsHouseNeeded())
            {
                ConstructBuilding(house);
            }
            else if (villagerCount < villagerCountGoal &&
                factionMgr.Slot.CapitalBuilding.TaskLauncherComp.GetTaskQueueCount() < 1 &&
                gameMgr.ResourceMgr.GetFactionResources(factionMgr.FactionID).Resources[coinSource.ID].GetCurrAmount() >= 100 &&
                factionMgr.Slot.GetFreePopulation() > 0)
            {
                factionMgr.Slot.CapitalBuilding.TaskLauncherComp.Add(0);
            }
            else if (gameMgr.ResourceMgr.HasRequiredResources(barracks.GetResources(), factionMgr.FactionID) && isBarracksNeeded)
            {
                ConstructBuilding(barracks);
                isBarracksNeeded = false;
            }
            else if (gameMgr.ResourceMgr.HasRequiredResources(tower.GetResources(), factionMgr.FactionID) &&
                !factionMgr.HasReachedLimit("tower",""))
            {
                ConstructBuilding(tower);
            }
            else if (TrainSoldiers())
            {

            }
            else
            {
                foreach (ConstructionTask task in constructionTasks)
                {
                    if (task.Builder.BuilderComp.GetTarget() != task.InConstruction)
                    {
                        AssignVillagerToBuild(task);
                    }
                }

                if (armyGroup == null)
                    return;

                armyGroup.Validate();

                int attackPowerID = gameMgr.ResourceMgr.GetResourceID(attackPower.Key);
                int defensePowerID = gameMgr.ResourceMgr.GetResourceID(defensePower.Key);

                int myAttack = gameMgr.ResourceMgr.GetFactionResources(factionMgr.FactionID).Resources[attackPowerID].GetCurrAmount();
                int enemyDefense = gameMgr.ResourceMgr.GetFactionResources(GameManager.PlayerFactionID).Resources[defensePowerID].GetCurrAmount();

                if (myAttack > enemyDefense)
                {
                    List<Building> buildings = factionMgr.GetEnemyBuildings();
                    Vector3 armyPos = armyGroup.GetLocation();
                    Building closestBuilding = null;
                    float closestDist = Mathf.Infinity;
                    foreach (Building b in buildings)
                    {
                        float dist = Vector3.Distance(b.transform.position, armyPos);
                        if (dist < closestDist)
                        {
                            closestBuilding = b;
                            closestDist = dist;
                        }
                    }

                    gameMgr.AttackMgr.LaunchAttack(armyGroup.AttackUnits, closestBuilding, closestBuilding.GetEntityCenterPos(), false);
                }
            }
        }

        public bool IsHouseNeeded()
        {
            int housePop = house.GetAddedPopulationSlots();
            return factionMgr.Slot.GetFreePopulation() + (housePop * GetBuildingInConstructionCount(house)) < housePop &&
                factionMgr.Slot.GetCurrentPopulation() < factionMgr.Slot.MaxPopulation &&
                gameMgr.ResourceMgr.HasRequiredResources(house.GetResources(), factionMgr.FactionID);
        }

        private bool TrainSoldiers()
        {
            if (gameMgr.ResourceMgr.GetFactionResources(factionMgr.FactionID).Resources[coinSource.ID].GetCurrAmount() < 100 ||
                factionMgr.Slot.GetFreePopulation() < 1)
            {
                isBarracksNeeded = false;
                return false;
            }

            foreach (Building building in factionMgr.GetBuildings())
            {
                if (building.GetCode() == barracks.GetCode() && building.IsBuilt)
                {
                    if (building.TaskLauncherComp.GetTaskQueueCount() < 1)
                    {
                        building.TaskLauncherComp.Add(0);
                        return true;
                    }
                }
            }

            if (gameMgr.ResourceMgr.GetFactionResources(factionMgr.FactionID).Resources[coinSource.ID].GetCurrAmount() > 100)
            {
                isBarracksNeeded = true;
            }

            return false;
        }

        private void ConstructBuilding(Building building)
        {
            if (BuildingPlacer.OnBuildingPlacementRequest(building, factionMgr.Slot.CapitalBuilding.gameObject, true, out Building placedBuilding))
            {
                ConstructionTask task = new ConstructionTask(placedBuilding);
                AssignVillagerToBuild(task);

                constructionTasks.Add(task);
            }
        }

        private void AssignVillagerToBuild(ConstructionTask task)
        {
            IdleBuilderTargetPicker targetPicker = new IdleBuilderTargetPicker(villager.GetCode());

            if (gameMgr.GridSearch.Search(task.InConstruction.transform.position,
                                        1000f,
                                        false,
                                        targetPicker.IsValidTarget,
                                        out FactionEntity potentialTarget) == ErrorMessage.none)
            {
                Unit newVillager = potentialTarget as Unit;
                newVillager.BuilderComp.SetTarget(task.InConstruction);

                task.Builder = newVillager;
            }
        }

        private void AssignVillagerToResource(Unit villager, Resource resource)
        {
            BasicTargetPicker targetPicker = new BasicTargetPicker(resource.GetCode());

            if (gameMgr.GridSearch.Search<Entity>(villager.transform.position,
                                        1000f,
                                        true,
                                        targetPicker.IsValidTarget,
                                        out Entity potentialTarget) == ErrorMessage.none)
            {
                Resource closestResource = potentialTarget as Resource;

                villager.CollectorComp.SetTarget(closestResource);

                CollectionTask task = new CollectionTask(villager.CollectorComp, resource.GetResourceType());

                collectionTasksDic[resource.GetResourceType().Key].Add(task);
            }
        }

        private int GetBuildingInConstructionCount(Building building)
        {
            int counter = 0;

            foreach (ConstructionTask task in constructionTasks)
            {
                if (task.InConstruction.GetCode() == building.GetCode())
                {
                    counter++;
                }
            }

            return counter;
        }
    }

    class ConstructionTask
    {
        public Building InConstruction { get; private set; }
        public Unit Builder { get; set; }

        public ConstructionTask(Building inConstruction, Unit builder = null)
        {
            InConstruction = inConstruction;
            Builder = builder;
        }

        public static bool IsConstructionTaskInvalid(ConstructionTask task)
        {
            return task.InConstruction.IsBuilt || task.InConstruction.HealthComp.IsDestroyed;
        }
    }

    class CollectionTask
    {
        public ResourceCollector Collector { get; set; }
        public ResourceTypeInfo ToCollect { get; set; }

        public CollectionTask(ResourceCollector collector, ResourceTypeInfo toCollect)
        {
            Collector = collector;
            ToCollect = toCollect;
        }

        public static bool IsCollectionTaskInvalid(CollectionTask task)
        {
            return task.Collector.GetTarget() == null || 
                task.Collector.GetTarget().GetResourceType().Key != task.ToCollect.Key ||
                task.Collector.InProgress;
        }
    }

    class ArmyGroup
    {
        public List<Unit> AttackUnits { get; set; }

        public ArmyGroup(Unit unit)
        {
            AttackUnits = new List<Unit>();
            AttackUnits.Add(unit);
        }

        public void Validate()
        {
            AttackUnits.RemoveAll(IsUnitDead);
        }

        private static bool IsUnitDead(Unit unit)
        {
            return unit == null || unit.HealthComp.IsDead();
        }

        public Vector3 GetLocation()
        {
            Vector3 ret = Vector3.zero;
            foreach (Unit unit in AttackUnits)
            {
                ret += unit.transform.position;
            }

            return ret / AttackUnits.Count;
        }
    }
}