using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Assertions;
using UnityEngine;

namespace RTSEngine
{
    public class AIBrain : MonoBehaviour
    {
        GameManager gameMgr;
        FactionManager factionMgr;

        [SerializeField] int actionsPerMinute = 60;

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

            Building house = factionMgr.Slot.GetTypeInfo().GetPopulationBuilding();

            if (BuildingPlacer.PendingCount < 2)
            {
                BuildingPlacer.OnBuildingPlacementRequest(house, factionMgr.Slot.CapitalBuilding.gameObject, 10f, 10f, false);
            }            
        }
    }
}
