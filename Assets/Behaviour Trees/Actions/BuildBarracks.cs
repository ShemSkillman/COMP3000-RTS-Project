using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TheKiwiCoder;

public class BuildBarracks : ActionNode
{
    protected override State PerformAction() {
        if (!blackboard.isBarracksNeeded)
        {
            Print("No need to build barracks.");
            return State.Success;
        }
        else if (!context.gameMgr.ResourceMgr.HasRequiredResources(context.Info.Barracks.GetResources(), context.factionMgr.FactionID))
        {
            Print("Not enough wood to build barracks.");
            return State.Failure;
        }
        else
        {
            context.buildingManager.ConstructBuilding(context.Info.Barracks);
            blackboard.isBarracksNeeded = false;

            Print("Placing barracks.");

            return State.Success;
        }
    }
}
