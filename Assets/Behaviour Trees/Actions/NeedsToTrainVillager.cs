using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TheKiwiCoder;

public class NeedsToTrainVillager : ActionNode
{
    protected override State PerformAction()
    {
        int villagerCountGoal = context.factionMgr.Slot.MaxPopulation / 2;
        int villagerCount = context.factionMgr.Villagers.Count + context.factionMgr.Slot.CapitalBuilding.TaskLauncherComp.GetTaskQueueCount();

        bool needsVillager = villagerCount < villagerCountGoal &&
                context.factionMgr.Slot.CapitalBuilding.TaskLauncherComp.GetTaskQueueCount() < 1 &&
                context.gameMgr.ResourceMgr.GetFactionResources(context.factionMgr.FactionID).Resources[context.Info.IronMine.ID].GetCurrAmount() >= 100 &&
                context.factionMgr.Slot.GetFreePopulation() > 0;

        if (needsVillager)
        {
            return State.Success;
        }
        else
        {
            return State.Failure;
        }        
    }
}
