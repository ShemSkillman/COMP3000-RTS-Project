using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TheKiwiCoder;

public class NeedsVillager : ActionNode
{
    bool madeDecision = false;
    bool needsVillager = false;

    protected override void OnStart() {
    }

    protected override void OnStop() {
    }

    protected override State OnUpdate()
    {
        if (madeDecision)
        {
            madeDecision = false;

            if (needsVillager)
            {
                return State.Success;
            }
            else
            {
                return State.Failure;
            }
        }

        int villagerCountGoal = context.factionMgr.Slot.MaxPopulation / 2;
        int villagerCount = context.factionMgr.Villagers.Count + context.factionMgr.Slot.CapitalBuilding.TaskLauncherComp.GetTaskQueueCount();

        needsVillager = villagerCount < villagerCountGoal &&
                context.factionMgr.Slot.CapitalBuilding.TaskLauncherComp.GetTaskQueueCount() < 1 &&
                context.gameMgr.ResourceMgr.GetFactionResources(context.factionMgr.FactionID).Resources[context.Info.IronMine.ID].GetCurrAmount() >= 100 &&
                context.factionMgr.Slot.GetFreePopulation() > 0;

        madeDecision = true;

        return State.Running;
    }
}
