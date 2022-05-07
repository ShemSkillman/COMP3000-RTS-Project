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
                return State.Failure;
            }

            if (context.buildingManager.ConstructBuilding(context.Info.TownCenter))
            {
                return State.Success;
            }
            else
            {
                return State.Failure;
            }
        }

        return State.Success;
    }
}
