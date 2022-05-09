using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TheKiwiCoder;
using RTSEngine;

public class BuildMilitaryBuilding : ActionNode
{
    Building buildNext;

    protected override State PerformAction() {
        if (!blackboard.isMilitaryBuildingNeeded && context.factionMgr.GetBuildingCategoryCount("military") >= 4)
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
            Print("Not enough wood to build " + buildNext.GetName());
            return State.Failure;
        }
        else
        {
            if (context.buildingManager.ConstructBuilding(buildNext))
            {
                Print("Placing " + buildNext.GetName());

                blackboard.isMilitaryBuildingNeeded = false;
                buildNext = null;

                return State.Success;
            }
            else
            {
                Print("Could not place " + buildNext.GetName() + " because no terrain space or/and builders were available.");
                return State.Failure;
            }
        }
    }
}
