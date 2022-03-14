using RTSEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ColdAlliances.AI;

namespace TheKiwiCoder {

    // The context is a shared object every node has access to.
    // Commonly used components and subsytems should be stored here
    // It will be somewhat specfic to your game exactly what to add here.
    // Feel free to extend this class 
    public class Context {
        public GameObject gameObject;
        public GameManager gameMgr;
        public FactionManager factionMgr;
        public AIEconomyManager economyManager;
        public AIBuildingManager buildingManager;
        public AICombatManager combatManager;

        public static Context CreateFromGameObject(GameObject gameObject, GameManager gameMgr, FactionManager factionMgr) {
            Context context = new Context();
            context.gameObject = gameObject;
            context.gameMgr = gameMgr;
            context.factionMgr = factionMgr;

            context.economyManager = gameObject.GetComponent<AIEconomyManager>();
            context.buildingManager = gameObject.GetComponent<AIBuildingManager>();
            context.combatManager = gameObject.GetComponent<AICombatManager>();

            return context;
        }

        public FactionTypeInfo Info { get { return factionMgr.Slot.GetTypeInfo(); } }
    }
}