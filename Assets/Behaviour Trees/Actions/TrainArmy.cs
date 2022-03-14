using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TheKiwiCoder;
using RTSEngine;

public class TrainArmy : ActionNode
{
    protected override State PerformAction() {
        if (context.gameMgr.ResourceMgr.GetFactionResources(context.factionMgr.FactionID).Resources[context.Info.IronMine.ID].GetCurrAmount() < 100 ||
                context.factionMgr.Slot.GetFreePopulation() < 1)
        {
            blackboard.isBarracksNeeded = false;

            Print("Cannot afford to train army.");

            return State.Success;
        }

        foreach (Building building in context.factionMgr.GetBuildings())
        {
            if (building.GetCode() == context.Info.Barracks.GetCode() && building.IsBuilt)
            {
                if (building.TaskLauncherComp.GetTaskQueueCount() < 1)
                {
                    building.TaskLauncherComp.Add(0);

                    Print("Training solider from barracks.");
                    return State.Running;
                }
            }
        }

        if (context.gameMgr.ResourceMgr.GetFactionResources(context.factionMgr.FactionID).Resources[context.Info.IronMine.ID].GetCurrAmount() > 100)
        {
            blackboard.isBarracksNeeded = true;
        }

        Print("No need to train more soldiers from barracks.");
        return State.Success;
    }
}
