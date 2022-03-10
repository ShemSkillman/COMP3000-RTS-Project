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
            List<Unit> woodCollectors = context.factionMgr.GetVillagersCollectingResource(context.Info.Tree.GetResourceType().Key);
            List<Unit> coinCollectors = context.factionMgr.GetVillagersCollectingResource(context.Info.IronMine.GetResourceType().Key);

            int woodCollectorCount = woodCollectors.Count;// + collectionTasksDic[woodSource.GetResourceType().Key].Count;
            int coinCollectorCount = coinCollectors.Count;// + collectionTasksDic[coinSource.GetResourceType().Key].Count;

            
            if (coinCollectorCount > woodCollectorCount)
            {
                AssignVillagerToResource(idleVillagers[0], context.Info.Tree);
            }
            else
            {
                AssignVillagerToResource(idleVillagers[0], context.Info.IronMine);
            }

            return State.Success;
        }
        else
        {
            return State.Failure;
        }        
    }

    private void AssignVillagerToResource(Unit villager, Resource resource)
    {
        BasicTargetPicker targetPicker = new BasicTargetPicker(resource.GetCode());

        if (context.gameMgr.GridSearch.Search<Entity>(villager.transform.position,
                                    1000f,
                                    true,
                                    targetPicker.IsValidTarget,
                                    out Entity potentialTarget) == ErrorMessage.none)
        {
            Resource closestResource = potentialTarget as Resource;

            villager.CollectorComp.SetTarget(closestResource);

            //CollectionTask task = new CollectionTask(villager.CollectorComp, resource.GetResourceType());

            //collectionTasksDic[resource.GetResourceType().Key].Add(task);
        }
        
    }
}