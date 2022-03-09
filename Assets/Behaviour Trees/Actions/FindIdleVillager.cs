using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TheKiwiCoder;
using RTSEngine;

public class FindIdleVillager : ActionNode
{
    bool isIdleVillager = false;

    protected override void OnStart() {
    }

    protected override void OnStop() {
    }

    protected override State PerformAction() {

        if (state == State.Running)
        {
            if (blackboard.idleVillager != null)
            {
                return State.Success;
            }
            else
            {
                return State.Failure;
            }
        }
        
        List<Unit> idleVillagers = context.factionMgr.GetIdleVillagers();

        if (idleVillagers.Count > 0)
        {
            blackboard.idleVillager = idleVillagers[0];
        }

        return State.Running;
    }
}