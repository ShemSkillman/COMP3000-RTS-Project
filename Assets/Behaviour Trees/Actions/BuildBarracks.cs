using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TheKiwiCoder;
using RTSEngine;

public class BuildBarracks : ActionNode
{
    Building buildNext;

    protected override State PerformAction() {
        if (!blackboard.isMilitaryBuildingNeeded)
        {
            Print("No need to build military building.");
            return State.Success;
        }
        
        if (buildNext == null)
        {
            int randomIndex = Random.Range(0, 4);

            switch (randomIndex)
            {
                case 0:
                    buildNext = context.Info.Barracks;
                    break;

                case 1:
                    buildNext = context.Info.ArcheryRange;
                    break;

                case 2:
                    buildNext = context.Info.Stables;
                    break;

                case 3:
                    buildNext = context.Info.Foundry;
                    break;
            }
        }
        
        if (!context.gameMgr.ResourceMgr.HasRequiredResources(buildNext.GetResources(), context.factionMgr.FactionID))
        {
            Print("Not enough wood to build military building.");
            return State.Failure;
        }
        else
        {
            context.buildingManager.ConstructBuilding(buildNext);
            blackboard.isMilitaryBuildingNeeded = false;
            buildNext = null;

            Print("Placing military building.");

            return State.Success;
        }
    }
}
