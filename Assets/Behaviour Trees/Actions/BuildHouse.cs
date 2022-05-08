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
        if (!context.factionMgr.HasReachedLimit(context.Info.House.GetCode(), "") &&
            context.factionMgr.Slot.GetFreePopulation() + (housePop * context.buildingManager.GetBuildingInConstructionCount(context.Info.House)) < housePop &&
            context.factionMgr.Slot.GetPopulationCapacity() < context.factionMgr.Slot.MaxPopulation)
        {
            if (context.buildingManager.ConstructBuilding(context.Info.House))
            {
                Print("Placing house.");
                return State.Running;
            }
            else
            {
                Print("Could not place house because no terrain space or/and builders were available.");
                return State.Failure;
            }
        }
        else
        {
            Print("No need to build house, population space is fine.");
            return State.Success;
        }
    }
}