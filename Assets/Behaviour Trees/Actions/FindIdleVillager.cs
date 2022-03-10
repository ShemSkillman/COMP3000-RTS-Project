using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TheKiwiCoder;
using RTSEngine;

public class FindIdleVillager : ActionNode
{
    protected override State PerformAction() {
        
        List<Unit> idleVillagers = context.factionMgr.GetIdleVillagers();

        if (idleVillagers.Count > 0)
        {
            return State.Success;
        }
        else
        {
            return State.Failure;
        }
    }
}