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

    [SerializeField] FactionEntity villagerPrefab;
     
    float timeSinceLastAction = 0f;
    float timeBetweenActions;

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

    private void OnEnable()
    {
        CustomEvents.UnitStopBuilding += OnStopBuilding;
    }

    private void OnDisable()
    {
        CustomEvents.UnitStopBuilding += OnStopBuilding;
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
        else
        {
            Building house = factionMgr.Slot.GetTypeInfo().GetPopulationBuilding();
            if (BuildingPlacer.OnBuildingPlacementRequest(house, factionMgr.Slot.CapitalBuilding.gameObject, true, out Building placedBuilding))
            {
                AssignVillagerToBuild(placedBuilding);
            }
        }        
    }

    public void OnStopBuilding(Unit unit, Building building)
    {
        if (unit.FactionID == factionMgr.FactionID &&
            !building.IsBuilt)
        {
            AssignVillagerToBuild(building);
        }
    }

    private void AssignVillagerToBuild(Building toBuild)
    {
        BasicTargetPicker targetPicker = new BasicTargetPicker(villagerPrefab);

        if (gameMgr.GridSearch.Search(toBuild.transform.position,
                                    1000f,
                                    false,
                                    targetPicker.IsValidTarget,
                                    out FactionEntity potentialTarget) == ErrorMessage.none)
        {
            Unit newVillager = potentialTarget as Unit;
            newVillager.BuilderComp.SetTarget(toBuild);
        }
    }
}
