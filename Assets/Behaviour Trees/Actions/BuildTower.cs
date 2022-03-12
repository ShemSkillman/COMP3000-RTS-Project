using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TheKiwiCoder;

public class BuildTower : ActionNode
{
    protected override State PerformAction() {
        if (!context.gameMgr.ResourceMgr.HasRequiredResources(context.Info.Tower.GetResources(), context.factionMgr.FactionID))
        {
            return State.Failure;
        }
        else if (!context.factionMgr.HasReachedLimit(context.Info.Tower.GetCode(), ""))
        {
            context.buildingManager.ConstructBuilding(context.Info.Tower);
            return State.Running;
        }
        else
        {
            return State.Success;
        }        
    }
}
