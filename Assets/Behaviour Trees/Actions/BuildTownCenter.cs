using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TheKiwiCoder;

public class BuildTownCenter : ActionNode
{
    protected override State PerformAction() {

        if (context.gameMgr.ResourceMgr.HasRequiredResources(context.Info.TownCenter.GetResources(), context.factionMgr.FactionID) &&
            !context.factionMgr.HasReachedLimit(context.Info.TownCenter.GetCode(), ""))
        {
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
