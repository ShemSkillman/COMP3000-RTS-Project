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
    [SerializeField] ResourceTypeInfo coin;
    [SerializeField] ResourceTypeInfo wood;
     
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
        print("Wood collectors: " + factionMgr.GetVillagersCollectingResource(wood.Key).Count);
        print("Coin collectors: " + factionMgr.GetVillagersCollectingResource(coin.Key).Count);

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
        int villagerCountGoal = factionMgr.Slot.MaxPopulation / 2;

        int villagerCount = factionMgr.Villagers.Count + factionMgr.Slot.CapitalBuilding.TaskLauncherComp.GetTaskQueueCount();

        if (villagerCount < villagerCountGoal &&
            factionMgr.Slot.CapitalBuilding.TaskLauncherComp.GetTaskQueueCount() < 2)
        {
            factionMgr.Slot.CapitalBuilding.TaskLauncherComp.Add(0);
        }
        else if (IsHouseNeeded())
        {
            ConstructBuilding(house);
        }
        else if (gameMgr.ResourceMgr.HasRequiredResources(tower.GetResources(), factionMgr.FactionID))
        {
            ConstructBuilding(tower);
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
        BasicTargetPicker targetPicker = new BasicTargetPicker(villager);

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