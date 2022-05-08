using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TheKiwiCoder;

public class BuildTower : ActionNode
{
    protected override State PerformAction() {
        if (!context.gameMgr.ResourceMgr.HasRequiredResources(context.Info.Tower.GetResources(), context.factionMgr.FactionID))
        {
            Print("Not enough wood to build " + context.Info.Tower.GetName());
            return State.Failure;
        }
        else if (!context.factionMgr.HasReachedLimit(context.Info.Tower.GetCode(), ""))
        {
            if (context.buildingManager.ConstructBuilding(context.Info.Tower))
            {
                Print("Placing tower.");
                return State.Success;
            }
            else
            {
                Print("Could not place " + context.Info.Tower.GetName() + " because no terrain space or/and builders were available.");
                return State.Failure;
            }
        }
        else
        {
            Print("No need to build tower because limit has been reached.");
            return State.Success;
        }        
    }
}
