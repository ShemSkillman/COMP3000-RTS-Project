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
                if (context.economyManager.AssignVillagerToResource(idleVillagers[0], context.Info.Tree))
                {
                    Print("Assigned" + idleVillagers[0].gameObject.name + " to " + context.Info.Tree.GetName());
                    return State.Running;
                }

                if (context.economyManager.AssignVillagerToResource(idleVillagers[0], context.Info.IronMine))
                {
                    Print("Assigned" + idleVillagers[0].gameObject.name + " to " + context.Info.IronMine.GetName());
                    return State.Running;
                }

            }
            else
            {
                if (context.economyManager.AssignVillagerToResource(idleVillagers[0], context.Info.IronMine))
                {
                    Print("Assigned" + idleVillagers[0].gameObject.name + " to " + context.Info.IronMine.GetName());
                    return State.Running;
                }

                if (context.economyManager.AssignVillagerToResource(idleVillagers[0], context.Info.Tree))
                {
                    Print("Assigned" + idleVillagers[0].gameObject.name + " to " + context.Info.Tree.GetName());
                    return State.Running;
                }
            }

            return State.Success;
        }
        else
        {
            Print("No more idle villagers to assign.");
            return State.Success;
        }        
    }
}