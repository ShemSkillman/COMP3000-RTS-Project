using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TheKiwiCoder;

public class TrainVillager : ActionNode
{
    protected override void OnStart() {
    }

    protected override void OnStop() {
    }

    protected override State OnUpdate() {
        int villagerCountGoal = context.factionMgr.Slot.MaxPopulation / 2;
        int villagerCount = context.factionMgr.Villagers.Count + context.factionMgr.Slot.CapitalBuilding.TaskLauncherComp.GetTaskQueueCount();

        if (villagerCount < villagerCountGoal &&
                context.factionMgr.Slot.CapitalBuilding.TaskLauncherComp.GetTaskQueueCount() < 1 &&
                context.gameMgr.ResourceMgr.GetFactionResources(context.factionMgr.FactionID).Resources[context.Info.IronMine.ID].GetCurrAmount() >= 100 &&
                context.factionMgr.Slot.GetFreePopulation() > 0)
        {
            context.factionMgr.Slot.CapitalBuilding.TaskLauncherComp.Add(0);

            return State.Success;
        }

        return State.Failure;
    }
}