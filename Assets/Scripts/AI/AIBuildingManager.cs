using RTSEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ColdAlliances.AI
{
    public class AIBuildingManager : MonoBehaviour
    {
        GameManager gameMgr;
        FactionManager factionMgr;

        public void Init(GameManager gameMgr, FactionManager factionMgr)
        {
            this.gameMgr = gameMgr;
            this.factionMgr = factionMgr;
        }
    }
}