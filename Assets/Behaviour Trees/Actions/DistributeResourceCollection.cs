using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TheKiwiCoder;
using RTSEngine;

public class DistributeResourceCollection : ActionNode
{
    protected override State PerformAction() {
        int coinCollectorCount = context.economyManager.GetResourceCollectorCount(context.Info.IronMine);
        int woodCollectorCount = context.economyManager.GetResourceCollectorCount(context.Info.Tree);

        if (Mathf.Abs(coinCollectorCount - woodCollectorCount) > 1)
        {
            List<Unit> idleVillagers = context.factionMgr.GetIdleVillagers();
            List<Unit> coinCollectors = context.factionMgr.GetVillagersCollectingResource(context.Info.IronMine.GetResourceType().Key);
            List<Unit> woodCollectors = context.factionMgr.GetVillagersCollectingResource(context.Info.Tree.GetResourceType().Key);

            Unit villager;
            if (idleVillagers.Count > 0)
            {
                villager = idleVillagers[0];
            }
            else
            {
                if (coinCollectorCount > woodCollectorCount)
                {
                    villager = coinCollectors[0];
                }
                else
                {
                    villager = woodCollectors[0];
                }
            }

            if (villager == null)
            {
                Debug.Log($"NULL VILLAGER - THIS SHOULD NEVER HAPPEN!");
            }

            if (coinCollectorCount > woodCollectorCount)
            {
                context.economyManager.AssignVillagerToResource(villager, context.Info.Tree);
            }
            else
            {
                context.economyManager.AssignVillagerToResource(villager, context.Info.IronMine);
            }

            return State.Success;
        }
        else
        {
            return State.Failure;
        }        
    }
}
