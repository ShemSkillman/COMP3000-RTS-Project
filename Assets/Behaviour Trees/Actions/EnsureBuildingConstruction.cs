using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TheKiwiCoder;

public class EnsureBuildingConstruction : ActionNode
{
    protected override State PerformAction() {

        if (context.buildingManager.AssignVillagerToBuildingWithNoBuilder())
        {
            return State.Running;
        }

        return State.Success;
    }
}
