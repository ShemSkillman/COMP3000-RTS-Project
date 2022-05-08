using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TheKiwiCoder;
using RTSEngine;

public class EnsureBuildingConstruction : ActionNode
{
    protected override State PerformAction() {

        if (context.buildingManager.AssignVillagerToBuildingWithNoBuilder(out Building building))
        {
            Print("Assigning villager to finish building " + building.GetName());
            return State.Running;
        }

        Print("All buildings that are under construction have enough villagers building them.");
        return State.Success;
    }
}
