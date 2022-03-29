using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TheKiwiCoder;
using RTSEngine;

public class TrainArmy : ActionNode
{
    int buildingIndex = 0;

    protected override State PerformAction() {
        List<Building> allBuildings = new List<Building>(context.factionMgr.GetBuildings());

        if (allBuildings.Count < 1)
        {
            return State.Success;
        }

        if (buildingIndex > allBuildings.Count - 1)
        {
            buildingIndex = 0;
        }

        int startIndex = buildingIndex;

        do
        {
            Building building = allBuildings[buildingIndex];

            if (building.GetCategory() == "military" && building.IsBuilt &&
                building.TaskLauncherComp.GetTaskQueueCount() < 1)
            {
                if (building.TaskLauncherComp.GetTask(0).HasRequiredResources() &&
                building.TaskLauncherComp.GetTask(0).UnitPopulationSlots <= context.factionMgr.Slot.GetFreePopulation())
                {
                    building.TaskLauncherComp.Add(0);

                    Print("Training unit from military building.");

                    buildingIndex++;
                    return State.Running;
                }
                else
                {
                    if (!building.TaskLauncherComp.GetTask(0).HasRequiredResources())
                    {
                        Print("Not enough resources to train military unit.");
                    }

                    if (!(building.TaskLauncherComp.GetTask(0).UnitPopulationSlots <= context.factionMgr.Slot.GetFreePopulation()))
                    {
                        Print("Not enough pop to train military unit.");
                    }

                    blackboard.isMilitaryBuildingNeeded = false;

                    return State.Success;
                }
            }

            buildingIndex++;

            if (buildingIndex > allBuildings.Count - 1)
            {
                buildingIndex = 0;
            }
        } while (buildingIndex != startIndex);

        blackboard.isMilitaryBuildingNeeded = true;

        Print("All existing military buildings are training units.");
        return State.Success;
    }
}
