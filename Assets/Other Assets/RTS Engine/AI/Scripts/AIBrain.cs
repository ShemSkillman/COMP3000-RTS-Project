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

        bool intiated = false;

        private void Start()
        {
            timeBetweenActions = 60f / actionsPerMinute;
        }

        public void Init(GameManager gameMgr, FactionManager factionMgr)
        {
            this.gameMgr = gameMgr;
            this.factionMgr = factionMgr;

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
            factionMgr.TownCenter.TaskLauncherComp.Add(0);
        }
    }
}
