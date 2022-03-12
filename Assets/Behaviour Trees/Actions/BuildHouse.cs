using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TheKiwiCoder;

public class BuildHouse : ActionNode
{
    protected override State PerformAction() {
        if (!context.gameMgr.ResourceMgr.HasRequiredResources(context.Info.House.GetResources(), context.factionMgr.FactionID))
        {
            Print("Not enough wood to build house.");
            return State.Failure;
        }

        int housePop = context.Info.House.GetAddedPopulationSlots();
        if (context.factionMgr.Slot.GetFreePopulation() + (housePop * context.buildingManager.GetBuildingInConstructionCount(context.Info.House)) < housePop &&
            context.factionMgr.Slot.GetCurrentPopulation() < context.factionMgr.Slot.MaxPopulation)
        {
            context.buildingManager.ConstructBuilding(context.Info.House);
            Print("Placing house.");
            return State.Running;
        }
        else
        {
            Print("No need to build house.");
            return State.Success;
        }
    }
}