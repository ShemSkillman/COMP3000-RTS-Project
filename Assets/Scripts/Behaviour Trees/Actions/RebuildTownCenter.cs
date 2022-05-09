using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TheKiwiCoder;

public class RebuildTownCenter : ActionNode
{
    protected override State PerformAction() {
        if (context.factionMgr.GetBuildingCount("town_center") <= 0)
        {
            if (!context.gameMgr.ResourceMgr.HasRequiredResources(context.Info.TownCenter.GetResources(), context.factionMgr.FactionID))
            {
                Print("Not enough wood to rebuild the town center.");
                return State.Failure;
            }

            if (context.buildingManager.ConstructBuilding(context.Info.TownCenter))
            {
                Print("Rebuilding town center.");
                return State.Success;
            }
            else
            {
                Print("Could not rebuild town center because no terrain space or/and builders were available.");
                return State.Failure;
            }
        }

        Print("Town center is present and does not need rebuilding.");
        return State.Success;
    }
}