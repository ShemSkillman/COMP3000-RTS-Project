using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TheKiwiCoder;

public class EnsureBuildingConstruction : ActionNode
{
    protected override State PerformAction() {

        if (context.buildingManager.AssignVillagerToBuildingWithNoBuilder())
        {
            Print("Assigning villager to finish building construction.");
            return State.Running;
        }

        Print("All buildings have villagers assigned to them for construction.");
        return State.Success;
    }
}
