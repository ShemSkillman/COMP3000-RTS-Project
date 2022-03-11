using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TheKiwiCoder;
using RTSEngine;

public class AssignIdleVillager : ActionNode
{
    protected override State PerformAction()
    {
        List<Unit> idleVillagers = context.factionMgr.GetIdleVillagers();

        if (idleVillagers.Count > 0)
        {            
            if (context.economyManager.GetResourceCollectorCount(context.Info.IronMine) > context.economyManager.GetResourceCollectorCount(context.Info.Tree))
            {
                context.economyManager.AssignVillagerToResource(idleVillagers[0], context.Info.Tree);
            }
            else
            {
                context.economyManager.AssignVillagerToResource(idleVillagers[0], context.Info.IronMine);
            }

            return State.Success;
        }
        else
        {
            return State.Failure;
        }        
    }
}