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
        FactionSlot factionSlot;

        [SerializeField] int actionsPerMinute = 60;

        float timeSinceLastAction = 0f;
        float timeBetweenActions;

        bool intiated = false;

        public void Init(GameManager gameMgr, FactionManager factionMgr)
        {
            this.gameMgr = gameMgr;
            this.factionMgr = factionMgr;

            timeBetweenActions = 60f / actionsPerMinute;

            factionSlot = gameMgr.GetFaction(factionMgr.FactionID);

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
            int villagerCountGoal = factionSlot.MaxPopulation / 2;

            int villagerCount = factionMgr.Villagers.Count + factionSlot.CapitalBuilding.TaskLauncherComp.GetTaskQueueCount();

            if (villagerCount < villagerCountGoal &&
                factionSlot.CapitalBuilding.TaskLauncherComp.GetTaskQueueCount() < 2)
            {
                factionSlot.CapitalBuilding.TaskLauncherComp.Add(0);
            }
        }
    }
}
