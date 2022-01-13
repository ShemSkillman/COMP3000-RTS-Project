using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Assertions;
using UnityEngine;
using RTSEngine;

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
     
    float timeSinceLastAction = 0f;
    float timeBetweenActions;

    List<ConstructionTask> constructionTasks = new List<ConstructionTask>();

    public NPCBuildingPlacer BuildingPlacer { private set; get; }

    bool intiated = false;

    public void Init(GameManager gameMgr, FactionManager factionMgr)
    {
        this.gameMgr = gameMgr;
        this.factionMgr = factionMgr;

        timeBetweenActions = 60f / actionsPerMinute;

        BuildingPlacer = GetComponentInChildren<NPCBuildingPlacer>();
        BuildingPlacer.Init(gameMgr, this, factionMgr);

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

        if (factionMgr.Villagers.Count > 0 && 
            (idleVillagers.Count > 0)) //|| coinCollectors.Count != woodCollectors.Count + 1))
        {

            print("assigning idle villagers");

            Unit villager;
            if (idleVillagers.Count > 0)
            {
                villager = idleVillagers[0];
                print("assigning idle villager");
            }
            else
            {
                return;

                if (coinCollectors.Count > woodCollectors.Count)
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

            if (coinCollectors.Count > woodCollectors.Count)
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
            print("Building house");
            ConstructBuilding(house);
        }
        else if (villagerCount < villagerCountGoal &&
            factionMgr.Slot.CapitalBuilding.TaskLauncherComp.GetTaskQueueCount() < 2)
        {
            print("Training villager");
            factionMgr.Slot.CapitalBuilding.TaskLauncherComp.Add(0);
        }
        else if (gameMgr.ResourceMgr.HasRequiredResources(tower.GetResources(), factionMgr.FactionID))
        {
            print("Building tower");
            ConstructBuilding(tower);
        }
        else
        {
            print("Assigned villager to building task");
            foreach (ConstructionTask task in constructionTasks)
            {
                if (task.Builder.BuilderComp.GetTarget() != task.InConstruction)
                {
                    AssignVillagerToBuild(task);
                }
            }
        }
    }

    public bool IsHouseNeeded()
    {
        int housePop = house.GetAddedPopulationSlots();
        return factionMgr.Slot.GetFreePopulation() + (housePop * GetBuildingInConstructionCount(house)) < housePop &&
            gameMgr.ResourceMgr.HasRequiredResources(house.GetResources(), factionMgr.FactionID);
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