using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Assertions;
using UnityEngine;
using RTSEngine;
using RTSEngine.EntityComponent;
using TheKiwiCoder;

namespace ColdAlliances.AI
{
    public class AIBrain : MonoBehaviour
    {
        public void Init(GameManager gameMgr, FactionManager factionMgr)
        {
            BehaviourTreeRunner behaviourTreeRunner = GetComponent<BehaviourTreeRunner>();
            behaviourTreeRunner.Init(gameMgr, factionMgr);

            AIEconomyManager economyManager = GetComponent<AIEconomyManager>();
            economyManager.Init(gameMgr, factionMgr);

            NPCBuildingPlacer buildingPlacer = GetComponent<NPCBuildingPlacer>();
            buildingPlacer.Init(gameMgr, this, factionMgr);

            AIBuildingManager buildingManager = GetComponent<AIBuildingManager>();
            buildingManager.Init(gameMgr, factionMgr, buildingPlacer);

            AICombatManager combatManager = GetComponent<AICombatManager>();
            combatManager.Init(gameMgr, factionMgr);
        }
    }
}